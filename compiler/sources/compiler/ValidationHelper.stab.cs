/*
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */
using java.lang;
using stab.query;
using stab.reflection;
using cnatural.syntaxtree;

namespace cnatural.compiler {
    
    enum BoxingKind {
        None,
        Box,
        Unbox
    }
    
    class ValidationHelper {
        static void setArrayInitializerTypes(ArrayInitializerExpressionNode initializer, TypeInfo type) {
            initializer.getUserData(typeof(ExpressionInfo)).Type = type;
            foreach (var e in initializer.Values) {
                if (e.ExpressionKind == ExpressionKind.ArrayInitializer) {
                    setArrayInitializerTypes((ArrayInitializerExpressionNode)e, type.ElementType);
                }
            }
        }
    
        static TypeInfo getVariableType(CompilerContext context, TypeInfo type) {
            switch (type.TypeKind) {
            case LowerBoundedWildcard:
            case UnboundedWildcard:
                return context.TypeSystem.ObjectType;
            default:
                return type;
            }
        }
    
        static void setBoxing(CompilerContext context, TypeInfo targetType, ExpressionNode expression) {
            var info = expression.getUserData(typeof(ExpressionInfo));
            if (info == null || targetType == null) {
                return;
            }
            var type = getType(context, expression);
            if (type == targetType) {
                return;
            }
            if (isAssignable(context, targetType, expression)) {
                if (targetType.IsPrimitive) {
                    if (!type.IsPrimitive) {
                        info.BoxingKind = BoxingKind.Unbox;
                        var unboxinMethod = context.TypeSystem.getUnboxingMethod(type);
                        info.BoxingMethod = unboxinMethod;
                        info.OriginalType = info.Type;
                        info.Type = unboxinMethod.ReturnType;
                    }
                } else if (type.IsPrimitive) {
                    info.BoxingKind = BoxingKind.Box;
                    var boxingMethod = context.TypeSystem.getBoxingMethod((targetType.IsObject) ? type : targetType);
                    info.BoxingMethod = boxingMethod;
                    info.OriginalType = info.Type;
                    info.Type = boxingMethod.ReturnType;
                } else if (targetType.IsNumeric && type.IsNumeric) {
                    info.BoxingKind = BoxingKind.Unbox;
                    info.BoxingMethod = context.TypeSystem.getUnboxingMethod(type);
                }
            }
        }
        
        static bool isMethod(ExpressionNode argNode) {
            var info = argNode.getUserData(typeof(ExpressionInfo));
            if (info == null || info.Type != null) {
                return false;
            } else if (info.IsConstant) {
                return false;
            } else if (info.Members != null) {
                foreach (var member in info.Members) {
                    if (member.MemberKind != MemberKind.Method) {
                        return false;
                    }
                }
            }
            return true;
        }
        
        static TypeInfo getType(CompilerContext context, ExpressionNode expression) {
            if (ValidationHelper.isMethod(expression)) {
                throw context.error(CompileErrorId.UnexpectedMethodReference, expression);
            }
            var info = expression.getUserData(typeof(ExpressionInfo));
            if (info.Type != null) {
                return info.Type;
            } else if (info.IsConstant) {
                context.ConstantBuilder.buildConstant(expression);
                return info.Type;
            } else if (info.Members != null) {
                foreach (var member in info.Members) {
                    switch (member.MemberKind) {
                    case Field:
                        var field = member.Field;
                        if (field.Value != null) {
                            info.IsConstant = true;
                            info.Value = field.Value;
                        }
                        info.Member = member;
                        info.Type = member.Type;
                        if (!isInDeprecatedContext(context)) {
							if (BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem, field)) {
								context.addWarning(CompileErrorId.DeprecatedField, expression,
										BytecodeHelper.getDisplayName(field.DeclaringType), field.Name);
							}
                        }
						if (context.CodeValidationContext.IsInMethod && context.CodeValidationContext.IsInLambda) {
	                    	if (!member.IsStatic && expression.ExpressionKind == ExpressionKind.SimpleName) {
				                var typeBuilder = context.LambdaScopes[context.CodeValidationContext.RootMethod];
				                if (typeBuilder.getField("this$0") == null) {
				                    typeBuilder.defineField("this$0", context.CurrentType);
				                }
	                    	}
                    	}
                        return member.Type;
                        
                    case Type:
                        info.Member = member;
                        info.Type = member.Type;
                        if (!isInDeprecatedContext(context)) {
							if (BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem, info.Type)) {
								context.addWarning(CompileErrorId.DeprecatedType, expression,
										BytecodeHelper.getDisplayName(info.Type));
							}
						}
                        return member.Type;
						
                    case Indexer:
                    case Property:
                        info.Member = member;
                        info.Type = member.Type;
                        if (!isInDeprecatedContext(context)) {
							if (member.GetAccessor != null) {
								if (member.SetAccessor == null) {
									if (BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem, member.GetAccessor)) {
										context.addWarning(CompileErrorId.DeprecatedProperty, expression,
												BytecodeHelper.getDisplayName(member.DeclaringType), member.Name);
									}
								} else {
									if (BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem, member.GetAccessor) &&
											BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem, member.SetAccessor)) {
										context.addWarning(CompileErrorId.DeprecatedProperty, expression,
												BytecodeHelper.getDisplayName(member.DeclaringType), member.Name);
									}
								}
							} else if (member.SetAccessor != null) {
								if (BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem, member.SetAccessor)) {
									context.addWarning(CompileErrorId.DeprecatedProperty, expression,
											BytecodeHelper.getDisplayName(member.DeclaringType), member.Name);
								}
							}
						}
						if (context.CodeValidationContext.IsInMethod && context.CodeValidationContext.IsInLambda) {
	                    	if (!member.IsStatic && expression.ExpressionKind == ExpressionKind.SimpleName) {
				                var typeBuilder = context.LambdaScopes[context.CodeValidationContext.RootMethod];
				                if (typeBuilder.getField("this$0") == null) {
				                    typeBuilder.defineField("this$0", context.CurrentType);
				                }
	                    	}
                    	}
                        return member.Type;
                        
                    case Local:
						if (context.CodeValidationContext.IsInMethod) {
	                        var currentMethod = context.CodeValidationContext.CurrentMethod;
	                      	var currentType = (TypeBuilder)currentMethod.DeclaringType;
	                        if (currentType.FullName.indexOf('#') == -1 && context.CodeValidationContext.IsInLambda) {
		                        if (currentMethod != member.Method) {
		                            member.IsUsedFromLambda = true;
					                var typeBuilder = context.LambdaScopes[context.CodeValidationContext.RootMethod];
					                context.getLocalField(typeBuilder, (LocalMemberInfo)member);
		                        }
	                        }
                        }
                        info.Member = member;
                        info.Type = member.Type;
                        return member.Type;
                        
                    default:
                        break;
                    }
                }
                if (info.ExtensionMethods != null && info.ExtensionMethods.any()) {
                    return info.Type = info.ExtensionMethods.first().Parameters.first().Type;
                }
            }
            throw new Exception("Internal error line " + (expression.getLine() + 1));
        }
        
		private static bool isInDeprecatedContext(CompilerContext context) {
			if (!BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem, context.CurrentType)) {
				if (!context.CodeValidationContext.IsInMethod || !BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem,
						context.CodeValidationContext.RootMethod)) {
					return false;
				}
			}
			return true;
		}
		
        static bool isAssignable(CompilerContext context, TypeInfo type, ExpressionNode expression) {
            var info = expression.getUserData(typeof(ExpressionInfo));
            if (info == null) {
                return !type.IsPrimitive;
            }
            var right = getType(context, expression);
            if (type.isAssignableFrom(right)) {
                return true;
            }
            if (expression.ExpressionKind == ExpressionKind.ArrayInitializer) {
                if (!type.IsArray) {
                    return false;
                }
                foreach (var e in ((ArrayInitializerExpressionNode)expression).Values) {
                    if (!isAssignable(context, type.ElementType, e)) {
                        return false;
                    }
                }
                return true;
            }
            if (type.IsNumeric && info.IsConstant) {
                switch (type.NumericTypeKind) {
                case Byte: {
                    long value;
                    switch (right.TypeKind) {
                    case Char:
                        value = ((Character)info.Value).charValue();
                        break;
                    case Int:
                    case Long:
                    case Short:
                        value = ((Number)info.Value).longValue();
                        break;
                    default:
                        return false;
                    }
                    return Byte.MIN_VALUE <= value && value <= Byte.MAX_VALUE;
                }
                case Char: {
                    long value;
                    switch (right.TypeKind) {
                    case Byte:
                    case Int:
                    case Long:
                    case Short:
                        value = ((Number)info.Value).longValue();
                        break;
                    default:
                        return false;
                    }
                    return Character.MIN_VALUE <= value && value <= Character.MAX_VALUE;
                }
                case Short: {
                    long value;
                    switch (right.TypeKind) {
                    case Byte:
                        return true;
                    case Char:
                        value = ((Character)info.Value).charValue();
                        break;
                    case Int:
                    case Long:
                        value = ((Number)info.Value).longValue();
                        break;
                    default:
                        return false;
                    }
                    return Short.MIN_VALUE <= value && value <= Short.MAX_VALUE;
                }
                case Int: {
                    long value;
                    switch (right.getTypeKind()) {
                    case Char:
                        value = ((Character)info.Value).charValue();
                        break;
                    case Byte:
                    case Short:
                        return true;
                    case Long:
                        value = ((Number)info.getValue()).longValue();
                        break;
                    default:
                        return false;
                    }
                    return Integer.MIN_VALUE <= value && value <= Integer.MAX_VALUE;
                }
                }
            }
            return false;
        }
    }
}
