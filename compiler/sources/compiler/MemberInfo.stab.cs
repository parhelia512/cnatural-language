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

namespace cnatural.compiler {

    public enum MemberKind {
        Field,
        Method,
        Property,
        Indexer,
        Type,
        Local
    }

    public abstract class MemberInfo {
        private class TypeMembers {
            HashMap<Object, Iterable<MemberInfo>> members = new HashMap<Object, Iterable<MemberInfo>>();
        }
    
        public static MemberInfo getInfo(TypeInfo type) {
            var result = type.getUserData(typeof(TypeMemberInfo));
            if (result == null) {
                result = new TypeMemberInfo(type);
                type.addUserData(result);
            }
            return result;
        }
    
        public static MemberInfo getInfo(FieldInfo field) {
            var result = field.getUserData(typeof(FieldMemberInfo));
            if (result == null) {
                result = new FieldMemberInfo(field);
                field.addUserData(result);
            }
            return result;
        }
    
        public static MemberInfo getInfo(MethodInfo method) {
            var result = method.getUserData(typeof(MethodMemberInfo));
            if (result == null) {
                result = new MethodMemberInfo(method);
                method.addUserData(result);
            }
            return result;
        }
    
        public static MemberInfo getInfo(MethodInfo getAccessor, MethodInfo setAccessor, String name) {
            var result = (getAccessor ?? setAccessor).getUserData(typeof(PropertyMemberInfo));
            if (result == null) {
                result = new PropertyMemberInfo(getAccessor, setAccessor, name);
                if (getAccessor != null) {
                    getAccessor.addUserData(result);
                }
                if (setAccessor != null) {
                    setAccessor.addUserData(result);
                }
            }
            return result;
        }
    
        public static MemberInfo getInfo(MethodInfo getAccessor, MethodInfo setAccessor) {
            var result = (getAccessor ?? setAccessor).getUserData(typeof(IndexerMemberInfo));
            if (result == null) {
                result = new IndexerMemberInfo(getAccessor, setAccessor);
                if (getAccessor != null) {
                    getAccessor.addUserData(result);
                }
                if (setAccessor != null) {
                    setAccessor.addUserData(result);
                }
            }
            return result;
        }

        public static MemberInfo getInfo(ParameterInfo parameter, MethodInfo method) {
            var result = parameter.getUserData(typeof(LocalMemberInfo));
            if (result == null) {
                result = new LocalMemberInfo(parameter.Name, parameter.Type, method, true);
                parameter.addUserData(result);
            }
            return result;
        }
        
        public static Iterable<MemberInfo> getMembers(Library typeSystem, TypeInfo scope, TypeInfo type,
                String name, bool useCache) {
            return getMembers(typeSystem, scope, type, name, Query.empty<TypeInfo>(), useCache);
        }
        
        public static Iterable<MemberInfo> getMembers(Library typeSystem, TypeInfo scope, TypeInfo type, String name,
                Iterable<TypeInfo> typeArguments, bool useCache) {
			try {
				var allMembers = getMembersRecursively(typeSystem, scope, type, name, typeArguments, useCache, new LookupContext());
				var members = allMembers.toList();
				return members.distinct().toList();
			} catch (IllegalArgumentException e) {
				throw new RuntimeException("Unexpected error in getMembers: \ntypeSystem " + typeSystem + 
				"\nscope " + scope + "\ntype " + type + "\nname " + name + "\ntypeArguments " + typeArguments + "\nuseCache " + useCache, e);
			}
        }
        
        private static Iterable<MemberInfo> getMembersRecursively(Library typeSystem, TypeInfo scope, TypeInfo type, String name,
                Iterable<TypeInfo> typeArguments, bool useCache, LookupContext context) {
            Iterable<MemberInfo> members;
			switch (type.TypeKind) {
			case Array:
			case LowerBoundedWildcard:
			case UnboundedWildcard:
				type = typeSystem.ObjectType;
				break;
			case UpperBoundedWildcard:
				type = type.WildcardBound;
				break;
			}
            if (type.IsGenericParameter) {
                if (type.GenericParameterBounds.any()) {
                    members = Query.empty();
                    foreach (var t in type.GenericParameterBounds) {
                        members = members.concat(getMembersRecursively(typeSystem, scope, t, name, typeArguments, useCache, context));
                    }
                } else {
                    members = getMembersRecursively(typeSystem, scope, typeSystem.ObjectType, name, typeArguments, useCache, context);
                }
            } else {
                members = getMembers(typeSystem, type, name, typeArguments, useCache, context);
                foreach (var t in type.getBaseTypes()) {
                    members = members.concat(getMembers(typeSystem, t, name, typeArguments, useCache, context));
                }
            }
            return filterMembers(typeSystem, scope, members);
        }
        
        private static Iterable<MemberInfo> getMembers(Library typeSystem, TypeInfo type, String name,
            Iterable<TypeInfo> typeArguments, bool useCache, LookupContext context) {
            var nTypeArgs = typeArguments.count();
            foreach (var mi in (useCache) ? getCachedMembers(typeSystem, type, name) : getMembers(typeSystem, type, name)) {
                switch (mi.MemberKind) {
                case Field:
                    if (nTypeArgs == 0 && context.lookupField) {
                        context.lookupField = false;
                        yield return mi;
                    }
                    break;
                    
                case Property:
                    if (nTypeArgs == 0  && context.lookupProperty) {
                        context.lookupProperty = false;
                        yield return mi;
                    }
                    break;
                    
                case Method:
                    if (nTypeArgs > 0) {
                        if (nTypeArgs == mi.Method.GenericArguments.count()) {
                            yield return MemberInfo.getInfo(typeSystem.getGenericMethod(mi.Method, typeArguments));
                        }
                    } else {
                        yield return mi;
                    }
                    break;

                case Type:
                    if (context.lookupClass) {
                        context.lookupClass = false;
                        if (nTypeArgs > 0) {
                            if (nTypeArgs == mi.Type.GenericArguments.count()) {
                                yield return mi;
                            }
                        } else {
                            if (mi.MemberKind == MemberKind.Type && mi.Type.GenericArguments.count() > 0) {
                                mi = MemberInfo.getInfo(mi.Type.RawType);
                            }
                            yield return mi;
                        }
                    }
                    break;
                }
            }
        }
        
        private class LookupContext {
            bool lookupField = true;
            bool lookupProperty = true;
            bool lookupClass = true;
        }
        
        public static Iterable<MemberInfo> getCachedMembers(Library typeSystem, TypeInfo type, String name) {
            var typeMembers = type.getUserData(typeof(TypeMembers));
            if (typeMembers == null) {
                typeMembers = new TypeMembers();
                type.addUserData(typeMembers);
            }
            var members = typeMembers.members[name];
            if (members == null) {
                members = getMembers(typeSystem, type, name);
                typeMembers.members[name] = members;
            }
            return members;
        }
        
        public static Iterable<MemberInfo> getMembers(Library typeSystem, TypeInfo type, String name) {
            var result = new ArrayList<MemberInfo>();
            
            foreach (var f in type.Fields) {
                if (f.Name.equals(name)) {
                    result.add(MemberInfo.getInfo(f));
                    break;
                }
            }

            MethodInfo getAccessor = null;
            MethodInfo setAccessor = null;
            foreach (var m in type.Methods) {
                if (getAccessor == null || setAccessor == null) {
                    foreach (var annotation in BytecodeHelper.getAnnotations(typeSystem, m)) {
                        if (BytecodeHelper.isPropertyGet(annotation) && name.equals(BytecodeHelper.getPropertyGetName(m, annotation))) {
                            getAccessor = m;
                            break;
                        }
                        if (BytecodeHelper.isPropertySet(annotation) && name.equals(BytecodeHelper.getPropertySetName(m, annotation))) {
                            setAccessor = m;
                            break;
                        }
                    }
                }
                if (m.Name.equals(name)) {
                    result.add(MemberInfo.getInfo(m));
                }
            }
            if (getAccessor != null || setAccessor != null) {
                result.add(MemberInfo.getInfo(getAccessor, setAccessor, name));
            }
            
            foreach (var t in type.NestedTypes) {
                if (name.equals(t.Name)) {
                    result.add(MemberInfo.getInfo(t));
                    break;
                }
            }
            
            return result;
        }

        public static Iterable<MemberInfo> getIndexers(Library typeSystem, TypeInfo scope, TypeInfo type,
                int indexes, bool useCache) {
            return getIndexersRecursively(typeSystem, scope, type, indexes, useCache).distinct().toList();
        }

        private static Iterable<MemberInfo> getIndexersRecursively(Library typeSystem, TypeInfo scope, TypeInfo type,
                int indexes, bool useCache) {
            Iterable<MemberInfo> members;
            if (type.IsArray) {
                members = getIndexersRecursively(typeSystem, scope, typeSystem.ObjectType, indexes, useCache);
            } else if (type.IsGenericParameter) {
                if (type.GenericParameterBounds.any()) {
                    members = Query.empty();
                    foreach (var t in type.GenericParameterBounds) {
                        members = members.concat(getIndexersRecursively(typeSystem, scope, t, indexes, useCache));
                    }
                } else {
                    members = getIndexersRecursively(typeSystem, scope, typeSystem.ObjectType, indexes, useCache);
                }
            } else {
                members = (useCache) ? getCachedIndexers(typeSystem, type, indexes) : getIndexers(typeSystem, type, indexes);
                foreach (var t in type.getBaseTypes()) {
                    members = members.concat((useCache) ? getCachedIndexers(typeSystem, t, indexes) : getIndexers(typeSystem, t, indexes));
                }
            }
            return filterMembers(typeSystem, scope, members);
        }
        
        private static Iterable<MemberInfo> filterMembers(Library typeSystem, TypeInfo scope, Iterable<MemberInfo> members) {
            HashSet<MemberInfo> exclude = null;
            foreach (var mi in members) {
                if (exclude != null && exclude.contains(mi)) {
                    continue;
                }
                if (!scope.canAccessMember(mi.DeclaringType, mi.IsPublic, mi.IsProtected, mi.IsPrivate)) {
                    continue;
                }
                if (mi.isOverridingMembers(typeSystem)) {
                    foreach (var m in mi.getOverridenMembers(typeSystem)) {
                        if (exclude == null) {
                            exclude = new HashSet<MemberInfo>();
                        }
                        exclude.add(m);
                    }
                }
                yield return mi;
            }
        }
        
        public static Iterable<MemberInfo> getCachedIndexers(Library typeSystem, TypeInfo type, int indexes) {
            var typeMembers = type.getUserData(typeof(TypeMembers));
            if (typeMembers == null) {
                typeMembers = new TypeMembers();
                type.addUserData(typeMembers);
            }
            var members = typeMembers.members[indexes];
            if (members == null) {
                members = getIndexers(typeSystem, type, indexes);
                typeMembers.members[indexes] = members;
            }
            return members;
        }
        
        public static Iterable<MemberInfo> getIndexers(Library typeSystem, TypeInfo type, int indexes) {
            List<MethodInfo> getters = null;
            List<MethodInfo> setters = null;
            foreach (var method in type.Methods) {
                foreach (var annotation in BytecodeHelper.getAnnotations(typeSystem, method)) {
                    if (BytecodeHelper.isIndexerGet(annotation)) {
                        var nparams = method.Parameters.count();
                        if (indexes == nparams || (method.IsVarargs && indexes >= nparams)) {
                            if (getters == null) {
                                getters = new ArrayList<MethodInfo>();
                            }
                            getters.add(method);
                        }
                        break;
                    }
                    if (BytecodeHelper.isIndexerSet(annotation)) {
                        var nparams = method.Parameters.count() - 1;
                        if (indexes == nparams || (method.IsVarargs && indexes >= nparams)) {
                            if (setters == null) {
                                setters = new ArrayList<MethodInfo>();
                            }
                            setters.add(method);
                        }
                        break;
                    }
                }
            }
            if (setters == null && getters == null) {
                return Query.empty();
            }
            var result = new ArrayList<MemberInfo>();
            var getters2 = (getters == null) ? Collections.emptySet<MethodInfo>() : new HashSet<MethodInfo>(getters);
            var setters2 = (setters == null) ? Collections.emptySet<MethodInfo>() : new HashSet<MethodInfo>(setters);
            if (getters == null) {
                getters = Collections.emptyList();
            }
            if (setters == null) {
                setters = Collections.emptyList();
            }
            foreach (var get in getters) {
                foreach (var set in setters) {
                    if (get.Parameters.count() == set.Parameters.count() - 1) {
                        var it1 = get.OriginalMethodDefinition.Parameters.iterator();
                        var it2 = set.OriginalMethodDefinition.Parameters.iterator();
                        var match = true;
                        while (it1.hasNext()) {
                            var t1 = it1.next().Type;
                            var t2 = it2.next().Type;
                            if (t1.IsGenericParameter) {
                                t1 = BytecodeHelper.getGenericParameterTypeErasure(typeSystem, t1);
                            }
                            if (t2.IsGenericParameter) {
                                t2 = BytecodeHelper.getGenericParameterTypeErasure(typeSystem, t2);
                            }
                            if (t1 != t2) {
                                match = false;
                                break;
                            }
                        }
                        if (match) {
                            var t1 = get.OriginalMethodDefinition.ReturnType;
                            if (t1.IsGenericParameter) {
                                t1 = BytecodeHelper.getGenericParameterTypeErasure(typeSystem, t1);
                            }
                            var t2 = it2.next().Type;
                            if (t2.IsGenericParameter) {
                                t2 = BytecodeHelper.getGenericParameterTypeErasure(typeSystem, t2);
                            }
                            if (t1 == t2) {
                                getters2.remove(get);
                                setters2.remove(set);
                                result.add(MemberInfo.getInfo(get, set));
                            }
                        }
                    }
                }
            }
            foreach (var m in getters2) {
                result.add(MemberInfo.getInfo(m, null));
            }
            foreach (var m in setters2) {
                result.add(MemberInfo.getInfo((MethodInfo)null, m));
            }
            return result;
        }
    
        protected MemberInfo(MemberKind memberKind) {
            this.MemberKind = memberKind;
        }
        
        public MemberKind MemberKind^;
        
        public abstract String Name { get; }
        
        public virtual TypeInfo DeclaringType {
            get {
                throw new IllegalStateException();
            }
        }
        
        public virtual FieldInfo Field {
            get {
                throw new IllegalStateException();
            }
        }
        
        public virtual MethodInfo Method {
            get {
                throw new IllegalStateException();
            }
        }
        
        public virtual MethodInfo GetAccessor {
            get {
                throw new IllegalStateException();
            }
        }
        
        public virtual MethodInfo SetAccessor {
            get {
                throw new IllegalStateException();
            }
        }
        
        public virtual TypeInfo Type {
            get {
                throw new IllegalStateException();
            }
        }

        public virtual bool IsParameter {
            get {
                throw new IllegalStateException();
            }
        }

        public virtual bool IsPublic {
            get {
                throw new IllegalStateException();
            }
        }

        public virtual bool IsProtected {
            get {
                throw new IllegalStateException();
            }
        }

        public virtual bool IsPrivate {
            get {
                throw new IllegalStateException();
            }
        }

        public virtual bool IsStatic {
            get {
                return true;
            }
        }
        
        public virtual bool IsUsedFromLambda {
            get {
                throw new IllegalStateException();
            }
            set {
                throw new IllegalStateException();
            }
        }
        
        public virtual bool isOverridingMembers(Library typeSystem) {
            throw new IllegalStateException();
        }
        
        public virtual Iterable<MemberInfo> getOverridenMembers(Library typeSystem) {
            throw new IllegalStateException();
        }
    }
    
    class TypeMemberInfo : MemberInfo {
        private TypeInfo type;
        private bool nested;

        TypeMemberInfo(TypeInfo type)
            : super(MemberKind.Type) {
            this.type = type;
            this.nested = type.DeclaringType != null;
        }
        
        public override String Name {
            get {
                return type.FullName;
            }
        }
        
        public override TypeInfo Type {
            get {
                return type;
            }
        }

        public override TypeInfo DeclaringType {
            get {
                return type.DeclaringType;
            }
        }
        
        public override bool isOverridingMembers(Library typeSystem) {
            return false;
        }
        
        public override bool IsPublic {
            get {
                return (nested) ? type.IsNestedPublic : type.IsPublic;
            }
        }
        
        public override bool IsProtected {
            get {
                return (nested) ? type.IsNestedProtected : false;
            }
        }
        
        public override bool IsPrivate {
            get {
                return (nested) ? type.IsNestedPrivate : false;
            }
        }
    }
    
    class FieldMemberInfo : MemberInfo {
        private FieldInfo field;
        
        FieldMemberInfo(FieldInfo field)
            : super(MemberKind.Field) {
            this.field = field;
        }
        
        public override String Name {
            get {
                return field.Name;
            }
        }
        
        public override FieldInfo Field {
            get {
                return field;
            }
        }
        
        public override TypeInfo Type {
            get {
                return field.Type;
            }
        }
        
        public override TypeInfo DeclaringType {
            get {
                return field.DeclaringType;
            }
        }
        
        public override bool isOverridingMembers(Library typeSystem) {
            return false;
        }
        
        public override bool IsPublic {
            get {
                return field.IsPublic;
            }
        }
        
        public override bool IsProtected {
            get {
                return field.IsProtected;
            }
        }
        
        public override bool IsPrivate {
            get {
                return field.IsPrivate;
            }
        }
        
        public override bool IsStatic {
            get {
                return field.IsStatic;
            }
        }
    }
    
    class MethodMemberInfo : MemberInfo {
        private MethodInfo method;
        private Set<MemberInfo> overridenMembers;
        
        MethodMemberInfo(MethodInfo method)
            : super(MemberKind.Method) {
            this.method = method;
        }
        
        public override String Name {
            get {
                return method.Name;
            }
        }

        public override MethodInfo Method {
            get {
                return method;
            }
        }
        
        public override TypeInfo DeclaringType {
            get {
                return method.DeclaringType;
            }
        }
        
        public override bool isOverridingMembers(Library typeSystem) {
            return getOverridenMembers(typeSystem).any();
        }

        public override Iterable<MemberInfo> getOverridenMembers(Library typeSystem) {
            if (overridenMembers == null) {
                if (!method.IsStatic) {
                    foreach (var t in method.DeclaringType.getBaseTypes()) {
                        foreach (var m in t.Methods) {
                            if (method.isOverriding(m)) {
                                if (overridenMembers == null) {
                                    overridenMembers = new HashSet<MemberInfo>();
                                }
                                overridenMembers.add(MemberInfo.getInfo(m));
                            }
                        }
                    }
                }
                if (overridenMembers == null) {
                    overridenMembers = Collections.emptySet<MemberInfo>();
                }
            }
            return overridenMembers;
        }
        
        public override bool IsPublic {
            get {
                return method.IsPublic;
            }
        }
        
        public override bool IsProtected {
            get {
                return method.IsProtected;
            }
        }
        
        public override bool IsPrivate {
            get {
                return method.IsPrivate;
            }
        }
        
        public override bool IsStatic {
            get {
                return method.IsStatic;
            }
        }
    }
    
    abstract class MemberWithAccessors : MemberInfo {
        private MethodInfo getAccessor;
        private MethodInfo setAccessor;
        
        MemberWithAccessors(MemberKind memberKind, MethodInfo getAccessor, MethodInfo setAccessor)
            : super(memberKind) {
            this.getAccessor = getAccessor;
            this.setAccessor = setAccessor;
        }
        
        public override MethodInfo GetAccessor {
            get {
                return getAccessor;
            }
        }
        
        public override MethodInfo SetAccessor {
            get {
                return setAccessor;
            }
        }
        
        public override TypeInfo DeclaringType {
            get {
                return (getAccessor ?? setAccessor).DeclaringType;
            }
        }
        
        public override bool IsPublic {
            get {
                if (getAccessor != null && getAccessor.IsPublic) {
                    return true;
                }
                return setAccessor != null && setAccessor.IsPublic;
            }
        }
        
        public override bool IsProtected {
            get {
                if (getAccessor != null) {
                    if (getAccessor.IsPublic) {
                        return false;
                    } else if (getAccessor.IsProtected) {
                        return setAccessor == null || !setAccessor.IsPublic;
                    }
                }
                return setAccessor != null && setAccessor.IsProtected;
            }
        }
        
        public override bool IsPrivate {
            get {
                if (getAccessor != null && !getAccessor.IsPrivate) {
                    return false;
                }
                return setAccessor == null || setAccessor.IsPrivate;
            }
        }
        
        public override bool IsStatic {
            get {
                return (getAccessor ?? setAccessor).IsStatic;
            }
        }
    }
    
    class IndexerMemberInfo : MemberWithAccessors {
        private TypeInfo type;
        private Set<MemberInfo> overridenMembers;
        
        IndexerMemberInfo(MethodInfo getAccessor, MethodInfo setAccessor)
            : super(MemberKind.Indexer, getAccessor, setAccessor) {
        }
        
        public override String Name {
            get {
                throw new IllegalStateException();
            }
        }
        
        public override TypeInfo Type {
            get {
                if (type == null) {
                    if (GetAccessor != null) {
                        type = GetAccessor.ReturnType;
                    } else {
                        type = SetAccessor.Parameters.last().Type;
                    }
                }
                return type;
            }
        }
        
        public override bool isOverridingMembers(Library typeSystem) {
            return getOverridenMembers(typeSystem).any();
        }

        public override Iterable<MemberInfo> getOverridenMembers(Library typeSystem) {
            if (overridenMembers == null) {
                if (!this.IsStatic) {
                    int nparams = (GetAccessor != null) ? GetAccessor.Parameters.count() : SetAccessor.Parameters.count() - 1;
                    foreach (var t in this.DeclaringType.getBaseTypes()) {
                        foreach (var mi in getIndexers(typeSystem, t, nparams)) {
                            if (mi.GetAccessor != null && GetAccessor != null && mi.GetAccessor.hasSameParameters(GetAccessor)) {
                                if (overridenMembers == null) {
                                    overridenMembers = new HashSet<MemberInfo>();
                                }
                                overridenMembers.add(mi);
                                break;
                            } else if (mi.SetAccessor != null && SetAccessor != null && mi.SetAccessor.hasSameParameters(SetAccessor)) {
                                if (overridenMembers == null) {
                                    overridenMembers = new HashSet<MemberInfo>();
                                }
                                overridenMembers.add(mi);
                                break;
                            }
                        }
                    }
                }
                if (overridenMembers == null) {
                    overridenMembers = Collections.emptySet<MemberInfo>();
                }
            }
            return overridenMembers;
        }
    }
    
    class PropertyMemberInfo : MemberWithAccessors {
        private String name;
        private TypeInfo type;
        private Set<MemberInfo> overridenMembers;
        
        PropertyMemberInfo(MethodInfo getAccessor, MethodInfo setAccessor, String name)
            : super(MemberKind.Property, getAccessor, setAccessor) {
            this.name = name;
        }
        
        public override String Name {
            get {
                return name;
            }
        }
        
        public override TypeInfo Type {
            get {
                if (type == null) {
                    if (GetAccessor != null) {
                        type = GetAccessor.ReturnType;
                    } else {
                        type = SetAccessor.Parameters.single().Type;
                    }
                }
                return type;
            }
        }
        
        public override bool isOverridingMembers(Library typeSystem) {
            return getOverridenMembers(typeSystem).any();
        }

        public override Iterable<MemberInfo> getOverridenMembers(Library typeSystem) {
            if (overridenMembers == null) {
                if (!this.IsStatic) {
                    foreach (var t in this.DeclaringType.getBaseTypes()) {
                        MethodInfo get = null;
                        MethodInfo set = null;
                        foreach (var m in t.Methods) {
                            var pinfo = m.getUserData(typeof(PropertyMemberInfo));
                            if (pinfo != null) {
                                if (!pinfo.Name.equals(name) || pinfo.IsStatic) {
                                    continue;
                                }
                                if (!pinfo.IsPrivate) {
                                    if (overridenMembers == null) {
                                        overridenMembers = new HashSet<MemberInfo>();
                                    }
                                    overridenMembers.add(pinfo);
                                }
                                break;
                            }
                            foreach (var annotation in BytecodeHelper.getAnnotations(typeSystem, m)) {
                                if (BytecodeHelper.isPropertyGet(annotation) && name.equals(BytecodeHelper.getPropertyGetName(m, annotation))) {
                                    get = m;
                                    break;
                                }
                                if (BytecodeHelper.isPropertySet(annotation) && name.equals(BytecodeHelper.getPropertySetName(m, annotation))) {
                                    set = m;
                                    break;
                                }
                            }
                        }
                        if (get != null || set != null) {
                            if (overridenMembers == null) {
                                overridenMembers = new HashSet<MemberInfo>();
                            }
                            overridenMembers.add(MemberInfo.getInfo(get, set, name));
                        }
                    }
                }
                if (overridenMembers == null) {
                    overridenMembers = Collections.emptySet<MemberInfo>();
                }
            }
            return overridenMembers;
        }
    }
    
    class LocalMemberInfo : MemberInfo {
        private String name;
        private TypeInfo type;
        private MethodInfo method;
        private bool parameter;
        private bool usedFromLambda;
    
        LocalMemberInfo(String name, TypeInfo type, MethodInfo method, bool parameter)
            : super(MemberKind.Local) {
            this.name = name;
            this.type = type;
            this.method = method;
            this.parameter = parameter;
        }
        
        public override String Name {
            get {
                return name;
            }
        }
        
        public override TypeInfo Type {
            get {
                return type;
            }
        }
        
        public override MethodInfo Method {
            get {
                return method;
            }
        }
        
        public override bool IsParameter {
            get {
                return parameter;
            }
        }
        
        public override bool IsUsedFromLambda {
            get {
                return usedFromLambda;
            }
            set {
                usedFromLambda = value;
            }
        }
		
		public bool Unused;
    }
}
