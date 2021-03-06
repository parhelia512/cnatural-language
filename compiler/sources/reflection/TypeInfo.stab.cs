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
using org.objectweb.asm;
using stab.query;
using cnatural.helpers;

namespace stab.reflection {

    public enum TypeKind {
        Array,
        Boolean,
        Byte,
        Char,
        Double,
        Float,
        GenericParameter,
        Int,
        Long,
        LowerBoundedWildcard,
        Reference,
        Short,
        UnboundedWildcard,
        UpperBoundedWildcard,
        Void
    }

    public enum NumericTypeKind {
        None,
        Byte,
        Char,
        Double,
        Float,
        Int,
        Long,
        Short
    }

    public abstract class TypeInfo : UserDataContainer {
		protected String signature*;
        private TypeInfo lowerBoundedWildcard;
        private TypeInfo upperBoundedWildcard;
        private TypeInfo arrayType;
        
        protected TypeInfo(Library library, TypeKind typeKind) {
        	this.Library = library;
            this.TypeKind = typeKind;
        }

		public Library Library^;
        
        public TypeKind TypeKind^;
        
		public abstract String DisplayName {
			get;
		}
		
        public abstract String FullName {
            get;
        }

        public virtual String PackageName {
            get {
				throw new UnsupportedOperationException();
            }
        }

        public virtual String Name {
            get {
				throw new UnsupportedOperationException();
            }
        }
        
        public virtual NumericTypeKind NumericTypeKind {
            get {
                return NumericTypeKind.None;
            }
        }
        
        public bool IsAbstract {
            get {
                return (this.Modifiers & Opcodes.ACC_ABSTRACT) != 0;
            }
        }
        
        public bool IsAnnotation {
            get {
                return (this.Modifiers & Opcodes.ACC_ANNOTATION) != 0;
            }
        }
        
        public bool IsEnum {
            get {
                return (this.Modifiers & Opcodes.ACC_ENUM) != 0;
            }
        }
        
        public bool IsFinal {
            get {
                return (this.Modifiers & Opcodes.ACC_FINAL) != 0;
            }
        }
        
        public bool IsInterface {
            get {
                return (this.Modifiers & Opcodes.ACC_INTERFACE) != 0;
            }
        }
        
        public bool IsPublic {
            get {
                return (this.Modifiers & Opcodes.ACC_PUBLIC) != 0;
            }
        }
        
        public bool IsSynthetic {
            get {
                return (this.Modifiers & Opcodes.ACC_SYNTHETIC) != 0;
            }
        }
        
        public bool IsNestedAbstract {
            get {
                return (this.NestedModifiers & Opcodes.ACC_ABSTRACT) != 0;
            }
        }

        public bool IsNestedAnnotation {
            get {
                return (this.NestedModifiers & Opcodes.ACC_ANNOTATION) != 0;
            }
        }

        public bool IsNestedEnum {
            get {
                return (this.NestedModifiers & Opcodes.ACC_ENUM) != 0;
            }
        }

        public bool IsNestedFinal {
            get {
                return (this.NestedModifiers & Opcodes.ACC_FINAL) != 0;
            }
        }

        public bool IsNestedInterface {
            get {
                return (this.NestedModifiers & Opcodes.ACC_INTERFACE) != 0;
            }
        }

        public bool IsNestedPrivate {
            get {
                return (this.NestedModifiers & Opcodes.ACC_PRIVATE) != 0;
            }
        }

        public bool IsNestedProtected {
            get {
                return (this.NestedModifiers & Opcodes.ACC_PROTECTED) != 0;
            }
        }

        public bool IsNestedPublic {
            get {
                return (this.NestedModifiers & Opcodes.ACC_PUBLIC) != 0;
            }
        }

        public bool IsNestedStatic {
            get {
                return (this.NestedModifiers & Opcodes.ACC_STATIC) != 0;
            }
        }

        public bool IsNestedSynthetic {
            get {
                return (this.NestedModifiers & Opcodes.ACC_SYNTHETIC) != 0;
            }
        }

        public virtual TypeInfo DeclaringType {
            get {
                return null;
            }
        }
        
        public virtual TypeInfo GenericTypeDefinition {
            get {
                return null;
            }
        }
        
        public virtual Iterable<TypeInfo> GenericArguments {
            get {
                return Query.empty();
            }
        }
        
        public virtual TypeInfo BaseType {
            get {
                return null;
            }
        }

        public virtual Iterable<TypeInfo> Interfaces {
            get {
                return Query.empty();
            }
        }

        public abstract String Descriptor {
            get;
        }
        
        public virtual Iterable<FieldInfo> Fields {
            get {
                return Query.empty();
            }
        }
        
        public virtual Iterable<MethodInfo> Methods {
            get {
                return Query.empty();
            }
        }
        
        public virtual Iterable<TypeInfo> NestedTypes {
            get {
                return Query.empty();
            }
        }
        
        public virtual Iterable<AnnotationValue> Annotations {
            get {
                return Query.empty();
            }
        }
        
        public virtual TypeInfo ElementType {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual Iterable<TypeInfo> GenericParameterBounds {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual TypeInfo WildcardBound {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public TypeInfo LowerBoundedWildcard {
            get {
                if (lowerBoundedWildcard == null) {
                    lowerBoundedWildcard = new WildcardType(this.Library, TypeKind.LowerBoundedWildcard, this);
                }
                return lowerBoundedWildcard;
            }
        }

        public TypeInfo UpperBoundedWildcard {
            get {
                if (upperBoundedWildcard == null) {
                    upperBoundedWildcard = new WildcardType(this.Library, TypeKind.UpperBoundedWildcard, this);
                }
                return upperBoundedWildcard;
            }
        }
        
        public TypeInfo ArrayType {
            get {
                if (arrayType == null) {
                    arrayType = new ArrayTypeInfo(this);
                }
                return arrayType;
            }
        }

        public virtual TypeInfo RawType {
            get {
				throw new UnsupportedOperationException();
            }
        }
        
        public bool IsRaw {
            get {
                return !this.GenericArguments.any() && this.GenericTypeDefinition != null;
            }
        }

        public bool IsGenericTypeDefinition {
            get {
                return this.GenericArguments.any() && this.GenericTypeDefinition == null;
            }
        }

        public TypeInfo OriginalTypeDefinition {
            get {
                var result = this;
                while (result.GenericTypeDefinition != null) {
                    result = result.GenericTypeDefinition;
                }
                return result;
            }
        }
        
        public bool IsPrimitive {
            get {
                switch (this.TypeKind) {
                case Boolean:
                case Byte:
                case Char:
                case Double:
                case Float:
                case Int:
                case Long:
                case Short:
                case Void:
                    return true;
                default:
                    return false;
                }
            }
        }
        
        public bool IsCategory2 {
            get {
                return this.TypeKind == TypeKind.Long || this.TypeKind == TypeKind.Double;
            }
        }
        
        public bool IsNumeric {
            get {
                return this.NumericTypeKind != NumericTypeKind.None;
            }
        }
        
        public bool IsArray {
            get {
                return this.TypeKind == TypeKind.Array;
            }
        }
        
        public bool IsGenericParameter {
            get {
                return this.TypeKind == TypeKind.GenericParameter;
            }
        }
        
        public FieldInfo getField(String name) {
            return this.Fields.where(p => p.Name.equals(name)).firstOrDefault();
        }
        
        public MethodInfo getMethod(String name, Iterable<TypeInfo> parameters) {
            var pcount = parameters.count();
            return this.Methods.where(m => m.Name.equals(name)
                && pcount == m.Parameters.count()
                && parameters.sequenceEqual(m.Parameters.select(p => p.Type))).firstOrDefault();
        }
        
        public TypeInfo getNestedType(String name) {
            return this.NestedTypes.where(p => p.Name.equals(name)).firstOrDefault();
        }
        
        public bool isAssignableFrom(TypeInfo type) {
            if (this == type || this.IsObject) {
                return true;
            }
			if (type.TypeKind == TypeKind.UpperBoundedWildcard) {
				type = type.WildcardBound;
			}
            if (this.IsNumeric) {
                if (!type.IsNumeric) {
                    return false;
                }
                switch (this.NumericTypeKind) {
                case Byte:
                    return type.NumericTypeKind == NumericTypeKind.Byte;
                    
                case Short:
                    return type.NumericTypeKind == NumericTypeKind.Byte || type.NumericTypeKind == NumericTypeKind.Short;
                    
                case Char:
                    return type.NumericTypeKind == NumericTypeKind.Char;
                    
                case Int:
                    switch (type.NumericTypeKind) {
                    case Long:
                    case Float:
                    case Double:
                        return false;
                    default:
                        return true;
                    }
                    
                case Long:
                    return type.NumericTypeKind != NumericTypeKind.Double && type.NumericTypeKind != NumericTypeKind.Float;
                    
                case Float:
                    return type.NumericTypeKind != NumericTypeKind.Double;

                default:
                    return true;
                }
            }
            if (this.IsBoolean && type.IsBoolean) {
                return true;
            }
            if (type.IsArray) {
                if (this.FullName.equals("java/lang/Cloneable") || this.FullName.equals("java/io/Serializable")) {
                    return true;
                } else if (this.IsArray) {
                    if (this.ElementType.IsPrimitive || type.ElementType.IsPrimitive) {
                        return this.ElementType == type.ElementType;
                    } else {
                        return this.ElementType.isAssignableFrom(type.ElementType);
                    }
                }
            }
			if (type.IsGenericParameter) {
				if (this.IsGenericParameter) {
					var gparams1 = this.GenericParameterBounds.count();
					var gparams2 = type.GenericParameterBounds.count();
					if (gparams1 == 0) {
						return true;
					}
					if (this.GenericParameterBounds.first().IsObject && (gparams2 == 0 || type.GenericParameterBounds.first().IsObject)) {
						return true;
					}
					return false;
				} else {
					foreach (var bound in type.GenericParameterBounds) {
						if (this.isAssignableFrom(bound)) {
							return true;
						}
					}
					return false;
				}
			}
            int typeArgs = this.GenericArguments.count();
            if (type.GenericArguments.count() != typeArgs) {
                if (typeArgs == 0 && this.OriginalTypeDefinition == type.OriginalTypeDefinition) {
                    return true;
                }
            }
            var t = type;
            do {
                if (t == this) {
                    return true;
                }
                if (typeArgs > 0 && this.OriginalTypeDefinition == t.OriginalTypeDefinition && typeArgs == t.GenericArguments.count()) {
                    var it1 = this.GenericArguments.iterator();
                    var it2 = t.GenericArguments.iterator();
                    var isCompatible = true;
                    while (it1.hasNext()) {
                        var t1 = it1.next();
                        var t2 = it2.next();
                        if (t1 == t2) {
                            continue;
                        }
                        switch (t1.TypeKind) {
                        case UnboundedWildcard:
                            break;
                        case UpperBoundedWildcard:
							if (t2.TypeKind == TypeKind.UpperBoundedWildcard) {
								t2 = t2.WildcardBound;
							}
                            if (!t1.WildcardBound.isAssignableFrom(t2)) {
                                isCompatible = false;
                            }
                            break;
                        case LowerBoundedWildcard:
							if (t2.TypeKind == TypeKind.LowerBoundedWildcard) {
								t2 = t2.WildcardBound;
							}
                            if (!t2.isAssignableFrom(t1.WildcardBound)) {
                                isCompatible = false;
                            }
                            break;
                        default:
                            isCompatible = false;
                            break;
                        }
                    }
                    if (isCompatible) {
                        return true;
                    }
                }
                if (t.Interfaces.any(p => this.isAssignableFrom(p))) {
                    return true;
                }
            } while ((t = t.BaseType) != null);
            return false;
        }
        
        public bool canAccessMember(TypeInfo declaringType, bool isPublic, bool isProtected, bool isPrivate) {
            if (this == declaringType.OriginalTypeDefinition || this == declaringType) {
                return true;
            }
            if (!declaringType.IsPublic && !declaringType.PackageName.equals(this.PackageName)) {
                return false;
            }
            var s = this;
            if (!declaringType.isAssignableFrom(s) && !declaringType.OriginalTypeDefinition.isAssignableFrom(s)) {
                while ((s = s.DeclaringType) != null) {
                    if (declaringType.isAssignableFrom(s) || declaringType.OriginalTypeDefinition.isAssignableFrom(s)) {
                        break;
                    }
                }
                if (s == null) {
                    if (declaringType.PackageName.equals(this.PackageName)) {
                        return !isPrivate;
                    } else {
                        return isPublic;
                    }
                }
            }
            if (s == declaringType.OriginalTypeDefinition || s == declaringType) {
                return true;
            }
            if (isPrivate) {
                return false;
            }
            if (isPublic || isProtected) {
                return true;
            }
            return declaringType.PackageName.equals(s.PackageName);
        }
        
        public Iterable<TypeInfo> getBaseClasses() {
            var t = this;
            while (t.BaseType != null) {
                yield return t.BaseType;
                t = t.BaseType;
            }
        }
        
        public Iterable<TypeInfo> getBaseTypes() {
            FunctionTT<TypeInfo, Iterable<TypeInfo>> f = null;
            f = p => Collections.singletonList(p).concat(p.Interfaces.selectMany(f));
            return this.Interfaces.selectMany(f).concat(this.getBaseClasses().selectMany(f)).distinct();
        }
        
        public bool IsObject {
            get {
                return this.TypeKind == TypeKind.Reference && this.BaseType == null;
            }
        }
        
        public bool IsBoolean {
            get {
                return this == this.Library.BooleanType || this.FullName.equals("java/lang/Boolean");
            }
        }
        
        public bool IsClosed {
            get {
                switch (this.TypeKind) {
                case GenericParameter:
                case LowerBoundedWildcard:
                case UpperBoundedWildcard:
                case UnboundedWildcard:
                    return false;
                case Array:
                    return this.ElementType.IsClosed;
                }
                foreach (var t in this.GenericArguments) {
                    if (!t.IsClosed) {
                        return false;
                    }
                }
                return true;
            }
        }

		public bool HasWildcardArgument {
			get {
                switch (this.TypeKind) {
                case LowerBoundedWildcard:
                case UpperBoundedWildcard:
                case UnboundedWildcard:
                    return true;
                case Array:
					switch (this.ElementType.TypeKind) {
					case LowerBoundedWildcard:
					case UpperBoundedWildcard:
					case UnboundedWildcard:
						return true;
					}
					return false;
                }
                foreach (var t in this.GenericArguments) {
					switch (t.TypeKind) {
					case LowerBoundedWildcard:
					case UpperBoundedWildcard:
					case UnboundedWildcard:
						return true;
					}
                }
                return false;
			}
		}
		
		public virtual String Signature {
			get {
				if (signature == null) {
					signature = getTypeSignature();
				}
				return signature;
			}
		}
        
		protected abstract String getTypeSignature();

        protected virtual int Modifiers {
            get {
                return Opcodes.ACC_PUBLIC;
            }
        }
        
        protected virtual int NestedModifiers {
            get {
                return Opcodes.ACC_PUBLIC;
            }
        }
    }
    
	// Primitive ==================================================================================
	
    class PrimitiveType : TypeInfo {
		private String descriptor;
		private String displayName;
	
        private NumericTypeKind numericTypeKind;
        
        PrimitiveType(Library library, TypeKind typeKind)
            : super(library, typeKind) {
			switch (typeKind) {
			case Void:
				displayName = "void";
				descriptor = "V";
				numericTypeKind = NumericTypeKind.None;
				break;
			case Boolean:
				displayName = "boolean";
				descriptor = "Z";
				numericTypeKind = NumericTypeKind.None;
				break;
			case Byte:
				displayName = "byte";
				descriptor = "B";
				numericTypeKind = NumericTypeKind.Byte;
				break;
			case Char:
				displayName = "char";
				descriptor = "C";
				numericTypeKind = NumericTypeKind.Char;
				break;
			case Short:
				displayName = "short";
				descriptor = "S";
				numericTypeKind = NumericTypeKind.Short;
				break;
			case Int:
				displayName = "int";
				descriptor = "I";
				numericTypeKind = NumericTypeKind.Int;
				break;
			case Long:
				displayName = "long";
				descriptor = "J";
				numericTypeKind = NumericTypeKind.Long;
				break;
			case Float:
				displayName = "float";
				descriptor = "F";
				numericTypeKind = NumericTypeKind.Float;
				break;
			case Double:
				displayName = "double";
				descriptor = "D";
				numericTypeKind = NumericTypeKind.Double;
				break;
			default:
				throw new IllegalArgumentException("typeKind = " + typeKind);
			}
        }
        
        public override NumericTypeKind NumericTypeKind {
            get {
                return numericTypeKind;
            }
        }
        
		public override String FullName {
			get {
				return this.Descriptor;
			}
		}
		
		public override String DisplayName {
			get {
				return displayName;
			}
		}
		
		public override String Descriptor {
			get {
				return descriptor;
			}
		}
		
		protected override String getTypeSignature() {
			return descriptor;
		}
    }
    
	// Wildcard ===================================================================================
	
    class WildcardType : TypeInfo {
        private TypeInfo wildcardBound;
        
        WildcardType(Library library, TypeKind typeKind, TypeInfo wildcardBound)
            : super(library, typeKind) {
            this.wildcardBound = wildcardBound;
        }
        
		public override String DisplayName {
			get {
				switch (this.TypeKind) {
				case UpperBoundedWildcard:
					return "? : " + wildcardBound.DisplayName;
				case LowerBoundedWildcard:
					return wildcardBound.DisplayName + " : ?";
				default:
					return "?";
				}
			}
		}
		
		public override String FullName {
			get {
				return "?";
			}
		}
		
		public override String Descriptor {
			get {
				return (this.TypeKind == TypeKind.UpperBoundedWildcard) ? wildcardBound.Descriptor : "Ljava/lang/Object;";
			}
		}
		
		public override TypeInfo WildcardBound {
			get {
				return wildcardBound;
			}
		}
		
		protected override String getTypeSignature() {
			switch (this.TypeKind) {
			case UpperBoundedWildcard:
				return "+" + wildcardBound.Signature;
			case LowerBoundedWildcard:
				return "-" + wildcardBound.Signature;
			default:
				return "*";
			}
		}
    }
    
	// Array ======================================================================================

	class ArrayTypeInfo : TypeInfo {
		private TypeInfo elementType;
		private String descriptor;
		private String displayName;

		ArrayTypeInfo(TypeInfo elementType)
				: super(elementType.Library, TypeKind.Array) {
			this.elementType = elementType;
		}
		
		public override String DisplayName {
			get {
				if (displayName == null) {
					displayName = elementType.DisplayName + "[]";
				}
				return displayName;
			}
		}
		
		public override String FullName {
			get {
				return this.Descriptor;
			}
		}
		
		public override String Descriptor {
			get {
				if (descriptor == null) {
					descriptor = "[" + elementType.Descriptor;
				}
				return descriptor;
			}
		}
		
		public override TypeInfo ElementType {
			get {
				return elementType;
			}
		}
		
		protected override int Modifiers {
			get {
				return elementType.Modifiers;
			}
		}
		
		protected override String getTypeSignature() {
			return "[" + elementType.Signature;
		}
	}

    class GenericParameterType : TypeInfo {
        ArrayList<TypeInfo> genericParameterBounds;
		private String name;
        private TypeInfo genericTypeDefinition;
        
        GenericParameterType(Library library, String name, TypeInfo genericTypeDefinition)
            : super(library, TypeKind.GenericParameter) {
            this.name = name;
            this.genericParameterBounds = new ArrayList<TypeInfo>();
            this.genericTypeDefinition = genericTypeDefinition;
        }
        
        public override Iterable<TypeInfo> GenericParameterBounds {
            get {
                return genericParameterBounds;
            }
        }
        
		public override String DisplayName {
			get {
				return this.FullName;
			}
		}
		
		public override String FullName {
			get {
				return name;
			}
		}
		
		public override String Descriptor {
			get {
				return (genericParameterBounds.isEmpty()) ? "Ljava/lang/Object;" : genericParameterBounds[0].Descriptor;
			}
		}
		
        public override TypeInfo GenericTypeDefinition {
            get {
                return genericTypeDefinition;
            }
        }
		
		protected override String getTypeSignature() {
			return "T" + name + ";";
		}
    }
    
    public abstract class ObjectTypeInfo : TypeInfo {
		private TypeInfo rawType;
        private String fullName;
        TypeInfo declaringType;
        private String packageName;
        private String name;
        private String descriptor;
        protected ArrayList<FieldInfo> fields*;
        protected ArrayList<MethodInfo> methods*;
        protected ArrayList<TypeInfo> nestedTypes*;
        
		public override string toString()
		{
			return FullName;
		}
		
    	protected ObjectTypeInfo(Library library, String fullName)
    			: super(library, TypeKind.Reference) {
			this.fullName = fullName;
			this.descriptor = "L" + fullName + ";";
            this.fields = new ArrayList<FieldInfo>();
            this.methods = new ArrayList<MethodInfo>();
            this.nestedTypes = new ArrayList<TypeInfo>();
    	}
    	
        public override TypeInfo DeclaringType {
            get {
                return declaringType;
            }
        }
		
		public override String FullName {
			get {
				return fullName;
			}
		}
        
        public override String PackageName {
            get {
                if (packageName == null) {
                    packageName = this.FullName.substring(0, this.FullName.lastIndexOf('/') + 1);
                }
                return packageName;
            }
        }

        public override String Name {
            get {
                if (name == null) {
                    int idx = this.FullName.lastIndexOf('$');
                    if (idx > 0) {
                        name = this.FullName.substring(idx + 1);
                    } else {
                        name = this.FullName.substring(this.FullName.lastIndexOf('/') + 1);
                    }
                }
                return name;
            }
        }

		public override String Descriptor {
			get {
				return descriptor;
			}
		}

		public override String DisplayName {
			get {
				return ReflectionHelper.getObjectTypeDisplayName(this);
			}
		}

        public override TypeInfo RawType {
            get {
                if (rawType != null) {
                    return rawType;
                }
                if (!this.GenericArguments.any()) {
                    throw new IllegalStateException();
                }
                if (this.DeclaringType == null && this.GenericTypeDefinition != null) {
                    return rawType = this.OriginalTypeDefinition.RawType;
                }
                return rawType = new RawTypeInfo(this.DeclaringType, this);
            }
        }

		protected override String getTypeSignature() {
			return ReflectionHelper.getObjectTypeSignature(this);
		}
    }
    
    abstract class ClassType : ObjectTypeInfo {
        protected Iterable<TypeInfo> genericArguments*;
        protected TypeInfo baseType*;
        protected Iterable<TypeInfo> interfaces*;
        
        protected ClassType(Library library, String fullName)
            : super(library, fullName) {
        }
        
        public override Iterable<TypeInfo> GenericArguments {
            get {
                if (genericArguments == null) {
                    initializeBaseTypes();
                }
                return genericArguments;
            }
        }
        
        public override TypeInfo BaseType {
            get {
                initializeBaseTypes();
                return baseType;
            }
        }
        
        public override Iterable<TypeInfo> Interfaces {
            get {
                initializeBaseTypes();
                return interfaces;
            }
        }
        
        public override Iterable<FieldInfo> Fields {
            get {
                initializeFields();
                return fields;
            }
        }
        
        public override Iterable<MethodInfo> Methods {
            get {
                initializeMethods();
                return methods;
            }
        }
        
        public override Iterable<TypeInfo> NestedTypes {
            get {
                initializeNestedTypes();
                return nestedTypes;
            }
        }
		
        protected abstract void initializeBaseTypes();
        protected abstract void initializeFields();
        protected abstract void initializeMethods();
        protected abstract void initializeNestedTypes();
    }
}
