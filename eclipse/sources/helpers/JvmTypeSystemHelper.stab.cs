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
using cnatural.helpers;

namespace cnatural.eclipse.helpers {

	public class JvmTypeSystemHelper {
	
		public static bool isStructurallyEqual(TypeInfo oldType, TypeInfo newType) {
			if (oldType.IsNestedPrivate) {
				return true;
			}
			if (!sameTypes(oldType.BaseType, newType.BaseType)) {
				return false;
			}
			
			foreach (var i in oldType.Interfaces) {
				bool found = false;
				foreach (var j in newType.Interfaces) {
					if (sameTypes(i, j)) {
						found = true;
						break;
					}
				}
				if (!found) {
					return false;
				}
			}
			
			foreach (var f in oldType.Fields.where(p => !p.IsSynthetic && !p.IsPrivate)) {
				var g = newType.getField(f.Name);
				if (g == null) {
					return false;
				}
				if (!sameTypes(f.Type, g.Type)) {
					return false;
				}
				if (f.IsStatic != g.IsStatic) {
					return false;
				}
				if (f.IsPublic != g.IsPublic) {
					return false;
				}
				if (f.IsProtected != g.IsProtected) {
					return false;
				}
				if (f.IsPrivate != g.IsPrivate) {
					return false;
				}
				if (f.IsFinal != g.IsFinal) {
					return false;
				}
			}
			foreach (var f in newType.Fields.where(p => !p.IsSynthetic && !p.IsPrivate)) {
				var g = oldType.getField(f.Name);
				if (g == null) {
					return false;
				}
			}
	
			foreach (var m in oldType.Methods.where(p => !p.IsSynthetic && !p.IsPrivate)) {
				var found = false;
				foreach (var n in newType.Methods.where(p => !p.IsSynthetic && !p.IsPrivate)) {
					if (sameMethods(m, n)) {
						if (m.IsStatic != n.IsStatic) {
							return false;
						}
						if (m.IsPublic != n.IsPublic) {
							return false;
						}
						if (m.IsProtected != n.IsProtected) {
							return false;
						}
						if (m.IsPrivate != n.IsPrivate) {
							return false;
						}
						if (m.IsFinal != n.IsFinal) {
							return false;
						}
						found = true;
						break;
					}
				}
				if (!found) {
					return false;
				}
			}
			
			foreach (var m in newType.Methods.where(p => !p.IsSynthetic && !p.IsPrivate)) {
				var found = false;
				foreach (var n in oldType.Methods) {
					if (sameMethods(m, n)) {
						found = true;
						break;
					}
				}
				if (!found) {
					return false;
				}
			}
			return true;
		}
		
		public static TypeInfo getType(Library typeSystem, String fullName) {
			TypeInfo result = null;
			int index;
			while ((index = fullName.indexOf('$')) != -1) {
				String prefix = fullName.substring(0, index);
				if (result == null) {
					result = typeSystem.getType(prefix);
				} else {
					result = result.getNestedType(prefix);
				}
				fullName = fullName.substring(index + 1);
			}
			if (result == null) {
				result = typeSystem.getType(fullName);
			} else {
				result = result.getNestedType(fullName);
			}
			return result;
		}
		
		public static void cloneTypes(Iterable<TypeInfo> types, Library targetTypeSystem) {
			var t0 = System.nanoTime();
			
			foreach (var type in types) {
				defineType(type, targetTypeSystem, null);
			}
			foreach (var type in types) {
				declareType(type, targetTypeSystem, (TypeBuilder)targetTypeSystem.getType(type.FullName), new Scope<String, TypeInfo>());
			}
			
			Environment.trace(targetTypeSystem, types.count() + " types cloned in " + ((System.nanoTime() - t0) / 1e6) + "ms");
		}

		private static bool sameMethods(MethodInfo oldMethod, MethodInfo newMethod) {
			if (!oldMethod.Name.equals(newMethod.Name)) {
				return false;
			}
			if (oldMethod.Parameters.count() != newMethod.Parameters.count()) {
				return false;
			}
			var it1 = oldMethod.getParameters().iterator();
			var it2 = newMethod.getParameters().iterator();
			while (it1.hasNext()) {
				if (!sameTypes(it1.next().Type, it2.next().Type)) {
					return false;
				}
			}
			return true;
		}

		private static bool sameTypes(TypeInfo oldType, TypeInfo newType) {
			switch (oldType.TypeKind) {
			case Boolean:
			case Byte:
			case Char:
			case Double:
			case Float:
			case Int:
			case Long:
			case Short:
			case Void:
			case UnboundedWildcard:
				return oldType.TypeKind == newType.TypeKind;
				
			case Array:
				if (newType.TypeKind != TypeKind.Array) {
					return false;
				}
				return sameTypes(oldType.ElementType, newType.ElementType);
				
			case LowerBoundedWildcard:
			case UpperBoundedWildcard:
				if (newType.TypeKind != oldType.TypeKind) {
					return false;
				}
				return sameTypes(oldType.WildcardBound, newType.WildcardBound);
				
			case GenericParameter:
				if (newType.TypeKind != TypeKind.GenericParameter) {
					return false;
				}
				return oldType.FullName.equals(newType.FullName);
				
			case Reference:
				if (newType.TypeKind != TypeKind.Reference) {
					return false;
				}
				if (oldType.OriginalTypeDefinition.FullName.equals(newType.OriginalTypeDefinition.FullName)) {
					if (oldType.GenericArguments.count() == newType.GenericArguments.count()) {
						var it1 = oldType.GenericArguments.iterator();
						var it2 = newType.GenericArguments.iterator();
						while (it1.hasNext()) {
							if (!sameTypes(it1.next(), it2.next())) {
								return false;
							}
						}
						return true;
					}
				}
				return false;
				
			default:
				throw new RuntimeException("Internal error " + oldType.getTypeKind());
			}
		}
		
		private static void defineType(TypeInfo type, Library targetTypeSystem, TypeBuilder declaringType) {
			if (type.IsSynthetic) {
				return;
			}
			TypeBuilder clone;
			if (declaringType == null) {
				clone = targetTypeSystem.defineType(type.FullName);
			} else {
				clone = declaringType.defineNestedType(type.Name);
				
				clone.setNestedAbstract(type.IsNestedAbstract);
				clone.setNestedAnnotation(type.IsNestedAnnotation);
				clone.setNestedEnum(type.IsNestedEnum);
				clone.setNestedFinal(type.IsNestedFinal);
				clone.setNestedInterface(type.IsNestedInterface);
				clone.setNestedPrivate(type.IsNestedPrivate);
				clone.setNestedProtected(type.IsNestedProtected);
				clone.setNestedPublic(type.IsNestedPublic);
				clone.setNestedStatic(type.IsNestedStatic);
			}
			
			clone.setAbstract(type.IsAbstract);
			clone.setAnnotation(type.IsNestedAnnotation);
			clone.setEnum(type.IsEnum);
			clone.setFinal(type.IsFinal);
			clone.setInterface(type.IsInterface);
			clone.setPublic(type.IsPublic);
			clone.setSuper(type.IsSynthetic);
			
			foreach (var ga in type.GenericArguments) {
				clone.addGenericArgument(ga.FullName);
			}
	
			foreach (var nt in type.getNestedTypes()) {
				defineType(nt, targetTypeSystem, clone);
			}
		}
		
		private static void declareType(TypeInfo type, Library targetTypeSystem, TypeBuilder clone, Scope<String, TypeInfo> genericArgs) {
			if (type.IsSynthetic) {
				return;
			}
			
			genericArgs.enterScope();
			foreach (var ga in clone.GenericArguments) {
				genericArgs.declareBinding(ga.FullName, ga);
			}
	
			clone.setBaseType(getType(targetTypeSystem, type.BaseType, genericArgs));
			
			foreach (var t in type.Interfaces) {
				clone.addInterface(getType(targetTypeSystem, t, genericArgs));
			}
	
			foreach (var av in type.Annotations) {
				var avb = clone.addAnnotation(getType(targetTypeSystem, av.Type, genericArgs), av.IsRuntimeVisible);
				cloneAnnotationValue(av, targetTypeSystem, avb, genericArgs);
			}
			
			foreach (var f in type.Fields.where(p => !p.IsSynthetic && !p.IsPrivate)) {
				var fb = clone.defineField(f.Name, getType(targetTypeSystem, f.Type, genericArgs));
				fb.setEnum(f.IsEnum);
				fb.setFinal(f.IsFinal);
				fb.setProtected(f.IsProtected);
				fb.setPublic(f.IsPublic);
				fb.setStatic(f.IsStatic);
				fb.setTransient(f.IsTransient);
				fb.setVolatile(f.IsVolatile);
				
				fb.setValue(f.Value);
				
				foreach (var av in f.getAnnotations()) {
					var avb = fb.addAnnotation(getType(targetTypeSystem, av.Type, genericArgs), av.IsRuntimeVisible);
					cloneAnnotationValue(av, targetTypeSystem, avb, genericArgs);
				}
			}
	
			foreach (var m in type.Methods.where(p => !p.IsSynthetic && !p.IsPrivate)) {
				var mb = clone.defineMethod(m.Name);
				mb.setAbstract(m.IsAbstract);
				mb.setBridge(m.IsBridge);
				mb.setFinal(m.IsFinal);
				mb.setNative(m.IsNative);
				mb.setProtected(m.IsProtected);
				mb.setPublic(m.IsPublic);
				mb.setStatic(m.IsStatic);
				mb.setStrict(m.IsStrict);
				mb.setSynchronized(m.IsSynchronized);
				mb.setVarargs(m.IsVarargs);
				
				foreach (var av in m.Annotations) {
					var avb = mb.addAnnotation(getType(targetTypeSystem, av.Type, genericArgs), av.IsRuntimeVisible);
					cloneAnnotationValue(av, targetTypeSystem, avb, genericArgs);
				}
				
				genericArgs.enterScope();
				foreach (var ga in m.GenericArguments) {
					var t = mb.addGenericArgument(ga.FullName);
					genericArgs.declareBinding(t.FullName, t);
				}
				
				mb.setReturnType(getType(targetTypeSystem, m.ReturnType, genericArgs));
				foreach (var p in m.Parameters) {
					var pb = mb.addParameter(getType(targetTypeSystem, p.Type, genericArgs));
					pb.setName(p.Name);
				}
				genericArgs.leaveScope();
			}
			
			foreach (var nt in type.NestedTypes) {
				declareType(nt, targetTypeSystem, (TypeBuilder)clone.getNestedType(nt.Name), genericArgs);
			}
			
			genericArgs.leaveScope();
		}

		private static void cloneAnnotationValue(AnnotationValue value, Library targetTypeSystem,
				AnnotationValueBuilder builder, Scope<String, TypeInfo> genericArgs) {
			foreach (var s in value.ArgumentNames) {
				var a = value.getArgument(s);
				switch (a.AnnotationArgumentKind) {
				case Annotation:
					cloneAnnotationValue((AnnotationValue)a, targetTypeSystem,
							builder.setAnnotationArgument(s, a.Type, a.IsRuntimeVisible), genericArgs);
					break;
				case Array:
					var aab = builder.setArrayArgument(s);
					foreach (var aa in a.Elements) {
						cloneAnnotationArgument(aa, targetTypeSystem, aab, genericArgs);
					}
					break;
				case Boolean:
					builder.setBooleanArgument(s, (Boolean)a.Value);
					break;
				case Byte:
					builder.setByteArgument(s, (Byte)a.Value);
					break;
				case Char:
					builder.setCharArgument(s, (Character)a.Value);
					break;
				case Double:
					builder.setDoubleArgument(s, (Double)a.Value);
					break;
				case Enum:
					builder.setEnumArgument(s, a.getType(), a.Name);
					break;
				case Float:
					builder.setFloatArgument(s, (Float)a.Value);
					break;
				case Int:
					builder.setIntArgument(s, (Integer)a.Value);
					break;
				case Long:
					builder.setLongArgument(s, (Long)a.Value);
					break;
				case Short:
					builder.setShortArgument(s, (Short)a.Value);
					break;
				case String:
					builder.setStringArgument(s, (String)a.Value);
					break;
				case Type:
					builder.setTypeArgument(s, a.Type);
					break;
				}
			}
		}
	
		private static void cloneAnnotationArgument(AnnotationArgument arg, Library targetTypeSystem,
				AnnotationArrayValueBuilder builder, Scope<String, TypeInfo> genericArgs) {
			switch (arg.AnnotationArgumentKind) {
			case Annotation:
				cloneAnnotationValue((AnnotationValue)arg, targetTypeSystem,
						builder.addAnnotationArgument(arg.Type, arg.IsRuntimeVisible), genericArgs);
				break;
			case Array:
				var avb = builder.addArrayArgument();
				foreach (var aa in arg.Elements) {
					cloneAnnotationArgument(aa, targetTypeSystem, avb, genericArgs);
				}
				break;
			case Boolean:
				builder.addBooleanArgument((Boolean)arg.Value);
				break;
			case Byte:
				builder.addByteArgument((Byte)arg.Value);
				break;
			case Char:
				builder.addCharArgument((Character)arg.Value);
				break;
			case Double:
				builder.addDoubleArgument((Double)arg.Value);
				break;
			case Enum:
				builder.addEnumArgument(arg.Type, arg.Name);
				break;
			case Float:
				builder.addFloatArgument((Float)arg.Value);
				break;
			case Int:
				builder.addIntArgument((Integer)arg.Value);
				break;
			case Long:
				builder.addLongArgument((Long)arg.Value);
				break;
			case Short:
				builder.addShortArgument((Short)arg.Value);
				break;
			case String:
				builder.addStringArgument((String)arg.Value);
				break;
			case Type:
				builder.addTypeArgument(arg.Type);
				break;
			}
		}
		
		private static TypeInfo getType(Library typeSystem, TypeInfo type, Scope<String, TypeInfo> genericArgs) {
			switch (type.TypeKind) {
			case Boolean:
			case Byte:
			case Char:
			case Double:
			case Float:
			case Int:
			case Long:
			case Short:
			case Void:
				return typeSystem.getPrimitiveType(type.TypeKind);
				
			case Reference:
				TypeInfo result;
				if (type.DeclaringType == null) {
					result = typeSystem.getType(type.OriginalTypeDefinition.FullName);
				} else {
					result = getType(typeSystem, type.DeclaringType, genericArgs).getNestedType(type.Name);
				}
				if (type != type.OriginalTypeDefinition) {
					result = typeSystem.getGenericType(result, type.GenericArguments.select(p => getType(typeSystem, p, genericArgs)).toList());
				}
				if (result == null) {
					throw new IllegalStateException("Type not found in target file system: " + type.FullName);
				}
				return result;
				
			case UnboundedWildcard:
				return typeSystem.UnboundedWildcard;
				
			case LowerBoundedWildcard:
				return getType(typeSystem, type.WildcardBound, genericArgs).LowerBoundedWildcard;
				
			case UpperBoundedWildcard:
				return getType(typeSystem, type.WildcardBound, genericArgs).UpperBoundedWildcard;
				
			case Array:
				return getType(typeSystem, type.ElementType, genericArgs).ArrayType;
				
			case GenericParameter:
				return genericArgs.getBindingValue(type.FullName);
			default:
				throw new RuntimeException("Internal error " + type.getTypeKind());
			}
		}
	}
}
