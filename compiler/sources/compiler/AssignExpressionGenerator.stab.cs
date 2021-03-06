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
using java.util;
using stab.query;
using stab.reflection;
using cnatural.syntaxtree;

namespace cnatural.compiler {

    class AssignExpressionGenerator : ExpressionHandler<ExpressionNode, Void> {
        private CompilerContext context;
        private ExpressionGenerator expressionGenerator;
        private bool postAssign;
        private AssignOperator operator;
        private ExpressionInfo rightInfo;
        private ExpressionNode rightExpression;
        private BoxingKind boxingKind;
    
        AssignExpressionGenerator(CompilerContext context, ExpressionGenerator expressionGenerator)
            : super(true) {
            this.context = context;
            this.expressionGenerator = expressionGenerator;
            this.boxingKind = BoxingKind.None;
        }
        
        private void initialize(ExpressionNode root) {
            if (root.ExpressionKind == ExpressionKind.Assign) {
                var assign = (AssignExpressionNode)root;
                operator = assign.Operator;
                rightExpression = assign.Right;
                rightInfo = rightExpression.getUserData(typeof(ExpressionInfo));
                if (rightInfo != null && rightInfo.BoxingKind == BoxingKind.Box) {
                    boxingKind = rightInfo.BoxingKind;
                    rightInfo.BoxingKind = BoxingKind.None;
                }
            } else {
                var unary = (UnaryExpressionNode)root;
                var oinfo = unary.Operand.getUserData(typeof(ExpressionInfo));
                var otype = oinfo.Type;
                if (oinfo.BoxingKind == BoxingKind.Unbox) {
                    boxingKind = BoxingKind.Box;
                    oinfo.BoxingKind = BoxingKind.None;
                }
                Object value;
                switch (otype.NumericTypeKind) {
                case Byte:
                    value = (byte)1;
                    break;
                case Char:
                    value = (char)1;
                    break;
                case Double:
                    value = 1d;
                    break;
                case Float:
                    value = 1f;
                    break;
                case Int:
                    value = 1;
                    break;
                case Long:
                    value = 1l;
                    break;
                case Short:
                    value = (short)1;
                    break;
                default:
                    throw new IllegalStateException("Internal error");
                }
                rightInfo = new ExpressionInfo(otype, value);
                switch (unary.Operator) {
                case PreIncrement:
                    operator = AssignOperator.Add;
                    break;
                case PreDecrement:
                    operator = AssignOperator.Subtract;
                    break;
                case PostIncrement:
                    operator = AssignOperator.Add;
                    postAssign = true;
                    break;
                case PostDecrement:
                    operator = AssignOperator.Subtract;
                    postAssign = true;
                    break;
                }
            }
        }
        
        protected override Void handleElementAccess(ElementAccessExpressionNode elementAccess, ExpressionNode root, bool nested) {
            initialize(root);
            if (operator == AssignOperator.Assign) {
                handleElementAccessAssign(elementAccess, nested);
            } else {
                handleElementAccessOperationAssign(elementAccess, nested);
            }
            return null;
        }
        
        protected override Void handleMemberAccess(MemberAccessExpressionNode memberAccess, ExpressionNode root, bool nested) {
            initialize(root);
            if (operator == AssignOperator.Assign) {
                handleMemberAccessAssign(memberAccess, nested);
            } else {
                handleMemberAccessOperationAssign(memberAccess, nested);
            }
            return null;
        }

        protected override Void handleSimpleName(SimpleNameExpressionNode simpleName, ExpressionNode root, bool nested) {
            initialize(root);
            if (operator == AssignOperator.Assign) {
                handleSimpleNameAssign(simpleName, nested);
            } else {
                handleSimpleNameOperationAssign(simpleName, nested);
            }
            return null;
        }
        
        private void handleSimpleNameAssign(SimpleNameExpressionNode simpleName, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            var member = simpleName.getUserData(typeof(ExpressionInfo)).Member;
            
            switch (member.MemberKind) {
            case Field: {
                var field = member.Field;
                var type = field.getType();
                emitRightExpression(field.IsStatic, null, type, nested);
                CompilerHelper.emitFieldModification(context, field);
                return;
            }
            case Local: {
                var local = (LocalMemberInfo)member;
                if (local.IsUsedFromLambda) {
                    var field = BytecodeGenerator.getLambdaScopeField(context, local);
                    emitRightExpression(false, local.Method, field.Type, nested);
                    CompilerHelper.emitFieldModification(context, field);
                } else {
                    emitLocalAssign(simpleName, local.Type, generator.getLocal(local.Name), nested);
                }
                return;
            }
            case Property: {
                var method = member.SetAccessor;
                var type = member.Type;
                emitRightExpression(method.IsStatic, null, type, nested);
                CompilerHelper.emitPropertyOrIndexerModification(context, method);
                return;
            }
            default:
                throw new Exception("Internal error: Expression cannot be assigned " + member.MemberKind);
            }
        }

        private void emitRightExpression(bool isStatic, MethodInfo localMethod, TypeInfo type, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            if (isStatic) {
                expressionGenerator.handleExpression(rightExpression, null, true);
                BytecodeGenerator.emitConversion(context, type, rightExpression);
                if (nested) {
                    generator.emit((type.IsCategory2) ? Opcode.Dup2 : Opcode.Dup);
                }
            } else {
                if (localMethod != null) {
                    BytecodeGenerator.emitLoadLambdaScope(context, generator, localMethod);
                } else {
                    BytecodeGenerator.emitThisAccess(context, generator);
                }
                expressionGenerator.handleExpression(rightExpression, null, true);
                BytecodeGenerator.emitConversion(context, type, rightExpression);
                if (nested) {
                    generator.emit((type.IsCategory2) ? Opcode.Dup2_X1 : Opcode.Dup_X1);
                }
            }
        }

        private void emitLocalAssign(SimpleNameExpressionNode simpleName, TypeInfo type, LocalInfo local, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            expressionGenerator.handleExpression(rightExpression, null, true);
            BytecodeGenerator.emitConversion(context, local.Type, rightExpression);
            if (nested) {
                generator.emit((type.IsCategory2) ? Opcode.Dup2 : Opcode.Dup);
            }
            Opcode opcode;
            if (boxingKind == BoxingKind.Box) {
                generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod((type.IsObject) ? rightInfo.Type : type));
                opcode = Opcode.Astore;
            } else {
                switch (type.TypeKind) {
                case Boolean:
                case Byte:
                case Char:
                case Short:
                case Int:
                    opcode = Opcode.Istore;
                    break;
                case Long:
                    opcode = Opcode.Lstore;
                    break;
                case Float:
                    opcode = Opcode.Fstore;
                    break;
                case Double:
                    opcode = Opcode.Dstore;
                    break;
                default:
                    opcode = Opcode.Astore;
                    break;
                }
            }
            generator.emit(opcode, local);
        }

        private void handleSimpleNameOperationAssign(SimpleNameExpressionNode simpleName, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            var member = simpleName.getUserData(typeof(ExpressionInfo)).Member;
            
            switch (member.MemberKind) {
            case Field: {
                var field = member.Field;
                if (!field.IsStatic) {
                    BytecodeGenerator.emitThisAccess(context, generator);
                    generator.emit(Opcode.Dup);
                }
                emitFieldOperationAssign(field, nested);
                break;
            }
            case Property: {
                var method = member.GetAccessor;
                if (!method.IsStatic) {
                    BytecodeGenerator.emitThisAccess(context, generator);
                    generator.emit(Opcode.Dup);
                }
                emitPropertyOperationAssign(method, member.SetAccessor, nested);
                break;
            }				
            case Local: {
                var local = (LocalMemberInfo)member;
                if (local.IsUsedFromLambda) {
                    BytecodeGenerator.emitLoadLambdaScope(context, generator, local.Method);
                    generator.emit(Opcode.Dup);
                    var field = BytecodeGenerator.getLambdaScopeField(context, local);
                    emitFieldOperationAssign(field, nested);
                } else {
                    emitLocalOperationAssign(generator.getLocal(local.Name), nested);
                }
                break;
            }				
            default:
                throw new Exception("Internal error");
            }
        }

        private void emitLocalOperationAssign(LocalInfo local, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            if (local.Type.IsBoolean) {
                emitLoad(nested, Opcode.Iload, local, false);
                if (rightExpression == null) {
                    generator.emit(Opcode.Iconst_1);
                } else {
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, local.Type);
                }
                emitIntOperator();
                emitStore(nested, Opcode.Istore, local, false);
            } else if (local.Type == context.TypeSystem.StringType) {
                emitLoad(nested, Opcode.Aload, local, false);
                var stringType = context.TypeSystem.StringType;
                generator.emit(Opcode.New, context.StringBuilderType);
                generator.emit(Opcode.Dup);
                generator.emit(Opcode.Invokespecial, context.StringBuilderConstructor);
                if (rightInfo.Type.IsCategory2) {
                    generator.emit(Opcode.Dup_X2);
                    generator.emit(Opcode.Pop);
                } else {
                    generator.emit(Opcode.Swap);
                }
                BytecodeGenerator.emitStringBuilderAppend(context, null);
                var isString = rightInfo != null && rightInfo.Type == stringType;
                var isStringAdd = rightExpression.ExpressionKind == ExpressionKind.Binary && isString;
                context.MethodGenerationContext.IsBuildingString = isStringAdd;
                expressionGenerator.handleExpression(rightExpression, null, true);
                if (!isStringAdd || (isString && rightInfo.IsConstant)) {
                    BytecodeGenerator.emitStringBuilderAppend(context, rightExpression.getUserData(typeof(ExpressionInfo)));
                }
                context.MethodGenerationContext.IsBuildingString = false;
                generator.emit(Opcode.Invokevirtual, context.ObjectToStringMethod);
                emitStore(nested, Opcode.Astore, local, false);
            } else if (BytecodeHelper.isDelegateType(local.Type)) {
                emitLoad(nested, Opcode.Aload, local, false);
                expressionGenerator.handleExpression(rightExpression, null, true);
                var delegateType = context.getTypeSystem().getType("stab/lang/Delegate");
                var argTypes = new ArrayList<TypeInfo> { delegateType, delegateType };
                if (operator == AssignOperator.Add) {
                    generator.emit(Opcode.Invokestatic, delegateType.getMethod("combine", argTypes));
                } else {
                    generator.emit(Opcode.Invokestatic, delegateType.getMethod("remove", argTypes));
                }
                generator.emit(Opcode.Checkcast, local.Type);
                emitStore(nested, Opcode.Astore, local, false);
            } else {
                TypeInfo rightType;
                switch (operator) {
                case LeftShift:
                case RightShift:
                case UnsignedRightShift:
                    rightType = context.TypeSystem.IntType;
                    break;
                default:
                    rightType = local.getType();
                    break;
                }
                switch (local.Type.NumericTypeKind) {
                case Byte:
                    emitLoad(nested, Opcode.Iload, local, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Iconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitIntOperator();
                    generator.emit(Opcode.I2b);
                    emitStore(nested, Opcode.Istore, local, false);
                    break;

                case Char:
                    emitLoad(nested, Opcode.Iload, local, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Iconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitIntOperator();
                    generator.emit(Opcode.I2c);
                    emitStore(nested, Opcode.Istore, local, false);
                    break;

                case Short:
                    emitLoad(nested, Opcode.Iload, local, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Iconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitIntOperator();
                    generator.emit(Opcode.I2s);
                    emitStore(nested, Opcode.Istore, local, false);
                    break;

                case Int:
                    if (boxingKind == BoxingKind.None
                     && rightInfo.IsConstant
                     && (operator == AssignOperator.Add || operator == AssignOperator.Subtract)) {
                        if (nested && postAssign) {
                            generator.emit(Opcode.Iload, local);
                        }
                        if (operator == AssignOperator.Add) {
                            generator.emit(Opcode.Iinc, local, ((Integer)rightInfo.Value).intValue());
                        } else {
                            generator.emit(Opcode.Iinc, local, -((Integer)rightInfo.Value).intValue());
                        }
                        if (nested && !postAssign) {
                            generator.emit(Opcode.Iload, local);
                        }
                    } else {
                        emitLoad(nested, Opcode.Iload, local, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Iconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitIntOperator();
                        emitStore(nested, Opcode.Istore, local, false);
                    }
                    break;

                case Long:
                    emitLoad(nested, Opcode.Lload, local, true);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Lconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitLongOperator();
                    emitStore(nested, Opcode.Lstore, local, true);
                    break;

                case Float:
                    emitLoad(nested, Opcode.Fload, local, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Fconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitFloatOperator();
                    emitStore(nested, Opcode.Fstore, local, false);
                    break;

                case Double:
                    emitLoad(nested, Opcode.Dload, local, true);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Dconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitDoubleOperator();
                    emitStore(nested, Opcode.Dstore, local, true);
                    break;

                default:
                    throw new IllegalStateException();
                }
            }
        }
        
        private void handleMemberAccessAssign(MemberAccessExpressionNode memberAccess, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            var member = memberAccess.getUserData(typeof(ExpressionInfo)).Member;
            
            switch (member.MemberKind) {
            case Field: {
                var field = member.Field;
                var type = field.Type;
                if (field.IsStatic) {
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    BytecodeGenerator.emitConversion(context, type, rightExpression);
					if (boxingKind == BoxingKind.Box) {
						generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod((type.IsObject) ? rightInfo.Type : type));
					}
                    if (nested) {
                        generator.emit((type.IsCategory2) ? Opcode.Dup2 : Opcode.Dup);
                    }
                } else {
                    expressionGenerator.handleExpression(memberAccess.TargetObject, null, true);
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    BytecodeGenerator.emitConversion(context, type, rightExpression);
					if (boxingKind == BoxingKind.Box) {
						generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod((type.IsObject) ? rightInfo.Type : type));
					}
					if (nested) {
                        generator.emit((type.IsCategory2) ? Opcode.Dup2_X1 : Opcode.Dup_X1);
                    }
                }
                CompilerHelper.emitFieldModification(context, field);
                break;
            }
            case Property: {
                var method = member.SetAccessor;
                var type = member.Type;
                if (method.IsStatic) {
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    BytecodeGenerator.emitConversion(context, type, rightExpression);
					if (boxingKind == BoxingKind.Box) {
						generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod((type.IsObject) ? rightInfo.Type : type));
					}
                    if (nested) {
                        generator.emit((type.IsCategory2) ? Opcode.Dup2 : Opcode.Dup);
                    }
                } else {
                    expressionGenerator.handleExpression(memberAccess.TargetObject, null, true);
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    BytecodeGenerator.emitConversion(context, type, rightExpression);
					if (boxingKind == BoxingKind.Box) {
						generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod((type.IsObject) ? rightInfo.Type : type));
					}
                    if (nested) {
                        generator.emit((type.IsCategory2) ? Opcode.Dup2_X1 : Opcode.Dup_X1);
                    }
                }
                CompilerHelper.emitPropertyOrIndexerModification(context, method);
                break;
            }
            default:
                throw new Exception("Internal error: member kind not supported: " + member.MemberKind);
            }
        }
        
        private void handleMemberAccessOperationAssign(MemberAccessExpressionNode memberAccess, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            var member = memberAccess.getUserData(typeof(ExpressionInfo)).Member;

            switch (member.MemberKind) {
            case Field: {
                var field = member.Field;
                if (!field.IsStatic) {
                    expressionGenerator.handleExpression(memberAccess.TargetObject, null, true);
                    generator.emit(Opcode.Dup);
                }
                emitFieldOperationAssign(field, nested);
                break;
            }
            case Property: {
                var method = member.GetAccessor;
                if (!method.IsStatic) {
                    expressionGenerator.handleExpression(memberAccess.TargetObject, null, true);
                    generator.emit(Opcode.Dup);
                }
                emitPropertyOperationAssign(method, member.SetAccessor, nested);
                break;
            }
            default:
                throw new Exception("Internal error: member kind not supported: " + member.MemberKind);
            }
        }
        
        private void emitFieldOperationAssign(FieldInfo field, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            CompilerHelper.emitFieldAccess(context, field);
            BytecodeGenerator.emitGenericCast(context, field.Type, field.DeclaringType.OriginalTypeDefinition.getField(field.Name).Type);
            emitFieldOrPropertyOperation(generator, field.Type, nested, field.IsStatic);
            CompilerHelper.emitFieldModification(context, field);
        }
        
        private void emitPropertyOperationAssign(MethodInfo getMethod, MethodInfo setMethod, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            CompilerHelper.emitPropertyAccess(context, getMethod);
            BytecodeGenerator.emitGenericCast(context, getMethod.ReturnType, getMethod.OriginalMethodDefinition.ReturnType);
            emitFieldOrPropertyOperation(generator, getMethod.ReturnType, nested, getMethod.IsStatic);
            CompilerHelper.emitPropertyOrIndexerModification(context, setMethod);
        }

        private void emitFieldOrPropertyOperation(CodeGenerator generator, TypeInfo type, bool nested, bool isStatic) {
            if (type.IsBoolean) {
                emitUnbox(nested, context.TypeSystem.BooleanType, isStatic, false);
                if (rightExpression == null) {
                    generator.emit(Opcode.Iconst_1);
                } else {
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, type);
                }
                emitIntOperator();
                generator.emit(Opcode.I2b);
                emitBox(nested, context.TypeSystem.BooleanType, isStatic, false);
            } else if (type == context.TypeSystem.StringType) {
                emitUnbox(nested, type, isStatic, false);
                var stringType = context.TypeSystem.StringType;
                generator.emit(Opcode.New, context.StringBuilderType);
                generator.emit(Opcode.Dup);
                generator.emit(Opcode.Invokespecial, context.StringBuilderConstructor);
                if (rightInfo.Type.IsCategory2) {
                    generator.emit(Opcode.Dup_X2);
                    generator.emit(Opcode.Pop);
                } else {
                    generator.emit(Opcode.Swap);
                }
                BytecodeGenerator.emitStringBuilderAppend(context, null);
                var isString = rightInfo != null && rightInfo.Type == stringType;
                var isStringAdd = rightExpression.ExpressionKind == ExpressionKind.Binary && isString;
                context.MethodGenerationContext.IsBuildingString = isStringAdd;
                expressionGenerator.handleExpression(rightExpression, null, true);
                if (!isStringAdd || (isString && rightInfo.IsConstant)) {
                    BytecodeGenerator.emitStringBuilderAppend(context, rightExpression.getUserData(typeof(ExpressionInfo)));
                }
                context.MethodGenerationContext.IsBuildingString = false;
                generator.emit(Opcode.Invokevirtual, context.ObjectToStringMethod);
                emitBox(nested, type, isStatic, false);
            } else if (BytecodeHelper.isDelegateType(type)) {
                emitUnbox(nested, type, isStatic, false);
                expressionGenerator.handleExpression(rightExpression, null, true);
                var delegateType = context.TypeSystem.getType("stab/lang/Delegate");
                var argTypes = new ArrayList<TypeInfo> { delegateType, delegateType };
                if (operator == AssignOperator.Add) {
                    generator.emit(Opcode.Invokestatic, delegateType.getMethod("combine", argTypes));
                } else {
                    generator.emit(Opcode.Invokestatic, delegateType.getMethod("remove", argTypes));
                }
                generator.emit(Opcode.Checkcast, type);
                emitBox(nested, type, isStatic, false);
            } else {
                TypeInfo rightType;
                switch (operator) {
                case LeftShift:
                case RightShift:
                case UnsignedRightShift:
                    rightType = context.TypeSystem.IntType;
                    break;
                default:
                    rightType = type;
                    break;
                }
                switch (type.NumericTypeKind) {
                case Byte:
                    emitUnbox(nested, context.TypeSystem.ByteType, isStatic, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Iconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitIntOperator();
                    generator.emit(Opcode.I2b);
                    emitBox(nested, context.TypeSystem.ByteType, isStatic, false);
                    break;

                case Char:
                    emitUnbox(nested, context.TypeSystem.CharType, isStatic, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Iconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitIntOperator();
                    generator.emit(Opcode.I2c);
                    emitBox(nested, context.TypeSystem.CharType, isStatic, false);
                    break;

                case Double:
                    emitUnbox(nested, context.TypeSystem.DoubleType, isStatic, true);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Dconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitDoubleOperator();
                    emitBox(nested, context.TypeSystem.DoubleType, isStatic, true);
                    break;

                case Float:
                    emitUnbox(nested, context.TypeSystem.FloatType, isStatic, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Fconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitFloatOperator();
                    emitBox(nested, context.TypeSystem.FloatType, isStatic, false);
                    break;

                case Int:
                    emitUnbox(nested, context.TypeSystem.IntType, isStatic, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Iconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitIntOperator();
                    emitBox(nested, context.TypeSystem.IntType, isStatic, false);
                    break;

                case Long:
                    emitUnbox(nested, context.TypeSystem.LongType, isStatic, true);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Lconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitLongOperator();
                    emitBox(nested, context.TypeSystem.LongType, isStatic, true);
                    break;

                case Short:
                    emitUnbox(nested, context.TypeSystem.ShortType, isStatic, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Iconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                    }
                    emitIntOperator();
                    generator.emit(Opcode.I2s);
                    emitBox(nested, context.TypeSystem.ShortType, isStatic, false);
                    break;

                default:
                    throw new Exception("Internal error");
                }
            }
        }
        
        private void handleElementAccessAssign(ElementAccessExpressionNode elementAccess, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            var tinfo = elementAccess.TargetObject.getUserData(typeof(ExpressionInfo));
            var ttype = tinfo.Type;
            if (!ttype.IsArray) {
                var info = elementAccess.getUserData(typeof(ExpressionInfo));
                var member = info.Member;
                var method = member.SetAccessor;
                if (!method.IsStatic) {
                    expressionGenerator.handleExpression(elementAccess.TargetObject, null, true);
                }
                var requireLocal = elementAccess.Indexes.size() > 1 || method.Parameters.first().Type.IsCategory2;
                var arguments = elementAccess.Indexes;
                expressionGenerator.emitArguments(arguments, method.Parameters, method.Parameters.count() - 1, method.IsVarargs);
                expressionGenerator.handleExpression(rightExpression, null, true);
                var type = member.Type;
                BytecodeGenerator.emitConversion(context, type, rightExpression);
                if (boxingKind == BoxingKind.Box) {
                    TypeInfo rtype;
                    if (type.IsObject) {
                        var rinfo = rightExpression.getUserData(typeof(ExpressionInfo));
                        rtype = rinfo.Type;
                    } else {
                        rtype = type;
                    }
                    generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod(rtype));
                }
                if (nested) {
                    var isCategory2 = type.IsCategory2;
                    if (requireLocal) {
                        generator.beginScope();
                        generator.emit((isCategory2) ? Opcode.Dup2 : Opcode.Dup);
                        generator.emit(BytecodeHelper.getStoreOpcode(type), generator.declareLocal(type, "elementValue$0"));
                    } else if (method.IsStatic) {
                        generator.emit((isCategory2) ? Opcode.Dup2_X1 : Opcode.Dup_X1);
                    } else {
                        generator.emit((isCategory2) ? Opcode.Dup2_X2 : Opcode.Dup_X2);
                    }
                }
                CompilerHelper.emitPropertyOrIndexerModification(context, method);
                if (method.ReturnType != context.TypeSystem.VoidType) {
                    generator.emit((method.ReturnType.IsCategory2) ? Opcode.Pop2 : Opcode.Pop);
                }
                if (nested && requireLocal) {
                    generator.emit(BytecodeHelper.getLoadOpcode(type), generator.getLocal("elementValue$0"));
                    generator.endScope();
                }
            } else {
                expressionGenerator.handleExpression(elementAccess.TargetObject, null, true);
                var index = elementAccess.Indexes[0];
                expressionGenerator.handleExpression(index, null, true);
                BytecodeHelper.emitNumericConversion(generator, index.getUserData(typeof(ExpressionInfo)).Type, context.TypeSystem.IntType);
                expressionGenerator.handleExpression(rightExpression, null, true);
                BytecodeGenerator.emitConversion(context, ttype.ElementType, rightExpression);
                if (boxingKind == BoxingKind.Box) {
                    var rtype = ttype.ElementType;
                    if (rtype.IsObject) {
		                var rinfo = rightExpression.getUserData(typeof(ExpressionInfo));
                        rtype = rinfo.Type;
                    }
                    generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod(rtype));
                }
                switch (ttype.ElementType.TypeKind) {
                case Boolean:
                case Byte:
                    if (nested) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    generator.emit(Opcode.Bastore);
                    break;
                case Char:
                    if (nested) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    generator.emit(Opcode.Castore);
                    break;
                case Float:
                    if (nested) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    generator.emit(Opcode.Fastore);
                    break;
                case Double:
                    if (nested) {
                        generator.emit(Opcode.Dup2_X2);
                    }
                    generator.emit(Opcode.Dastore);
                    break;
                case Int:
                    if (nested) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    generator.emit(Opcode.Iastore);
                    break;
                case Long:
                    if (nested) {
                        generator.emit(Opcode.Dup2_X2);
                    }
                    generator.emit(Opcode.Lastore);
                    break;
                case Short:
                    if (nested) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    generator.emit(Opcode.Sastore);
                    break;
                default:
                    if (nested) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    generator.emit(Opcode.Aastore);
                    break;
                }
            }
        }
        
        private void handleElementAccessOperationAssign(ElementAccessExpressionNode elementAccess, bool nested) {
            var generator = context.MethodGenerationContext.Generator;
            var tinfo = elementAccess.TargetObject.getUserData(typeof(ExpressionInfo));
            var ttype = tinfo.Type;
            if (!ttype.IsArray) {
                var info = elementAccess.getUserData(typeof(ExpressionInfo));
                var method = info.Member.GetAccessor;
                var isStatic = method.IsStatic;
                if (!isStatic) {
                    expressionGenerator.handleExpression(elementAccess.TargetObject, null, true);
                }
                var requireLocals = elementAccess.Indexes.size() > 1 || method.Parameters.first().Type.IsCategory2;
                if (requireLocals) {
                    generator.beginScope();
                    if (!isStatic) {
                        generator.emit(Opcode.Dup);
                        generator.emit(Opcode.Astore, generator.declareLocal(ttype, "elementTarget$0"));
                    }
                }
                var arguments = elementAccess.Indexes;
                var parameters = method.Parameters;
                int nparams = method.Parameters.count();
                var varargs = method.IsVarargs;
                int fixedLength = (varargs) ?  nparams - 1 : nparams;
                var it1 = parameters.iterator();
                var it2 = arguments.iterator();
                int i;
                for (i = 0; i < fixedLength; i++) {
                    var p = it1.next();
                    var e = it2.next();
                    expressionGenerator.emitNestedExpression(e, p.Type);
                    if (requireLocals) {
                        generator.emit((p.Type.IsCategory2) ? Opcode.Dup2 : Opcode.Dup);
                        generator.emit(BytecodeHelper.getStoreOpcode(p.Type), generator.declareLocal(p.Type, "elementIndex$" + i));
                    }
                }
                if (varargs) {
                    int nvarargs = arguments.size() - fixedLength;
                    var paramType = it1.next().Type;
                    if (nvarargs == 1) {
                        var e = arguments[i];
                        var ei = e.getUserData(typeof(ExpressionInfo));
                        if (ei == null) {
                            generator.emit(Opcode.Aconst_Null);
                        } else if (ei.Type.IsArray && paramType.isAssignableFrom(ei.Type)) {
                            expressionGenerator.handleExpression(e, null, true);
                            BytecodeGenerator.emitConversion(context, paramType, e);
                        } else {
                            BytecodeHelper.emitIntConstant(generator, 1);
                            expressionGenerator.emitArray(1, paramType, it2);
                        }
                    } else {
                        BytecodeHelper.emitIntConstant(generator, nvarargs);
                        expressionGenerator.emitArray(1, paramType, it2);
                    }
                    if (requireLocals) {
                        generator.emit(Opcode.Dup);
                        generator.emit(BytecodeHelper.getStoreOpcode(paramType), generator.declareLocal(paramType, "elementIndex$" + i));
                    }
                }
                if (!requireLocals) {
                    generator.emit((isStatic) ? Opcode.Dup : Opcode.Dup2);
                }
                CompilerHelper.emitIndexerAccess(context, method);
                if (requireLocals && nested && postAssign) {
                    generator.emit((method.ReturnType.IsCategory2) ? Opcode.Dup2 : Opcode.Dup);
                    generator.emit(BytecodeHelper.getStoreOpcode(method.ReturnType),
                        generator.declareLocal(method.ReturnType, "elementResult$0"));
                }
                if (method.ReturnType.IsBoolean) {
                    if (!requireLocals && nested && postAssign) {
                        generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                    }
                    emitUnbox(false, context.TypeSystem.BooleanType, false, false);
                    if (rightExpression == null) {
                        generator.emit(Opcode.Iconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                        BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, method.ReturnType);
                    }
                    emitIntOperator();
                    generator.emit(Opcode.I2b);
                    emitBox(false, context.TypeSystem.BooleanType, false, false);
                    if (!requireLocals && nested && !postAssign) {
                        generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                    }
                } else if (method.ReturnType == context.TypeSystem.StringType) {
                    if (!requireLocals && nested && postAssign) {
                        generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                    }
                    var stringType = context.TypeSystem.StringType;
                    generator.emit(Opcode.New, context.StringBuilderType);
                    generator.emit(Opcode.Dup);
                    generator.emit(Opcode.Invokespecial, context.StringBuilderConstructor);
                    if (rightInfo.Type.IsCategory2) {
                        generator.emit(Opcode.Dup_X2);
                        generator.emit(Opcode.Pop);
                    } else {
                        generator.emit(Opcode.Swap);
                    }
                    BytecodeGenerator.emitStringBuilderAppend(context, null);
                    var isString = rightInfo != null && rightInfo.Type == stringType;
                    bool isStringAdd = rightExpression.ExpressionKind == ExpressionKind.Binary && isString;
                    context.MethodGenerationContext.IsBuildingString = isStringAdd;
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    if (!isStringAdd || (isString && rightInfo.IsConstant)) {
                        BytecodeGenerator.emitStringBuilderAppend(context, rightExpression.getUserData(typeof(ExpressionInfo)));
                    }
                    context.MethodGenerationContext.IsBuildingString = false;
                    generator.emit(Opcode.Invokevirtual, context.ObjectToStringMethod);
                    if (!requireLocals && nested && !postAssign) {
                        generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                    }
                } else if (BytecodeHelper.isDelegateType(method.ReturnType)) {
                    if (!requireLocals && nested && postAssign) {
                        generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                    }
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    var delegateType = context.TypeSystem.getType("stab/lang/Delegate");
                    var argTypes = new ArrayList<TypeInfo> { delegateType, delegateType };
                    if (operator == AssignOperator.Add) {
                        generator.emit(Opcode.Invokestatic, delegateType.getMethod("combine", argTypes));
                    } else {
                        generator.emit(Opcode.Invokestatic, delegateType.getMethod("remove", argTypes));
                    }
                    generator.emit(Opcode.Checkcast, method.ReturnType);
                    if (!requireLocals && nested && !postAssign) {
                        generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                    }
                } else {
                    TypeInfo rightType;
                    switch (operator) {
                    case LeftShift:
                    case RightShift:
                    case UnsignedRightShift:
                        rightType = context.TypeSystem.IntType;
                        break;
                    default:
                        rightType = method.ReturnType;
                        break;
                    }
                    switch (method.ReturnType.NumericTypeKind) {
                    case Byte:
                        if (!requireLocals && nested && postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        emitUnbox(false, context.TypeSystem.ByteType, false, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Iconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitIntOperator();
                        generator.emit(Opcode.I2b);
                        emitBox(false, context.TypeSystem.ByteType, false, false);
                        if (!requireLocals && nested && !postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        break;

                    case Char:
                        if (!requireLocals && nested && postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        emitUnbox(false, context.TypeSystem.CharType, false, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Iconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitIntOperator();
                        generator.emit(Opcode.I2c);
                        emitBox(false, context.TypeSystem.CharType, false, false);
                        if (!requireLocals && nested && !postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        break;

                    case Double:
                        if (!requireLocals && nested && postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup2_X1 : Opcode.Dup2_X2);
                        }
                        emitUnbox(false, context.TypeSystem.DoubleType, false, true);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Dconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitDoubleOperator();
                        emitBox(false, context.TypeSystem.DoubleType, false, true);
                        if (!requireLocals && nested && !postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup2_X1 : Opcode.Dup2_X2);
                        }
                        break;

                    case Float:
                        if (!requireLocals && nested && postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        emitUnbox(false, context.TypeSystem.FloatType, false, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Fconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitFloatOperator();
                        emitBox(false, context.TypeSystem.FloatType, false, false);
                        if (!requireLocals && nested && !postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        break;

                    case Int:
                        if (!requireLocals && nested && postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        emitUnbox(false, context.TypeSystem.IntType, false, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Iconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitIntOperator();
                        emitBox(false, context.TypeSystem.IntType, false, false);
                        if (!requireLocals && nested && !postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        break;

                    case Long:
                        if (!requireLocals && nested && postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup2_X1 : Opcode.Dup2_X2);
                        }
                        emitUnbox(false, context.TypeSystem.LongType, false, true);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Lconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitLongOperator();
                        emitBox(false, context.TypeSystem.LongType, false, true);
                        if (!requireLocals && nested && !postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup2_X1 : Opcode.Dup2_X2);
                        }
                        break;

                    case Short:
                        if (!requireLocals && nested && postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        emitUnbox(false, context.TypeSystem.ShortType, false, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Iconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.getType(), rightType);
                        }
                        emitIntOperator();
                        generator.emit(Opcode.I2s);
                        emitBox(false, context.TypeSystem.ShortType, false, false);
                        if (!requireLocals && nested && !postAssign) {
                            generator.emit((isStatic) ? Opcode.Dup_X1 : Opcode.Dup_X2);
                        }
                        break;

                    default:
                        throw new Exception("Internal error");
                    }
                }
                if (requireLocals) {
                    generator.emit(BytecodeHelper.getStoreOpcode(method.ReturnType),
                        generator.declareLocal(method.ReturnType, "elementValue$0"));
                    if (!isStatic) {
                        generator.emit(Opcode.Aload, generator.getLocal("elementTarget$0"));
                    }
                    int n = 0;
                    foreach (var p in method.Parameters) {
                        generator.emit(BytecodeHelper.getLoadOpcode(p.Type), generator.getLocal("elementIndex$" + n++));
                    }
                    generator.emit(BytecodeHelper.getLoadOpcode(method.ReturnType), generator.getLocal("elementValue$0"));
                }
                CompilerHelper.emitPropertyOrIndexerModification(context, info.Member.SetAccessor);
                if (requireLocals) {
                    if (nested) {
                        if (postAssign) {
                            generator.emit(BytecodeHelper.getLoadOpcode(method.ReturnType), generator.getLocal("elementResult$0"));
                        } else {
                            generator.emit(BytecodeHelper.getLoadOpcode(method.ReturnType), generator.getLocal("elementValue$0"));
                        }
                    }
                    generator.endScope();
                }
            } else {
                expressionGenerator.handleExpression(elementAccess.TargetObject, null, true);
                var index = elementAccess.Indexes[0];
                expressionGenerator.handleExpression(index, null, true);
                BytecodeHelper.emitNumericConversion(generator, index.getUserData(typeof(ExpressionInfo)).Type, context.TypeSystem.IntType);
                
                generator.emit(Opcode.Dup2);
                if (ttype.ElementType.IsBoolean) {
                    generator.emit(Opcode.Baload);
                    if (nested && postAssign) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    if (rightExpression == null) {
                        generator.emit(Opcode.Iconst_1);
                    } else {
                        expressionGenerator.handleExpression(rightExpression, null, true);
                    }
                    emitIntOperator();
                    generator.emit(Opcode.I2b);
                    if (nested && !postAssign) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    generator.emit(Opcode.Bastore);
                } else if (ttype.ElementType == context.TypeSystem.StringType) {
                    generator.emit(Opcode.Aaload);
                    if (nested && postAssign) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    var stringType = context.TypeSystem.StringType;
                    generator.emit(Opcode.New, context.StringBuilderType);
                    generator.emit(Opcode.Dup);
                    generator.emit(Opcode.Invokespecial, context.StringBuilderConstructor);
                    if (rightInfo.getType().IsCategory2) {
                        generator.emit(Opcode.Dup_X2);
                        generator.emit(Opcode.Pop);
                    } else {
                        generator.emit(Opcode.Swap);
                    }
                    BytecodeGenerator.emitStringBuilderAppend(context, null);
                    var isString = rightInfo != null && rightInfo.getType() == stringType;
                    var isStringAdd = rightExpression.getExpressionKind() == ExpressionKind.Binary && isString;
                    context.MethodGenerationContext.IsBuildingString = isStringAdd;
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    if (!isStringAdd || (isString && rightInfo.IsConstant)) {
                        BytecodeGenerator.emitStringBuilderAppend(context, rightExpression.getUserData(typeof(ExpressionInfo)));
                    }
                    context.MethodGenerationContext.IsBuildingString = false;
                    generator.emit(Opcode.Invokevirtual, context.ObjectToStringMethod);
                    if (nested && !postAssign) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    generator.emit(Opcode.Bastore);
                } else if (BytecodeHelper.isDelegateType(ttype.ElementType)) {
                    generator.emit(Opcode.Aaload);
                    if (nested && postAssign) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    expressionGenerator.handleExpression(rightExpression, null, true);
                    var delegateType = context.TypeSystem.getType("stab/lang/Delegate");
                    var argTypes = new ArrayList<TypeInfo> { delegateType, delegateType };
                    if (operator == AssignOperator.Add) {
                        generator.emit(Opcode.Invokestatic, delegateType.getMethod("combine", argTypes));
                    } else {
                        generator.emit(Opcode.Invokestatic, delegateType.getMethod("remove", argTypes));
                    }
                    generator.emit(Opcode.Checkcast, ttype);
                    if (nested && !postAssign) {
                        generator.emit(Opcode.Dup_X2);
                    }
                    generator.emit(Opcode.Bastore);
                } else {
                    TypeInfo rightType;
                    switch (operator) {
                    case LeftShift:
                    case RightShift:
                    case UnsignedRightShift:
                        rightType = context.TypeSystem.IntType;
                        break;
                    default:
                        rightType = ttype.ElementType;
                        break;
                    }
                    switch (ttype.ElementType.NumericTypeKind) {
                    case Byte:
                        emitAload(nested, Opcode.Baload, context.TypeSystem.ByteType, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Iconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitIntOperator();
                        generator.emit(Opcode.I2b);
                        emitAstore(nested, Opcode.Bastore, context.TypeSystem.ByteType, false);
                        break;
                    case Char:
                        emitAload(nested, Opcode.Caload, context.TypeSystem.CharType, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Iconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitIntOperator();
                        generator.emit(Opcode.I2c);
                        emitAstore(nested, Opcode.Castore, context.TypeSystem.CharType, false);
                        break;
                    case Float:
                        emitAload(nested, Opcode.Faload, context.TypeSystem.FloatType, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Fconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitFloatOperator();
                        emitAstore(nested, Opcode.Fastore, context.TypeSystem.FloatType, false);
                        break;
                    case Double:
                        emitAload(nested, Opcode.Daload, context.TypeSystem.DoubleType, true);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Dconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitDoubleOperator();
                        emitAstore(nested, Opcode.Dastore, context.TypeSystem.DoubleType, true);
                        break;
                    case Int:
                        emitAload(nested, Opcode.Iaload, context.TypeSystem.IntType, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Iconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitIntOperator();
                        emitAstore(nested, Opcode.Iastore, context.TypeSystem.IntType, false);
                        break;
                    case Long:
                        emitAload(nested, Opcode.Laload, context.TypeSystem.LongType, true);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Lconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitLongOperator();
                        emitAstore(nested, Opcode.Lastore, context.TypeSystem.LongType, true);
                        break;
                    case Short:
                        emitAload(nested, Opcode.Saload, context.TypeSystem.ShortType, false);
                        if (rightExpression == null) {
                            generator.emit(Opcode.Iconst_1);
                        } else {
                            expressionGenerator.handleExpression(rightExpression, null, true);
                            BytecodeHelper.emitNumericConversion(generator, rightInfo.Type, rightType);
                        }
                        emitIntOperator();
                        generator.emit(Opcode.I2s);
                        emitAstore(nested, Opcode.Sastore, context.TypeSystem.ShortType, false);
                        break;
                    default:
                        throw new IllegalStateException();
                    }
                }
            }
        }

        private void emitUnbox(bool nested, TypeInfo type, bool isStatic, bool category2) {
            var generator = context.MethodGenerationContext.Generator;
            switch (boxingKind) {
            case Box:
                if (nested && postAssign) {
                    generator.emit(Opcode.Dup);
                }
                generator.emit(Opcode.Invokevirtual, context.TypeSystem.getUnboxingMethod(type));
                break;
            case Unbox:
                if (nested && postAssign) {
                    generator.emit((category2) ? (isStatic) ? Opcode.Dup2 : Opcode.Dup2_X1 : (isStatic) ? Opcode.Dup : Opcode.Dup_X1);
                }
                generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod(type));
                break;
            default:
                if (nested && postAssign) {
                    generator.emit((category2) ? (isStatic) ? Opcode.Dup2 : Opcode.Dup2_X1 : (isStatic) ? Opcode.Dup : Opcode.Dup_X1);
                }
                break;
            }
        }

        private void emitBox(bool nested, TypeInfo type, bool isStatic, bool category2) {
            var generator = context.MethodGenerationContext.Generator;
            switch (boxingKind) {
            case Box:
                generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod(type));
                if (nested && !postAssign) {
                    generator.emit(Opcode.Dup);
                }
                break;
            case Unbox:
                if (nested && !postAssign) {
                    generator.emit((category2) ? (isStatic) ? Opcode.Dup2 : Opcode.Dup2_X1 : (isStatic) ? Opcode.Dup : Opcode.Dup_X1);
                }
                generator.emit(Opcode.Invokevirtual, context.TypeSystem.getUnboxingMethod(type));
                break;
            default:
                if (nested && !postAssign) {
                    generator.emit((category2) ? (isStatic) ? Opcode.Dup2 : Opcode.Dup2_X1 : (isStatic) ? Opcode.Dup : Opcode.Dup_X1);
                }
                break;
            }
        }
        
        private void emitAload(bool nested, Opcode opcode, TypeInfo type, bool category2) {
            var generator = context.MethodGenerationContext.Generator;
            switch (boxingKind) {
            case Box:
                generator.emit(Opcode.Aaload);
                if (nested && postAssign) {
                    generator.emit(Opcode.Dup_X2);
                }
                generator.emit(Opcode.Invokevirtual, context.TypeSystem.getUnboxingMethod(type));
                break;
            case Unbox:
                generator.emit(opcode);
                generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod(type));
                if (nested && postAssign) {
                    generator.emit((category2) ? Opcode.Dup2_X2 : Opcode.Dup_X2);
                }
                break;
            default:
                generator.emit(opcode);
                if (nested && postAssign) {
                    generator.emit((category2) ? Opcode.Dup2_X2 : Opcode.Dup_X2);
                }
                break;
            }
        }
        
        private void emitAstore(bool nested, Opcode opcode, TypeInfo type, bool category2) {
            var generator = context.MethodGenerationContext.Generator;
            switch (boxingKind) {
            case Box:
                generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod(type));
                if (nested && !postAssign) {
                    generator.emit(Opcode.Dup_X2);
                }
                generator.emit(Opcode.Aastore);
                break;
            case Unbox:
                if (nested && !postAssign) {
                    generator.emit((category2) ? Opcode.Dup2_X2 : Opcode.Dup_X2);
                }
                generator.emit(Opcode.Invokevirtual, context.TypeSystem.getUnboxingMethod(type));
                generator.emit(opcode);
                break;
            default:
                if (nested && !postAssign) {
                    generator.emit((category2) ? Opcode.Dup2_X2 : Opcode.Dup_X2);
                }
                generator.emit(opcode);
                break;
            }
        }

        private void emitLoad(bool nested, Opcode opcode, LocalInfo local, bool category2) {
            var generator = context.MethodGenerationContext.Generator;
            switch (boxingKind) {
            case Box:
                generator.emit(Opcode.Aload, local);
                if (nested && postAssign) {
                    generator.emit(Opcode.Dup);
                }
                generator.emit(Opcode.Invokevirtual, context.TypeSystem.getUnboxingMethod(local.Type));
                break;
            case Unbox:
                generator.emit(opcode, local);
                if (nested && postAssign) {
                    generator.emit((category2) ? Opcode.Dup2 : Opcode.Dup);
                }
                generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod(local.Type));
                break;
            default:
                generator.emit(opcode, local);
                if (nested && postAssign) {
                    generator.emit((category2) ? Opcode.Dup2 : Opcode.Dup);
                }
                break;
            }
        }
        
        private void emitStore(bool nested, Opcode opcode, LocalInfo local, bool category2) {
            var generator = context.MethodGenerationContext.Generator;
            switch (boxingKind) {
            case Box:
                generator.emit(Opcode.Invokestatic, context.TypeSystem.getBoxingMethod(local.Type));
                if (nested && !postAssign) {
                    generator.emit(Opcode.Dup);
                }
                generator.emit(Opcode.Astore, local);
                break;
            case Unbox:
                if (nested && !postAssign) {
                    generator.emit((category2) ? Opcode.Dup2 : Opcode.Dup);
                }
                generator.emit(Opcode.Invokevirtual, context.TypeSystem.getUnboxingMethod(local.Type));
                generator.emit(opcode, local);
                break;
            default:
                if (nested && !postAssign) {
                    generator.emit((category2) ? Opcode.Dup2 : Opcode.Dup);
                }
                generator.emit(opcode, local);
                break;
            }
        }
        
        private void emitIntOperator() {
            var generator = context.MethodGenerationContext.Generator;
            switch (operator) {
            case Add:
                generator.emit(Opcode.Iadd);
                break;

            case Divide:
                generator.emit(Opcode.Idiv);
                break;
                
            case And:
                generator.emit(Opcode.Iand);
                break;
                
            case LeftShift:
                generator.emit(Opcode.Ishl);
                break;
                
            case Modulo:
                generator.emit(Opcode.Irem);
                break;
                
            case Multiply:
                generator.emit(Opcode.Imul);
                break;
                
            case Or:
                generator.emit(Opcode.Ior);
                break;
                
            case RightShift:
                generator.emit(Opcode.Ishr);
                break;
                
            case Subtract:
                generator.emit(Opcode.Isub);
                break;
                
            case UnsignedRightShift:
                generator.emit(Opcode.Iushr);
                break;
                
            case Xor:
                generator.emit(Opcode.Ixor);
                break;
                
            default:
                throw new IllegalStateException();
            }
        }

        private void emitLongOperator() {
            var generator = context.MethodGenerationContext.Generator;
            switch (operator) {
            case Add:
                generator.emit(Opcode.Ladd);
                break;

            case Divide:
                generator.emit(Opcode.Ldiv);
                break;
                
            case And:
                generator.emit(Opcode.Land);
                break;
                
            case LeftShift:
                generator.emit(Opcode.Lshl);
                break;
                
            case Modulo:
                generator.emit(Opcode.Lrem);
                break;
                
            case Multiply:
                generator.emit(Opcode.Lmul);
                break;
                
            case Or:
                generator.emit(Opcode.Lor);
                break;
                
            case RightShift:
                generator.emit(Opcode.Lshr);
                break;
                
            case Subtract:
                generator.emit(Opcode.Lsub);
                break;
                
            case UnsignedRightShift:
                generator.emit(Opcode.Lushr);
                break;
                
            case Xor:
                generator.emit(Opcode.Lxor);
                break;
                
            default:
                throw new IllegalStateException();
            }
        }
        
        private void emitFloatOperator() {
            var generator = context.MethodGenerationContext.Generator;
            switch (operator) {
            case Add:
                generator.emit(Opcode.Fadd);
                break;

            case Divide:
                generator.emit(Opcode.Fdiv);
                break;
                
            case Modulo:
                generator.emit(Opcode.Frem);
                break;
                
            case Multiply:
                generator.emit(Opcode.Fmul);
                break;
                
            case Subtract:
                generator.emit(Opcode.Fsub);
                break;
                
            default:
                throw new IllegalStateException();
            }
        }
        
        private void emitDoubleOperator() {
            var generator = context.MethodGenerationContext.Generator;
            switch (operator) {
            case Add:
                generator.emit(Opcode.Dadd);
                break;

            case Divide:
                generator.emit(Opcode.Ddiv);
                break;
                
            case Modulo:
                generator.emit(Opcode.Drem);
                break;
                
            case Multiply:
                generator.emit(Opcode.Dmul);
                break;
                
            case Subtract:
                generator.emit(Opcode.Dsub);
                break;
                
            default:
                throw new IllegalStateException();
            }
        }
    }
}
