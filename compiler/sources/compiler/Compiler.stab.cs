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
using java.io;
using java.lang;
using java.lang.annotation;
using java.nio.charset;
using java.util;
using stab.query;
using stab.reflection;
using cnatural.helpers;
using cnatural.parser;
using cnatural.syntaxtree;

namespace cnatural.compiler {

	public class C♮ {
		public string Version = "1.1";
	}

    public class CompilerParameters {
        public CompilerParameters() {
            this.ClassPath = new ArrayList<String>();
            this.Symbols = new ArrayList<String>();
            this.AnnotatedLibraryPaths = new ArrayList<String>();
			this.GenerateClassFiles = true;
        }

        public List<String> ClassPath^;
        public List<String> Symbols^;
        public List<String> AnnotatedLibraryPaths^;
        public String DocumentationPath;
		public Library TypeSystem;
		public Library AnnotatedTypeSystem;
		public bool GenerateClassFiles;
		public ICompilationProgressTracker ProgressTracker;
    }

	public interface ICompilationProgressTracker {
		void compilationStageFinished(CompilationStage stage);
		void compilationFinished();
	}
	
	public enum CompilationStage {
		None,
		Parsing,
		TypesDefinition,
		SuperTypesDefinition,
		MembersDefinition,
		AnnotationsDefinition,
		CodeValidation,
		Success
	}
	
    public class CompilerResults {
        CodeErrorManager codeErrorManager;
        HashMap<String, byte[]> classFiles;
		CompilationStage compilationStage;
    
        public CompilerResults() {
            this.codeErrorManager = new CodeErrorManager();
            this.classFiles = new HashMap<String, byte[]>();
			this.compilationStage = CompilationStage.None;
        }
        
		public CompilationStage CompilationStage {
			get {
				return compilationStage;
			}
		}
		
        public Iterable<CodeError> Errors {
            get {
                return codeErrorManager.Errors;
            }
        }
        
        public HashMap<String, byte[]> ClassFiles {
            get {
                return classFiles;
            }
        }
    }

    public class PackageInfo {
        HashSet<String> packages = new HashSet<String>();
        HashMap<String, MemberInfo> memberInfos = new HashMap<String, MemberInfo>();
        HashMap<String, Iterable<String>> packageAliases = new HashMap<String, Iterable<String>>();
        
        public Set<String> UsedPackages {
        	get {
        		return packages;
        	}
        }
        
        public Map<String, MemberInfo> UsedTypes {
        	get {
        		return memberInfos;
        	}
        }
        
        public Map<String, Iterable<String>> PackageAliases {
        	get {
        		return packageAliases;
        	}
        }
    }
    
    public class Compiler {
        private CompilerContext context;
        private StatementValidator statementValidator;
        private ExpressionValidator expressionValidator;
        private ReachabilityChecker reachabilityChecker;
        private AssignmentChecker assignmentChecker;
        private BytecodeGenerator bytecodeGenerator;
        private HashMap<String, PartialTypeInfo> partialTypes;
        private QueryTranslator queryTranslator;
        private DocumentationBuilder documentationBuilder;
        
        public Compiler() {
            this.partialTypes = new HashMap<String, PartialTypeInfo>();
        }
        
        public CompilerResults compileFromFiles(CompilerParameters parameters, File[] files) {
            var results = new CompilerResults();
            this.context = new CompilerContext(parameters, results);
            this.statementValidator = new StatementValidator(this.context);
            this.expressionValidator = new ExpressionValidator(this.context);
            this.statementValidator.ExpressionValidator = this.expressionValidator;
            this.expressionValidator.StatementValidator = this.statementValidator;
            this.reachabilityChecker = new ReachabilityChecker(context);
            this.assignmentChecker = new AssignmentChecker(context);
            this.bytecodeGenerator = new BytecodeGenerator(context);
            bool tragicError = false;
			
            var buffer = new char[4096];
            var sb = new StringBuilder();
            var parser = new Parser();
            
            foreach (var file in files) {
                sb.setLength(0);
                InputStreamReader reader = null;
                try {
                    reader = new InputStreamReader(new FileInputStream(file), Charset.forName("UTF-8"));
                    int read;
                    while ((read = reader.read(buffer)) != -1) {
                        sb.append(buffer, 0, read);
                    }
                    
                    var text = new char[sb.length()];
                    sb.getChars(0, sizeof(text), text, 0);
                    if (sizeof(text) > 0) {
                        if (text[sizeof(text) - 1] == '\u001a') {
                            text[sizeof(text) - 1] = ' ';
                        }
                    }
                    var preprocessor = new Preprocessor(results.codeErrorManager, text);
                    preprocessor.Filename = file.getAbsolutePath();
					preprocessor.Symbols.addAll(parameters.Symbols);
                    
                    var scanner = new PreprocessedTextScanner(results.codeErrorManager, preprocessor.preprocess());
                    scanner.Filename = file.getAbsolutePath();
                    var compilationUnit = parser.parseCompilationUnit(scanner);
                    
                    if (compilationUnit != null) {
                        compilationUnit.Symbols = preprocessor.Symbols;
                        context.CompilationUnits.add(compilationUnit);
                    }
                } catch (CodeErrorException) {
				} catch (Exception e) {
					e.printStackTrace();
					tragicError = true;
					break;
                } finally {
                    if (reader != null) {
                        try {
                            reader.close();
                        } catch (IOException) {
                        }
                    }
                }
            }
            if (!tragicError) {
				if (!context.HasErrors) {
					if (parameters.ProgressTracker != null) {
						parameters.ProgressTracker.compilationStageFinished(CompilationStage.Parsing);
					}
					doCompile();
				}
			}
            this.context = null;
            this.statementValidator = null;
            this.expressionValidator = null;
            this.reachabilityChecker = null;
            this.assignmentChecker = null;
            this.queryTranslator = null;
            this.documentationBuilder = null;
			
			if (parameters.ProgressTracker != null) {
				parameters.ProgressTracker.compilationFinished();
			}
            return results;
        }

        public CompilerResults compileFromCompilationUnits(CompilerParameters parameters, CompilationUnitNode[] compilationUnits) {
            var results = new CompilerResults();
            this.context = new CompilerContext(parameters, results);
            this.statementValidator = new StatementValidator(this.context);
            this.expressionValidator = new ExpressionValidator(this.context);
            this.statementValidator.ExpressionValidator = this.expressionValidator;
            this.expressionValidator.StatementValidator = this.statementValidator;
            this.reachabilityChecker = new ReachabilityChecker(context);
            this.assignmentChecker = new AssignmentChecker(context);
            this.bytecodeGenerator = new BytecodeGenerator(context);
            
			foreach (var cu in compilationUnits) {
				context.CompilationUnits.add(cu);
			}
			doCompile();
            
            this.context = null;
            this.statementValidator = null;
            this.expressionValidator = null;
            this.reachabilityChecker = null;
            this.assignmentChecker = null;
            this.queryTranslator = null;
            this.documentationBuilder = null;
			
			if (parameters.ProgressTracker != null) {
				parameters.ProgressTracker.compilationFinished();
			}
            return results;
        }
        
        private void doCompile() {
			if (context.Parameters.TypeSystem == null) {
				int nPaths = context.Parameters.ClassPath.size();
				var classPath = context.Parameters.ClassPath.toArray(new String[nPaths]);
				context.TypeSystem = new Library(classPath);
				
				nPaths = context.Parameters.AnnotatedLibraryPaths.size();
				context.AnnotatedTypeSystem = new Library(context.Parameters.AnnotatedLibraryPaths.toArray(new String[nPaths]),
						context.TypeSystem);
			} else {
				context.TypeSystem = context.Parameters.TypeSystem;
				if (context.Parameters.AnnotatedTypeSystem != null) {
					context.AnnotatedTypeSystem = context.Parameters.AnnotatedTypeSystem;
				} else {
					context.AnnotatedTypeSystem = context.Parameters.TypeSystem;
				}
			}
            
			// Define all types
			defineTypes();
			if (context.HasErrors) {
				context.Results.compilationStage = CompilationStage.Parsing;
				return;
			}
			if (context.Parameters.ProgressTracker != null) {
				context.Parameters.ProgressTracker.compilationStageFinished(CompilationStage.TypesDefinition);
			}
			
			// Set the base types
			setBaseTypes();
			if (context.HasErrors) {
				context.Results.compilationStage = CompilationStage.TypesDefinition;
				return;
			}
			if (context.Parameters.ProgressTracker != null) {
				context.Parameters.ProgressTracker.compilationStageFinished(CompilationStage.SuperTypesDefinition);
			}
			
			// Define all members
			defineMembers();
			if (context.HasErrors) {
				context.Results.compilationStage = CompilationStage.SuperTypesDefinition;
				return;
			}
			if (context.Parameters.ProgressTracker != null) {
				context.Parameters.ProgressTracker.compilationStageFinished(CompilationStage.MembersDefinition);
			}

			// Annotations generation
			generateAnnotations();
			if (context.HasErrors) {
				context.Results.compilationStage = CompilationStage.MembersDefinition;
				return;
			}
			if (context.Parameters.ProgressTracker != null) {
				context.Parameters.ProgressTracker.compilationStageFinished(CompilationStage.AnnotationsDefinition);
			}
			
			// Code validation
			validateCode();
			if (context.HasErrors) {
				context.Results.compilationStage = CompilationStage.AnnotationsDefinition;
				return;
			}
			if (context.Parameters.ProgressTracker != null) {
				context.Parameters.ProgressTracker.compilationStageFinished(CompilationStage.CodeValidation);
			}

			if (context.Parameters.GenerateClassFiles) {
				// Bytecode generation
				generateBytecode();
				if (context.HasErrors) {
					context.Results.compilationStage = CompilationStage.CodeValidation;
					return;
				}
			
				// Class files creation
				foreach (var typeBuilder in context.TypeBuilders) {
					if (!typeBuilder.IsCreated) {
						context.Results.classFiles[typeBuilder.FullName.replace('/', '.')] = typeBuilder.createType(context.TypeSystem);
					}
				}
			}
			
			// Documentation file
			if (context.Parameters.DocumentationPath != null) {
				buildDocumentation(new File(context.Parameters.DocumentationPath));
			}
			if (context.Parameters.ProgressTracker != null) {
				context.Parameters.ProgressTracker.compilationStageFinished(CompilationStage.Success);
			}
			context.Results.compilationStage = CompilationStage.Success;
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        // Type definitions
        //
        
        private void defineTypes() {
            foreach (var compilationUnit in context.CompilationUnits) {
                context.Text = compilationUnit.Text;
                try {
                    defineTypes("", compilationUnit.Body);
                } catch (CodeErrorException) {
                }
            }
            
            context.MemberResolver = new MemberResolver(context.TypeSystem, context.AnnotatedTypeSystem);
        }
        
        private void defineTypes(String packageName, PackageBodyNode packageBody) {
            foreach (var member in packageBody.Members) {
                switch (member.PackageMemberKind) {
                case Package:
                    defineTypes(packageName, (PackageDeclarationNode)member);
                    break;
                case Class:
                    defineClass(packageName, (ClassDeclarationNode)member);
                    break;
                case Interface:
                    defineInterface(packageName, (InterfaceDeclarationNode)member);
                    break;
                case Delegate:
                    defineDelegate(packageName, (DelegateDeclarationNode)member);
                    break;
                default:
                    throw new Exception("Internal error: unhandled member kind: " + member.PackageMemberKind);
                }
            }
        }
        
        private void defineTypes(String packageName, PackageDeclarationNode packageDeclaration) {
            if (packageDeclaration.Body != null) {
                defineTypes(getPackageName(packageName, packageDeclaration, '/'), packageDeclaration.Body);
            }
        }

        private String getPackageName(String outerPackage, PackageDeclarationNode packageDeclaration, char separator) {
            var sb = new StringBuilder();
            var first = true;
            if (!Helper.isNullOrEmpty(outerPackage)) {
                sb.append(outerPackage);
                first = false;
            }
            foreach (var identifier in packageDeclaration.Identifiers) {
                if (first) {
                    first = false;
                } else {
                    sb.append(separator);
                }
                var id = context.getIdentifier(identifier.Offset, identifier.Length);
                sb.append(id);
                identifier.addUserData(id);
            }
            return sb.toString();
        }
        
        private void defineClass(String packageName, ClassDeclarationNode classDeclaration) {
            var className = getTypeName(packageName, classDeclaration.NameOffset, classDeclaration.NameLength);
            TypeBuilder typeBuilder = null;
            if (classDeclaration.IsPartial) {
                var partialTypeInfo = this.partialTypes[className];
                if (partialTypeInfo != null) {
                    setClassModifiers(classDeclaration, partialTypeInfo.typeBuilder);
                    typeBuilder = partialTypeInfo.typeBuilder;
                }
            }
            if (typeBuilder == null) {
                typeBuilder = defineType(className, classDeclaration);
                setClassModifiers(classDeclaration, typeBuilder);
                if (classDeclaration.IsPartial) {
                    this.partialTypes[className] = new PartialTypeInfo(typeBuilder);
                }
            }
            setTypeParameters(typeBuilder, classDeclaration.TypeParameters, classDeclaration);
            classDeclaration.addUserData(typeBuilder);
            typeBuilder.addUserData(classDeclaration);
            typeBuilder.setSuper(true);
            defineNestedTypes(typeBuilder, classDeclaration);
        }

        private void defineNestedClass(TypeBuilder declaringClass, ClassDeclarationNode classDeclaration) {
            var shortName = context.getIdentifier(classDeclaration.NameOffset, classDeclaration.NameLength);
            var className = declaringClass.FullName + '$' + shortName;
            TypeBuilder typeBuilder = null;
            if (classDeclaration.IsPartial) {
                var partialTypeInfo = this.partialTypes[className];
                if (partialTypeInfo != null) {
                    setNestedClassModifiers(classDeclaration, partialTypeInfo.typeBuilder);
                    typeBuilder = partialTypeInfo.typeBuilder;
                }
            }
            if (typeBuilder == null) {
                typeBuilder = defineNestedType(declaringClass, className, shortName, classDeclaration);
                setNestedClassModifiers(classDeclaration, typeBuilder);
                if (classDeclaration.IsPartial) {
                    this.partialTypes[className] = new PartialTypeInfo(typeBuilder);
                }
            }
            setTypeParameters(typeBuilder, classDeclaration.TypeParameters, classDeclaration);
            classDeclaration.addUserData(typeBuilder);
            typeBuilder.addUserData(classDeclaration);
            typeBuilder.setSuper(true);
            typeBuilder.setNestedStatic(true);
            defineNestedTypes(typeBuilder, classDeclaration);
        }
        
        private void defineNestedTypes(TypeBuilder declaringClass, ClassDeclarationNode classDeclaration) {
            foreach (var member in classDeclaration.Members) {
                switch (member.TypeMemberKind) {
                case Class:
                    defineNestedClass(declaringClass, (ClassDeclarationNode)member);
                    break;
                case Interface:
                    defineNestedInterface(declaringClass, (InterfaceDeclarationNode)member);
                    break;
                case Delegate:
                    defineNestedDelegate(declaringClass, (DelegateDeclarationNode)member);
                    break;
                case Constructor:
                case Destructor:
                case EnumConstant:
                case Field:
                case Indexer:
                case Method:
                case Property:
                    break;
                default:
                    throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                }
            }
        }
        
        private void defineInterface(String packageName, InterfaceDeclarationNode interfaceDeclaration) {
            var className = getTypeName(packageName, interfaceDeclaration.NameOffset, interfaceDeclaration.NameLength);
            TypeBuilder typeBuilder = null;
            if (interfaceDeclaration.IsPartial) {
                var partialTypeInfo = this.partialTypes[className];
                if (partialTypeInfo != null) {
                    setInterfaceModifiers(interfaceDeclaration, partialTypeInfo.typeBuilder);
                    typeBuilder = partialTypeInfo.typeBuilder;
                }
            }
            if (typeBuilder == null) {
                typeBuilder = defineType(className, interfaceDeclaration);
                setInterfaceModifiers(interfaceDeclaration, typeBuilder);
                if (interfaceDeclaration.IsPartial) {
                    this.partialTypes[className] = new PartialTypeInfo(typeBuilder);
                }
            }
            setTypeParameters(typeBuilder, interfaceDeclaration.TypeParameters, interfaceDeclaration);
            interfaceDeclaration.addUserData(typeBuilder);
            typeBuilder.setInterface(true);
            typeBuilder.setAbstract(true);
        }
        
        private void defineNestedInterface(TypeBuilder declaringClass, InterfaceDeclarationNode interfaceDeclaration) {
            var shortName = context.getIdentifier(interfaceDeclaration.NameOffset, interfaceDeclaration.NameLength);
            var className = declaringClass.FullName + '$' + shortName;
            TypeBuilder typeBuilder = null;
            if (interfaceDeclaration.IsPartial) {
                var partialTypeInfo = this.partialTypes[className];
                if (partialTypeInfo != null) {
                    setNestedInterfaceModifiers(interfaceDeclaration, partialTypeInfo.typeBuilder);
                    typeBuilder = partialTypeInfo.typeBuilder;
                }
            }
            if (typeBuilder == null) {
                typeBuilder = defineNestedType(declaringClass, className, shortName, interfaceDeclaration);
                setNestedInterfaceModifiers(interfaceDeclaration, typeBuilder);
                if (interfaceDeclaration.IsPartial) {
                    this.partialTypes[className] = new PartialTypeInfo(typeBuilder);
                }
            }
            setTypeParameters(typeBuilder, interfaceDeclaration.TypeParameters, interfaceDeclaration);
            interfaceDeclaration.addUserData(typeBuilder);
            typeBuilder.setInterface(true);
            typeBuilder.setAbstract(true);
        }
        
        private void defineDelegate(String packageName, DelegateDeclarationNode delegateDeclaration) {
            var className = getTypeName(packageName, delegateDeclaration.NameOffset, delegateDeclaration.NameLength);
            var typeBuilder = defineType(className, delegateDeclaration);
            typeBuilder.setAbstract(true);
            delegateDeclaration.addUserData(typeBuilder);
            setDelegateModifiers(delegateDeclaration, typeBuilder);
        }
        
        private void defineNestedDelegate(TypeBuilder declaringClass, DelegateDeclarationNode delegateDeclaration) {
            var shortName = context.getIdentifier(delegateDeclaration.NameOffset, delegateDeclaration.NameLength);
            var className = declaringClass.FullName + '$' + shortName;
            var typeBuilder = defineNestedType(declaringClass, className, shortName, delegateDeclaration);
            typeBuilder.setAbstract(true);
            delegateDeclaration.addUserData(typeBuilder);
            setNestedDelegateModifiers(delegateDeclaration, typeBuilder);
        }

        private String getTypeName(String packageName, int offset, int length) {
            var sb = new StringBuilder();
            if (!Helper.isNullOrEmpty(packageName)) {
                sb.append(packageName).append('/');
            }
            context.appendIdentifier(sb, offset, length);
            return sb.toString();
        }
        
        private void setTypeParameters(TypeBuilder typeBuilder, List<SimpleNameTypeReferenceNode> typeParameters, SyntaxNode node) {
            if (typeParameters.size() > 0) {
                var genericParams = getTypeParameterNames(typeParameters);
                int i = 0;
                if (typeBuilder.GenericArguments.any()) {
                    var it1 = genericParams.iterator();
                    var it2 = typeBuilder.GenericArguments.iterator();
                    while (it1.hasNext() && it2.hasNext()) {
                        if (!it1.next().equals(it2.next().FullName)) {
                            throw context.error(CompileErrorId.PartialTypeArgumentsMismatch, node,
                                BytecodeHelper.getDisplayName(typeBuilder));
                        }
                        i++;
                    }
                    if (it2.hasNext()) {
                        throw context.error(CompileErrorId.PartialTypeArgumentsMismatch, node,
                            BytecodeHelper.getDisplayName(typeBuilder));
                    }
                }
                for (; i < genericParams.size(); i++) {
                    typeBuilder.addGenericArgument(genericParams[i]);
                }
            }
        }
        
        private void setTypeParameters(MethodBuilder methodBuilder, List<SimpleNameTypeReferenceNode> typeParameters, SyntaxNode node) {
            if (typeParameters.size() > 0) {
                var genericParams = getTypeParameterNames(typeParameters);
                int i = 0;
                if (methodBuilder.GenericArguments.any()) {
                    var it1 = genericParams.iterator();
                    var it2 = methodBuilder.GenericArguments.iterator();
                    while (it1.hasNext() && it2.hasNext()) {
                        if (!it1.next().equals(it2.next().FullName)) {
                            throw context.error(CompileErrorId.PartialTypeArgumentsMismatch, node,
                                BytecodeHelper.getDisplayName(methodBuilder.DeclaringType) + '.' + methodBuilder.Name);
                        }
                        i++;
                    }
                    if (it2.hasNext()) {
                        throw context.error(CompileErrorId.PartialTypeArgumentsMismatch, node,
                            BytecodeHelper.getDisplayName(methodBuilder.DeclaringType) + '.' + methodBuilder.Name);
                    }
                }
                for (; i < genericParams.size(); i++) {
                    methodBuilder.addGenericArgument(genericParams[i]);
                }
            }
        }
        
        private List<String> getTypeParameterNames(List<SimpleNameTypeReferenceNode> typeParameters) {
            if (typeParameters.size() > 0) {
                var result = new ArrayList<String>();
                foreach (var t in typeParameters) {
                    var name = context.getIdentifier(t.NameOffset, t.NameLength);
                    result.add(name);
                }
                return result;
            }
            return Collections.emptyList();
        }

        private TypeBuilder defineType(String name, SyntaxNode node) {
			if (context.TypeSystem.typeExists(name)) {
				throw context.error(CompileErrorId.AlreadyDefinedType, node, name.replace('/', '.').replace('$', '.'));
			}
			var result = context.TypeSystem.defineType(name);
			context.TypeBuilders.add(result);
			return result;
        }

        private TypeBuilder defineNestedType(TypeBuilder declaringClass, String className, String shortName, SyntaxNode node) {
            if (context.TypeSystem.typeExists(className)) {
                throw context.error(CompileErrorId.AlreadyDefinedType, node, className.replace('/', '.').replace('$', '.'));
            }
            var result = declaringClass.defineNestedType(shortName);
            context.TypeBuilders.add(result);
            return result;
        }

        private void setClassModifiers(ClassDeclarationNode classDeclaration, TypeBuilder typeBuilder) {
            foreach (var modifier in classDeclaration.Modifiers) {
                switch (modifier) {
                case Public:
                    typeBuilder.setPublic(true);
                    break;
                    
                case Final:
                case Static:
                    if (typeBuilder.IsAbstract || typeBuilder.IsFinal) {
                        context.addError(CompileErrorId.FinalAbstractStaticClass, classDeclaration);
                    } else {
                        typeBuilder.setFinal(true);
                    }
                    break;
                    
                case Abstract:
                    if (typeBuilder.IsFinal) {
                        context.addError(CompileErrorId.FinalAbstractStaticClass, classDeclaration);
                    } else {
                        typeBuilder.setAbstract(true);
                    }
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, classDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
        }
        
        private void setNestedClassModifiers(ClassDeclarationNode classDeclaration, TypeBuilder typeBuilder) {
            foreach (var modifier in classDeclaration.Modifiers) {
                switch (modifier) {
                case Public:
                    if (typeBuilder.IsNestedPrivate || typeBuilder.IsNestedProtected) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, classDeclaration);
                    } else {
                        typeBuilder.setPublic(true);
                        typeBuilder.setNestedPublic(true);
                    }
                    break;
                    
                case Protected:
                    if (typeBuilder.IsNestedPrivate || typeBuilder.IsNestedPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, classDeclaration);
                    } else {
                        typeBuilder.setNestedProtected(true);
                    }
                    break;
                    
                case Private:
                    if (typeBuilder.IsNestedProtected || typeBuilder.IsNestedPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, classDeclaration);
                    } else {
                        typeBuilder.setNestedPrivate(true);
                    }
                    break;
                    
                case Final:
                case Static:
                    if (typeBuilder.IsAbstract || typeBuilder.IsFinal) {
                        context.addError(CompileErrorId.FinalAbstractStaticClass, classDeclaration);
                    } else {
                        typeBuilder.setFinal(true);
                        typeBuilder.setNestedFinal(true);
                    }
                    break;
                    
                case Abstract:
                    if (typeBuilder.IsFinal) {
                        context.addError(CompileErrorId.FinalAbstractStaticClass, classDeclaration);
                    } else {
                        typeBuilder.setAbstract(true);
                        typeBuilder.setNestedAbstract(true);
                    }
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, classDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
        }
        
        private void setInterfaceModifiers(InterfaceDeclarationNode interfaceDeclaration, TypeBuilder typeBuilder) {
            foreach (var modifier in interfaceDeclaration.Modifiers) {
                switch (modifier) {
                case Public:
                    typeBuilder.setPublic(true);
                    break;
                    
                case Abstract:
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, interfaceDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
        }

        private void setNestedInterfaceModifiers(InterfaceDeclarationNode interfaceDeclaration, TypeBuilder typeBuilder) {
            foreach (var modifier in interfaceDeclaration.Modifiers) {
                switch (modifier) {
                case Public:
                    if (typeBuilder.IsNestedPrivate || typeBuilder.IsNestedProtected) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, interfaceDeclaration);
                    } else {
                        typeBuilder.setPublic(true);
                        typeBuilder.setNestedPublic(true);
                    }
                    break;
                    
                case Protected:
                    if (typeBuilder.IsNestedPrivate || typeBuilder.IsNestedPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, interfaceDeclaration);
                    } else {
                        typeBuilder.setNestedProtected(true);
                    }
                    break;
                    
                case Private:
                    if (typeBuilder.IsNestedProtected || typeBuilder.IsNestedPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, interfaceDeclaration);
                    } else {
                        typeBuilder.setNestedPrivate(true);
                    }
                    break;
                    
                case Abstract:
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, interfaceDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
        }

        private void setDelegateModifiers(DelegateDeclarationNode delegateDeclaration, TypeBuilder typeBuilder) {
            foreach (var modifier in delegateDeclaration.Modifiers) {
                switch (modifier) {
                case Public:
                    typeBuilder.setPublic(true);
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, delegateDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
        }
        
        private void setNestedDelegateModifiers(DelegateDeclarationNode delegateDeclaration, TypeBuilder typeBuilder) {
            foreach (var modifier in delegateDeclaration.Modifiers) {
                switch (modifier) {
                case Public:
                    if (typeBuilder.IsNestedPrivate || typeBuilder.IsNestedProtected) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, delegateDeclaration);
                    } else {
                        typeBuilder.setPublic(true);
                        typeBuilder.setNestedPublic(true);
                    }
                    break;
                    
                case Protected:
                    if (typeBuilder.IsNestedPrivate || typeBuilder.IsNestedPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, delegateDeclaration);
                    } else {
                        typeBuilder.setNestedProtected(true);
                    }
                    break;
                    
                case Private:
                    if (typeBuilder.IsNestedProtected || typeBuilder.IsNestedPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, delegateDeclaration);
                    } else {
                        typeBuilder.setNestedPrivate(true);
                    }
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, delegateDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        // Base types
        //

        private void setBaseTypes() {
            try {
                context.MemberResolver.initialize();
                foreach (var compilationUnit in context.CompilationUnits) {
                    context.Text = compilationUnit.Text;
                    context.MemberResolver.enterScope();
                    try {
                        setBaseTypes(compilationUnit.Body);
                    } catch (CodeErrorException) {
                    } finally {
                        context.MemberResolver.leaveScope();
                    }
                }
            } finally {
                context.MemberResolver.dispose();
            }
        }

        private void setBaseTypes(PackageBodyNode packageBody) {
            var packageName = context.MemberResolver.getCurrentPackage();
            var packageInfo = new PackageInfo();
            packageBody.addUserData(packageInfo);
            foreach (var usingDirective in packageBody.UsingDirectives) {
                if (usingDirective.AliasLength != 0) {
                    var name = context.getIdentifier(usingDirective.AliasOffset, usingDirective.AliasLength);
                    if (packageInfo.memberInfos.containsKey(name) || packageInfo.packageAliases.containsKey(name)) {
                        context.addWarning(CompileErrorId.MultipleUsingDirective, usingDirective, name);
                    }
                    var typeInfo = CompilerHelper.resolveTypeReference(context, packageName, usingDirective.TypeOrPackage, false, true);
                    if (typeInfo != null) {
                        packageInfo.memberInfos[name] = MemberInfo.getInfo(typeInfo);
                    } else {
                        var pname = CompilerHelper.getName(context, usingDirective.TypeOrPackage);
                        if (context.MemberResolver.TypeFinder.packageExists(pname)) {
                            packageInfo.packageAliases[name] = pname;
                        } else {
                            context.addError(CompileErrorId.UnresolvedTypeName, usingDirective,
                                MemberResolver.getPackageName(pname).replace('/', '.'));
                        }
                    }
                } else {
                    var pname = CompilerHelper.getFullName(context, usingDirective.TypeOrPackage);
                    if (packageInfo.packages.contains(pname)) {
                        context.addWarning(CompileErrorId.MultipleUsingDirective, usingDirective, pname.replace('/', '.'));
                    } else {
                        packageInfo.packages.add(pname);
                    }
                }
            }
            context.MemberResolver.usingDirective(packageInfo.memberInfos, packageInfo.packages, packageInfo.packageAliases);
            
            foreach (var member in packageBody.Members) {
                switch (member.PackageMemberKind) {
                case Package:
                    setBaseTypes((PackageDeclarationNode)member);
                    break;
                case Class:
                    setClassBaseTypes((ClassDeclarationNode)member);
                    break;
                case Interface:
                    setInterfaceBaseTypes((InterfaceDeclarationNode)member);
                    break;
                case Delegate:
                    setDelegateBaseTypes((DelegateDeclarationNode)member);
                    break;
                default:
                    throw new Exception("Internal error: unhandled member kind: " + member.PackageMemberKind);
                }
            }
        }

        private void setBaseTypes(PackageDeclarationNode packageDeclaration) {
            if (packageDeclaration.Body != null) {
                foreach (var identifier in packageDeclaration.Identifiers) {
                    context.MemberResolver.enterPackage(context.getIdentifier(identifier.Offset, identifier.Length));
                }
                try {
                    setBaseTypes(packageDeclaration.Body);
                } finally {
                    for (int i = packageDeclaration.Identifiers.size() - 1; i >= 0; --i) {
                        context.MemberResolver.leavePackage();
                    }
                }
            }
        }

        private void setClassBaseTypes(ClassDeclarationNode classDeclaration) {
            var typeBuilder = classDeclaration.getUserData(typeof(TypeBuilder));
            context.MemberResolver.enterType(typeBuilder);
            try {
                var first = true;
                foreach (var typeReference in classDeclaration.ClassBase) {
                    var type = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, typeReference);
                    if (first) {
                        first = false;
                        if (type.IsInterface) {
                            typeBuilder.addInterface(type);
                        } else {
                            if (type.IsGenericParameter) {
                                context.addError(CompileErrorId.DeriveFromTypeVariable, typeReference, BytecodeHelper.getDisplayName(type));
                                continue;
                            }
                            if (typeBuilder.BaseType != null && typeBuilder.BaseType != type) {
                                context.addError(CompileErrorId.DifferentPartialBaseClass,
                                    typeReference, BytecodeHelper.getDisplayName(typeBuilder));
                                continue;
                            }
                            if (type.IsFinal) {
                                context.addError(CompileErrorId.FinalBaseClass, typeReference,
                                    BytecodeHelper.getDisplayName(typeBuilder), BytecodeHelper.getDisplayName(type));
                                continue;
                            }
                            if ((typeBuilder.IsPublic && !type.IsPublic) ||
                            	(!typeBuilder.IsNestedPrivate && type.IsNestedPrivate)) {
                                context.addError(CompileErrorId.InconsistentBaseTypeAccessibility, typeReference,
                                    BytecodeHelper.getDisplayName(typeBuilder), BytecodeHelper.getDisplayName(type));
                            }
                            typeBuilder.setBaseType(type);
                        }
                    } else {
                        if (!type.IsInterface) {
							if (typeBuilder.BaseType != null) {
								context.addError(CompileErrorId.MultipleBaseClass, typeReference,
									BytecodeHelper.getDisplayName(typeBuilder),
									BytecodeHelper.getDisplayName(type),
									BytecodeHelper.getDisplayName(typeBuilder.BaseType));
							} else {
								context.addError(CompileErrorId.BaseClassBeforeInterfaces, typeReference,
									BytecodeHelper.getDisplayName(type));
							}
                        }
                        typeBuilder.addInterface(type);
                    }
                }
                if (typeBuilder.BaseType == null) {
                    if (classDeclaration.IsEnum) {
                        typeBuilder.setEnum(true);
                        var enumType = context.TypeSystem.getType("java/lang/Enum");
                        typeBuilder.setBaseType(context.TypeSystem.getGenericType(enumType, Collections.singletonList<TypeInfo>(typeBuilder)));
                    } else {
                        typeBuilder.setBaseType(context.TypeSystem.ObjectType);
                    }
                }
                
                foreach (var member in classDeclaration.Members) {
                    switch (member.TypeMemberKind) {
                    case Class:
                        setClassBaseTypes((ClassDeclarationNode)member);
                        break;
                    case Interface:
                        setInterfaceBaseTypes((InterfaceDeclarationNode)member);
                        break;
                    case Delegate:
                        setDelegateBaseTypes((DelegateDeclarationNode)member);
                        break;
                    case Constructor:
                    case Destructor:
                    case EnumConstant:
                    case Field:
                    case Indexer:
                    case Method:
                    case Property:
                        break;
                    default:
                        throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                    }
                }

            } finally {
                context.MemberResolver.leaveType();
            }
        }

        private void setInterfaceBaseTypes(InterfaceDeclarationNode interfaceDeclaration) {
            var typeBuilder = interfaceDeclaration.getUserData(typeof(TypeBuilder));
            context.MemberResolver.enterType(typeBuilder);
            try {
                typeBuilder.setBaseType(context.TypeSystem.ObjectType);
                foreach (var typeReference in interfaceDeclaration.InterfaceBase) {
                    var type = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, typeReference);
                    if (!type.IsInterface) {
                        context.addError(CompileErrorId.ClassInInterfaceList, typeReference,
                            BytecodeHelper.getDisplayName(typeBuilder), BytecodeHelper.getDisplayName(type));
                            continue;
                    }
                    typeBuilder.addInterface(type);
                    if (type.FullName.equals("java/lang/annotation/Annotation")) {
                        typeBuilder.setAnnotation(true);
                    }
                }
            } finally {
                context.MemberResolver.leaveType();
            }
        }

        private void setDelegateBaseTypes(DelegateDeclarationNode delegateDeclaration) {
            var typeBuilder = delegateDeclaration.getUserData(typeof(TypeBuilder));
            typeBuilder.setBaseType(context.getType("stab/lang/Delegate", delegateDeclaration));
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        // Members declaration
        
        private void defineMembers() {
            try {
                context.MemberResolver.initialize();
                foreach (var compilationUnit in context.CompilationUnits) {
                    context.Text = compilationUnit.Text;
                    context.MemberResolver.enterScope();
                    try {
                        defineMembers(compilationUnit.Body);
                    } catch (CodeErrorException) {
                    } finally {
                        context.MemberResolver.leaveScope();
                    }
                }
    
                // Implicit constructors
                foreach (var typeBuilder in context.TypeBuilders) {
                    if (typeBuilder.IsInterface || typeBuilder.Methods.any(m => m.IsConstructor)) {
                        continue;
                    }
                    var methodBuilder = typeBuilder.defineMethod("<init>");
                    context.ConstructorBuilders.add(methodBuilder);
                    if (typeBuilder.IsFinal) {
                        var decl = typeBuilder.getUserData(typeof(ClassDeclarationNode));
                        if (decl.Modifiers.contains(Modifier.Static)) {
                            methodBuilder.setPrivate(true);
                        } else {
                            methodBuilder.setPublic(true);
                        }
                    } else if (typeBuilder.IsEnum) {
                        methodBuilder.setPrivate(true);
                        var pb = methodBuilder.addParameter(context.TypeSystem.StringType);
                        pb.setName("name$0");
                        pb = methodBuilder.addParameter(context.TypeSystem.IntType);
                        pb.setName("ordinal$0");
                    } else if (typeBuilder.IsAbstract) {
                        methodBuilder.setProtected(true);
                    } else {
                        methodBuilder.setPublic(true);
                    }
                    methodBuilder.setReturnType(context.TypeSystem.VoidType);
                }
                
                foreach (var ti in partialTypes.values()) {
                    foreach (var mi in ti.partialMethods.values()) {
                        if (mi.implementingPart == null) {
                            var methodBuilder = mi.definingPart.getUserData(typeof(MethodBuilder));
                            methodBuilder.IsExcludedFromCompilation = true;
                            context.addWarning(CompileErrorId.NoPartialImplementingDeclaration, mi.definingPart);
                        }
                    }
                }
            } finally {
                context.MemberResolver.dispose();
            }
        }
        
        private void defineMembers(PackageBodyNode packageBody) {
            var packageInfo = packageBody.getUserData(typeof(PackageInfo));
            context.MemberResolver.usingDirective(packageInfo.memberInfos, packageInfo.packages, packageInfo.packageAliases);
            
            foreach (var member in packageBody.Members) {
                switch (member.PackageMemberKind) {
                case Package:
                    defineMembers((PackageDeclarationNode)member);
                    break;
                case Class:
                    defineClassMembers((ClassDeclarationNode)member);
                    break;
                case Interface:
                    defineInterfaceMembers((InterfaceDeclarationNode)member);
                    break;
                case Delegate:
                    defineDelegateMembers((DelegateDeclarationNode)member);
                    break;
                default:
                    throw new Exception("Internal error: unhandled member kind: " + member.PackageMemberKind);
                }
            }
        }
        
        private void defineMembers(PackageDeclarationNode packageDeclaration) {
            if (packageDeclaration.Body != null) {
                foreach (var identifier in packageDeclaration.Identifiers) {
                    context.MemberResolver.enterPackage(context.getIdentifier(identifier.Offset, identifier.Length));
                }
                try {
                    defineMembers(packageDeclaration.Body);
                } finally {
                    for (int i = packageDeclaration.Identifiers.size() - 1; i >= 0; --i) {
                        context.MemberResolver.leavePackage();
                    }
                }
            }
        }

        private void defineClassMembers(ClassDeclarationNode classDeclaration) {
            var typeBuilder = classDeclaration.getUserData(typeof(TypeBuilder));
            context.CurrentType = typeBuilder;
            context.MemberResolver.enterType(typeBuilder);
            try {
                setTypeConstraints(classDeclaration.ConstraintsClauses, typeBuilder);
                if (typeBuilder.IsEnum) {
                    var fieldBuilder = typeBuilder.defineField("ENUM$VALUES", typeBuilder.ArrayType);
                    fieldBuilder.setPrivate(true);
                    fieldBuilder.setFinal(true);
                    fieldBuilder.setStatic(true);
                    
                    var methodBuilder = typeBuilder.defineMethod("valueOf");
                    methodBuilder.setPublic(true);
                    methodBuilder.setStatic(true);
                    methodBuilder.setReturnType(typeBuilder);
                    var param = methodBuilder.addParameter(context.TypeSystem.StringType);
                    param.setName("str");
                    
                    methodBuilder = typeBuilder.defineMethod("values");
                    methodBuilder.setPublic(true);
                    methodBuilder.setStatic(true);
                    methodBuilder.setReturnType(typeBuilder.ArrayType);
                }
                
                foreach (var member in classDeclaration.Members) {
                    switch (member.TypeMemberKind) {
                    case Class:
                        defineClassMembers((ClassDeclarationNode)member);
                        break;
                    case Interface:
                        defineInterfaceMembers((InterfaceDeclarationNode)member);
                        break;
                    case Delegate:
                        defineDelegateMembers((DelegateDeclarationNode)member);
                        break;
                    case Method:
                        defineClassMethod((MethodDeclarationNode)member, classDeclaration.IsPartial, typeBuilder);
                        break;
                    case Field:
                        defineClassField((FieldDeclarationNode)member, typeBuilder);
                        break;
                    case EnumConstant:
                        defineEnumConstant((EnumConstantDeclarationNode)member, typeBuilder);
                        break;
                    case Indexer:
                        defineClassIndexer((IndexerDeclarationNode)member, typeBuilder);
                        break;
                    case Property:
                        defineClassProperty((PropertyDeclarationNode)member, typeBuilder);
                        break;
                    case Constructor:
                        defineClassConstructor((ConstructorDeclarationNode)member, typeBuilder);
                        break;
                    case Destructor:
                        defineClassDestructor((DestructorDeclarationNode)member, typeBuilder);
                        break;
                    default:
                        throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                    }
                    context.CurrentType = typeBuilder;
                }
            } finally {
                context.MemberResolver.leaveType();
            }
        }

        private void defineInterfaceMembers(InterfaceDeclarationNode interfaceDeclaration) {
            var typeBuilder = interfaceDeclaration.getUserData(typeof(TypeBuilder));
            context.CurrentType = typeBuilder;
            context.MemberResolver.enterType(typeBuilder);
            try {
                setTypeConstraints(interfaceDeclaration.ConstraintsClauses, typeBuilder);
                
                foreach (var member in interfaceDeclaration.Members) {
                    switch (member.TypeMemberKind) {
                    case Method:
                        defineInterfaceMethod((MethodDeclarationNode)member, typeBuilder);
                        break;
                    case Indexer:
                        defineInterfaceIndexer((IndexerDeclarationNode)member, typeBuilder);
                        break;
                    case Property:
                        defineInterfaceProperty((PropertyDeclarationNode)member, typeBuilder);
                        break;
                    case EnumConstant:
                    case Class:
                    case Interface:
                    case Delegate:
                    case Constructor:
                    case Destructor:
                    case Field:
                    default:
                        throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                    }
                    context.CurrentType = typeBuilder;
                }
            } finally {
                context.MemberResolver.leaveType();
            }
        }

        private void defineDelegateMembers(DelegateDeclarationNode delegateDeclaration) {
            var typeBuilder = delegateDeclaration.getUserData(typeof(TypeBuilder));
            context.CurrentType = typeBuilder;
            setTypeParameters(typeBuilder, delegateDeclaration.TypeParameters, delegateDeclaration);
            try {
                context.MemberResolver.enterType(typeBuilder);
                
                // invoke()
                var methodBuilder = typeBuilder.defineMethod("invoke");
                methodBuilder.setReturnType(CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, delegateDeclaration.ReturnType));
                foreach (var p in delegateDeclaration.Parameters) {
                    var pb = methodBuilder.addParameter(CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, p.Type));
                    pb.setName(context.getIdentifier(p.NameOffset, p.NameLength));
                }
                methodBuilder.setAbstract(true);
                methodBuilder.setPublic(true);
                
                // TODO: async invoke
                
                // Constructor
                methodBuilder = typeBuilder.defineMethod("<init>");
                methodBuilder.setProtected(true);
                methodBuilder.setReturnType(context.TypeSystem.VoidType);
                var pb = methodBuilder.addParameter(context.TypeSystem.ObjectType);
                pb.setName("target");
            } finally {
                context.MemberResolver.leaveType();
            }
        }
        
        private void setTypeConstraints(Iterable<TypeParameterConstraintsClauseNode> constraintClauses, TypeBuilder typeBuilder) {
            var packageName = typeBuilder.PackageName;
            foreach (var constraint in constraintClauses) {
                var name = context.getIdentifier(constraint.NameOffset, constraint.NameLength);
                TypeInfo bound = CompilerHelper.resolveTypeReference(context, packageName, constraint.Constraints[0]);
                if (bound.IsFinal) {
                    context.addError(CompileErrorId.ConstraintNotInterfaceOrFinal, constraint.Constraints[0],
                        BytecodeHelper.getDisplayName(bound));
                }
                typeBuilder.addGenericConstraint(name, bound);
                for (int i = 1; i < constraint.Constraints.size(); i++) {
                    bound = CompilerHelper.resolveTypeReference(context, packageName, constraint.Constraints[i]);
                    if (!bound.IsInterface || bound.IsFinal) {
                        context.addError(CompileErrorId.ConstraintNotInterfaceOrFinal, constraint.Constraints[i],
                            BytecodeHelper.getDisplayName(bound));
                    }
                    typeBuilder.addGenericConstraint(name, bound);
                }
            }
        }
        
        private MethodBuilder lookupMethod(TypeBuilder typeBuilder, List<SimpleNameTypeReferenceNode> typeParameters,
                List<ParameterNode> parameters, String name) {
            var typeParams = getTypeParameterNames(typeParameters);
            foreach (var meth in typeBuilder.Methods) {
                if (!isCompatible(meth, name, typeParams, parameters)) {
                    continue;
                }
                context.MemberResolver.enterMethod(meth);
                int i = 0;
                var paramTypes = new ArrayList<TypeInfo>();
                try {
                    foreach (var p in parameters) {
                        var t = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, p.Type, false, true);
                        if (t == null) {
                            break;
                        }
                        paramTypes.add(t);
                        i++;
                    }
                } finally {
                    context.MemberResolver.leaveMethod();
                }
                if (i < typeParams.size()) {
                    continue;
                }
                var methodBuilder = (MethodBuilder)typeBuilder.getMethod(name, paramTypes);
                if (methodBuilder != null) {
                    return methodBuilder;
                }
            }
            return null;
        }

        private bool isCompatible(MethodInfo method, String name, List<String> typeParameters, List<ParameterNode> parameters) {
            if (!method.Name.equals(name)) {
                return false;
            }
            if (method.GenericArguments.count() != typeParameters.size()) {
                return false;
            }
            if (method.Parameters.count() != parameters.size()) {
                return false;
            }
            int i = 0;
            foreach (var t in method.GenericArguments) {
                if (!t.FullName.equals(typeParameters[i])) {
                    return false;
                }
                i++;
            }
            return true;
        }

        private void defineClassMethod(MethodDeclarationNode methodDeclaration, bool partial, TypeBuilder typeBuilder) {
            var name = context.getIdentifier(methodDeclaration.NameOffset, methodDeclaration.NameLength);
            if (name.equals("finalize") && methodDeclaration.Parameters.size() == 0) {
                context.addError(CompileErrorId.FinalizeMethodOverride, methodDeclaration);
            }
            if (methodDeclaration.IsPartial) {
                if (!partial) {
                    context.addError(CompileErrorId.PartialMethodWithinPartialClass, methodDeclaration);
                }
                if (methodDeclaration.ReturnType != context.TypeSystem.VoidType) {
                    context.addError(CompileErrorId.PartialMethodNotVoid, methodDeclaration);
                }
            }
            var methodBuilder = lookupMethod(typeBuilder, methodDeclaration.TypeParameters, methodDeclaration.Parameters, name);
            if (methodBuilder != null) {
                if (!methodDeclaration.IsPartial) {
                    context.addError(CompileErrorId.AlreadyDefinedMethod, methodDeclaration, BytecodeHelper.getDisplayName(typeBuilder), name);
                    return;
                }
                var partialInfo = partialTypes[typeBuilder.FullName];
                if (!partialInfo.partialMethods.containsKey(methodBuilder)) {
                    context.addError(CompileErrorId.AlreadyDefinedMethod, methodDeclaration, BytecodeHelper.getDisplayName(typeBuilder), name);
                }
                var partialMethodInfo = partialInfo.partialMethods[methodBuilder];
                if (methodDeclaration.Body == null) {
                    if (partialMethodInfo.definingPart != null) {
                        context.addError(CompileErrorId.MultiplePartialDefiningDeclarations, methodDeclaration);
                    }
                    partialMethodInfo.definingPart = methodDeclaration;
                } else {
                    if (partialMethodInfo.implementingPart != null) {
                        context.addError(CompileErrorId.MultiplePartialImplementingDeclarations, methodDeclaration);
                    }
                    partialMethodInfo.implementingPart = methodDeclaration;
                }
                setPartialMethodModifiers(methodDeclaration, methodBuilder);
                setMethodConstraints(methodDeclaration.ConstraintsClauses, methodBuilder);
                methodDeclaration.addUserData(methodBuilder);
            } else {
                methodBuilder = typeBuilder.defineMethod(name);
                methodDeclaration.addUserData(methodBuilder);
                setTypeParameters(methodBuilder, methodDeclaration.TypeParameters, methodDeclaration);
                context.MemberResolver.enterMethod(methodBuilder);
                try {
                    setMethodModifiers(methodDeclaration, methodBuilder);
                    var returnType = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, methodDeclaration.ReturnType);
                    methodBuilder.setReturnType(returnType);
                    var i = 0;
                    foreach (var parameter in methodDeclaration.Parameters) {
                        var type = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, parameter.Type);
                        var paramBuilder = methodBuilder.addParameter(type);
                        paramBuilder.setName(context.getIdentifier(parameter.NameOffset, parameter.NameLength));
                        if (parameter.Modifier == ParameterModifier.Params) {
                            if (i < methodDeclaration.Parameters.size() - 1) {
                                context.addError(CompileErrorId.ParamsNotLast, parameter);
                            }
                            if (!type.IsArray) {
                                context.addError(CompileErrorId.ParamsNotArray, parameter);
                            }
                            methodBuilder.setVarargs(true);
                        } else if (parameter.Modifier == ParameterModifier.This) {
                            if (!methodBuilder.IsStatic) {
                                context.addError(CompileErrorId.ThisParameterNotStatic, parameter);
                            }
                            if (i > 0) {
                                context.addError(CompileErrorId.ThisNotFirst, parameter);
                            }
                            methodBuilder.addAnnotation(context.getType("stab/lang/ExtensionMethod", parameter), false);
                        }
                        i++;
                    }
                    setMethodConstraints(methodDeclaration.ConstraintsClauses, methodBuilder);
                    if (methodDeclaration.IsPartial) {
                        var partialInfo = partialTypes[typeBuilder.getFullName()];
                        var partialMethodInfo = new PartialMethodInfo();
                        if (methodDeclaration.Body == null) {
                            partialMethodInfo.definingPart = methodDeclaration;
                        } else {
                            partialMethodInfo.implementingPart = methodDeclaration;
                        }
                        partialInfo.partialMethods[methodBuilder] = partialMethodInfo;
                    }
                } finally {
                    context.MemberResolver.leaveMethod();
                }
            }
        }
        
        private void setMethodModifiers(MethodDeclarationNode methodDeclaration, MethodBuilder methodBuilder) {
            var isFinal = true;
            foreach (var modifier in methodDeclaration.Modifiers) {
                switch (modifier) {
                case Public:
                    if (methodBuilder.IsPrivate || methodBuilder.IsProtected) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, methodDeclaration);
                    } else {
                        methodBuilder.setPublic(true);
                    }
                    break;
                    
                case Protected:
                    if (methodBuilder.IsPrivate || methodBuilder.IsPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, methodDeclaration);
                    } else {
                        methodBuilder.setProtected(true);
                    }
                    break;
                    
                case Private:
                    if (methodBuilder.IsProtected || methodBuilder.IsPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, methodDeclaration);
                    } else {
                        methodBuilder.setPrivate(true);
                        isFinal = false;
                    }
                    break;
                    
                case Final:
                    if (methodBuilder.IsAbstract) {
                        context.addError(CompileErrorId.FinalAbstractStaticMethod, methodDeclaration);
                    } else {
                        methodBuilder.setFinal(true);
                    }
                    break;
                    
                case Abstract:
                    if (methodBuilder.IsFinal || methodBuilder.IsStatic) {
                        context.addError(CompileErrorId.FinalAbstractStaticMethod, methodDeclaration);
                    } else {
                        methodBuilder.setAbstract(true);
                        isFinal = false;
                    }
                    break;
                    
                case Static:
                    if (methodBuilder.IsAbstract) {
                        context.addError(CompileErrorId.FinalAbstractStaticMethod, methodDeclaration);
                    } else {
                        methodBuilder.setStatic(true);
                    }
                    break;
                    
                case Synchronized:
                    methodBuilder.setSynchronized(true);
                    break;
                    
                case Native:
                    methodBuilder.setNative(true);
                    break;
                    
                case Strictfp:
                    methodBuilder.setStrict(true);
                    break;
                    
                case Virtual:
                case Override:
                    // TODO: check if not private
                    isFinal = false;
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, methodDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
            methodBuilder.setFinal(isFinal);
        }

        private void setPartialMethodModifiers(MethodDeclarationNode methodDeclaration, MethodBuilder methodBuilder) {
            foreach (var modifier in methodDeclaration.Modifiers) {
                switch (modifier) {
                case Final:
                    break;
                    
                case Static:
                    methodBuilder.setStatic(true);
                    break;
                    
                case Synchronized:
                    methodBuilder.setSynchronized(true);
                    break;
                    
                case Strictfp:
                    methodBuilder.setStrict(true);
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, methodDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
            methodBuilder.setPrivate(true);
        }
        
        private void setMethodConstraints(Iterable<TypeParameterConstraintsClauseNode> constraintClauses, MethodBuilder methodBuilder) {
            var packageName = methodBuilder.DeclaringType.PackageName;
            foreach (var constraint in constraintClauses) {
                var name = context.getIdentifier(constraint.NameOffset, constraint.NameLength);
                var bound = CompilerHelper.resolveTypeReference(context, packageName, constraint.Constraints[0]);
                if (bound.IsFinal) {
                    context.addError(CompileErrorId.ConstraintNotInterfaceOrFinal, constraint.Constraints[0],
                        BytecodeHelper.getDisplayName(bound));
                }
                methodBuilder.addGenericConstraint(name, bound);
                for (int i = 1; i < constraint.Constraints.size(); i++) {
                    bound = CompilerHelper.resolveTypeReference(context, packageName, constraint.Constraints[i]);
                    if (!bound.IsInterface || bound.IsFinal) {
                        context.addError(CompileErrorId.ConstraintNotInterfaceOrFinal, constraint.Constraints[i],
                            BytecodeHelper.getDisplayName(bound));
                    }
                    methodBuilder.addGenericConstraint(name, bound);
                }
            }
        }

        private void defineClassField(FieldDeclarationNode fieldDeclaration, TypeBuilder typeBuilder) {
            var type = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, fieldDeclaration.Type);
            foreach (var decl in fieldDeclaration.Declarators) {
                var name = context.getIdentifier(decl.NameOffset, decl.NameLength);
                if (typeBuilder.getField(name) != null) {
                    context.addError(CompileErrorId.AlreadyDefinedField, fieldDeclaration,
                        BytecodeHelper.getDisplayName(typeBuilder), name);
                }
                var fieldBuilder = typeBuilder.defineField(name, type);
                decl.addUserData(fieldBuilder);
                setFieldModifiers(fieldDeclaration, fieldBuilder);
            }
        }

        private void setFieldModifiers(FieldDeclarationNode fieldDeclaration, FieldBuilder fieldBuilder) {
            foreach (var modifier in fieldDeclaration.Modifiers) {
                switch (modifier) {
                case Public:
                    if (fieldBuilder.IsPrivate || fieldBuilder.IsProtected) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, fieldDeclaration);
                    } else {
                        fieldBuilder.setPublic(true);
                    }
                    break;
                    
                case Protected:
                    if (fieldBuilder.IsPrivate || fieldBuilder.IsPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, fieldDeclaration);
                    } else {
                        fieldBuilder.setProtected(true);
                    }
                    break;
                    
                case Private:
                    if (fieldBuilder.IsProtected || fieldBuilder.IsPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, fieldDeclaration);
                    } else {
                        fieldBuilder.setPrivate(true);
                    }
                    break;
                    
                case Final:
                    if (fieldBuilder.IsVolatile) {
                        context.addError(CompileErrorId.FinalVolatile, fieldDeclaration);
                    } else {
                        fieldBuilder.setFinal(true);
                    }
                    break;
                    
                case Static:
                    fieldBuilder.setStatic(true);
                    break;
                    
                case Transient:
                    fieldBuilder.setTransient(true);
                    break;
                    
                case Volatile:
                    if (fieldBuilder.IsFinal) {
                        context.addError(CompileErrorId.FinalVolatile, fieldDeclaration);
                    } else {
                        fieldBuilder.setVolatile(true);
                    }
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, fieldDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
        }

        private void defineEnumConstant(EnumConstantDeclarationNode enumConstant, TypeBuilder typeBuilder) {
            var name = context.getIdentifier(enumConstant.NameOffset, enumConstant.NameLength);
            if (typeBuilder.getField(name) != null) {
                context.addError(CompileErrorId.AlreadyDefinedField, enumConstant,
                    BytecodeHelper.getDisplayName(typeBuilder), name);
            }
            var fieldBuilder = typeBuilder.defineField(name, typeBuilder);
            enumConstant.addUserData(fieldBuilder);
            fieldBuilder.setEnum(true);
            fieldBuilder.setPublic(true);
            fieldBuilder.setStatic(true);
            fieldBuilder.setFinal(true);
        }

        private void defineClassIndexer(IndexerDeclarationNode indexerDeclaration, TypeBuilder typeBuilder) {
            defineTypeIndexer(indexerDeclaration, typeBuilder);
        }

        private void defineTypeIndexer(IndexerDeclarationNode indexerDeclaration, TypeBuilder typeBuilder) {
            var isInterface = typeBuilder.IsInterface;
            var type = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, indexerDeclaration.Type);
            var get = indexerDeclaration.GetAccessor;
            var set = indexerDeclaration.SetAccessor;
            if (!isInterface) {
                checkAccessors(indexerDeclaration.Modifiers, get, set, indexerDeclaration);
            }
            
            var paramTypes = new TypeInfo[indexerDeclaration.Parameters.size()];
            var paramNames = new String[sizeof(paramTypes)];
            var isVarargs = false;
            int i = 0;
            foreach (var p in indexerDeclaration.Parameters) {
                paramTypes[i] = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, p.Type);
                paramNames[i] = context.getIdentifier(p.NameOffset, p.NameLength);
                if (p.Modifier == ParameterModifier.Params) {
                    if (i < indexerDeclaration.Parameters.size() - 1) {
                        context.addError(CompileErrorId.ParamsNotLast, p);
                    }
                    if (!paramTypes[i].IsArray) {
                        context.addError(CompileErrorId.ParamsNotArray, p);
                    }
                    isVarargs = true;
                }
                i++;
            }
            foreach (var meth in typeBuilder.Methods) {
                foreach (var ann in meth.Annotations) {
                    if (BytecodeHelper.isIndexerGet(ann)) {
                        // TODO: check the parameter types for redefinition
                    } else if (BytecodeHelper.isIndexerSet(ann)) {
                        // TODO: check the parameter types for redefinition
                    }
                }
            }
            if (get != null) {
                var methodBuilder = typeBuilder.defineMethod("getItem");
                methodBuilder.setVarargs(isVarargs);
                get.addUserData(methodBuilder);
                if (isInterface) {
                    methodBuilder.setAbstract(true);
                    methodBuilder.setPublic(true);
                } else {
                    setPropertyOrIndexerModifiers(get, indexerDeclaration.getModifiers(), methodBuilder);
                }
                methodBuilder.setReturnType(type);
                methodBuilder.addAnnotation(context.getType("stab/lang/IndexerGet", get), false);
                for (i = 0; i < sizeof(paramTypes); i++) {
                    var paramBuilder = methodBuilder.addParameter(paramTypes[i]);
                    paramBuilder.setName(paramNames[i]);
                }
            }
            if (set != null) {
                var methodBuilder = typeBuilder.defineMethod("setItem");
                methodBuilder.setVarargs(isVarargs);
                set.addUserData(methodBuilder);
                if (isInterface) {
                    methodBuilder.setAbstract(true);
                    methodBuilder.setPublic(true);
                } else {
                    setPropertyOrIndexerModifiers(set, indexerDeclaration.getModifiers(), methodBuilder);
                }
                methodBuilder.setReturnType(context.TypeSystem.VoidType);
                for (i = 0; i < sizeof(paramTypes); i++) {
                    var paramBuilder = methodBuilder.addParameter(paramTypes[i]);
                    paramBuilder.setName(paramNames[i]);
                    if (paramNames[i].equals("value")) {
                        throw context.error(CompileErrorId.ValueParameterConflict, set);
                    }
                }
                var paramBuilder = methodBuilder.addParameter(type);
                paramBuilder.setName("value");
                methodBuilder.addAnnotation(context.getType("stab/lang/IndexerSet", set), false);
            }
        }

        private void defineClassProperty(PropertyDeclarationNode propertyDeclaration, TypeBuilder typeBuilder) {
            defineTypeProperty(propertyDeclaration, typeBuilder);
        }
        
        private void defineTypeProperty(PropertyDeclarationNode propertyDeclaration, TypeBuilder typeBuilder) {
            var isInterface = typeBuilder.IsInterface;
            var name = context.getIdentifier(propertyDeclaration.NameOffset, propertyDeclaration.NameLength);
            foreach (var meth in typeBuilder.Methods) {
                foreach (var ann in meth.Annotations) {
                    if (BytecodeHelper.isPropertyGet(ann)) {
                        if (BytecodeHelper.getPropertyGetName(meth, ann).equals(name)) {
                            context.addError(CompileErrorId.AlreadyDefinedProperty, propertyDeclaration,
                                BytecodeHelper.getDisplayName(typeBuilder), name);
                        }
                    } else if (BytecodeHelper.isPropertySet(ann)) {
                        if (BytecodeHelper.getPropertySetName(meth, ann).equals(name)) {
                            context.addError(CompileErrorId.AlreadyDefinedProperty, propertyDeclaration,
                                BytecodeHelper.getDisplayName(typeBuilder), name);
                        }
                    }
                }
            }
            var type = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, propertyDeclaration.Type);
            var get = propertyDeclaration.GetAccessor;
            var set = propertyDeclaration.SetAccessor;
            if (!isInterface) {
                checkAccessors(propertyDeclaration.Modifiers, get, set, propertyDeclaration);
            }
            if (get != null) {
                var methodName = "get" + name;
                if (type.IsBoolean) {
                    if (name.length() > 2 && name.startsWith("Is") && Character.isUpperCase(name[2])) {
                        methodName = Character.toLowerCase(name[0]) + name.substring(1);
                    }
                }
                var methodBuilder = typeBuilder.defineMethod(methodName);
                get.addUserData(methodBuilder);
                if (isInterface) {
                    methodBuilder.setAbstract(true);
                    methodBuilder.setPublic(true);
                } else {
                    setPropertyOrIndexerModifiers(get, propertyDeclaration.getModifiers(), methodBuilder);
                }
                methodBuilder.setReturnType(type);
                methodBuilder.addAnnotation(context.getType("stab/lang/PropertyGet", get), false);
            }
            if (set != null) {
                var methodBuilder = typeBuilder.defineMethod("set" + name);
                set.addUserData(methodBuilder);
                if (isInterface) {
                    methodBuilder.setAbstract(true);
                    methodBuilder.setPublic(true);
                } else {
                    setPropertyOrIndexerModifiers(set, propertyDeclaration.getModifiers(), methodBuilder);
                }
                methodBuilder.setReturnType(context.TypeSystem.VoidType);
                var paramBuilder = methodBuilder.addParameter(type);
                paramBuilder.setName("value");
                methodBuilder.addAnnotation(context.getType("stab/lang/PropertySet", set), false);
            }
        }
        
        private void defineClassConstructor(ConstructorDeclarationNode constructorDeclaration, TypeBuilder typeBuilder) {
            var name = context.getIdentifier(constructorDeclaration.NameOffset, constructorDeclaration.NameLength);
            if (!name.equals(typeBuilder.Name)) {
                context.addError(CompileErrorId.MethodWithoutReturnType, constructorDeclaration, name);
            }
            if (constructorDeclaration.Modifiers.contains(Modifier.Static)) {
                // TODO: check that modifiers.size() == 1
                var clinit = (MethodBuilder)typeBuilder.getMethod("<clinit>", Query.empty<TypeInfo>());
                if (clinit != null) {
                    context.addError(CompileErrorId.AlreadyDefinedStaticInitializer, constructorDeclaration,
                        BytecodeHelper.getDisplayName(typeBuilder));
                }
                clinit = typeBuilder.defineMethod("<clinit>");
                clinit.setStatic(true);
                clinit.setReturnType(context.TypeSystem.VoidType);
                constructorDeclaration.addUserData(clinit);
            } else {
				if (typeBuilder.IsEnum) {
					// TODO: check if the contructor is private
				}
                var methodBuilder = lookupMethod(typeBuilder, constructorDeclaration.TypeParameters, constructorDeclaration.Parameters, "<init>");
                if (methodBuilder != null) {
                    context.addError(CompileErrorId.AlreadyDefinedConstructor, constructorDeclaration,
                            BytecodeHelper.getDisplayName(typeBuilder));
                }
                methodBuilder = typeBuilder.defineMethod("<init>");
                methodBuilder.setReturnType(context.TypeSystem.VoidType);
                constructorDeclaration.addUserData(methodBuilder);
                setTypeParameters(methodBuilder, constructorDeclaration.getTypeParameters(), constructorDeclaration);
                context.MemberResolver.enterMethod(methodBuilder);
                try {
                    setConstructorModifiers(constructorDeclaration, methodBuilder);
                    if (typeBuilder.IsEnum) {
                        var pb = methodBuilder.addParameter(context.TypeSystem.StringType);
                        pb.setName("name$0");
                        pb = methodBuilder.addParameter(context.TypeSystem.IntType);
                        pb.setName("ordinal$0");
                    }
                    foreach (var p in constructorDeclaration.Parameters) {
                        var t = CompilerHelper.resolveTypeReference(context, typeBuilder.PackageName, p.Type);
                        var pb = methodBuilder.addParameter(t);
                        pb.setName(context.getIdentifier(p.NameOffset, p.NameLength));
                        if (p.Modifier == ParameterModifier.Params) {
                            methodBuilder.setVarargs(true);
                        }
                    }
                    setMethodConstraints(constructorDeclaration.ConstraintsClauses, methodBuilder);
                } finally {
                    context.MemberResolver.leaveMethod();
                }
            }
        }
        
        private void setConstructorModifiers(ConstructorDeclarationNode constructorDeclaration, MethodBuilder methodBuilder) {
            foreach (var modifier in constructorDeclaration.Modifiers) {
                switch (modifier) {
                case Public:
                    if (methodBuilder.IsPrivate || methodBuilder.IsProtected) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, constructorDeclaration);
                    } else {
                        methodBuilder.setPublic(true);
                    }
                    break;
                    
                case Protected:
                    if (methodBuilder.IsPrivate || methodBuilder.IsPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, constructorDeclaration);
                    } else {
                        methodBuilder.setProtected(true);
                    }
                    break;
                    
                case Private:
                    if (methodBuilder.IsProtected || methodBuilder.IsPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, constructorDeclaration);
                    } else {
                        methodBuilder.setPrivate(true);
                    }
                    break;
                    
                case Synchronized:
                    methodBuilder.setSynchronized(true);
                    break;
                    
                case Native:
                    methodBuilder.setNative(true);
                    break;
                    
                case Strictfp:
                    methodBuilder.setStrict(true);
                    break;
                    
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, constructorDeclaration, modifier.toString().toLowerCase());
                    break;
                }
            }
        }
        
        private void defineClassDestructor(DestructorDeclarationNode destructorDeclaration, TypeBuilder typeBuilder) {
            if (typeBuilder.IsEnum) {
                // TODO: error
            }
            var methodBuilder = (MethodBuilder)typeBuilder.getMethod("finalize", Query.empty<TypeInfo>());
            if (methodBuilder != null) {
                context.addError(CompileErrorId.AlreadyDefinedDestructor, destructorDeclaration,
                        BytecodeHelper.getDisplayName(typeBuilder));
            }
            var name = context.getIdentifier(destructorDeclaration.NameOffset, destructorDeclaration.NameLength);
            if (!name.equals(typeBuilder.Name)) {
                context.addError(CompileErrorId.InvalidDestructorName, destructorDeclaration, name);
            }
            methodBuilder = typeBuilder.defineMethod("finalize");
            methodBuilder.setReturnType(context.TypeSystem.VoidType);
            methodBuilder.setProtected(true);
            destructorDeclaration.addUserData(methodBuilder);
        }
        
        private void defineInterfaceMethod(MethodDeclarationNode methodDeclaration, TypeBuilder typeBuilder) {
            var packageName = typeBuilder.PackageName;
            var name = context.getIdentifier(methodDeclaration.NameOffset, methodDeclaration.NameLength);
            var methodBuilder = lookupMethod(typeBuilder, methodDeclaration.TypeParameters, methodDeclaration.Parameters, name);
            if (methodBuilder != null) {
                context.addError(CompileErrorId.AlreadyDefinedMethod, methodDeclaration,
                        BytecodeHelper.getDisplayName(typeBuilder), name);
            }
            methodBuilder = typeBuilder.defineMethod(context.getIdentifier(methodDeclaration.NameOffset, methodDeclaration.NameLength));
            methodDeclaration.addUserData(methodBuilder);
            setTypeParameters(methodBuilder, methodDeclaration.TypeParameters, methodDeclaration);
            context.MemberResolver.enterMethod(methodBuilder);
            try {
                methodBuilder.setPublic(true);
                methodBuilder.setAbstract(true);
                methodBuilder.setReturnType(CompilerHelper.resolveTypeReference(context, packageName, methodDeclaration.ReturnType));
                foreach (var p in methodDeclaration.Parameters) {
                    var pb = methodBuilder.addParameter(CompilerHelper.resolveTypeReference(context, packageName, p.Type));
                    pb.setName(context.getIdentifier(p.NameOffset, p.NameLength));
                    if (p.Modifier == ParameterModifier.Params) {
                        methodBuilder.setVarargs(true);
                    }
                }
                setMethodConstraints(methodDeclaration.ConstraintsClauses, methodBuilder);
            } finally {
                context.MemberResolver.leaveMethod();
            }
        }
        
        private void defineInterfaceIndexer(IndexerDeclarationNode indexerDeclaration, TypeBuilder typeBuilder) {
            defineTypeIndexer(indexerDeclaration, typeBuilder);
        }
        
        private void defineInterfaceProperty(PropertyDeclarationNode propertyDeclaration, TypeBuilder typeBuilder) {
            defineTypeProperty(propertyDeclaration, typeBuilder);
        }
        
        private void checkAccessors(EnumSet<Modifier> modifiers, AccessorDeclarationNode get, AccessorDeclarationNode set, SyntaxNode node) {
            if (get != null && set != null) {
                if (!get.Modifiers.isEmpty() && !set.Modifiers.isEmpty()) {
                    context.addError(CompileErrorId.IllegalGetOrSetModifiers, node);
                }
                if ((get.Body == null) != (set.Body == null)) {
                    context.addError(CompileErrorId.IllegalGetOrSetBody, node);
                }
            } else if (get != null) {
                if (!get.Modifiers.isEmpty()) {
                    context.addError(CompileErrorId.IllegalGetOrSetModifiers, node);
                }
                if (!modifiers.contains(Modifier.Abstract) && get.Body == null) {
                    context.addError(CompileErrorId.IllegalGetOrSetBody, node);
                }
            } else {
                if (!set.Modifiers.isEmpty()) {
                    context.addError(CompileErrorId.IllegalGetOrSetModifiers, node);
                }
                if (!modifiers.contains(Modifier.Abstract) && set.Body == null) {
                    context.addError(CompileErrorId.IllegalGetOrSetBody, node);
                }
            }
        }
        
        private void setPropertyOrIndexerModifiers(AccessorDeclarationNode accessor, EnumSet<Modifier> propertyModifiers,
                MethodBuilder methodBuilder) {
            var protectionSet = false;
            var isFinal = true;
            var modifiers = accessor.Modifiers;
            foreach (var mod in modifiers) {
                switch (mod) {
                case Public:
                    if (methodBuilder.IsPrivate || methodBuilder.IsProtected) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, accessor);
                    } else {
                        methodBuilder.setPublic(true);
                        protectionSet = true;
                    }
                    break;

                case Private:
                    if (methodBuilder.IsPublic || methodBuilder.IsProtected) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, accessor);
                    } else {
                        methodBuilder.setPrivate(true);
                        protectionSet = true;
                    }
                    isFinal = false;
                    break;
                    
                case Protected:
                    if (methodBuilder.IsPrivate || methodBuilder.IsPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, accessor);
                    } else {
                        methodBuilder.setProtected(true);
                        protectionSet = true;
                    }
                    break;
                default:
                    context.addError(CompileErrorId.UnexpectedModifier, accessor, mod.toString().toLowerCase());
                    break;
                }
            }
            foreach (var mod in propertyModifiers) {
                switch (mod) {
                case Public:
                    if (!protectionSet) {
                        methodBuilder.setPublic(true);
                    }
                    break;

                case Private:
                    if (methodBuilder.IsPublic || methodBuilder.IsProtected) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, accessor);
                    } else {
                        if (!protectionSet) {
                            methodBuilder.setPrivate(true);
                        }
                    }
                    break;
                    
                case Protected:
                    if (methodBuilder.IsPublic) {
                        context.addError(CompileErrorId.PublicProtectedPrivate, accessor);
                    } else {
                        if (!protectionSet) {
                            methodBuilder.setProtected(true);
                        }
                    }
                    break;

                case Final:
                    methodBuilder.setFinal(true);
                    break;
                    
                case Abstract:
                    if (methodBuilder.IsFinal || methodBuilder.IsStatic) {
                        context.addError(CompileErrorId.FinalAbstractStaticMethod, accessor);
                    } else {
                        methodBuilder.setAbstract(true);
                    }
                    isFinal = false;
                    break;

                case Static:
                    if (methodBuilder.IsAbstract) {
                        context.addError(CompileErrorId.FinalAbstractStaticMethod, accessor);
                    } else {
                        methodBuilder.setStatic(true);
                    }
                    break;

                case Synchronized:
                    methodBuilder.setSynchronized(true);
                    break;

                case Native:
                    methodBuilder.setNative(true);
                    break;

                case Strictfp:
                    methodBuilder.setStrict(true);
                    break;

                case Virtual:
                case Override:
                    isFinal = false;
                    break;

                default:
                    context.addError(CompileErrorId.UnexpectedModifier, accessor, mod.toString().toLowerCase());
                    break;
                }
            }
            methodBuilder.setFinal(isFinal);
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        // Annotations generation

        private void generateAnnotations() {
            try {
                context.MemberResolver.initialize();
                foreach (var compilationUnit in context.CompilationUnits) {
                    context.Text = compilationUnit.Text;
                    context.MemberResolver.enterScope();
                    try {
                        generateAnnotations(compilationUnit.Body, true);
                    } catch (CodeErrorException) {
                    } finally {
                        context.MemberResolver.leaveScope();
                    }
                }
            } finally {
                context.MemberResolver.dispose();
            }
            try {
                context.MemberResolver.initialize();
                foreach (var compilationUnit in context.CompilationUnits) {
                    context.Text = compilationUnit.Text;
                    context.MemberResolver.enterScope();
                    try {
                        generateAnnotations(compilationUnit.Body, false);
                    } catch (CodeErrorException) {
                    } finally {
                        context.MemberResolver.leaveScope();
                    }
                }
            } finally {
                context.MemberResolver.dispose();
            }
        }

        private void generateAnnotations(PackageBodyNode packageBody, bool annotationTypesOnly) {
            var packageInfo = packageBody.getUserData(typeof(PackageInfo));
            context.MemberResolver.usingDirective(packageInfo.memberInfos, packageInfo.packages, packageInfo.packageAliases);

            foreach (var member in packageBody.Members) {
                switch (member.PackageMemberKind) {
                case Package:
                    generateAnnotations((PackageDeclarationNode)member, annotationTypesOnly);
                    break;
                case Class:
                    generateClassAnnotations((ClassDeclarationNode)member, annotationTypesOnly);
                    break;
                case Interface:
                    generateInterfaceAnnotations((InterfaceDeclarationNode)member, annotationTypesOnly);
                    break;
                case Delegate:
                    generateDelegateAnnotations((DelegateDeclarationNode)member, annotationTypesOnly);
                    break;
                default:
                    throw new Exception("Internal error: unhandled member kind: " + member.PackageMemberKind);
                }
            }
        }
                
        private void generateAnnotations(PackageDeclarationNode packageDeclaration, bool annotationTypesOnly) {
            if (packageDeclaration.Body != null) {
                foreach (var identifier in packageDeclaration.Identifiers) {
                    context.MemberResolver.enterPackage(context.getIdentifier(identifier.Offset, identifier.Length));
                }
                
                if (!annotationTypesOnly && packageDeclaration.Annotations.size() > 0) {
                    validateAnnotationSection(packageDeclaration.Annotations, ElementType.PACKAGE);
                    var packageInfoName = context.MemberResolver.getCurrentPackage() + "/package-info";
                    if (!context.TypeSystem.typeExists(packageInfoName)) {
                        var t = context.TypeSystem.defineType(packageInfoName);
                        context.TypeBuilders.add(t);
                        t.setInterface(true);
                        t.setAbstract(true);
                        t.setBaseType(context.TypeSystem.ObjectType);
                    }
                    var packageInfo = context.TypeSystem.getType(packageInfoName);
                    if (packageInfo instanceof TypeBuilder) {
                        bytecodeGenerator.generateAnnotationsBytecode(packageDeclaration.Annotations, (TypeBuilder)packageInfo);
                    } else {
                        context.addError(CompileErrorId.PackageAnnotatedExternally, packageDeclaration);
                    }
                }
                try {
                    generateAnnotations(packageDeclaration.Body, annotationTypesOnly);
                } finally {
                    for (int i = packageDeclaration.Identifiers.size() - 1; i >= 0; --i) {
                        context.MemberResolver.leavePackage();
                    }
                }
            }
        }
        private void generateClassAnnotations(ClassDeclarationNode classDeclaration, bool annotationTypesOnly) {
            var typeBuilder = classDeclaration.getUserData(typeof(TypeBuilder));
            if (!annotationTypesOnly) {
                validateAnnotationSection(classDeclaration.Annotations, ElementType.TYPE);
                bytecodeGenerator.generateAnnotationsBytecode(classDeclaration.Annotations, typeBuilder);
                if (classDeclaration.Modifiers.contains(Modifier.Static)) {
                    typeBuilder.addAnnotation(context.getType("stab/lang/StaticClass", classDeclaration), false);
                }
            }
            
            context.CurrentType = typeBuilder;
            context.MemberResolver.enterType(typeBuilder);
            try {
                foreach (var member in classDeclaration.Members) {
                    switch (member.TypeMemberKind) {
                    case Class:
                        generateClassAnnotations((ClassDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Interface:
                        generateInterfaceAnnotations((InterfaceDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Delegate:
                        generateDelegateAnnotations((DelegateDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Method:
                        generateMethodAnnotations((MethodDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Field:
                        generateFieldAnnotations((FieldDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Indexer:
                        generateIndexerAnnotations((IndexerDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Property:
                        generatePropertyAnnotations((PropertyDeclarationNode)member, annotationTypesOnly);
                        break;
                    case EnumConstant:
                        generateEnumConstantAnnotations((EnumConstantDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Constructor:
                        generateConstructorAnnotations((ConstructorDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Destructor:
                        generateDestructorAnnotations((DestructorDeclarationNode)member, annotationTypesOnly);
                        break;
                    default:
                        throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                    }
                    context.CurrentType = typeBuilder;
                }
            } finally {
                context.MemberResolver.leaveType();
            }
        }

        private void generateInterfaceAnnotations(InterfaceDeclarationNode interfaceDeclaration, bool annotationTypesOnly) {
            var typeBuilder = interfaceDeclaration.getUserData(typeof(TypeBuilder));
            var isAnnotation = typeBuilder.Interfaces.any(p => p.FullName.equals("java/lang/annotation/Annotation"));
            if (annotationTypesOnly) {
                if (isAnnotation) {
                    validateAnnotationSection(interfaceDeclaration.Annotations, ElementType.TYPE);
                    bytecodeGenerator.generateAnnotationsBytecode(interfaceDeclaration.Annotations, typeBuilder);
                }
            } else {
                if (!isAnnotation) {
                    validateAnnotationSection(interfaceDeclaration.Annotations, ElementType.TYPE);
                    bytecodeGenerator.generateAnnotationsBytecode(interfaceDeclaration.Annotations, typeBuilder);
                }
            }
            
            context.CurrentType = typeBuilder;
            context.MemberResolver.enterType(typeBuilder);
            try {
                foreach (var member in interfaceDeclaration.Members) {
                    switch (member.TypeMemberKind) {
                    case Method:
                        generateInterfaceMethodAnnotations((MethodDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Indexer:
                        generateIndexerAnnotations((IndexerDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Property:
                        generatePropertyAnnotations((PropertyDeclarationNode)member, annotationTypesOnly);
                        break;
                    case Class:
                    case Delegate:
                    case Interface:
                    case Constructor:
                    case Destructor:
                    case EnumConstant:
                    case Field:
                    default:
                        throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                    }
                }
            } finally {
                context.MemberResolver.leaveType();
            }
        }

        private void generateDestructorAnnotations(DestructorDeclarationNode destructorDeclaration, bool annotationTypesOnly) {
            if (annotationTypesOnly) {
                return;
            }
            validateAnnotationSection(destructorDeclaration.Annotations, ElementType.METHOD);
            var methodBuilder = destructorDeclaration.getUserData(typeof(MethodBuilder));
            bytecodeGenerator.generateAnnotationsBytecode(destructorDeclaration.Annotations, methodBuilder);
            setExceptions(methodBuilder, destructorDeclaration);
        }

        private void generateConstructorAnnotations(ConstructorDeclarationNode constructorDeclaration, bool annotationTypesOnly) {
            if (annotationTypesOnly) {
                return;
            }
            validateAnnotationSection(constructorDeclaration.Annotations, ElementType.METHOD);
            var methodBuilder = constructorDeclaration.getUserData(typeof(MethodBuilder));
            if (!methodBuilder.IsStatic) {
                var typeInfo = methodBuilder.DeclaringType;
                if (typeInfo.Annotations.firstOrDefault(p => p.Type.FullName.equals("stab/lang/StaticClass")) != null) {
                    context.addError(CompileErrorId.StaticClassConstructor, constructorDeclaration);
                }
            }
            bytecodeGenerator.generateAnnotationsBytecode(constructorDeclaration.Annotations, methodBuilder);
            var it = methodBuilder.Parameters.iterator();
            foreach (var p in constructorDeclaration.Parameters) {
                validateAnnotationSection(p.Annotations, ElementType.PARAMETER);
                bytecodeGenerator.generateAnnotationsBytecode(p.Annotations, (ParameterBuilder)it.next());
            }
            setExceptions(methodBuilder, constructorDeclaration);
        }

        private void generateEnumConstantAnnotations(EnumConstantDeclarationNode enumConstant, bool annotationTypesOnly) {
            if (annotationTypesOnly) {
                return;
            }
            validateAnnotationSection(enumConstant.Annotations, ElementType.FIELD);
            var field = enumConstant.getUserData(typeof(FieldBuilder));
            bytecodeGenerator.generateAnnotationsBytecode(enumConstant.Annotations, field);
        }

        private void generateIndexerAnnotations(IndexerDeclarationNode indexerDeclaration, bool annotationTypesOnly) {
            if (annotationTypesOnly) {
                return;
            }
            validateAnnotationSection(indexerDeclaration.Annotations, ElementType.METHOD);
            var get = indexerDeclaration.GetAccessor;
            if (get != null) {
                validateAnnotationSection(get.Annotations, ElementType.METHOD);
            }
            var set = indexerDeclaration.SetAccessor;
            if (set != null) {
                validateAnnotationSection(set.Annotations, ElementType.METHOD);
            }
            bytecodeGenerator.emitAccessorAnnotations(get, indexerDeclaration.Annotations);
            bytecodeGenerator.emitAccessorAnnotations(set, indexerDeclaration.Annotations);
            if (get != null) {
                var methodBuilder = get.getUserData(typeof(MethodBuilder));
                if (!methodBuilder.IsStatic) {
                    var typeInfo = methodBuilder.DeclaringType;
                    if (typeInfo.Annotations.firstOrDefault(p => p.Type.FullName.equals("stab/lang/StaticClass")) != null) {
                        context.addError(CompileErrorId.StaticClassInstanceMember, get);
                    }
                }
				var it = methodBuilder.Parameters.iterator();
				foreach (var p in indexerDeclaration.Parameters) {
					validateAnnotationSection(p.Annotations, ElementType.PARAMETER);
					bytecodeGenerator.generateAnnotationsBytecode(p.Annotations, (ParameterBuilder)it.next());
				}
                setExceptions(methodBuilder, get);
            }
            if (set != null) {
                var methodBuilder = set.getUserData(typeof(MethodBuilder));
                if (!methodBuilder.IsStatic) {
                    var typeInfo = methodBuilder.DeclaringType;
                    if (typeInfo.Annotations.firstOrDefault(p => p.Type.FullName.equals("stab/lang/StaticClass")) != null) {
                        context.addError(CompileErrorId.StaticClassInstanceMember, set);
                    }
                }
				var it = methodBuilder.Parameters.iterator();
				foreach (var p in indexerDeclaration.Parameters) {
					validateAnnotationSection(p.Annotations, ElementType.PARAMETER);
					bytecodeGenerator.generateAnnotationsBytecode(p.Annotations, (ParameterBuilder)it.next());
				}
                setExceptions(methodBuilder, set);
            }
        }

        private void generatePropertyAnnotations(PropertyDeclarationNode propertyDeclaration, bool annotationTypesOnly) {
            if (annotationTypesOnly) {
                return;
            }
            validateAnnotationSection(propertyDeclaration.Annotations, ElementType.METHOD);
            var get = propertyDeclaration.GetAccessor;
            if (get != null) {
                validateAnnotationSection(get.Annotations, ElementType.METHOD);
            }
            var set = propertyDeclaration.SetAccessor;
            if (set != null) {
                validateAnnotationSection(set.Annotations, ElementType.METHOD);
            }
            bytecodeGenerator.emitAccessorAnnotations(get, propertyDeclaration.Annotations);
            bytecodeGenerator.emitAccessorAnnotations(set, propertyDeclaration.Annotations);
            if (get != null) {
                var methodBuilder = get.getUserData(typeof(MethodBuilder));
                if (!methodBuilder.IsStatic) {
                    var typeInfo = methodBuilder.DeclaringType;
                    if (typeInfo.Annotations.firstOrDefault(p => p.Type.FullName.equals("stab/lang/StaticClass")) != null) {
                        context.addError(CompileErrorId.StaticClassInstanceMember, get);
                    }
                }
                setExceptions(methodBuilder, get);
            }
            if (set != null) {
                var methodBuilder = set.getUserData(typeof(MethodBuilder));
                if (!methodBuilder.IsStatic) {
                    var typeInfo = methodBuilder.DeclaringType;
                    if (typeInfo.Annotations.firstOrDefault(p => p.Type.FullName.equals("stab/lang/StaticClass")) != null) {
                        context.addError(CompileErrorId.StaticClassInstanceMember, set);
                    }
                }
                setExceptions(methodBuilder, set);
            }
        }

                
        private void generateInterfaceMethodAnnotations(MethodDeclarationNode methodDeclaration, bool annotationTypesOnly) {
            if (annotationTypesOnly) {
                return;
            }
            validateAnnotationSection(methodDeclaration.Annotations, ElementType.METHOD);
            var method = methodDeclaration.getUserData(typeof(MethodBuilder));
            if (methodDeclaration.DefaultValue != null) {
                validateAnnotationArgument(method, methodDeclaration.DefaultValue);
            }
            var methodBuilder = methodDeclaration.getUserData(typeof(MethodBuilder));
            bytecodeGenerator.generateInterfaceMethodAnnotations(methodDeclaration);
            var it = methodBuilder.Parameters.iterator();
            foreach (var p in methodDeclaration.Parameters) {
                validateAnnotationSection(p.Annotations, ElementType.PARAMETER);
                bytecodeGenerator.generateAnnotationsBytecode(p.Annotations, (ParameterBuilder)it.next());
            }
            setExceptions(methodBuilder, methodDeclaration);
        }

        private void generateDelegateAnnotations(DelegateDeclarationNode delegateDeclaration, bool annotationTypesOnly) {
            if (annotationTypesOnly) {
                return;
            }
            validateAnnotationSection(delegateDeclaration.Annotations, ElementType.TYPE);
            var typeBuilder = delegateDeclaration.getUserData(typeof(TypeBuilder));
            bytecodeGenerator.generateAnnotationsBytecode(delegateDeclaration.Annotations, typeBuilder);
			var methodBuilder = (MethodBuilder)typeBuilder.getMethods().first(p => p.Name.equals("invoke"));
            var it = methodBuilder.Parameters.iterator();
            foreach (var p in delegateDeclaration.Parameters) {
                validateAnnotationSection(p.Annotations, ElementType.PARAMETER);
                bytecodeGenerator.generateAnnotationsBytecode(p.Annotations, (ParameterBuilder)it.next());
            }
        }

        private void generateMethodAnnotations(MethodDeclarationNode methodDeclaration, bool annotationTypesOnly) {
            if (annotationTypesOnly) {
                return;
            }
            validateAnnotationSection(methodDeclaration.Annotations, ElementType.METHOD);
            foreach (var section in methodDeclaration.Annotations) {
                foreach (var annotation in section.Annotations) {
                    if (annotation.getUserData(typeof(TypeInfo)).FullName.equals("stab/lang/Conditional")) {
                        var methodBuilder = methodDeclaration.getUserData(typeof(MethodBuilder));
                        if (methodBuilder.ReturnType != context.TypeSystem.VoidType) {
                            context.addError(CompileErrorId.ConditionalMethodNotVoid, methodDeclaration);
                        }
                    }
                }
            }
            var methodBuilder = methodDeclaration.getUserData(typeof(MethodBuilder));
            if (!methodBuilder.IsStatic) {
                var typeInfo = methodBuilder.DeclaringType;
                if (typeInfo.Annotations.firstOrDefault(p => p.Type.FullName.equals("stab/lang/StaticClass")) != null) {
                    context.addError(CompileErrorId.StaticClassInstanceMember, methodDeclaration);
                }
            }
            bytecodeGenerator.generateAnnotationsBytecode(methodDeclaration.Annotations, methodBuilder);
            var it = methodBuilder.Parameters.iterator();
            foreach (var p in methodDeclaration.Parameters) {
                validateAnnotationSection(p.Annotations, ElementType.PARAMETER);
                bytecodeGenerator.generateAnnotationsBytecode(p.Annotations, (ParameterBuilder)it.next());
            }
            setExceptions(methodBuilder, methodDeclaration);
        }
        
        private void generateFieldAnnotations(FieldDeclarationNode fieldDeclaration, bool annotationTypesOnly) {
            if (annotationTypesOnly) {
                return;
            }
            validateAnnotationSection(fieldDeclaration.Annotations, ElementType.FIELD);
            foreach (var decl in fieldDeclaration.Declarators) {
                var fieldBuilder = decl.getUserData(typeof(FieldBuilder));
                if (!fieldBuilder.IsStatic) {
                    var typeInfo = fieldBuilder.DeclaringType;
                    if (typeInfo.Annotations.firstOrDefault(p => p.Type.FullName.equals("stab/lang/StaticClass")) != null) {
                        context.addError(CompileErrorId.StaticClassInstanceMember, fieldDeclaration);
                    }
                }
                bytecodeGenerator.generateAnnotationsBytecode(fieldDeclaration.Annotations, fieldBuilder);
            }
        }
        
        private void validateAnnotationSection(List<AnnotationSectionNode> annotations, ElementType elementType) {
            foreach (var section in annotations) {
                foreach (var annotation in section.Annotations) {
                    var type = CompilerHelper.resolveTypeReference(context, context.CurrentType.PackageName, annotation.Type);
                    if (!type.Interfaces.any(p => p.FullName.equals("java/lang/annotation/Annotation"))) {
                        throw context.error(CompileErrorId.AnnotationTypeExpected, annotation, BytecodeHelper.getDisplayName(type));
                    }
                    annotation.addUserData(type);
                    var init = annotation.Initializer;
                    if (init != null) {
                        if (init.ExpressionKind == ExpressionKind.ObjectInitializer) {
                            foreach (var mi in ((ObjectInitializerExpressionNode)init).MemberInitializers) {
                                var name = context.getIdentifier(mi.NameOffset, mi.NameLength);
                                validateAnnotationArgument(type, name, mi.Value);
                            }
                            break;
                        } else {
                            validateAnnotationArgument(type, "value", init);
                        }
                    }
                }
            }
        }
        
        private void validateAnnotationArgument(TypeInfo annotationType, String name, ExpressionNode expression) {
            var method = annotationType.getMethod(name, Query.empty<TypeInfo>());
            if (method == null) {
                context.addError(CompileErrorId.UnresolvedName, expression, name);
            } else {
                validateAnnotationArgument(method, expression);
            }
        }

        private void validateAnnotationArgument(MethodInfo method, ExpressionNode expression) {
            context.CodeValidationContext.IsStatic = true;
            switch (expression.ExpressionKind) {
            case Annotation: {
                var annotationSection = (AnnotationSectionNode)expression;
                var info = new ExpressionInfo(method.ReturnType);
                annotationSection.addOrReplaceUserData(info);
                validateAnnotationSection(Collections.singletonList(annotationSection), ElementType.PARAMETER);
                break;
            }
            case ArrayInitializer: {
                if (!method.ReturnType.IsArray) {
                    throw context.error(CompileErrorId.UnexpectedArrayArgument, expression);
                }
                var arrayInitializer = (ArrayInitializerExpressionNode)expression;
                var info = new ExpressionInfo(method.ReturnType);
                arrayInitializer.addOrReplaceUserData(info);
                foreach (var e in ((ArrayInitializerExpressionNode)expression).Values) {
                    validateAnnotationArrayArgumentElement(method.ReturnType.ElementType, e);
                }
                break;
            }
            default:
                expressionValidator.handleExpression(expression, method.ReturnType, true);
                var info = expression.getUserData(typeof(ExpressionInfo));
                ValidationHelper.getType(context, expression);
                if (expression.ExpressionKind == ExpressionKind.MemberAccess && info.Members == null) {
                    throw context.error(CompileErrorId.TypeExpressionNotAllowed, expression);
                }
                if (info == null) {
                    throw context.error(CompileErrorId.UnexpectedNull, expression);
                } else {
                    if (!ValidationHelper.isAssignable(context, method.ReturnType, expression)) {
                        throw context.error(CompileErrorId.NoImplicitConversion, expression,
                                BytecodeHelper.getDisplayName(info.Type),
                                BytecodeHelper.getDisplayName(method.ReturnType));
                    }
                }
                if (info.IsConstant) {
                    ValidationHelper.getType(context, expression);
                    CompilerHelper.convertConstant(context, expression, info, method.ReturnType);
                } else if (!ValidationHelper.getType(context, expression).IsEnum && !info.Type.FullName.equals("java/lang/Class")) {
                    throw context.error(CompileErrorId.NoImplicitConversion, expression,
                            BytecodeHelper.getDisplayName(info.Type),
                            BytecodeHelper.getDisplayName(method.ReturnType));
                }
                break;
            }
        }

        private void validateAnnotationArrayArgumentElement(TypeInfo elementType, ExpressionNode expression) {
            context.CodeValidationContext.IsStatic = true;
            switch (expression.ExpressionKind) {
            case Annotation: {
                var annotationSection = (AnnotationSectionNode)expression;
                var info = new ExpressionInfo(elementType);
                annotationSection.addOrReplaceUserData(info);
                validateAnnotationSection(Collections.singletonList(annotationSection), ElementType.PARAMETER);
                break;
            }
            case ArrayInitializer: {
                var arrayInitializer = (ArrayInitializerExpressionNode)expression;
                var info = new ExpressionInfo(elementType);
                arrayInitializer.addOrReplaceUserData(info);
                foreach (var e in arrayInitializer.Values) {
                    validateAnnotationArrayArgumentElement(elementType.ElementType, e);
                }
                break;
            }
            default:
                expressionValidator.handleExpression(expression, elementType, true);
                var info = expression.getUserData(typeof(ExpressionInfo));
                // TODO: detect only methods members
                ValidationHelper.getType(context, expression);
                if (info == null) {
                    context.addError(CompileErrorId.UnexpectedNull, expression);
                } else {
                    if (expression.ExpressionKind == ExpressionKind.MemberAccess && info.Members == null) {
                        throw context.error(CompileErrorId.TypeExpressionNotAllowed, expression);
                    }
                    if (!ValidationHelper.isAssignable(context, elementType, expression)) {
                        throw context.error(CompileErrorId.NoImplicitConversion, expression,
                                BytecodeHelper.getDisplayName(info.Type),
                                BytecodeHelper.getDisplayName(elementType));
                    }
                }
                if (info.IsConstant) {
                    ValidationHelper.getType(context, expression);
                    CompilerHelper.convertConstant(context, expression, info, elementType);
                } else if (!ValidationHelper.getType(context, expression).IsEnum && !info.Type.FullName.equals("java/lang/Class")) {
                    throw context.error(CompileErrorId.NoImplicitConversion, expression,
                            BytecodeHelper.getDisplayName(info.Type),
                            BytecodeHelper.getDisplayName(elementType));
                }
                break;
            }
        }
        
        private void setExceptions(MethodBuilder methodBuilder, SyntaxNode node) {
            foreach (var av in methodBuilder.Annotations) {
                if (av.Type.FullName.equals("stab/lang/Throws")) {
                    foreach (var elt in av.getArgument("value").Elements) {
                        var t = elt.Type;
                        if (!context.TypeSystem.getType("java/lang/Throwable").isAssignableFrom(t)) {
                            context.addError(CompileErrorId.NoImplicitConversion, node,
                                BytecodeHelper.getDisplayName(t), "java.lang.Throwable");
                        } else {
                            methodBuilder.addException(t);
                        }
                    }
                }
            }
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        // Code validation
        
        private void validateCode() {
            try {
                context.MemberResolver.initialize();
                foreach (var compilationUnit in context.CompilationUnits) {
                    context.Text = compilationUnit.Text;
                    queryTranslator = new QueryTranslator(context.Text);
                    context.MemberResolver.enterScope();
                    try {
                        validateCode(compilationUnit.Body);
                    } catch (CodeErrorException) {
                    } finally {
                        context.MemberResolver.leaveScope();
                    }
                }
                
                // Implicit constructors validation
                foreach (var methodBuilder in context.ConstructorBuilders) {
                    var baseConstructor = methodBuilder.DeclaringType.BaseType.getMethod("<init>",
                            (methodBuilder.DeclaringType.IsEnum)
                                ? new ArrayList<TypeInfo> { context.TypeSystem.StringType, context.TypeSystem.IntType }
                                : Query.empty<TypeInfo>());
                    if (baseConstructor == null) {
                        context.addError(CompileErrorId.BaseMissingDefaultConstructor,
                            methodBuilder.DeclaringType.getUserData(typeof(ClassDeclarationNode)),
                            BytecodeHelper.getDisplayName(methodBuilder.DeclaringType.BaseType));
                    }
                }
            } finally {
                context.MemberResolver.dispose();
            }
        }

        private void validateCode(PackageBodyNode packageBody) {
            var packageInfo = packageBody.getUserData(typeof(PackageInfo));
            context.MemberResolver.usingDirective(packageInfo.memberInfos, packageInfo.packages, packageInfo.packageAliases);
        
            foreach (var member in packageBody.Members) {
                switch (member.PackageMemberKind) {
                case Package:
                    validateCode((PackageDeclarationNode)member);
                    break;
                case Class:
                    validateClassCode((ClassDeclarationNode)member);
                    break;
                case Interface:
                    validateInterfaceCode((InterfaceDeclarationNode)member);
                	break;
                case Delegate:
                    break;
                default:
                    throw new Exception("Internal error: unhandled member kind: " + member.PackageMemberKind);
                }
            }
        }
        
        private void validateCode(PackageDeclarationNode packageDeclaration) {
            if (packageDeclaration.Body != null) {
                foreach (var identifier in packageDeclaration.Identifiers) {
                    context.MemberResolver.enterPackage(context.getIdentifier(identifier.Offset, identifier.Length));
                }
                try {
                    validateCode(packageDeclaration.Body);
                } finally {
                    for (int i = packageDeclaration.Identifiers.size() - 1; i >= 0; --i) {
                        context.MemberResolver.leavePackage();
                    }
                }
            }
        }

        private void validateInterfaceCode(InterfaceDeclarationNode interfaceDeclaration) {
            foreach (var m in interfaceDeclaration.Members) {
            	var node = (SyntaxNode)m;
                switch (m.TypeMemberKind) {
				case Method:
		            var methodBuilder = node.getUserData(typeof(MethodBuilder));
	                var member = MemberInfo.getInfo(methodBuilder);
	                foreach (var mi in member.getOverridenMembers(context.AnnotatedTypeSystem)) {
	                    var meth = mi.Method;
	                    if (methodBuilder.ReturnType.IsPrimitive != meth.ReturnType.IsPrimitive ||
	                 		!meth.ReturnType.isAssignableFrom(methodBuilder.ReturnType)) {
	                        context.addError(CompileErrorId.IncompatibleOverridingReturnType, node,
	                                BytecodeHelper.getDisplayName(methodBuilder.DeclaringType),
	                                BytecodeHelper.getDisplayName(methodBuilder),
	                                BytecodeHelper.getDisplayName(meth.DeclaringType));
	                    }
	                }
					break;
				// TODO: same checking for properties and indexers
				}
            }
        }

        private void validateClassCode(ClassDeclarationNode classDeclaration) {
            var typeBuilder = classDeclaration.getUserData(typeof(TypeBuilder));
            context.CurrentType = typeBuilder;
            context.MemberResolver.enterType(typeBuilder);
            try {
                var t = visitBaseTypes(typeBuilder, new HashSet<TypeInfo>());
                if (t != null) {
                    throw context.error(CompileErrorId.CircularBaseTypeDependency, classDeclaration,
                        BytecodeHelper.getDisplayName(typeBuilder), BytecodeHelper.getDisplayName(t));
                }
                
                if (!typeBuilder.IsAbstract) {
                    var abstractMethods =
                        typeBuilder.getBaseTypes().takeWhile(p => p.IsAbstract).selectMany(p => p.Methods).where(p => p.IsAbstract);
                    var nonAbstractMethods =
                        typeBuilder.Methods.concat(typeBuilder.getBaseClasses().selectMany(p => p.Methods)).where(p => !p.IsAbstract).toList();
                    foreach (var m in abstractMethods) {
                        if (!nonAbstractMethods.any(p => p.isOverriding(m))) {
                            context.addError(CompileErrorId.AbstractMethodNotImplemented, classDeclaration,
                                BytecodeHelper.getDisplayName(typeBuilder),
                                BytecodeHelper.getDisplayName(m.DeclaringType),
                                BytecodeHelper.getDisplayName(m));
                        }
                    }
                }
                
                foreach (var member in classDeclaration.Members) {
                    switch (member.TypeMemberKind) {
                    case Class:
                        validateClassCode((ClassDeclarationNode)member);
                        break;
                    case Method:
                        validateClassMethodCode((MethodDeclarationNode)member);
                        break;
                    case Field:
                        validateClassFieldCode((FieldDeclarationNode)member);
                        break;
                    case Indexer:
                        validateClassIndexerCode((IndexerDeclarationNode)member);
                        break;
                    case Property:
                        validateClassPropertyCode((PropertyDeclarationNode)member);
                        break;
                    case EnumConstant:
                        validateEnumConstantCode((EnumConstantDeclarationNode)member);
                        break;
                    case Constructor:
                        validateConstructorCode((ConstructorDeclarationNode)member);
                        break;
                    case Destructor:
                        validateDestructorCode((DestructorDeclarationNode)member);
                        break;
                    case Interface:
                    case Delegate:
                        break;
                    default:
                        throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                    }
                    context.CurrentType = typeBuilder;
                }
            } finally {
                context.MemberResolver.leaveType();
            }
        }

        private static TypeInfo visitBaseTypes(TypeInfo type, HashSet<TypeInfo> visited) {
            if (!visited.add(type)) {
                return type.IsInterface ? type : type.BaseType;
            }
            if (type.IsInterface) {
                foreach (var i in type.Interfaces) {
                    var t = visitBaseTypes(i, visited);
                    if (t != null) {
                        return i;
                    }
                }
                return null;
            } else {
                if (type.BaseType == null) {
                    return null;
                }
                return visitBaseTypes(type.BaseType, visited);
            }
        }

        private void validateDestructorCode(DestructorDeclarationNode destructorDeclaration) {
            var methodBuilder = destructorDeclaration.getUserData(typeof(MethodBuilder));
            methodBuilder.addOrReplaceUserData(destructorDeclaration);
            try {
                context.CodeValidationContext.enterMethod(methodBuilder);
                queryTranslator.translate(destructorDeclaration.Body);
                this.statementValidator.handleStatement(destructorDeclaration.Body, null);
                if (!context.HasErrors) {
                    validateControlFlow(methodBuilder, destructorDeclaration.Body, true);
                }
            } finally {
                context.CodeValidationContext.leaveMethod();
            }
        }

        private void validateConstructorCode(ConstructorDeclarationNode constructorDeclaration) {
            var methodBuilder = constructorDeclaration.getUserData(typeof(MethodBuilder));
            methodBuilder.addOrReplaceUserData(constructorDeclaration);
            var typeBuilder = (TypeBuilder)methodBuilder.DeclaringType;
            try {
                context.CodeValidationContext.enterMethod(methodBuilder);
                
                var initializer = constructorDeclaration.Initializer;
                var isStatic = constructorDeclaration.Modifiers.contains(Modifier.Static);
                if (initializer != null) {
					context.CodeValidationContext.IsStatic = true;
                    var initType = (initializer.IsSuper) ? typeBuilder.BaseType : typeBuilder;
                    var constructors = initType.Methods.where(p => p.Name.equals("<init>"));
                    foreach (var arg in initializer.Arguments) {
                        if (arg.ExpressionKind != ExpressionKind.Lambda) {
                            expressionValidator.handleExpression(arg, null, true);
                        }
                    }
					context.CodeValidationContext.IsStatic = false;
                    var constructor = expressionValidator.MethodResolver.resolveMethod(constructors, initializer.Arguments, context.TypeSystem.VoidType);
					if (constructor == null) {
                        context.addError(CompileErrorId.BaseMissingConstructor,
                            constructorDeclaration,
                            BytecodeHelper.getDisplayName(typeBuilder.BaseType), initializer.Arguments.size());
					}
                    var info = new ExpressionInfo(initType);
                    info.Method = constructor;
                    initializer.addUserData(info);
                } else if (!isStatic) {
                    // TODO: check overloading
                    MethodInfo base;
                    if (typeBuilder.IsEnum) {
                        var paramTypes = new ArrayList<TypeInfo> { context.TypeSystem.StringType, context.TypeSystem.IntType };
                        base = typeBuilder.BaseType.getMethod("<init>", paramTypes);
                    } else {
                        base = typeBuilder.getBaseType().getMethod("<init>", Query.empty<TypeInfo>());
                    }
                    if (base == null) {
                        context.addError(CompileErrorId.BaseMissingDefaultConstructor,
                            constructorDeclaration,
                            BytecodeHelper.getDisplayName(typeBuilder.BaseType));
                    } else {
                        var info = new ExpressionInfo(typeBuilder.BaseType);
                        info.Method = base;
                        constructorDeclaration.addUserData(info);
                    }
                }
                queryTranslator.translate(constructorDeclaration.Body);
                statementValidator.handleStatement(constructorDeclaration.Body, null);
                if (!context.HasErrors) {
                    validateControlFlow(methodBuilder, constructorDeclaration.Body, !isStatic);
                }
            } finally {
                context.CodeValidationContext.leaveMethod();
            }
        }

        private void validateEnumConstantCode(EnumConstantDeclarationNode enumConstant) {
            context.CodeValidationContext.IsStatic = true;
            foreach (var e in enumConstant.Arguments) {
                expressionValidator.handleExpression(e, null, true);
            }
            var fieldBuilder = enumConstant.getUserData(typeof(FieldBuilder));
            var typeBuilder = (TypeBuilder)fieldBuilder.DeclaringType;
            var constructors = typeBuilder.Methods.where(p => p.Name.equals("<init>"));
            var arguments = new ArrayList<ExpressionNode>();
            var arg1 = new SimpleNameExpressionNode();
            arg1.addUserData(new ExpressionInfo(context.TypeSystem.StringType));
            arguments.add(arg1);
            var arg2 = new SimpleNameExpressionNode();
            arg2.addUserData(new ExpressionInfo(context.TypeSystem.IntType));
            arguments.add(arg2);
            arguments.addAll(enumConstant.Arguments);
            var constructor = expressionValidator.MethodResolver.resolveMethod(constructors, arguments, context.TypeSystem.VoidType);
			if (constructor == null) {
				context.addError(CompileErrorId.NoAccessibleConstructors, enumConstant,
						BytecodeHelper.getDisplayName(typeBuilder));
			}
            var info = new ExpressionInfo(typeBuilder);
            info.setMethod(constructor);
            enumConstant.addUserData(info);
            var clinit = (MethodBuilder)typeBuilder.getMethod("<clinit>", Query.empty<TypeInfo>());
            if (clinit == null) {
                clinit = typeBuilder.defineMethod("<clinit>");
                clinit.setStatic(true);
                clinit.setReturnType(context.TypeSystem.VoidType);
            }
        }

        private void validateClassIndexerCode(IndexerDeclarationNode indexerDeclaration) {
            var get = indexerDeclaration.GetAccessor;
            var set = indexerDeclaration.SetAccessor;
            var getBuilder = (get == null) ? null : get.getUserData(typeof(MethodBuilder));
            var setBuilder = (set == null) ? null : set.getUserData(typeof(MethodBuilder));
            
            var memberInfo = MemberInfo.getInfo(getBuilder, setBuilder);
            indexerDeclaration.addUserData(memberInfo);

            foreach (var mi in memberInfo.getOverridenMembers(context.AnnotatedTypeSystem)) {
                if (mi.Type != memberInfo.Type) {
                    context.addError(CompileErrorId.IncompatibleOverridingReturnType, indexerDeclaration,
                            BytecodeHelper.getDisplayName(memberInfo.DeclaringType),
                            "this[]",
                            BytecodeHelper.getDisplayName(mi.DeclaringType));
                }
            }
            
            if (indexerDeclaration.Modifiers.contains(Modifier.Abstract) && !memberInfo.DeclaringType.IsAbstract) {
                context.addError(CompileErrorId.AbstractMemberInClass, indexerDeclaration,
	                    BytecodeHelper.getDisplayName(memberInfo.DeclaringType),
    	                BytecodeHelper.getIndexerDisplayName(getBuilder ?? setBuilder));
            }
            
            var overriden = memberInfo.getOverridenMembers(context.AnnotatedTypeSystem);
            if (indexerDeclaration.Modifiers.contains(Modifier.Override)) {
                var baseIndexer = overriden.where(p => !p.DeclaringType.IsInterface).firstOrDefault();
                if (baseIndexer == null) {
                    context.addError(CompileErrorId.NoIndexerToOverride, indexerDeclaration,
                        BytecodeHelper.getDisplayName((getBuilder ?? setBuilder).DeclaringType));
                } else {
                    var meth = baseIndexer.GetAccessor;
                    if (meth == null) {
                        if (get != null) {
                            context.addError(CompileErrorId.NoOverridableGetAccessor, get,
                                BytecodeHelper.getDisplayName(baseIndexer.DeclaringType) + "."
                                + BytecodeHelper.getIndexerDisplayName(getBuilder));
                        }
                        meth = baseIndexer.SetAccessor;
                    }
                    if (meth.IsFinal) {
                        context.addError(CompileErrorId.FinalIndexerOverride, indexerDeclaration,
                                BytecodeHelper.getDisplayName(baseIndexer.DeclaringType));
                    }
                    if (baseIndexer.SetAccessor == null) {
                        if (set != null) {
                            context.addError(CompileErrorId.NoOverridableSetAccessor, set,
                                BytecodeHelper.getDisplayName(baseIndexer.getDeclaringType()) + "."
                                + BytecodeHelper.getIndexerDisplayName(setBuilder));
                        }
                    }
                }
            } else {
                foreach (var mi in overriden) {
                    if (!mi.DeclaringType.IsInterface) {
                        context.addError(CompileErrorId.MissingOverrideIndexer, indexerDeclaration,
                            BytecodeHelper.getDisplayName(mi.DeclaringType));
                        break;
                    }
                }
            }

            validateAccessors(get, set);
        }

        private void validateClassPropertyCode(PropertyDeclarationNode propertyDeclaration) {
            var get = propertyDeclaration.GetAccessor;
            var set = propertyDeclaration.SetAccessor;
            var getBuilder = (get == null) ? null : get.getUserData(typeof(MethodBuilder));
            var setBuilder = (set == null) ? null : set.getUserData(typeof(MethodBuilder));

            var name = context.getIdentifier(propertyDeclaration.NameOffset, propertyDeclaration.NameLength);
            var memberInfo = MemberInfo.getInfo(getBuilder, setBuilder, name);
            propertyDeclaration.addUserData(memberInfo);

            foreach (var mi in memberInfo.getOverridenMembers(context.AnnotatedTypeSystem)) {
                if (mi.Type != memberInfo.Type) {
                    context.addError(CompileErrorId.IncompatibleOverridingReturnType, propertyDeclaration,
                            BytecodeHelper.getDisplayName(memberInfo.DeclaringType),
                            memberInfo.Name,
                            BytecodeHelper.getDisplayName(mi.DeclaringType));
                }
            }

            if (propertyDeclaration.Modifiers.contains(Modifier.Abstract) && !memberInfo.DeclaringType.IsAbstract) {
                context.addError(CompileErrorId.AbstractMemberInClass, propertyDeclaration,
	                    BytecodeHelper.getDisplayName(memberInfo.DeclaringType), name);
            }
            
            var overriden = memberInfo.getOverridenMembers(context.AnnotatedTypeSystem);
            if (propertyDeclaration.Modifiers.contains(Modifier.Override)) {
                MemberInfo baseProperty = overriden.where(p => !p.DeclaringType.IsInterface).firstOrDefault();
                if (baseProperty == null) {
                    context.addError(CompileErrorId.NoPropertyToOverride, propertyDeclaration,
                            BytecodeHelper.getDisplayName((getBuilder ?? setBuilder).DeclaringType), name);
                } else {
                    var meth = baseProperty.GetAccessor;
                    if (meth == null) {
                        if (get != null) {
                            context.addError(CompileErrorId.NoOverridableGetAccessor, get,
                                BytecodeHelper.getDisplayName(baseProperty.DeclaringType) + "."
                                + BytecodeHelper.getPropertyDisplayName(getBuilder, name));
                        }
                        meth = baseProperty.SetAccessor;
                    }
                    if (meth.IsFinal) {
                        context.addError(CompileErrorId.FinalPropertyOverride, propertyDeclaration,
                            BytecodeHelper.getDisplayName(baseProperty.DeclaringType), name);
                    }
                    if (baseProperty.SetAccessor == null) {
                        if (set != null) {
                            context.addError(CompileErrorId.NoOverridableSetAccessor, set,
                                BytecodeHelper.getDisplayName(baseProperty.DeclaringType) + "."
                                + BytecodeHelper.getPropertyDisplayName(getBuilder, name));
                        }
                    }
                }
            } else {
                foreach (var mi in overriden) {
                    if (!mi.DeclaringType.IsInterface) {
                        context.addError(CompileErrorId.MissingOverrideProperty, propertyDeclaration,
                                BytecodeHelper.getDisplayName(mi.DeclaringType));
                        break;
                    }
                }
            }
            
            validateAccessors(get, set);
        }

        private void validateAccessors(AccessorDeclarationNode get, AccessorDeclarationNode set) {
            if (get != null && get.Body != null) {
                var methodBuilder = get.getUserData(typeof(MethodBuilder));
                methodBuilder.addOrReplaceUserData(get);
                try {
                    context.CodeValidationContext.enterMethod(methodBuilder);

                    queryTranslator.translate(get.Body);
                    statementValidator.handleStatement(get.Body, null);
                    if (!context.HasErrors) {
                        validateControlFlow(methodBuilder, get.Body, true);
                    }
                } finally {
                    context.CodeValidationContext.leaveMethod();
                }
            }
            if (set != null && set.Body != null) {
                var methodBuilder = set.getUserData(typeof(MethodBuilder));
                methodBuilder.addOrReplaceUserData(set);
                try {
                    context.CodeValidationContext.enterMethod(methodBuilder);
                    methodBuilder.addUserData(context.MemberResolver.getLocal("value"));

                    queryTranslator.translate(set.Body);
                    statementValidator.handleStatement(set.Body, null);
                    if (!context.HasErrors) {
                        validateControlFlow(methodBuilder, set.Body, true);
                    }
                } finally {
                    context.CodeValidationContext.leaveMethod();
                }
            }
        }

        private void validateClassMethodCode(MethodDeclarationNode methodDeclaration) {
            var methodBuilder = methodDeclaration.getUserData(typeof(MethodBuilder));
            methodBuilder.addOrReplaceUserData(methodDeclaration);
            try {
                context.CodeValidationContext.enterMethod(methodBuilder);
                context.CodeValidationContext.IsStatic = methodBuilder.IsStatic;
            
                var member = MemberInfo.getInfo(methodBuilder);
                foreach (var mi in member.getOverridenMembers(context.AnnotatedTypeSystem)) {
                    var meth = mi.Method;
                    if (!meth.ReturnType.isAssignableFrom(methodBuilder.ReturnType)) {
                        context.addError(CompileErrorId.IncompatibleOverridingReturnType, methodDeclaration,
                                BytecodeHelper.getDisplayName(methodBuilder.DeclaringType),
                                BytecodeHelper.getDisplayName(methodBuilder),
                                BytecodeHelper.getDisplayName(meth.DeclaringType));
                    }
                    if ((meth.IsPublic && !methodBuilder.IsPublic) || methodBuilder.IsPrivate) {
		                context.addError(CompileErrorId.ReduceMethodVisibility, methodDeclaration,
			                    BytecodeHelper.getDisplayName(methodBuilder.DeclaringType),
			                    BytecodeHelper.getDisplayName(methodBuilder),
			                    BytecodeHelper.getDisplayName(meth.DeclaringType));
                    }
                }
                checkAccessibility(methodBuilder, methodDeclaration);
                checkOverloading(methodBuilder, methodDeclaration.Modifiers, methodDeclaration);
                if (methodDeclaration.Body != null) {
                    queryTranslator.translate(methodDeclaration.Body);
                    this.statementValidator.handleStatement(methodDeclaration.Body, null);
                    if (!context.HasErrors) {
                        validateControlFlow(methodBuilder, methodDeclaration.Body, true);
                    }
                } else if (!methodDeclaration.IsPartial && !methodBuilder.IsAbstract && !methodBuilder.IsNative) {
                    context.addError(CompileErrorId.MethodWithoutBody, methodDeclaration,
                        BytecodeHelper.getDisplayName(methodBuilder.DeclaringType), BytecodeHelper.getDisplayName(methodBuilder));
                } else if (methodBuilder.IsAbstract && !methodBuilder.DeclaringType.IsAbstract) {
                    context.addError(CompileErrorId.AbstractMemberInClass, methodDeclaration,
                        BytecodeHelper.getDisplayName(methodBuilder.DeclaringType), BytecodeHelper.getDisplayName(methodBuilder));
                }
            } finally {
                context.CodeValidationContext.leaveMethod();
            }
        }

        private void checkAccessibility(MethodInfo method, SyntaxNode node) {
            if (!method.IsPublic || !method.DeclaringType.IsPublic) {
                return;
            }
            if (!method.ReturnType.IsPrimitive && !method.ReturnType.IsPublic) {
                context.addError(CompileErrorId.InconsistentAccessibility, node,
                    BytecodeHelper.getDisplayName(method.ReturnType), method.Name);
            }
        }
        
        private void checkOverloading(MethodInfo method, EnumSet<Modifier> modifiers, SyntaxNode node) {
            var superMethod = method.DeclaringType.getBaseClasses().selectMany(p => p.Methods).firstOrDefault(p => method.isOverriding(p));
            if (superMethod != null) {
                if (modifiers.contains(Modifier.Virtual)) {
                    context.addError(CompileErrorId.IllegalVirtualMethod, node, BytecodeHelper.getDisplayName(method.DeclaringType),
                            BytecodeHelper.getDisplayName(method), BytecodeHelper.getDisplayName(superMethod.DeclaringType));
                }
                if (superMethod.IsFinal) {
					context.addError(CompileErrorId.IllegalOverrideMethod, node, BytecodeHelper.getDisplayName(method.DeclaringType),
							BytecodeHelper.getDisplayName(method), BytecodeHelper.getDisplayName(superMethod.DeclaringType));
                } else if (!modifiers.contains(Modifier.Override)) {
                    context.addError(CompileErrorId.MissingOverride, node, BytecodeHelper.getDisplayName(method.DeclaringType),
                            BytecodeHelper.getDisplayName(method), BytecodeHelper.getDisplayName(superMethod.DeclaringType));
                }
                if (!BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem, method.DeclaringType)
                        && BytecodeHelper.isDeprecated(context.AnnotatedTypeSystem, superMethod)) {
                    context.addWarning(CompileErrorId.OverridingDeprecated, node, BytecodeHelper.getDisplayName(method.DeclaringType),
                            BytecodeHelper.getDisplayName(method), BytecodeHelper.getDisplayName(superMethod.DeclaringType));
                }
            } else if (modifiers.contains(Modifier.Override)) {
                context.addError(CompileErrorId.NoMethodToOverride, node,
                        BytecodeHelper.getDisplayName(method.DeclaringType), BytecodeHelper.getDisplayName(method));
            }
        }
        
        private void validateControlFlow(MethodBuilder methodBuilder, BlockStatementNode body, bool insertReturn) {
            var uninitializedLocals = this.reachabilityChecker.checkMethod(methodBuilder, body, insertReturn);
            foreach (var li in uninitializedLocals) {
                if (li.assignmentCount > 0) {
                    if (li.referenceCount > 0) {
                        this.assignmentChecker.visitStatement(li);
                    } else {
                        context.addWarning(CompileErrorId.VariableNeverRead, li.declaration, li.local.Name);
                    }
                } else if (li.referenceCount > 0) {
                    context.addError(CompileErrorId.VariableUninitialized, li.declaration, li.local.Name);
                } else {
					li.local.Unused = true;
                    context.addWarning(CompileErrorId.VariableNeverUsed, li.declaration, li.local.Name);
                }
            }
        }
        
        private void validateClassFieldCode(FieldDeclarationNode fieldDeclaration) {
            var first = true;
            foreach (var decl in fieldDeclaration.Declarators) {
                if (decl.Value == null) {
                    continue;
                }
                var fieldBuilder = decl.getUserData(typeof(FieldBuilder));
                context.CodeValidationContext.IsStatic = fieldBuilder.IsStatic;
                decl.Value = queryTranslator.translate(decl.Value, first);
                expressionValidator.handleExpression(decl.Value, fieldBuilder.Type, true);
                ValidationHelper.setBoxing(context, fieldBuilder.Type, decl.Value);
                if (!ValidationHelper.isAssignable(context, fieldBuilder.Type, decl.Value)) {
                    var vinfo = decl.Value.getUserData(typeof(ExpressionInfo));
                    var vtype = (vinfo == null) ? null : vinfo.Type;
                    context.addError(CompileErrorId.NoImplicitConversion, decl.Value,
                        BytecodeHelper.getDisplayName(vtype),
                        BytecodeHelper.getDisplayName(fieldBuilder.Type));
                }
                if (fieldBuilder.IsStatic) {
                    var ei = decl.Value.getUserData(typeof(ExpressionInfo));
                    if (ei != null && (!ei.IsConstant || ei.BoxingKind != BoxingKind.None)) {
                        var typeBuilder = (TypeBuilder)fieldBuilder.DeclaringType;
                        var clinit = (MethodBuilder)typeBuilder.getMethod("<clinit>", Query.empty<TypeInfo>());
                        if (clinit == null) {
                            clinit = typeBuilder.defineMethod("<clinit>");
                            clinit.setStatic(true);
                            clinit.setReturnType(context.TypeSystem.VoidType);
                        }
                    }
                }
                first = false;
            }
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        // Bytecode generation
        
        private void generateBytecode() {
            foreach (var compilationUnit in context.CompilationUnits) {
                context.Text = compilationUnit.Text;
                context.Symbols = compilationUnit.Symbols;
                generateBytecode(compilationUnit.Body);
            }
        
            bytecodeGenerator.generateConstructorsBytecode();
        }

        private void generateBytecode(PackageBodyNode packageBody) {
            foreach (var member in packageBody.Members) {
                switch (member.PackageMemberKind) {
                case Package:
                    generateBytecode((PackageDeclarationNode)member);
                    break;
                case Class:
                	context.CodeValidationContext.LambdaCount = 0;
                    generateClassBytecode((ClassDeclarationNode)member);
                    break;
                case Delegate:
                    bytecodeGenerator.generateDelegateBytecode((DelegateDeclarationNode)member);
                    break;
                case Interface:
                    break;
                default:
                    throw new Exception("Internal error: unhandled member kind: " + member.PackageMemberKind);
                }
            }
        }

        private void generateBytecode(PackageDeclarationNode packageDeclaration) {
            if (packageDeclaration.Body != null) {
                generateBytecode(packageDeclaration.Body);
            }
        }

        private void generateClassBytecode(ClassDeclarationNode classDeclaration) {
            var typeBuilder = classDeclaration.getUserData(typeof(TypeBuilder));
            typeBuilder.setSourceFile(PathHelper.getFileName(classDeclaration.Filename));
            context.CurrentType = typeBuilder;
            
            if (typeBuilder.IsEnum) {
                bytecodeGenerator.generateImplicitEnumMembersBytecode(typeBuilder);
            }
            
            foreach (var member in classDeclaration.Members) {
                switch (member.TypeMemberKind) {
                case Class:
                    generateClassBytecode((ClassDeclarationNode)member);
                    break;
                case Method:
                    bytecodeGenerator.generateMethodBytecode((MethodDeclarationNode)member);
                    break;
                case Field:
                    bytecodeGenerator.generateFieldBytecode((FieldDeclarationNode)member);
                    break;
                case Delegate:
                    bytecodeGenerator.generateDelegateBytecode((DelegateDeclarationNode)member);
                    break;
                case Indexer:
                    bytecodeGenerator.generateIndexerBytecode((IndexerDeclarationNode)member);
                    break;
                case Property:
                    bytecodeGenerator.generatePropertyBytecode((PropertyDeclarationNode)member);
                    break;
                case Constructor:
                    bytecodeGenerator.generateConstructorBytecode((ConstructorDeclarationNode)member);
                    break;
                case Destructor:
                    bytecodeGenerator.generateDestructorBytecode((DestructorDeclarationNode)member);
                    break;
                case Interface:
                case EnumConstant:
                    break;
                default:
                    throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                }
                context.CurrentType = typeBuilder;
            }
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        // Documentation
        
        private void buildDocumentation(File file) {
            var doc = XmlHelper.load(new StringReader("<?xml version=\"1.0\"?><doc><members/></doc>"));
            this.documentationBuilder = new DocumentationBuilder(context, (org.w3c.dom.Element)doc.getDocumentElement().getFirstChild());
            foreach (var compilationUnit in context.CompilationUnits) {
                context.Text = compilationUnit.Text;
                context.MemberResolver.enterScope();
                try {
                    buildDocumentation("", compilationUnit.Body);
                } finally {
                    context.MemberResolver.leaveScope();
                }
            }
            OutputStream stream = null;
            try {
                stream = new FileOutputStream(file);
                XmlHelper.save(doc, stream);
            } finally {
                if (stream != null) {
                    stream.close();
                }
            }
        }

        private void buildDocumentation(String packageName, PackageBodyNode packageBody) {
            var packageInfo = packageBody.getUserData(typeof(PackageInfo));
            context.MemberResolver.usingDirective(packageInfo.memberInfos, packageInfo.packages, packageInfo.packageAliases);
            
            foreach (var member in packageBody.Members) {
                switch (member.PackageMemberKind) {
                case Package:
                    buildDocumentation(packageName, (PackageDeclarationNode)member);
                    break;
                case Class:
                    buildClassDocumentation((ClassDeclarationNode)member);
                    break;
                case Delegate:
                    buildDelegateDocumentation((DelegateDeclarationNode)member);
                    break;
                case Interface:
                    buildInterfaceDocumentation((InterfaceDeclarationNode)member);
                    break;
                default:
                    throw new Exception("Internal error: unhandled member kind: " + member.PackageMemberKind);
                }
            }
        }

        private void buildDocumentation(String packageName, PackageDeclarationNode packageDeclaration) {
            if (packageDeclaration.Body != null) {
                var name = getPackageName(packageName, packageDeclaration, '.');
                foreach (var identifier in packageDeclaration.Identifiers) {
                    context.MemberResolver.enterPackage(context.getIdentifier(identifier.Offset, identifier.Length));
                }
                try {
                    if (packageDeclaration.DocumentationLength > 0) {
                        documentationBuilder.buildPackageDocumentation(name, packageDeclaration);
                    }
                    buildDocumentation(name, packageDeclaration.Body);
                } finally {
                    for (int i = packageDeclaration.Identifiers.size() - 1; i >= 0; --i) {
                        context.MemberResolver.leavePackage();
                    }
                }
            }
        }

        private void buildClassDocumentation(ClassDeclarationNode classDeclaration) {
            var typeBuilder = classDeclaration.getUserData(typeof(TypeBuilder));
            context.CurrentType = typeBuilder;
            context.MemberResolver.enterType(typeBuilder);
            try {
                if (classDeclaration.DocumentationLength > 0) {
                    documentationBuilder.buildTypeDocumentation(typeBuilder, classDeclaration);
                }
                
                foreach (var member in classDeclaration.Members) {
                    switch (member.TypeMemberKind) {
                    case Class:
                        buildClassDocumentation((ClassDeclarationNode)member);
                        break;
                    case Method:
                        buildMethodDocumentation((MethodDeclarationNode)member);
                        break;
                    case Field:
                        buildFieldDocumentation((FieldDeclarationNode)member);
                        break;
                    case Delegate:
                        buildDelegateDocumentation((DelegateDeclarationNode)member);
                        break;
                    case Indexer:
                        buildIndexerDocumentation((IndexerDeclarationNode)member);
                        break;
                    case Property:
                        buildPropertyDocumentation((PropertyDeclarationNode)member);
                        break;
                    case Constructor:
                        buildConstructorDocumentation((ConstructorDeclarationNode)member);
                        break;
                    case Destructor:
                        buildDestructorDocumentation((DestructorDeclarationNode)member);
                        break;
                    case Interface:
                        buildInterfaceDocumentation((InterfaceDeclarationNode)member);
                        break;
                    case EnumConstant:
                        buildEnumConstantDocumentation((EnumConstantDeclarationNode)member);
                        break;
                    default:
                        throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                    }
                    context.CurrentType = typeBuilder;
                }
            } finally {
                context.MemberResolver.leaveType();
            }
        }

        private void buildInterfaceDocumentation(InterfaceDeclarationNode interfaceDeclaration) {
            var typeBuilder = interfaceDeclaration.getUserData(typeof(TypeBuilder));
            context.CurrentType = typeBuilder;
            context.MemberResolver.enterType(typeBuilder);
            try {
                if (interfaceDeclaration.DocumentationLength > 0) {
                    documentationBuilder.buildTypeDocumentation(typeBuilder, interfaceDeclaration);
                }
                
                foreach (var member in interfaceDeclaration.Members) {
                    switch (member.TypeMemberKind) {
                    case Method:
                        buildMethodDocumentation((MethodDeclarationNode)member);
                        break;
                    case Indexer:
                        buildIndexerDocumentation((IndexerDeclarationNode)member);
                        break;
                    case Property:
                        buildPropertyDocumentation((PropertyDeclarationNode)member);
                        break;
                    case Class:
                    case Delegate:
                    case Interface:
                    case Constructor:
                    case Destructor:
                    case EnumConstant:
                    case Field:
                    default:
                        throw new Exception("Internal error: unhandled member kind: " + member.TypeMemberKind);
                    }
                }
            } finally {
                context.MemberResolver.leaveType();
            }
        }

        private void buildDelegateDocumentation(DelegateDeclarationNode delegateDeclaration) {
            if (delegateDeclaration.DocumentationLength > 0) {
                var typeBuilder = delegateDeclaration.getUserData(typeof(TypeBuilder));
                context.CurrentType = typeBuilder;
                documentationBuilder.buildTypeDocumentation(typeBuilder, delegateDeclaration);
            }
        }
        
        private void buildFieldDocumentation(FieldDeclarationNode fieldDeclaration) {
            if (fieldDeclaration.DocumentationLength > 0) {
                foreach (var decl in fieldDeclaration.Declarators) {
                    var fieldBuilder = decl.getUserData(typeof(FieldBuilder));
                    documentationBuilder.buildFieldDocumentation(fieldBuilder, fieldDeclaration);
                }
            }
        }
        
        private void buildEnumConstantDocumentation(EnumConstantDeclarationNode enumConstantDeclaration) {
            if (enumConstantDeclaration.DocumentationLength > 0) {
                var fieldBuilder = enumConstantDeclaration.getUserData(typeof(FieldBuilder));
                documentationBuilder.buildFieldDocumentation(fieldBuilder, enumConstantDeclaration);
            }
        }
        
        private void buildConstructorDocumentation(ConstructorDeclarationNode constructorDeclaration) {
            if (constructorDeclaration.DocumentationLength > 0) {
                var methodBuilder = constructorDeclaration.getUserData(typeof(MethodBuilder));
                documentationBuilder.buildMethodDocumentation(methodBuilder, constructorDeclaration);
            }
        }
        
        private void buildDestructorDocumentation(DestructorDeclarationNode destructorDeclaration) {
            if (destructorDeclaration.DocumentationLength > 0) {
                var methodBuilder = destructorDeclaration.getUserData(typeof(MethodBuilder));
                documentationBuilder.buildMethodDocumentation(methodBuilder, destructorDeclaration);
            }
        }
        
        private void buildMethodDocumentation(MethodDeclarationNode methodDeclaration) {
            if (methodDeclaration.DocumentationLength > 0) {
                var methodBuilder = methodDeclaration.getUserData(typeof(MethodBuilder));
                documentationBuilder.buildMethodDocumentation(methodBuilder, methodDeclaration);
            }
        }
        
        private void buildIndexerDocumentation(IndexerDeclarationNode indexerDeclaration) {
            if (indexerDeclaration.DocumentationLength > 0) {
                var get = indexerDeclaration.GetAccessor;
                var set = indexerDeclaration.SetAccessor;
                var getBuilder = (get == null) ? null : get.getUserData(typeof(MethodBuilder));
                var setBuilder = (set == null) ? null : set.getUserData(typeof(MethodBuilder));
                documentationBuilder.buildIndexerDocumentation(getBuilder ?? setBuilder, indexerDeclaration);
            }
        }
        
        private void buildPropertyDocumentation(PropertyDeclarationNode propertyDeclaration) {
            if (propertyDeclaration.DocumentationLength > 0) {
                var get = propertyDeclaration.GetAccessor;
                var set = propertyDeclaration.SetAccessor;
                var getBuilder = (get == null) ? null : get.getUserData(typeof(MethodBuilder));
                var setBuilder = (set == null) ? null : set.getUserData(typeof(MethodBuilder));
                var name = propertyDeclaration.getUserData(typeof(MemberInfo)).Name;
                documentationBuilder.buildPropertyDocumentation(getBuilder ?? setBuilder, name, propertyDeclaration);
            }
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        
        private class PartialTypeInfo {
            TypeBuilder typeBuilder;
            HashMap<MethodBuilder, PartialMethodInfo> partialMethods;
            
            PartialTypeInfo(TypeBuilder typeBuilder) {
                this.typeBuilder = typeBuilder;
                this.partialMethods = new HashMap<MethodBuilder, PartialMethodInfo>();
            }
        }
        
        private class PartialMethodInfo {
            MethodDeclarationNode definingPart;
            MethodDeclarationNode implementingPart;
            
            PartialMethodInfo() {
            }
        }
    }
}
