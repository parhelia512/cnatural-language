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
using stab.lang;
using stab.tools.helpers;
using stab.tools.syntaxtree;

namespace stab.tools.parser {

    public class Parser {
        private IScanner scanner;
        private LexicalUnit lexicalUnit;
        private bool newLineOccured;
        private String savedFilename;
        private int savedLine;
        private int savedColumn;
		private int savedStartPosition;
        private IntIterable savedDisabledWarnings;
        private bool wasSingleLineDocComment;
        private int docCommentStartPosition;
        private int docCommentEndPosition;
 
        public Parser() {
        }
        
        public CompilationUnitNode parseCompilationUnit(IScanner scanner) {
            this.scanner = scanner;
            if (nextLexicalUnit(false) == LexicalUnit.EndOfStream) {
                return null;
            } else {
                return parseCompilationUnit();
            }
        }

        public ExpressionNode parseExpression(IScanner scanner) {
            this.scanner = scanner;
            if (nextLexicalUnit(false) == LexicalUnit.EndOfStream) {
                return null;
            } else {
                return parseExpression();
            }
        }

        private CompilationUnitNode parseCompilationUnit() {
            var compilationUnit = new CompilationUnitNode(scanner.Text) { Body = parsePackageBody(true, 0) };
			compilationUnit.StartPosition = 0;
			compilationUnit.EndPosition = sizeof(compilationUnit.Text);
			return compilationUnit;
        }

        private PackageBodyNode parsePackageBody(bool global, int startPosition) {
            var result = new PackageBodyNode { StartPosition = startPosition };
			startPosition = scanner.StartPosition;
            while (lexicalUnit == LexicalUnit.Keyword && scanner.Keyword == Keyword.Using) {
                if (!isIdentifier(nextLexicalUnit(true))) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                var usingDirective = new UsingDirectiveNode { StartPosition = startPosition };
                setScannerState(usingDirective);
                result.UsingDirectives.add(usingDirective);
                int sp = scanner.StartPosition;
                int len = getLexicalUnitLength();
                var restorePoint = this.createRestorePoint();
                if (nextLexicalUnit(true) == LexicalUnit.Assign) {
                    usingDirective.AliasOffset = sp;
                    usingDirective.AliasLength = len;
                    nextLexicalUnit(true);
                } else {
                    restore(restorePoint);
                }
                usingDirective.TypeOrPackage = parsePackageOrTypeName(true);
                usingDirective.EndPosition = parseSemiColon(false, false);
            }

            var done = false;
            var modifiers = EnumSet.noneOf(typeof(Modifier));
            var annotations = new ArrayList<AnnotationSectionNode>();
            do {
                switch (lexicalUnit) {
                case Keyword:
                case ContextualKeyword:
                    switch (scanner.Keyword) {
                    case Package:
                        if (modifiers.size() > 0) {
                            addError(ParseErrorId.PackageWithModifiers);
                            modifiers.clear();
                        }
                        var ns = new PackageDeclarationNode { StartPosition = scanner.StartPosition };
                        if (docCommentEndPosition > 0) {
                            ns.DocumentationOffset = docCommentStartPosition;
                            ns.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                            docCommentEndPosition = 0;
                        }
                        setScannerState(ns);
                        ns.Annotations.addAll(annotations);
                        annotations.clear();
                        do {
                            if (!isIdentifier(nextLexicalUnit(true))) {
                                throw error(ParseErrorId.IdentifierExpected);
                            }
                            var part = new SourceCodePartNode { Offset = scanner.StartPosition, Length = getLexicalUnitLength() };
                            setScannerState(part);
                            ns.Identifiers.add(part);
                        } while (nextLexicalUnit(true) == LexicalUnit.Dot);
                        if (lexicalUnit != LexicalUnit.OpenBrace) {
                            throw error(ParseErrorId.OpenBraceExpected);
                        }
						startPosition = scanner.StartPosition;
                        if (nextLexicalUnit(true) != LexicalUnit.CloseBrace) {
                            ns.Body = parsePackageBody(false, startPosition);
                        }
                        if (lexicalUnit != LexicalUnit.CloseBrace) {
                            throw error(ParseErrorId.CloseBraceExpected);
                        }
						ns.EndPosition = scanner.EndPosition;
                        if (nextLexicalUnit(false) == LexicalUnit.SemiColon) {
                            nextLexicalUnit(false);
						}
                        result.Members.add(ns);
                        break;

                    case Public:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        addModifier(modifiers, Modifier.Public);
                        nextLexicalUnit(true);
                        break;

                    case Protected:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        addModifier(modifiers, Modifier.Protected);
                        nextLexicalUnit(true);
                        break;

                    case Private:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        addModifier(modifiers, Modifier.Private);
                        nextLexicalUnit(true);
                        break;

                    case Abstract:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        addModifier(modifiers, Modifier.Abstract);
                        nextLexicalUnit(true);
                        break;

                    case Final:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        addModifier(modifiers, Modifier.Final);
                        nextLexicalUnit(true);
                        break;

                    case Static:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        addModifier(modifiers, Modifier.Static);
                        nextLexicalUnit(true);
                        break;

                    case Strictfp:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        addModifier(modifiers, Modifier.Strictfp);
                        nextLexicalUnit(true);
                        break;

                    case Partial:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        switch (nextLexicalUnit(true)) {
                        case Keyword:
                            switch (scanner.Keyword) {
                            case Class:
                                nextLexicalUnit(true);
                                result.Members.add(parseClass(annotations, modifiers, false, true, startPosition));
                                modifiers.clear();
                                annotations.clear();
                                break;

                            case Interface:
                                nextLexicalUnit(true);
                                result.Members.add(parseInterface(annotations, modifiers, true, startPosition));
                                modifiers.clear();
                                annotations.clear();
                                break;

                            case Enum:
                                nextLexicalUnit(true);
                                result.Members.add(parseClass(annotations, modifiers, true, true, startPosition));
                                modifiers.clear();
                                annotations.clear();
                                break;
                                
                            default:
                                throw error(ParseErrorId.ClassInterfaceEnumExpected);
                            }
                            break;

                        default:
                            throw error(ParseErrorId.ClassInterfaceEnumExpected);
                        }
                        break;

                    case Class:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        nextLexicalUnit(true);
                        result.Members.add(parseClass(annotations, modifiers, false, false, startPosition));
                        modifiers.clear();
                        annotations.clear();
                        break;

                    case Interface:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        nextLexicalUnit(true);
                        result.Members.add(parseInterface(annotations, modifiers, false, startPosition));
                        modifiers.clear();
                        annotations.clear();
                        break;

                    case Enum:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        nextLexicalUnit(true);
                        result.Members.add(parseClass(annotations, modifiers, true, false, startPosition));
                        modifiers.clear();
                        annotations.clear();
                        break;

                    case Delegate:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        nextLexicalUnit(true);
                        result.Members.add(parseDelegate(annotations, modifiers, startPosition));
                        modifiers.clear();
                        annotations.clear();
                        break;

                    default:
                        throw error(ParseErrorId.ClassInterfaceEnumDelegateExpected);
                    }
                    break;

                case OpenBracket:
                    if (modifiers.size() > 0) {
                        throw error(ParseErrorId.ClassInterfaceEnumDelegateExpected);
                    }
                    annotations.add(parseAnnotationSection());
                    break;

                case CloseBrace:
                    if (global) {
                        throw error(ParseErrorId.ClassInterfaceEnumDelegateExpected);
                    } else {
                        if (modifiers.size() > 0 || annotations.size() > 0) {
                            throw error(ParseErrorId.ClassInterfaceEnumDelegateExpected);
                        }
                        done = true;
                    }
                    break;

                case EndOfStream:
                    if (!global) {
                        throw error(ParseErrorId.ClassInterfaceEnumDelegateExpected);
                    } else {
                        if (modifiers.size() > 0 || annotations.size() > 0) {
                            throw error(ParseErrorId.ClassInterfaceEnumDelegateExpected);
                        }
                        done = true;
                    }
                    break;

                default:
                    throw error(ParseErrorId.NoPackageMembers);
                }
            } while (!done);
			result.EndPosition = scanner.EndPosition;
            return result;
        }

        private ClassDeclarationNode parseClass(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers, bool isEnum,
                bool partial, int startPosition) {
            if (!isIdentifier(lexicalUnit)) {
                throw error(ParseErrorId.IdentifierExpected);
            }
            var result = new ClassDeclarationNode { IsEnum = isEnum, IsPartial = partial,
                    NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength(), StartPosition = startPosition };
            if (docCommentEndPosition > 0) {
                result.DocumentationOffset = docCommentStartPosition;
                result.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                docCommentEndPosition = 0;
            }
            setScannerState(result);
            result.Modifiers.addAll(modifiers);
            result.Annotations.addAll(annotations);
            nextLexicalUnit(true);
            if (!isEnum) {
                parseTypeParameters(result.TypeParameters);
            }
            parseClassBase(result.ClassBase);
            if (!isEnum) {
                parseTypeParameterConstraintsClauses(result.ConstraintsClauses);
            }
            if (lexicalUnit != LexicalUnit.OpenBrace) {
                throw error(ParseErrorId.OpenBraceExpected);
            }
            result.EndPosition = parseClassBody(annotations, modifiers, isEnum, result.Members);
            if (lexicalUnit == LexicalUnit.SemiColon) {
                nextLexicalUnit(false);
            }
            return result;
        }

        private int parseClassBody(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers, bool isEnum,
                List<ITypeMember> members) {
			int startPosition = 0;
            if (nextLexicalUnit(true, LexicalUnit.CloseBrace) != LexicalUnit.CloseBrace) {
                var done = false;
                modifiers.clear();
                annotations.clear();
                if (isEnum && lexicalUnit != LexicalUnit.SemiColon) {
                    do {
                        while (lexicalUnit == LexicalUnit.OpenBracket) {
                            annotations.add(parseAnnotationSection());
                        }
                        if (!isIdentifier(lexicalUnit)) {
                            throw error(ParseErrorId.IdentifierExpected);
                        }
                        members.add(parseEnumConstant(annotations));
                        switch (lexicalUnit) {
                        case Comma:
                            if (nextLexicalUnit(true) == LexicalUnit.CloseBrace) {
								int result = scanner.EndPosition;
                                if (nextLexicalUnit(false) == LexicalUnit.SemiColon) {
                                    nextLexicalUnit(false);
                                }
                                return result;
                            }
                            break;
                            
                        case CloseBrace:
							int result = scanner.EndPosition;
                            if (nextLexicalUnit(false) == LexicalUnit.SemiColon) {
                                nextLexicalUnit(false);
                            }
                            return result;
                            
                        case SemiColon:
                            done = true;
                            nextLexicalUnit(true);
                            break;
                        }
                    } while (!done);
                    done = false;
                }
                do {
                    while (lexicalUnit == LexicalUnit.OpenBracket) {
                        annotations.add(parseAnnotationSection());
                    }
                    switch (lexicalUnit) {
                    case Keyword:
                    case ContextualKeyword:
                        switch (scanner.Keyword) {
						case Public:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Public);
                            nextLexicalUnit(true);
                            break;

                        case Protected:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Protected);
                            nextLexicalUnit(true);
                            break;

                        case Private:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Private);
                            nextLexicalUnit(true);
                            break;

                        case Abstract:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Abstract);
                            nextLexicalUnit(true);
                            break;

                        case Override:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Override);
                            nextLexicalUnit(true);
                            break;

                        case Virtual:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Virtual);
                            nextLexicalUnit(true);
                            break;

                        case Native:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Native);
                            nextLexicalUnit(true);
                            break;

                        case Final:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Final);
                            nextLexicalUnit(true);
                            break;

                        case Static:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Static);
                            nextLexicalUnit(true);
                            break;

                        case Synchronized:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Synchronized);
                            nextLexicalUnit(true);
                            break;

                        case Transient:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Transient);
                            nextLexicalUnit(true);
                            break;

                        case Volatile:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            addModifier(modifiers, Modifier.Volatile);
                            nextLexicalUnit(true);
                            break;

                        case Partial:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            switch (nextLexicalUnit(true)) {
                            case Keyword:
                                switch (scanner.Keyword) {
                                case Class:
                                    nextLexicalUnit(true);
                                    members.add(parseClass(annotations, modifiers, false, true, startPosition));
                                    modifiers.clear();
                                    annotations.clear();
                                    break;

                                case Interface:
                                    nextLexicalUnit(true);
                                    members.add(parseInterface(annotations, modifiers, true, startPosition));
                                    modifiers.clear();
                                    annotations.clear();
                                    break;

                                case Enum:
                                    nextLexicalUnit(true);
                                    members.add(parseClass(annotations, modifiers, true, true, startPosition));
                                    modifiers.clear();
                                    annotations.clear();
                                    break;
                                    
                                default:
                                    throw error(ParseErrorId.ClassInterfaceEnumExpected);
                                }
                                break;

                            default:
                                members.add(parseClassMember(annotations, modifiers, true, startPosition));
                                modifiers.clear();
                                annotations.clear();
                                break;
                            }
                            break;

                        case Class:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            nextLexicalUnit(true);
                            members.add(parseClass(annotations, modifiers, false, false, startPosition));
                            modifiers.clear();
                            annotations.clear();
                            break;

                        case Interface:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            nextLexicalUnit(true);
                            members.add(parseInterface(annotations, modifiers, false, startPosition));
                            modifiers.clear();
                            annotations.clear();
                            break;

                        case Delegate:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            nextLexicalUnit(true);
                            members.add(parseDelegate(annotations, modifiers, startPosition));
                            modifiers.clear();
                            annotations.clear();
                            break;

                        case Enum:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            nextLexicalUnit(true);
                            members.add(parseClass(annotations, modifiers, true, false, startPosition));
                            modifiers.clear();
                            annotations.clear();
                            break;

                        default:
							if (modifiers.size() == 0) {
								startPosition = scanner.StartPosition;
							}
                            members.add(parseClassMember(annotations, modifiers, false, startPosition));
                            modifiers.clear();
                            annotations.clear();
                            break;
                        }
                        break;

                    case CloseBrace:
                        if (modifiers.size() > 0 || annotations.size() > 0) {
                            throw error(ParseErrorId.ClassInterfaceEnumDelegateExpected);
                        }
                        done = true;
                        break;

                    case Complement:
                        nextLexicalUnit(true);
                        members.add(parseDestructorDeclaration(annotations, modifiers));
                        modifiers.clear();
                        annotations.clear();
                        break;

                    case Identifier:
						if (modifiers.size() == 0) {
							startPosition = scanner.StartPosition;
						}
                        members.add(parseClassMember(annotations, modifiers, false, startPosition));
                        modifiers.clear();
                        annotations.clear();
                        break;

                    default:
                        throw error(ParseErrorId.ClassInterfaceEnumDelegateExpected);
                    }
                } while (!done);
            }
            if (lexicalUnit != LexicalUnit.CloseBrace) {
                throw error(ParseErrorId.CloseBraceExpected);
            }
            docCommentEndPosition = 0;
			int result = scanner.EndPosition;
            nextLexicalUnit(false);
			return result;
        }

        private void addModifier(EnumSet<Modifier> modifiers, Modifier modifier) {
            if (modifiers.contains(modifier)) {
                addError(ParseErrorId.DuplicateModifier, modifier.toString().toLowerCase());
            } else {
                modifiers.add(modifier);
            }
        }

        private ITypeMember parseClassMember(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers, bool partial,
        		int startPosition) {
            var restorePoint = this.createRestorePoint();
            int sp = scanner.StartPosition;
            int len = getLexicalUnitLength();
            saveScannerState();
            switch (nextLexicalUnit(true)) {
            case OpenParenthesis:
                nextLexicalUnit(true);
                return parseConstructorDeclaration(annotations, modifiers, partial, sp, len,
                    Collections.emptyList<SimpleNameTypeReferenceNode>(), startPosition);
            case LessThan:
                var typeParameters = new ArrayList<SimpleNameTypeReferenceNode>();
                if (parseTypeParameters(typeParameters, false)) {
                    if (lexicalUnit == LexicalUnit.OpenParenthesis) {
                        return parseConstructorDeclaration(annotations, modifiers, partial, sp, len, typeParameters, startPosition);
                    }
                }
                goto default;
                
            default:
                this.restore(restorePoint);
                var type = parseType(true, true);
                if (lexicalUnit == LexicalUnit.Keyword && scanner.Keyword == Keyword.This) {
                    saveScannerState();
                    nextLexicalUnit(true);
                    return parseIndexerDeclaration(annotations, modifiers, partial, type, false, startPosition);
                }
                return parseFieldOrPropertyOrMethod(annotations, modifiers, partial, type, startPosition);
            }
        }

        private InterfaceDeclarationNode parseInterface(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers,
                bool partial, int startPosition) {
            if (!isIdentifier(lexicalUnit)) {
                throw error(ParseErrorId.IdentifierExpected);
            }
            var declaration = new InterfaceDeclarationNode { IsPartial = partial,
                NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength(), StartPosition = startPosition };
            if (docCommentEndPosition > 0) {
                declaration.DocumentationOffset = docCommentStartPosition;
                declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                docCommentEndPosition = 0;
            }
            setScannerState(declaration);
            declaration.Modifiers.addAll(modifiers);
            declaration.Annotations.addAll(annotations);
            nextLexicalUnit(true);
            parseTypeParameters(declaration.TypeParameters);
            parseClassBase(declaration.InterfaceBase);
            parseTypeParameterConstraintsClauses(declaration.ConstraintsClauses);
            if (lexicalUnit != LexicalUnit.OpenBrace) {
                throw error(ParseErrorId.OpenBraceExpected);
            }
            if (nextLexicalUnit(true) != LexicalUnit.CloseBrace) {
                var done = false;
                annotations.clear();
                do {
                    while (lexicalUnit == LexicalUnit.OpenBracket) {
                        annotations.add(parseAnnotationSection());
                    }
                    switch (lexicalUnit) {
                    case CloseBrace:
                        if (annotations.size() > 0) {
                            addError(ParseErrorId.TypeExpected);
                            annotations.clear();
                        }
                        done = true;
                        break;

                    case Keyword:
                    case ContextualKeyword:
                    case Identifier:
                        declaration.Members.add(parseInterfaceMember(annotations, modifiers, startPosition));
                        modifiers.clear();
                        annotations.clear();
                        break;

                    default:
                        throw error(ParseErrorId.CloseBraceExpected);
                    }
                } while (!done);
            }
            if (lexicalUnit != LexicalUnit.CloseBrace) {
                throw error(ParseErrorId.CloseBraceExpected);
            }
            docCommentEndPosition = 0;
			declaration.EndPosition = scanner.EndPosition;
            if (nextLexicalUnit(false) == LexicalUnit.SemiColon) {
				declaration.EndPosition = scanner.EndPosition;
                nextLexicalUnit(false);
            }
            return declaration;
        }

		private Modifier checkSetterAccess() {
			Modifier setterAccess = Modifier.Public;
			if (lexicalUnit == LexicalUnit.Xor) {
				setterAccess = Modifier.Private;
                nextLexicalUnit(true);			
			} else if (lexicalUnit == LexicalUnit.Plus) {
				setterAccess = Modifier.Protected;
                nextLexicalUnit(true);			
			}
			return setterAccess;
		}
		
        private ITypeMember parseFieldOrPropertyOrMethod(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers,
                bool partial, TypeReferenceNode type, int startPosition) {
            if (!isIdentifier(lexicalUnit)) {
                throw error(ParseErrorId.IdentifierExpected);
            }
            int sp = scanner.StartPosition;
            int len = getLexicalUnitLength();
            saveScannerState();
            var typeParameters = new ArrayList<SimpleNameTypeReferenceNode>();
            nextLexicalUnit(true);
            parseTypeParameters(typeParameters);
			Modifier setterAccess = checkSetterAccess();
			bool forceAsField = false;
			if (lexicalUnit == LexicalUnit.Multiply) {
				forceAsField = true;
                nextLexicalUnit(true);			
			}
            switch (lexicalUnit) {
            case OpenParenthesis:
				if (setterAccess != Modifier.Public || forceAsField) {
					throw error(ParseErrorId.IdentifierExpected);
				}
                var methodDeclaration = new MethodDeclarationNode { IsPartial = partial, ReturnType = type, NameOffset = sp, NameLength = len,
						StartPosition = startPosition };
                if (docCommentEndPosition > 0) {
                    methodDeclaration.DocumentationOffset = docCommentStartPosition;
                    methodDeclaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                    docCommentEndPosition = 0;
                }
                setSavedScannerState(methodDeclaration);
                methodDeclaration.Modifiers.addAll(modifiers);
                methodDeclaration.Annotations.addAll(annotations);
                foreach (var t in typeParameters) {
                    if (t.TypeReferenceKind != TypeReferenceKind.SimpleName || t.TypeArguments.size() > 0) {
                        throw error(ParseErrorId.SimpleNameExpected);
                    }
                    methodDeclaration.TypeParameters.add((SimpleNameTypeReferenceNode)t);
                }

                if (nextLexicalUnit(true) != LexicalUnit.CloseParenthesis) {
                    parseFormalParameters(methodDeclaration.Parameters, LexicalUnit.CloseParenthesis);
                } else {
                    nextLexicalUnit(true);
                }
                parseTypeParameterConstraintsClauses(methodDeclaration.ConstraintsClauses);
                switch (lexicalUnit) {
                case OpenBrace:
                    methodDeclaration.Body = parseBlockStatement();
					methodDeclaration.EndPosition = methodDeclaration.Body.EndPosition;
                    break;

                case SemiColon:
					methodDeclaration.EndPosition = scanner.EndPosition;
                    nextLexicalUnit(true);
                    break;

                default:
                    throw error(ParseErrorId.OpenBraceExpected);
                }
                return methodDeclaration;

            case OpenBrace:
				if (setterAccess != Modifier.Public || forceAsField) {
					throw error(ParseErrorId.IdentifierExpected);
				}
                var propertyDeclaration = new PropertyDeclarationNode { Type = type, NameOffset = sp, NameLength = len,
						StartPosition = startPosition };
                if (docCommentEndPosition > 0) {
                    propertyDeclaration.DocumentationOffset = docCommentStartPosition;
                    propertyDeclaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                    docCommentEndPosition = 0;
                }
                setSavedScannerState(propertyDeclaration);
                propertyDeclaration.Modifiers.addAll(modifiers);
                nextLexicalUnit(true);
                parseAccessorDeclaration(propertyDeclaration, false);
                if (lexicalUnit != LexicalUnit.CloseBrace) {
                    parseAccessorDeclaration(propertyDeclaration, false);
                }
                docCommentEndPosition = 0;
				propertyDeclaration.EndPosition = scanner.EndPosition;
                nextLexicalUnit(false);
                return propertyDeclaration;

            case Assign: {
				if (setterAccess != Modifier.Public || forceAsField) {
					throw error(ParseErrorId.IdentifierExpected);
				}
                var declaration = new FieldDeclarationNode { Type = type, StartPosition = startPosition };
                if (docCommentEndPosition > 0) {
                    declaration.DocumentationOffset = docCommentStartPosition;
                    declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                    docCommentEndPosition = 0;
                }
                setSavedScannerState(declaration);
                declaration.Modifiers.addAll(modifiers);
                declaration.Annotations.addAll(annotations);
                nextLexicalUnit(true);
                var decl = new VariableDeclaratorNode { NameOffset = sp, NameLength = len };
                setSavedScannerState(decl);
                decl.Value = parseFieldInitializer();
                decl.EndPosition = decl.Value.EndPosition;
                declaration.Declarators.add(decl);
                while (lexicalUnit == LexicalUnit.Comma) {
                    if (!isIdentifier(nextLexicalUnit(true))) {
                        throw error(ParseErrorId.IdentifierExpected);
                    }
                    decl = new VariableDeclaratorNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() };
                    setScannerState(decl);
                    decl.EndPosition = scanner.EndPosition;
                    declaration.Declarators.add(decl);
                    if (nextLexicalUnit(true) == LexicalUnit.Assign) {
                        nextLexicalUnit(true);
                        decl.Value = parseFieldInitializer();
                        decl.EndPosition = decl.Value.EndPosition;
                    }
                }
                docCommentEndPosition = 0;
                declaration.EndPosition = parseSemiColon(false, false);
                return declaration;
            }

            case Comma: {
				if (setterAccess != Modifier.Public || forceAsField) {
					throw error(ParseErrorId.IdentifierExpected);
				}
                var declaration = new FieldDeclarationNode { Type = type, StartPosition = startPosition };
                if (docCommentEndPosition > 0) {
                    declaration.DocumentationOffset = docCommentStartPosition;
                    declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                    docCommentEndPosition = 0;
                }
                setSavedScannerState(declaration);
                declaration.Modifiers.addAll(modifiers);
                declaration.Annotations.addAll(annotations);
                var decl = new VariableDeclaratorNode { NameOffset = sp, NameLength = len };
                setSavedScannerState(decl);
                decl.EndPosition = sp + len;
                declaration.Declarators.add(decl);
                do {
                    if (!isIdentifier(nextLexicalUnit(true))) {
                        throw error(ParseErrorId.IdentifierExpected);
                    }
                    decl = new VariableDeclaratorNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() };
                    setScannerState(decl);
                    declaration.Declarators.add(decl);
                    decl.EndPosition = scanner.EndPosition;
                    if (nextLexicalUnit(true) == LexicalUnit.Assign) {
                        nextLexicalUnit(true);
                        decl.Value = parseFieldInitializer();
                        decl.EndPosition = decl.Value.EndPosition;
                    }
                } while (lexicalUnit == LexicalUnit.Comma);
                docCommentEndPosition = 0;
                declaration.EndPosition = parseSemiColon(false, false);
                return declaration;
            }

            case SemiColon: {
				if ((modifiers.contains(Modifier.Public) || modifiers.contains(Modifier.Protected)) && !forceAsField) {
					var propertyShortDeclaration = new PropertyDeclarationNode { Type = type, NameOffset = sp, NameLength = len,
							StartPosition = startPosition };
					if (docCommentEndPosition > 0) {
						propertyShortDeclaration.DocumentationOffset = docCommentStartPosition;
						propertyShortDeclaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
						docCommentEndPosition = 0;
					}
					setSavedScannerState(propertyShortDeclaration);
					propertyShortDeclaration.Modifiers.addAll(modifiers);
					fakeParseShortProperty(propertyShortDeclaration, setterAccess, false);
					docCommentEndPosition = 0;
					propertyShortDeclaration.EndPosition = scanner.EndPosition;
					nextLexicalUnit(false);
					return propertyShortDeclaration;
				}
                var declaration = new FieldDeclarationNode { Type = type, StartPosition = startPosition };
                if (docCommentEndPosition > 0) {
                    declaration.DocumentationOffset = docCommentStartPosition;
                    declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                    docCommentEndPosition = 0;
                }
                setSavedScannerState(declaration);
                declaration.Modifiers.addAll(modifiers);
                declaration.Annotations.addAll(annotations);
                var decl = new VariableDeclaratorNode { NameOffset = sp, NameLength = len };
                setSavedScannerState(decl);
                decl.EndPosition = sp + len;
                declaration.Declarators.add(decl);
                docCommentEndPosition = 0;
				declaration.EndPosition = scanner.EndPosition;
                nextLexicalUnit(false);
                return declaration;
            }

            default:
                throw error(ParseErrorId.UnexpectedLexicalUnit);
            }
        }
        
        private ExpressionNode parseFieldInitializer() {
            if (lexicalUnit == LexicalUnit.OpenBrace) {
            	saveScannerState();
                nextLexicalUnit(true);
                return parseArrayInitializer();
            } else {
                return parseExpression();
            }
        }

        private ITypeMember parseEnumConstant(List<AnnotationSectionNode> annotations) {
            var result = new EnumConstantDeclarationNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength(),
					StartPosition = scanner.StartPosition };
            if (docCommentEndPosition > 0) {
                result.DocumentationOffset = docCommentStartPosition;
                result.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                docCommentEndPosition = 0;
            }
            setScannerState(result);
            result.Annotations.addAll(annotations);
            int endPosition = scanner.EndPosition;
            if (nextLexicalUnit(true) == LexicalUnit.OpenParenthesis) {
                if (nextLexicalUnit(true) != LexicalUnit.CloseParenthesis) {
                    endPosition = parseArguments(result.Arguments);
                } else {
                	endPosition = scanner.EndPosition;
                    nextLexicalUnit(true);
                }
            }
			result.EndPosition = endPosition;
            return result;
        }
        
        private ITypeMember parseConstructorDeclaration(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers,
                bool partial, int position, int length, List<SimpleNameTypeReferenceNode> typeParameters, int startPosition) {
            var declaration = new ConstructorDeclarationNode { NameOffset = position, NameLength = length, StartPosition = startPosition };
            if (docCommentEndPosition > 0) {
                declaration.DocumentationOffset = docCommentStartPosition;
                declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                docCommentEndPosition = 0;
            }
            setSavedScannerState(declaration);
            declaration.Modifiers.addAll(modifiers);
            declaration.Annotations.addAll(annotations);
            declaration.TypeParameters.addAll(typeParameters);
            if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                parseFormalParameters(declaration.Parameters, LexicalUnit.CloseParenthesis);
            } else {
                nextLexicalUnit(true);
            }
            if (lexicalUnit == LexicalUnit.Colon) {
                if (modifiers.contains(Modifier.Static)) {
                    throw error(ParseErrorId.UnexpectedLexicalUnit);
                }
                bool isSuper = false;
                switch (nextLexicalUnit(true)) {
                case Keyword:
                    switch (scanner.Keyword) {
                    case Super:
                        isSuper = true;
                        break;

                    case This:
                        break;

                    default:
                        throw error(ParseErrorId.SuperOrThisExpected);
                    }
                    break;

                default:
                    throw error(ParseErrorId.SuperOrThisExpected);
                }
                saveScannerState();
                if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                    throw error(ParseErrorId.OpenParenthesisExpected);
                }
                var initializer = new ConstructorInitializerNode();
                setSavedScannerState(initializer);
                initializer.IsSuper = isSuper;
                declaration.Initializer = initializer;
                nextLexicalUnit(true);
                initializer.EndPosition = parseArguments(initializer.Arguments);
            }
            parseTypeParameterConstraintsClauses(declaration.ConstraintsClauses);
            switch (lexicalUnit) {
            case OpenBrace:
                declaration.Body = parseBlockStatement();
				declaration.EndPosition = declaration.Body.EndPosition;
                break;

            case SemiColon:
				declaration.EndPosition = scanner.StartPosition;
                docCommentEndPosition = 0;
                nextLexicalUnit(true);
                break;

            default:
                throw error(ParseErrorId.OpenBraceExpected);
            }
            return declaration;
        }
        
        private ITypeMember parseDestructorDeclaration(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers) {
            if (!isIdentifier(lexicalUnit)) {
                throw error(ParseErrorId.IdentifierExpected);
            }
            var declaration = new DestructorDeclarationNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() };
            if (docCommentEndPosition > 0) {
                declaration.DocumentationOffset = docCommentStartPosition;
                declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                docCommentEndPosition = 0;
            }
            setScannerState(declaration);
            declaration.Modifiers.addAll(modifiers);
            declaration.Annotations.addAll(annotations);
            if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                throw error(ParseErrorId.OpenParenthesisExpected);
            }
            if (nextLexicalUnit(true) != LexicalUnit.CloseParenthesis) {
                throw error(ParseErrorId.CloseParenthesisExpected);
            }
            nextLexicalUnit(true);
            switch (lexicalUnit) {
            case OpenBrace:
                declaration.Body = parseBlockStatement();
				declaration.EndPosition = declaration.Body.EndPosition;
                break;

            case SemiColon:
				declaration.EndPosition = scanner.StartPosition;
                docCommentEndPosition = 0;
                nextLexicalUnit(true);
                break;

            default:
                throw error(ParseErrorId.OpenBraceExpected);
            }
            return declaration;
        }
        
        private void parseClassBase(List<TypeReferenceNode> classBase) {
            if (lexicalUnit != LexicalUnit.Colon) {
                return;
            }
            nextLexicalUnit(true);
            classBase.add(parseClassType(true));
            while (lexicalUnit == LexicalUnit.Comma) {
                nextLexicalUnit(true);
                classBase.add(parseInterfaceType(true));
            }
        }
        
        private DelegateDeclarationNode parseDelegate(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers, int startPosition) {
            var declaration = new DelegateDeclarationNode { ReturnType = parseType(true, true), StartPosition = startPosition };
            if (docCommentEndPosition > 0) {
                declaration.DocumentationOffset = docCommentStartPosition;
                declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                docCommentEndPosition = 0;
            }
            declaration.Modifiers.addAll(modifiers);
            declaration.Annotations.addAll(annotations);
            if (!isIdentifier(lexicalUnit)) {
                throw error(ParseErrorId.IdentifierExpected);
            }
            setScannerState(declaration);
            declaration.NameOffset = scanner.StartPosition;
            declaration.NameLength = getLexicalUnitLength();
            nextLexicalUnit(true);
            parseTypeParameters(declaration.TypeParameters);
            if (lexicalUnit != LexicalUnit.OpenParenthesis) {
                throw error(ParseErrorId.OpenParenthesisExpected);
            }
            if (nextLexicalUnit(true) != LexicalUnit.CloseParenthesis) {
                parseFormalParameters(declaration.Parameters, LexicalUnit.CloseParenthesis);
            } else {
                nextLexicalUnit(true);
            }
            parseTypeParameterConstraintsClauses(declaration.ConstraintsClauses);
            docCommentEndPosition = 0;
            declaration.EndPosition = parseSemiColon(false, false);
            return declaration;
        }

        private IInterfaceMember parseInterfaceMember(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers, int startPosition) {
            var type = parseType(true, true);
            if (lexicalUnit == LexicalUnit.Keyword && scanner.Keyword == Keyword.This) {
                saveScannerState();
                nextLexicalUnit(true);
                return parseIndexerDeclaration(annotations, modifiers, false, type, true, startPosition);
            }
            if (!isIdentifier(lexicalUnit)) {
                throw error(ParseErrorId.IdentifierExpected);
            }
            int sp = scanner.StartPosition;
            int len = getLexicalUnitLength();
            saveScannerState();
			var lexicalUnitShorter = nextLexicalUnit(true);
            if (lexicalUnitShorter == LexicalUnit.OpenBrace) {
                var declaration = new PropertyDeclarationNode { Type = type, NameOffset = sp, NameLength = len, StartPosition = startPosition };
                if (docCommentEndPosition > 0) {
                    declaration.DocumentationOffset = docCommentStartPosition;
                    declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                    docCommentEndPosition = 0;
                }
                setSavedScannerState(declaration);
                declaration.Modifiers.addAll(modifiers);
                declaration.Annotations.addAll(annotations);
                nextLexicalUnit(true);
                parseAccessorDeclaration(declaration, true);
                if (lexicalUnit != LexicalUnit.CloseBrace) {
                    parseAccessorDeclaration(declaration, true);
                }
				declaration.EndPosition = scanner.EndPosition;
                nextLexicalUnit(false);
                return declaration;
			} 
			Modifier setterAccess = Modifier.Public;
			if (lexicalUnitShorter == LexicalUnit.Xor) {
				setterAccess = Modifier.Private;
				lexicalUnitShorter = nextLexicalUnit(true);
			}
			if (lexicalUnitShorter == LexicalUnit.SemiColon) {
				var propertyShortDeclaration = new PropertyDeclarationNode { Type = type, NameOffset = sp, NameLength = len,
						StartPosition = startPosition };
				if (docCommentEndPosition > 0) {
					propertyShortDeclaration.DocumentationOffset = docCommentStartPosition;
					propertyShortDeclaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
					docCommentEndPosition = 0;
				}
				setSavedScannerState(propertyShortDeclaration);
				propertyShortDeclaration.Modifiers.addAll(modifiers);
                propertyShortDeclaration.Annotations.addAll(annotations);
				fakeParseShortProperty(propertyShortDeclaration, setterAccess, true);
				docCommentEndPosition = 0;
				propertyShortDeclaration.EndPosition = scanner.EndPosition;
				nextLexicalUnit(false);
				return propertyShortDeclaration;
			}
			var declaration = new MethodDeclarationNode { ReturnType = type, NameOffset = sp, NameLength = len, StartPosition = startPosition };
			if (docCommentEndPosition > 0) {
				declaration.DocumentationOffset = docCommentStartPosition;
				declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
				docCommentEndPosition = 0;
			}
			setSavedScannerState(declaration);
			declaration.Modifiers.addAll(modifiers);
			declaration.Annotations.addAll(annotations);
			parseTypeParameters(declaration.TypeParameters);
			if (lexicalUnit != LexicalUnit.OpenParenthesis) {
				throw error(ParseErrorId.OpenParenthesisExpected);
			}
			if (nextLexicalUnit(true) != LexicalUnit.CloseParenthesis) {
				parseFormalParameters(declaration.Parameters, LexicalUnit.CloseParenthesis);
			} else {
				nextLexicalUnit(true);
			}
			if (lexicalUnit == LexicalUnit.Assign) {
				nextLexicalUnit(true);
				declaration.DefaultValue = parseAnnotationValue();
			} else {
				parseTypeParameterConstraintsClauses(declaration.ConstraintsClauses);
			}
			docCommentEndPosition = 0;
			declaration.EndPosition = parseSemiColon(false, false);
			return declaration;
        }
    
        private void parseFormalParameters(List<ParameterNode> parameters, LexicalUnit endLexicalUnit) {
            while (true) {
                var param = new ParameterNode();
                parameters.add(param);
                while (lexicalUnit == LexicalUnit.OpenBracket) {
                    param.Annotations.add(parseAnnotationSection());
                }
                setScannerState(param);
                switch (lexicalUnit) {
                case Keyword:
                    switch (scanner.Keyword) {
                    case Params:
                        param.Modifier = ParameterModifier.Params;
                        nextLexicalUnit(true);
                        break;

                    case This:
                        if (parameters.size() > 1) {
                            throw error(ParseErrorId.UnexpectedLexicalUnit);
                        }
                        param.Modifier = ParameterModifier.This;
                        nextLexicalUnit(true);
                        break;
                    }
                    break;
                }
                param.Type = parseType(true);
                if (!isIdentifier(lexicalUnit)) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                param.NameOffset = scanner.StartPosition;
                param.NameLength = getLexicalUnitLength();
                param.EndPosition = scanner.EndPosition;
                nextLexicalUnit(true);
                if (param.Modifier == ParameterModifier.Params) {
                    if (param.Type.TypeReferenceKind != TypeReferenceKind.Array) {
                        throw error(ParseErrorId.ArrayTypeExpected);
                    }
                    break;
                }
                if (lexicalUnit == LexicalUnit.Comma) {
                    nextLexicalUnit(true);
                } else {
                    break;
                }
            }
            if (lexicalUnit != endLexicalUnit) {
                if (endLexicalUnit == LexicalUnit.CloseParenthesis) {
                    throw error(ParseErrorId.CloseParenthesisExpected);
                } else if (endLexicalUnit == LexicalUnit.CloseBracket) {
                    throw error(ParseErrorId.CloseBracketExpected);
                } else {
                    throw error(ParseErrorId.CommaExpected);
                }
            }
            nextLexicalUnit(false);
        }

        private void parseTypeParameterConstraintsClauses(List<TypeParameterConstraintsClauseNode> constraints) {
            while (true) {
                if (lexicalUnit != LexicalUnit.ContextualKeyword || scanner.Keyword != Keyword.Where) {
                    return;
                }
                int startPosition = scanner.StartPosition;
                if (!isIdentifier(nextLexicalUnit(true))) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                var clause = new TypeParameterConstraintsClauseNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength(),
                		StartPosition = startPosition };
                setScannerState(clause);
                constraints.add(clause);
                if (nextLexicalUnit(true) != LexicalUnit.Colon) {
                    throw error(ParseErrorId.ColonExpected);
                }
                int endPosition;
                do {
                    nextLexicalUnit(true);
                    var type = parseInterfaceType(true);
                    clause.Constraints.add(type);
                    endPosition = scanner.StartPosition;
                } while (lexicalUnit == LexicalUnit.Comma);
                clause.EndPosition = endPosition;
            }
        }
        
        private IndexerDeclarationNode parseIndexerDeclaration(List<AnnotationSectionNode> annotations, EnumSet<Modifier> modifiers,
                bool partial, TypeReferenceNode type, bool inInterface, int startPosition) {
            var declaration = new IndexerDeclarationNode { Type = type, StartPosition = startPosition };
            if (docCommentEndPosition > 0) {
                declaration.DocumentationOffset = docCommentStartPosition;
                declaration.DocumentationLength = docCommentEndPosition - docCommentStartPosition;
                docCommentEndPosition = 0;
            }
            setSavedScannerState(declaration);
            declaration.Modifiers.addAll(modifiers);
            declaration.Annotations.addAll(annotations);
            if (lexicalUnit != LexicalUnit.OpenBracket) {
                throw error(ParseErrorId.OpenBracketExpected);
            }
            if (nextLexicalUnit(true) != LexicalUnit.CloseBracket) {
                parseFormalParameters(declaration.Parameters, LexicalUnit.CloseBracket);
            } else {
                nextLexicalUnit(true);
            }
			Modifier setterAccess = Modifier.Public;
			if (lexicalUnit == LexicalUnit.Xor) {
				if (!inInterface) {
					throw error(ParseErrorId.OpenBraceExpected);
				}
				setterAccess = Modifier.Private;
                nextLexicalUnit(true);			
			}
			if (lexicalUnit == LexicalUnit.SemiColon) {
				if (!inInterface) {
					throw error(ParseErrorId.OpenBraceExpected);
				}
				fakeParseShortProperty(declaration, setterAccess, inInterface);
				docCommentEndPosition = 0;
				declaration.EndPosition = scanner.EndPosition;
				nextLexicalUnit(false);
				return declaration;
			}
            if (lexicalUnit != LexicalUnit.OpenBrace) {
                throw error(ParseErrorId.OpenBraceExpected);
            }
            nextLexicalUnit(true);
            parseAccessorDeclaration(declaration, inInterface);
            if (lexicalUnit != LexicalUnit.CloseBrace) {
                parseAccessorDeclaration(declaration, inInterface);
            }
            docCommentEndPosition = 0;
			declaration.EndPosition = scanner.EndPosition;
            nextLexicalUnit(false);
            return declaration;
        }

        private void parseTypeParameters(List<SimpleNameTypeReferenceNode> parameters) {
            parseTypeParameters(parameters, true);
        }
    
        private bool parseTypeParameters(List<SimpleNameTypeReferenceNode> parameters, bool fail) {
            if (lexicalUnit != LexicalUnit.LessThan) {
                return true;
            }
            nextLexicalUnit(true);
            while (true) {
                var type = new SimpleNameTypeReferenceNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() };
                setScannerState(type);
                parameters.add(type);
                if (!isIdentifier(lexicalUnit)) {
                    if (fail) {
                        throw error(ParseErrorId.IdentifierExpected);
                    } else {
                        return false;
                    }
                }
                type.EndPosition = scanner.EndPosition;
                if (nextLexicalUnit(true) != LexicalUnit.Comma) {
                    break;
                }
                nextLexicalUnit(true);
            }
            if (lexicalUnit != LexicalUnit.GreaterThan) {
                if (fail) {
                    throw error(ParseErrorId.GreaterThanExpected);
                } else {
                    return false;
                }
            }
            nextLexicalUnit(false);
            return true;
        }
    
	    private void fakeParseShortProperty(IAccessorsOwner declaration, Modifier setterAccess, bool inInterface) {
            var accessor = new AccessorDeclarationNode();
            setScannerState(accessor);
            declaration.GetAccessor = accessor;
			accessor.EndPosition = scanner.EndPosition;
			if (!inInterface || setterAccess == Modifier.Public) {
				accessor = new AccessorDeclarationNode();
				setScannerState(accessor);
				if (declaration.Modifiers.any(x => isLess(x, setterAccess)))
					accessor.Modifiers.add(setterAccess);
				declaration.SetAccessor = accessor;
				accessor.EndPosition = scanner.EndPosition;
			}
		}
		
		private static bool isLess(Modifier m1, Modifier m2) {
			if (m1 == Modifier.Public && m2 != Modifier.Public)
				return true;
			return (m1 == Modifier.Protected && m2 == Modifier.Private);
		}
		
        private void parseAccessorDeclaration(IAccessorsOwner declaration, bool inInterface) {
            var accessor = new AccessorDeclarationNode();
            while (lexicalUnit == LexicalUnit.OpenBracket) {
                accessor.Annotations.add(parseAnnotationSection());
            }
            setScannerState(accessor);
            switch (lexicalUnit) {
            case Keyword:
                switch (scanner.Keyword) {
                case Protected:
                    accessor.Modifiers.add(Modifier.Protected);
					nextLexicalUnit(true);
                    break;

                case Private:
                    accessor.Modifiers.add(Modifier.Private);
					nextLexicalUnit(true);
                    break;
                }
                break;
            }
            switch (lexicalUnit) {
            case ContextualKeyword:
                switch (scanner.Keyword) {
                case Get:
                    if (declaration.GetAccessor != null) {
                        throw error(ParseErrorId.DuplicateAccessor);
                    }
                    declaration.GetAccessor = accessor;
                    nextLexicalUnit(true);
                    break;

                case Set:
                    if (declaration.SetAccessor != null) {
                        throw error(ParseErrorId.DuplicateAccessor);
                    }
                    declaration.SetAccessor = accessor;
                    nextLexicalUnit(true);
                    break;

                default:
                    throw error(ParseErrorId.AccessorExpected);
                }
                break;

            default:
                throw error(ParseErrorId.AccessorExpected);
            }
            switch (lexicalUnit) {
            case SemiColon:
				accessor.EndPosition = scanner.EndPosition;
                nextLexicalUnit(false);
                break;

            case OpenBrace:
                if (!inInterface) {
                    accessor.Body = parseBlockStatement();
					accessor.EndPosition = accessor.Body.EndPosition;
                    break;
                }
                goto default;

            default:
                throw error(ParseErrorId.SemiColonExpected);
            }
        }
        
        private StatementNode parseStatement() {
            saveScannerState();
            switch (lexicalUnit) {
            case OpenBrace:
                return parseBlockStatement();

            case SemiColon:
            	var emptyStatement = new EmptyStatementNode { EndPosition = scanner.EndPosition };
            	setScannerState(emptyStatement);
                nextLexicalUnit(false);
                return emptyStatement;

            case Keyword:
            case ContextualKeyword:
                switch (scanner.Keyword) {
                case Byte:
                case Char:
                case Short:
                case Int:
                case Long:
                case Boolean:
                case Double:
                case Float:
                case Void:
				case String:
                    return parseLabeledOrLocalDeclarationOrExpressionStatement();

                case Yield: {
                    var yieldStatement = new YieldStatementNode();
                    setScannerState(yieldStatement);
                    switch (nextLexicalUnit(true)) {
                    case Keyword:
                        switch (scanner.Keyword) {
                        case Return:
                            nextLexicalUnit(true);
                            yieldStatement.Value = parseExpression();
                            break;
                        case Break:
                            nextLexicalUnit(true);
                            break;
                        default:
                            throw error(ParseErrorId.ReturnOrBreakExpected);
                        }
                        break;
                    default:
                        throw error(ParseErrorId.ReturnOrBreakExpected);
                    }
                    yieldStatement.EndPosition = parseSemiColon(false, false);
                    return yieldStatement;
                }

                case Try: {
                    var tryStatement = new TryStatementNode();
                    setScannerState(tryStatement);
                    nextLexicalUnit(true);
                    tryStatement.Block = parseBlockStatement();
                    var hasCatch = false;
                    var done = false;
                	int endPosition = 0;
                    do {
                        switch (lexicalUnit) {
                        case Keyword:
                            switch (scanner.Keyword) {
                            case Catch:
                                var catchClause = new CatchClauseNode();
                                setScannerState(catchClause);
                                if (nextLexicalUnit(true) == LexicalUnit.OpenParenthesis) {
                                    nextLexicalUnit(true);
                                    catchClause.ExceptionType = parseType(true);
                                    if (isIdentifier(lexicalUnit)) {
                                        catchClause.NameOffset = scanner.StartPosition;
                                        catchClause.NameLength = getLexicalUnitLength();
                                        nextLexicalUnit(true);
                                    }
                                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                                        throw error(ParseErrorId.CloseParenthesisExpected);
                                    }
                                    nextLexicalUnit(true);
                                }
                                catchClause.Block = parseBlockStatement();
                                tryStatement.CatchClauses.add(catchClause);
                                endPosition = catchClause.Block.EndPosition;
                                catchClause.EndPosition = endPosition;
                                hasCatch = true;
                                break;

                            case Finally:
                                nextLexicalUnit(true);
                                tryStatement.Finally = parseBlockStatement();
                                endPosition = tryStatement.Finally.EndPosition;
                                done = true;
                                break;

                            default:
                                if (!hasCatch) {
                                    throw error(ParseErrorId.FinallyExpected);
                                }
                                done = true;
                                break;
                            }
                            break;

                        default:
                            if (!hasCatch) {
                                throw error(ParseErrorId.FinallyExpected);
                            }
                            done = true;
                            break;
                        }
                    } while (!done);
                    tryStatement.EndPosition = endPosition;
                    return tryStatement;
                }

                case Using: {
                    var usingStatement = new UsingStatementNode();
                    setScannerState(usingStatement);
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    if (lexicalUnit == LexicalUnit.ContextualKeyword && scanner.Keyword == Keyword.Var) {
                        var restorePoint = this.createRestorePoint();
                        saveScannerState();
                        if (isIdentifier(nextLexicalUnit(true))) {
                            usingStatement.ResourceAcquisition = parseLocalDeclarationStatement(null, false);
                        } else {
                            this.restore(restorePoint);
                        }
                    }
                    if (usingStatement.ResourceAcquisition == null) {
                        var restorePoint = this.createRestorePoint();
                        saveScannerState();
                        var type = parseType(false);
                        if (type != null && isIdentifier(lexicalUnit)) {
                            usingStatement.ResourceAcquisition = parseLocalDeclarationStatement(type, false);
                            if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                                this.restore(restorePoint);
                                usingStatement.ResourceAcquisition = null;
                            }
                        } else {
                            this.restore(restorePoint);
                        }
                    }
                    if (usingStatement.ResourceAcquisition == null) {
                        var exp = new ExpressionStatementNode();
                        setScannerState(exp);
                        exp.Expression = parseExpression();
                        usingStatement.ResourceAcquisition = exp;
                    }
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    usingStatement.Statement = parseEmbeddedStatement();
                    usingStatement.EndPosition = usingStatement.Statement.EndPosition;
                    return usingStatement;
                }

                case Return: {
                    var returnStatement = new ReturnStatementNode();
                    setScannerState(returnStatement);
                    if (nextLexicalUnit(true) != LexicalUnit.SemiColon) {
                        returnStatement.Value = parseExpression();
                    }
                    returnStatement.EndPosition = parseSemiColon(false, false);
                    return returnStatement;
                }

                case Throw: {
                    var throwStatement = new ThrowStatementNode();
                    setScannerState(throwStatement);
                    if (nextLexicalUnit(true) != LexicalUnit.SemiColon) {
                        throwStatement.Exception = parseExpression();
                    }
                    throwStatement.EndPosition = parseSemiColon(false, false);
                    return throwStatement;
                }

                case If: {
                    var ifStatement = new IfStatementNode();
                    setScannerState(ifStatement);
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    ifStatement.Condition = parseExpression();
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    ifStatement.IfTrue = parseEmbeddedStatement();
                    if (lexicalUnit == LexicalUnit.Keyword && scanner.Keyword == Keyword.Else) {
                        nextLexicalUnit(true);
                        ifStatement.IfFalse = parseEmbeddedStatement();
                        ifStatement.EndPosition = ifStatement.IfFalse.EndPosition;
                    } else {
                    	ifStatement.EndPosition = ifStatement.IfTrue.EndPosition;
                    }
                    return ifStatement;
                }

                case While: {
                    var whileStatement = new WhileStatementNode();
                    setScannerState(whileStatement);
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    whileStatement.Condition = parseExpression();
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    whileStatement.Statement = parseEmbeddedStatement();
                    whileStatement.EndPosition = whileStatement.Statement.EndPosition;
                    return whileStatement;
                }

                case Do: {
                    var doStatement = new DoStatementNode();
                    setScannerState(doStatement);
                    nextLexicalUnit(true);
                    doStatement.Statement = parseEmbeddedStatement();
                    if (lexicalUnit != LexicalUnit.Keyword || scanner.Keyword != Keyword.While) {
                        throw error(ParseErrorId.WhileExpected);
                    }
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    doStatement.Condition = parseExpression();
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    doStatement.EndPosition = parseSemiColon(true, false);
                    return doStatement;
                }

                case Switch: {
                    var switchStatement = new SwitchStatementNode();
                    setScannerState(switchStatement);
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    switchStatement.Selector = parseExpression();
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    if (nextLexicalUnit(true) != LexicalUnit.OpenBrace) {
                        throw error(ParseErrorId.OpenBraceExpected);
                    }
                    nextLexicalUnit(true);
                    var done = false;
                    int endPosition = 0;
                    do {
                        switch (lexicalUnit) {
                        case Keyword:
                            var filename = scanner.Filename;
                            var line = scanner.StartLine;
                            var column = scanner.StartColumn;
                            var disabledWarnings = scanner.CodeErrorManager.DisabledWarnings;
	                    	int startPosition = scanner.StartPosition;
                            ExpressionNode expr = null;
                            switch (scanner.Keyword) {
                            case Case:
                                nextLexicalUnit(true);
                                expr = parseExpression();
                                break;

                            case Default:
                                nextLexicalUnit(true);
                                break;

                            default:
                                throw error(ParseErrorId.CloseBraceExpected);
                            }
                            if (lexicalUnit != LexicalUnit.Colon) {
                                throw error(ParseErrorId.ColonExpected);
                            }
                            var switchSection = new SwitchSectionNode { Filename = filename, Line = line, Column = column,
                                    DisabledWarnings = disabledWarnings, StartPosition = startPosition };
                            switchSection.CaseExpression = expr;
                            switchStatement.Sections.add(switchSection);
                            int end = scanner.EndPosition;
                            nextLexicalUnit(true);
                            var caseDone = false;
                            do {
                                switch (lexicalUnit) {
                                case Keyword:
                                    switch (scanner.Keyword) {
                                    case Case:
                                    case Default:
	                                	switchSection.EndPosition = end;
                                        caseDone = true;
                                        break;

                                    default:
                                    	var statement = parseStatement();
                                        switchSection.Statements.add(statement);
	                                    endPosition = statement.EndPosition;
	                                    switchSection.EndPosition = endPosition;
                                        break;
                                    }
                                    break;

                                case CloseBrace:
                                	endPosition = scanner.EndPosition;
                                	switchSection.EndPosition = end;
                                    caseDone = true;
                                    break;

                                default:
                                	var statement = parseStatement();
                                    switchSection.Statements.add(statement);
                                    endPosition = statement.EndPosition;
                                    switchSection.EndPosition = endPosition;
                                    break;
                                }
                            } while (!caseDone);
                            break;

                        case CloseBrace:
                            done = true;
                            endPosition = scanner.EndPosition;
                            nextLexicalUnit(false);
                            break;

                        default:
                            throw error(ParseErrorId.CloseBraceExpected);
                        }
                    } while (!done);
                    switchStatement.EndPosition = endPosition;
                    return switchStatement;
                }

                case Foreach: {
                    var foreachStatement = new ForeachStatementNode();
                    setScannerState(foreachStatement);
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    if (lexicalUnit == LexicalUnit.ContextualKeyword && scanner.Keyword == Keyword.Var) {
                        var restorePoint = this.createRestorePoint();
                        if (isIdentifier(nextLexicalUnit(true))) {
                            foreachStatement.NameOffset = scanner.StartPosition;
                            foreachStatement.NameLength = getLexicalUnitLength();
                        } else {
                            this.restore(restorePoint);
                        }
                    }
                    if (foreachStatement.NameLength == 0) {
                        var t = parseType(true);
                        if (!isIdentifier(lexicalUnit)) {
                            throw error(ParseErrorId.IdentifierExpected);
                        }
                        foreachStatement.Type = t;
                        foreachStatement.NameOffset = scanner.StartPosition;
                        foreachStatement.NameLength = getLexicalUnitLength();
                    }
                    if (nextLexicalUnit(true) != LexicalUnit.Keyword && scanner.Keyword != Keyword.In) {
                        throw error(ParseErrorId.InExpected);
                    }
                    nextLexicalUnit(true);
                    foreachStatement.Source = parseExpression();
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    foreachStatement.Statement = parseEmbeddedStatement();
                    foreachStatement.EndPosition = foreachStatement.Statement.EndPosition;
                    return foreachStatement;
                }

                case For: {
                    var forStatement = new ForStatementNode();
                    setScannerState(forStatement);
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    StatementNode statement = null;
                    nextLexicalUnit(true);
                    if (lexicalUnit == LexicalUnit.ContextualKeyword && scanner.Keyword == Keyword.Var) {
                        var restorePoint = this.createRestorePoint();
                        saveScannerState();
                        if (isIdentifier(nextLexicalUnit(true))) {
                            statement = parseLocalDeclarationStatement(null, false);
                        } else {
                            this.restore(restorePoint);
                        }
                    }
                    if (statement == null && lexicalUnit != LexicalUnit.SemiColon) {
                        statement = parseLocalDeclarationOrExpressionStatement(false);
                    }
                    if (statement != null) {
                        forStatement.Initializer.add(statement);
                        if (statement.StatementKind != StatementKind.LocalDeclaration) {
                            while (lexicalUnit == LexicalUnit.Comma) {
                                nextLexicalUnit(true);
                                forStatement.Initializer.add(parseExpressionStatement(false));
                            }
                        }
                    }
                    parseSemiColon(false, true);
                    if (lexicalUnit != LexicalUnit.SemiColon) {
                        forStatement.Condition = parseExpression();
                    }
                    parseSemiColon(false, true);
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        forStatement.Iterator.add(parseExpressionStatement(false));
                        while (lexicalUnit == LexicalUnit.Comma) {
                            nextLexicalUnit(true);
                            forStatement.Iterator.add(parseExpressionStatement(false));
                        }
                    }
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    forStatement.Statement = parseEmbeddedStatement();
                    forStatement.EndPosition = forStatement.Statement.EndPosition;
                    return forStatement;
                }

                case Synchronized: {
                    var syncStatement = new SynchronizedStatementNode();
                    setScannerState(syncStatement);
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    syncStatement.Lock = parseExpression();
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    syncStatement.Statement = parseEmbeddedStatement();
                    syncStatement.EndPosition = syncStatement.Statement.EndPosition;
                    return syncStatement;
                }

                case Goto: {
                    saveScannerState();
                    switch (nextLexicalUnit(true)) {
                    case Keyword:
                        switch (scanner.Keyword) {
                        case Case: {
                            var result = new GotoCaseStatementNode();
                            setSavedScannerState(result);
                            nextLexicalUnit(true);
                            result.Expression = parseExpression();
                            result.EndPosition = parseSemiColon(false, false);
                            return result;
                        }
                        case Default: {
                            var result = new GotoCaseStatementNode();
                            setSavedScannerState(result);
                            result.EndPosition = parseSemiColon(true, false);
                            return result;
                        }
                        default:
                            throw error(ParseErrorId.IdentifierExpected);
                        }

                    case Identifier:
                    case ContextualKeyword:
                        var gotoStatement = new GotoStatementNode();
                        setSavedScannerState(gotoStatement);
                        gotoStatement.LabelOffset = scanner.StartPosition;
                        gotoStatement.LabelLength = getLexicalUnitLength();
                        gotoStatement.EndPosition = parseSemiColon(true, false);
                        return gotoStatement;

                    default:
                        throw error(ParseErrorId.IdentifierExpected);
                    }
                }

                case Continue: {
                    var continueStatement = new ContinueStatementNode();
                    setScannerState(continueStatement);
                    continueStatement.EndPosition = parseSemiColon(true, false);
                    return continueStatement;
                }

                case Break: {
                    var breakStatement = new BreakStatementNode();
                    setScannerState(breakStatement);
                    breakStatement.EndPosition = parseSemiColon(true, false);
                    return breakStatement;
                }

                case Var: {
                    var restorePoint = this.createRestorePoint();
                    saveScannerState();
                    if (isIdentifier(nextLexicalUnit(true))) {
                        return parseLocalDeclarationStatement(null, true);
                    } else {
                        this.restore(restorePoint);
                        return parseLabeledOrLocalDeclarationOrExpressionStatement();
                    }
                }

                default:
                    return parseLabeledOrLocalDeclarationOrExpressionStatement();
                }

            default:
                return parseLabeledOrLocalDeclarationOrExpressionStatement();
            }
        }
        
        private StatementNode parseEmbeddedStatement() {
            var result = parseStatement();
            switch (result.StatementKind) {
            case LocalDeclaration:
                throw error(ParseErrorId.UnexpectedVariableDeclaration);
            case Labeled:
                throw error(ParseErrorId.UnexpectedLabeledStatement);
            }
            return result;
        }
        
        private StatementNode parseLocalDeclarationOrExpressionStatement(bool eatSemiColon) {
            var restorePoint = this.createRestorePoint();
            saveScannerState();
            var type = parseType(false);
            if (type != null && isIdentifier(lexicalUnit)) {
                return parseLocalDeclarationStatement(type, eatSemiColon);
            } else {
                this.restore(restorePoint);
                return parseExpressionStatement(eatSemiColon);
            }
        }

        private StatementNode parseLabeledOrLocalDeclarationOrExpressionStatement() {
            var restorePoint = this.createRestorePoint();
            saveScannerState();
            if (isIdentifier(lexicalUnit)) {
                int sp = scanner.StartPosition;
                int len = getLexicalUnitLength();
                if (nextLexicalUnit(true) == LexicalUnit.Colon) {
                    nextLexicalUnit(true);
                    var labeled = new LabeledStatementNode { NameOffset = sp, NameLength = len };
                    setSavedScannerState(labeled);
                    labeled.Statement = parseStatement();
                    labeled.EndPosition = labeled.Statement.EndPosition;
                    return labeled;
                }
                this.restore(restorePoint);
            }
            var type = parseType(false);
            if (type != null && isIdentifier(lexicalUnit)) {
                return parseLocalDeclarationStatement(type, true);
            } else {
                this.restore(restorePoint);
                return parseExpressionStatement(true);
            }
        }

        private LocalDeclarationStatementNode parseLocalDeclarationStatement(TypeReferenceNode type, bool eatSemiColon) {
            var declaration = new LocalDeclarationStatementNode();
            setSavedScannerState(declaration);
            declaration.Type = type;
            var declarator = new VariableDeclaratorNode();
            setScannerState(declarator);
            declarator.NameOffset = scanner.StartPosition;
            declarator.NameLength = getLexicalUnitLength();
            declaration.Declarators.add(declarator);
            nextLexicalUnit(true);
            if (lexicalUnit == LexicalUnit.Assign) {
                nextLexicalUnit(true);
                declarator.Value = parseLocalVariableInitializer();
                declarator.EndPosition = declarator.Value.EndPosition;
            } else {
            	declarator.EndPosition = declarator.NameOffset + declarator.NameLength;
            }
            int endPosition = declarator.EndPosition;
            while (lexicalUnit == LexicalUnit.Comma) {
                if (!isIdentifier(nextLexicalUnit(true))) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                declarator = new VariableDeclaratorNode();
                setScannerState(declarator);
                declarator.NameOffset = scanner.StartPosition;
                declarator.NameLength = getLexicalUnitLength();
                declaration.Declarators.add(declarator);
                nextLexicalUnit(true);
                if (lexicalUnit == LexicalUnit.Assign) {
                    nextLexicalUnit(true);
                    declarator.Value = parseLocalVariableInitializer();
	                declarator.EndPosition = declarator.Value.EndPosition;
	            } else {
	            	declarator.EndPosition = declarator.NameOffset + declarator.NameLength;
                }
                endPosition = declarator.EndPosition;
            }
            if (eatSemiColon) {
                declaration.EndPosition = parseSemiColon(false, false);
            } else {
            	declaration.EndPosition = endPosition;
            }
            return declaration;
        }
        
        private ExpressionNode parseLocalVariableInitializer() {
            if (lexicalUnit == LexicalUnit.OpenBrace) {
            	saveScannerState();
                nextLexicalUnit(true);
                return parseArrayInitializer();
            } else {
                return parseExpression();
            }
        }

        private StatementNode parseExpressionStatement(bool eatSemiColon) {
            var result = new ExpressionStatementNode();
            setScannerState(result);
            var expression = parseExpression();
            result.Expression = expression;
            switch (expression.ExpressionKind) {
            case Invocation:
            case ObjectCreation:
            case Assign:
                break;

            case Unary:
                switch (((UnaryExpressionNode)expression).Operator) {
                case PreDecrement:
                case PreIncrement:
                case PostDecrement:
                case PostIncrement:
                    break;

                default:
                    throw error(ParseErrorId.UnexpectedLexicalUnit);
                }
                break;

            default:
                throw error(ParseErrorId.UnexpectedLexicalUnit);
            }
            if (eatSemiColon) {
                result.EndPosition = parseSemiColon(false, false);
            } else {
            	result.EndPosition = scanner.EndPosition;
            }
            return result;
        }

        private BlockStatementNode parseBlockStatement() {
            if (lexicalUnit != LexicalUnit.OpenBrace) {
                throw error(ParseErrorId.OpenBraceExpected);
            }
            var result = new BlockStatementNode { StartPosition = scanner.StartPosition };
            setScannerState(result);
            nextLexicalUnit(true);
            while (lexicalUnit != LexicalUnit.CloseBrace) {
                result.Statements.add(parseStatement());
            }
            docCommentEndPosition = 0;
			result.EndPosition = scanner.EndPosition;
            nextLexicalUnit(false);
            return result;
        }
        
        private ExpressionNode parseExpression() {
            ExpressionNode result = null;
            var restorePoint = this.createRestorePoint();
            switch (lexicalUnit) {
            case Identifier:
            case VerbatimIdentifier:
            case OpenParenthesis:
                result = parseLambdaExpression();
                break;

            case ContextualKeyword:
                if (scanner.Keyword == Keyword.From) {
                    result = parseQueryExpression();
                } else {
                    result = parseLambdaExpression();
                }
                break;
            }
            if (result != null) {
                return result;
            } else {
                this.restore(restorePoint);
            }

            result = parseConditionalExpression();
            switch (result.ExpressionKind) {
            case Conditional:
            case Binary:
                return result;
            }
            AssignOperator op = null;
            switch (lexicalUnit) {
            case Assign:
                nextLexicalUnit(true);
                op = AssignOperator.Assign;
                break;

            case AddAssign:
                nextLexicalUnit(true);
                op = AssignOperator.Add;
                break;

            case AndAssign:
                nextLexicalUnit(true);
                op = AssignOperator.And;
                break;

            case DivideAssign:
                nextLexicalUnit(true);
                op = AssignOperator.Divide;
                break;

            case LeftShiftAssign:
                nextLexicalUnit(true);
                op = AssignOperator.LeftShift;
                break;

            case ModuloAssign:
                nextLexicalUnit(true);
                op = AssignOperator.Modulo;
                break;

            case MultiplyAssign:
                nextLexicalUnit(true);
                op = AssignOperator.Multiply;
                break;

            case OrAssign:
                nextLexicalUnit(true);
                op = AssignOperator.Or;
                break;

            case SubtractAssign:
                nextLexicalUnit(true);
                op = AssignOperator.Subtract;
                break;

            case XorAssign:
                nextLexicalUnit(true);
                op = AssignOperator.Xor;
                break;

            case GreaterThan:
                switch (scanner.nextLexicalUnit()) {
                case GreaterThanOrEqual:
                    nextLexicalUnit(true);
                    op = AssignOperator.RightShift;
                    break;
                case GreaterThan:
                    if (scanner.nextLexicalUnit() != LexicalUnit.GreaterThanOrEqual) {
                        throw error(ParseErrorId.UnexpectedLexicalUnit);
                    }
                    nextLexicalUnit(true);
                    op = AssignOperator.UnsignedRightShift;
                    break;
                default:
                    throw error(ParseErrorId.UnexpectedLexicalUnit);
                }
                break;
            }
            if (op != null) {
                var assign = new AssignExpressionNode();
                copyScannerState(result, assign);
                assign.Left = result;
                assign.Operator = op;
                assign.Right = parseExpression();
                assign.EndPosition = assign.Right.EndPosition;
                result = assign;
            }
            return result;
        }
        
        private ExpressionNode parseLambdaExpression() {
            switch (lexicalUnit) {
            case ContextualKeyword:
            case VerbatimIdentifier:
            case Identifier: {
                int sp = scanner.StartPosition;
                int len = getLexicalUnitLength();
                saveScannerState();
                if (nextLexicalUnit(false) == LexicalUnit.Lambda) {
                    var lambda = new LambdaExpressionNode();
                    setSavedScannerState(lambda);
                    var param = new ParameterNode { NameOffset = sp, NameLength = len };
                    setSavedScannerState(param);
                    param.EndPosition = sp + len;
                    lambda.Parameters.add(param);
                    if (nextLexicalUnit(true) == LexicalUnit.OpenBrace) {
                        lambda.Body = parseBlockStatement();
                    } else {
                    	var expression = parseExpression();
                    	var expressionStatement = new ExpressionStatementNode { Expression = expression };
                    	copyScannerState(expression, expressionStatement);
                        lambda.Body = expressionStatement;
                    }
                    lambda.EndPosition = lambda.Body.EndPosition;
                    return lambda;
                }
	            break;
            }

            default: {
                var implicitSignature = true;
                var prev = LexicalUnit.Comma;
                var restorePoint = this.createRestorePoint();
                while (nextLexicalUnit(false) != LexicalUnit.CloseParenthesis) {
                    switch (lexicalUnit) {
                    case Identifier:
                    case ContextualKeyword:
                    case VerbatimIdentifier:
                        if (implicitSignature) {
                            if (prev != LexicalUnit.Comma) {
                                implicitSignature = false;
                            }
                        }
                        break;

                    case Comma:
                        if (implicitSignature) {
                            if (!isIdentifier(prev)) {
                                implicitSignature = false;
                            }
                        }
                        break;

                    case Keyword:
                        switch (scanner.Keyword) {
                        case Byte:
                        case Char:
                        case Short:
                        case Int:
                        case Long:
                        case Boolean:
                        case Double:
                        case Float:
                        case Void:
						case String:
                            break;

                        default:
                            return null;
                        }
                        implicitSignature = false;
                        break;

                    case GreaterThan:
                    case LessThan:
                    case Dot:
                    case OpenBracket:
                    case CloseBracket:
                    case QuestionMark:
                        implicitSignature = false;
                        break;

                    default:
                        return null;
                    }
                    prev = lexicalUnit;
                }
                if (nextLexicalUnit(false) != LexicalUnit.Lambda) {
                    return null;
                }
                this.restore(restorePoint);
                nextLexicalUnit(true);
                var lambda = new LambdaExpressionNode();
                setScannerState(lambda);
                if (implicitSignature) {
                    parseImplicitAnonymousFunctionSignature(lambda.Parameters);
                } else {
                    parseExplicitAnonymousFunctionSignature(lambda.Parameters);
                }
                if (nextLexicalUnit(true) == LexicalUnit.OpenBrace) {
                    lambda.Body = parseBlockStatement();
                } else {
                	var expression = parseExpression();
                	var expressionStatement = new ExpressionStatementNode { Expression = expression };
                	copyScannerState(expression, expressionStatement);
                    lambda.Body = expressionStatement;
                }
                lambda.EndPosition = lambda.Body.EndPosition;
                return lambda;
            }
            }
            return null;
        }
        
        private void parseImplicitAnonymousFunctionSignature(List<ParameterNode> parameters) {
            while (lexicalUnit != LexicalUnit.CloseParenthesis) {
                var param = new ParameterNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() };
                setScannerState(param);
                param.EndPosition = scanner.EndPosition;
                parameters.add(param);
                if (nextLexicalUnit(true) == LexicalUnit.Comma) {
                    nextLexicalUnit(true);
                }
            }
            nextLexicalUnit(false);
        }

        private void parseExplicitAnonymousFunctionSignature(List<ParameterNode> parameters) {
            while (lexicalUnit != LexicalUnit.CloseParenthesis) {
                saveScannerState();
                var type = parseType(true);
                if (!isIdentifier(lexicalUnit)) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                var param = new ParameterNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength(), Type = type };
                setSavedScannerState(param);
                param.EndPosition = scanner.EndPosition;
                parameters.add(param);
                switch (nextLexicalUnit(true)) {
                default:
                    throw error(ParseErrorId.CloseParenthesisExpected);

                case Comma:
                    if (nextLexicalUnit(true) == LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.IdentifierExpected);
                    }
                    break;

                case CloseParenthesis:
                    break;
                }
            }
            nextLexicalUnit(false);
        }
        
        private ExpressionNode parseQueryExpression() {
            var restorePoint = this.createRestorePoint();
            var result = new QueryExpressionNode();
            setScannerState(result);
            var doParseType = true;
			var lex = nextLexicalUnit(false);
            if (!(isIdentifier(lex) || (lex == LexicalUnit.Keyword && scanner.Keyword == Keyword.String))) {
                this.restore(restorePoint);
                return null;
            } else {
                int sp = scanner.StartPosition;
                int len = getLexicalUnitLength();
                switch (nextLexicalUnit(false)) {
                case SemiColon:
                case Assign:
                case Comma:
                    this.restore(restorePoint);
                    return null;

                case Keyword:
                    if (scanner.Keyword == Keyword.In) {
                        doParseType = false;
                        result.From.NameOffset = sp;
                        result.From.NameLength = len;
                        break;
                    }
                    goto default;
                    
                default:
                    this.restore(restorePoint);
                    nextLexicalUnit(true);
                    break;
                }
            }
            if (doParseType) {
                result.From.Type = parseType(false);
                if (result.From.Type == null) {
                    this.restore(restorePoint);
                    return null;
                }
                if (!isIdentifier(lexicalUnit)) {
                    this.restore(restorePoint);
                    return null;
                }
                result.From.NameOffset = scanner.StartPosition;
                result.From.NameLength = getLexicalUnitLength();
                nextLexicalUnit(true);
            }
            if (lexicalUnit != LexicalUnit.Keyword || scanner.Keyword != Keyword.In) {
                throw error(ParseErrorId.InExpected);
            }
            nextLexicalUnit(true);
            result.From.Origin = parseExpression();
            parseQueryBody(result.Body, result.From.Origin.EndPosition);
            result.EndPosition = result.Body.EndPosition;
            return result;
        }
        
        private void parseQueryBody(QueryBodyNode body, int endPosition) {
            var done = false;
            do {
                switch (lexicalUnit) {
                case ContextualKeyword:
                case Keyword:
                    switch (scanner.Keyword) {
                    case From:
                        var from = new FromQueryBodyClauseNode();
                        parseFromClause(from);
                        body.Clauses.add(from);
                        endPosition = from.EndPosition;
                        break;

                    case Join:
                        var join = new JoinQueryBodyClauseNode();
                        parseJoinClause(join);
                        body.Clauses.add(join);
                        endPosition = join.EndPosition;
                        break;

                    case Let:
                        var let = new LetQueryBodyClauseNode();
                        parseLetClause(let);
                        body.Clauses.add(let);
                        endPosition = let.EndPosition;
                        break;

                    case Orderby:
                        var orderby = new OrderbyQueryBodyClauseNode();
                        parseOrderbyClause(orderby);
                        body.Clauses.add(orderby);
                        endPosition = orderby.EndPosition;
                        break;

                    case Where:
                        var where = new WhereQueryBodyClauseNode();
                        parseWhereClause(where);
                        body.Clauses.add(where);
                        endPosition = where.EndPosition;
                        break;

                    default:
                        done = true;
                        break;
                    }
                    break;

                default:
                    done = true;
                    break;
                }
            } while (!done);
            switch (lexicalUnit) {
            case ContextualKeyword:
            case Keyword:
                switch (scanner.Keyword) {
                case Select:
                    nextLexicalUnit(true);
                    body.SelectOrGroup = parseExpression();
                    endPosition = body.SelectOrGroup.EndPosition;
                    break;

                case Group:
                    nextLexicalUnit(true);
                    body.SelectOrGroup = parseExpression();
                    if (lexicalUnit != LexicalUnit.ContextualKeyword || scanner.Keyword != Keyword.By) {
                        throw error(ParseErrorId.ByExpected);
                    }
                    nextLexicalUnit(true);
                    body.By = parseExpression();
                    endPosition = body.By.EndPosition;
                    break;
                default:
                    throw error(ParseErrorId.SelectOrGroupExpected);
                }
                break;
            default:
                throw error(ParseErrorId.SelectOrGroupExpected);
            }
            if (lexicalUnit == LexicalUnit.ContextualKeyword && scanner.Keyword == Keyword.Into) {
                if (!isIdentifier(nextLexicalUnit(true))) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                body.Continuation = new QueryContinuationNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() };
                endPosition = scanner.EndPosition;
                nextLexicalUnit(true);
                parseQueryBody(body.Continuation.Body, endPosition);
                endPosition = body.Continuation.Body.EndPosition;
            }
            body.EndPosition = endPosition;
        }
        
        private void parseFromClause(FromQueryBodyClauseNode from) {
            setScannerState(from);
            var doParseType = true;
            if (isIdentifier(nextLexicalUnit(true))) {
                int sp = scanner.StartPosition;
                int len = getLexicalUnitLength();
                var restorePoint = this.createRestorePoint();
                if (nextLexicalUnit(true) == LexicalUnit.Keyword && scanner.Keyword == Keyword.In) {
                    doParseType = false;
                    from.NameOffset = sp;
                    from.NameLength = len;
                } else {
                    this.restore(restorePoint);
                }
            }
            if (doParseType) {
                from.Type = parseType(true);
                if (!isIdentifier(lexicalUnit)) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                from.NameOffset = scanner.StartPosition;
                from.NameLength = getLexicalUnitLength();
                nextLexicalUnit(true);
            }
            if (lexicalUnit != LexicalUnit.Keyword || scanner.Keyword != Keyword.In) {
                throw error(ParseErrorId.InExpected);
            }
            nextLexicalUnit(true);
            from.Origin = parseExpression();
            from.EndPosition = from.Origin.EndPosition;
        }
        
        private void parseJoinClause(JoinQueryBodyClauseNode join) {
            setScannerState(join);
            var doParseType = true;
            if (isIdentifier(nextLexicalUnit(true))) {
                int sp = scanner.StartPosition;
                int len = getLexicalUnitLength();
                var restorePoint = this.createRestorePoint();
                if (nextLexicalUnit(true) == LexicalUnit.Keyword && scanner.Keyword == Keyword.In) {
                    doParseType = false;
                    join.NameOffset = sp;
                    join.NameLength = len;
                } else {
                    this.restore(restorePoint);
                }
            }
            if (doParseType) {
                join.Type = parseType(true);
                if (!isIdentifier(lexicalUnit)) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                join.NameOffset = scanner.StartPosition;
                join.NameLength = getLexicalUnitLength();
                nextLexicalUnit(true);
            }
            if (lexicalUnit != LexicalUnit.Keyword || scanner.Keyword != Keyword.In) {
                throw error(ParseErrorId.InExpected);
            }
            nextLexicalUnit(true);
            join.Origin = parseExpression();
            if (lexicalUnit != LexicalUnit.ContextualKeyword || scanner.Keyword != Keyword.On) {
                throw error(ParseErrorId.OnExpected);
            }
            nextLexicalUnit(true);
            join.EqualsLeftOperand = parseExpression();
            if (lexicalUnit != LexicalUnit.ContextualKeyword || scanner.Keyword != Keyword.Equals) {
                throw error(ParseErrorId.EqualsExpected);
            }
            nextLexicalUnit(true);
            join.EqualsRightOperand = parseExpression();
            if (lexicalUnit == LexicalUnit.ContextualKeyword && scanner.Keyword == Keyword.Into) {
                if (!isIdentifier(nextLexicalUnit(true))) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                join.ResultOffset = scanner.StartPosition;
                join.ResultLength = getLexicalUnitLength();
                join.EndPosition = scanner.EndPosition;
                nextLexicalUnit(false);
            } else {
            	join.EndPosition = join.EqualsRightOperand.EndPosition;
            }
        }

        private void parseLetClause(LetQueryBodyClauseNode let) {
            setScannerState(let);
            if (!isIdentifier(nextLexicalUnit(true))) {
                throw error(ParseErrorId.IdentifierExpected);
            }
            let.NameOffset = scanner.StartPosition;
            let.NameLength = getLexicalUnitLength();
            if (nextLexicalUnit(true) != LexicalUnit.Assign) {
                throw error(ParseErrorId.AssignExpected);
            }
            nextLexicalUnit(true);
            let.Value = parseExpression();
            let.EndPosition = let.Value.EndPosition;
        }

        private void parseOrderbyClause(OrderbyQueryBodyClauseNode orderby) {
            setScannerState(orderby);
            int endPosition;
            do {
                nextLexicalUnit(true);
                var ordering = new OrderingNode();
                setScannerState(ordering);
                ordering.KeySelector = parseExpression();
            	endPosition = ordering.KeySelector.EndPosition;
                if (lexicalUnit == LexicalUnit.ContextualKeyword) {
                    switch (scanner.Keyword) {
                    case Ascending:
                    	endPosition = scanner.EndPosition;
                        nextLexicalUnit(true);
                        break;
                    case Descending:
                    	endPosition = scanner.EndPosition;
                        nextLexicalUnit(true);
                        ordering.Descending = true;
                        break;
                    }
                }
                ordering.EndPosition = endPosition;
                orderby.Orderings.add(ordering);
            } while (lexicalUnit == LexicalUnit.Comma);
            orderby.EndPosition = endPosition;
        }

        private void parseWhereClause(WhereQueryBodyClauseNode where) {
            setScannerState(where);
            nextLexicalUnit(true);
            where.Predicat = parseExpression();
            where.EndPosition = where.Predicat.EndPosition;
        }

        private ExpressionNode parseConditionalExpression() {
            var filename = scanner.Filename;
            var line = scanner.StartLine;
            var column = scanner.StartColumn;
            var disabledWarnings = scanner.CodeErrorManager.DisabledWarnings;
            var startPosition = scanner.StartPosition;
            
            var result = parseBinaryExpression();
            if (lexicalUnit == LexicalUnit.QuestionMark) {
                nextLexicalUnit(true);
                var ifTrue = parseExpression();
                if (lexicalUnit != LexicalUnit.Colon) {
                    throw error(ParseErrorId.ColonExpected);
                }
                nextLexicalUnit(true);
                var expr = new ConditionalExpressionNode { Filename = filename, Line = line, Column = column, DisabledWarnings = disabledWarnings,
                        StartPosition = startPosition, Condition = result, IfTrue = ifTrue, IfFalse = parseExpression() };
                expr.EndPosition = expr.IfFalse.EndPosition;
                result = expr;
            }
            return result;
        }

        private ExpressionNode parseBinaryExpression() {
            return parseBinaryExpression(Integer.MAX_VALUE, parseUnaryExpression());
        }

        private ExpressionNode parseBinaryExpression(int precedence, ExpressionNode leftExpression) {
            for (;;) {
                var prec = 0;
                var doParseType = false;
                var op = BinaryOperator.Add;
                var restorePoint = this.createRestorePoint();
                switch (lexicalUnit) {
                case Multiply:
                    prec = 1;
                    op = BinaryOperator.Multiply;
                    break;

                case Divide:
                    prec = 1;
                    op = BinaryOperator.Divide;
                    break;

                case Percent:
                    prec = 1;
                    op = BinaryOperator.Modulo;
                    break;

                case Plus:
                    prec = 2;
                    op = BinaryOperator.Add;
                    break;

                case Minus:
                    prec = 2;
                    op = BinaryOperator.Subtract;
                    break;

                case LeftShift:
                    prec = 3;
                    op = BinaryOperator.LeftShift;
                    break;

                case GreaterThan:
                    switch (scanner.nextLexicalUnit()) {
                    case GreaterThan:
                        restorePoint = createRestorePoint();
                        switch (scanner.nextLexicalUnit()) {
                        case GreaterThan:
                            prec = 3;
                            op = BinaryOperator.UnsignedRightShift;
                            break;
                        default:
                            this.restore(restorePoint);
                            prec = 3;
                            op = BinaryOperator.RightShift;
                            break;
                        case GreaterThanOrEqual:
                            this.restore(restorePoint);
                            return leftExpression;
                        }
                        break;
                    default:
                        this.restore(restorePoint);
                        prec = 4;
                        op = BinaryOperator.GreaterThan;
                        break;
                    case GreaterThanOrEqual:
                        this.restore(restorePoint);
                        return leftExpression;
                    }
                    break;

                case LessThan:
                    prec = 4;
                    op = BinaryOperator.LessThan;
                    break;

                case LessThanOrEqual:
                    prec = 4;
                    op = BinaryOperator.LessThanOrEqual;
                    break;

                case GreaterThanOrEqual:
                    prec = 4;
                    op = BinaryOperator.GreaterThanOrEqual;
                    break;

                case Keyword:
                    switch (scanner.Keyword) {
                    case As:
                        prec = 4;
                        doParseType = true;
                        op = BinaryOperator.As;
                        break;

                    case Instanceof:
                        prec = 4;
                        doParseType = true;
                        op = BinaryOperator.Instanceof;
                        break;

                    default:
                        return leftExpression;
                    }
                    break;

                case Equal:
                    prec = 5;
                    op = BinaryOperator.Equal;
                    break;

                case NotEqual:
                    prec = 5;
                    op = BinaryOperator.NotEqual;
                    break;

                case LogicalAnd:
                    prec = 6;
                    op = BinaryOperator.LogicalAnd;
                    break;

                case Xor:
                    prec = 7;
                    op = BinaryOperator.Xor;
                    break;

                case LogicalOr:
                    prec = 8;
                    op = BinaryOperator.LogicalOr;
                    break;

                case And:
                    prec = 9;
                    op = BinaryOperator.And;
                    break;

                case Or:
                    prec = 10;
                    op = BinaryOperator.Or;
                    break;

                case NullCoalescing:
                    prec = 11;
                    op = BinaryOperator.NullCoalescing;
                    break;

                default:
                    return leftExpression;
                }
                if (prec > precedence) {
                    if (op == BinaryOperator.RightShift) {
                        this.restore(restorePoint);
                    }
                    return leftExpression;
                }
                nextLexicalUnit(true);
                var binary = new BinaryExpressionNode { Operator = op, LeftOperand = leftExpression };
                copyScannerState(leftExpression, binary);
                if (doParseType) {
                    var type = new TypeExpressionNode { TypeReference = parseType(true) };
                    copyScannerState(type.TypeReference, type);
                    type.EndPosition = type.TypeReference.EndPosition;
                    binary.RightOperand = type;
                } else {
                    binary.RightOperand = parseBinaryExpression(prec - 1, parseUnaryExpression());
                }
                binary.EndPosition = binary.RightOperand.EndPosition;
                leftExpression = binary;
            }
        }

        private ExpressionNode parseUnaryExpression() {
            switch (lexicalUnit) {
            case Plus:
                return createUnaryExpression(UnaryOperator.Plus);

            case Minus:
                return createUnaryExpression(UnaryOperator.Minus);

            case Complement:
                return createUnaryExpression(UnaryOperator.Complement);

            case Not:
                return createUnaryExpression(UnaryOperator.Not);

            case Increment:
                return createUnaryExpression(UnaryOperator.PreIncrement);

            case Decrement:
                return createUnaryExpression(UnaryOperator.PreDecrement);

            case OpenParenthesis:
                var cast = parseCastExpression(false);
                if (cast == null) {
                    var restorePoint = this.createRestorePoint();
                    try {
                        return parsePrimaryExpression();
                    } catch (CodeErrorException) {
                        this.restore(restorePoint);
                        return parseCastExpression(true);
                    }
                } else {
                    return cast;
                }
            }
            return parsePrimaryExpression();
        }
        
        private UnaryExpressionNode createUnaryExpression(UnaryOperator operator) {
            var result = new UnaryExpressionNode { Operator = operator };
            setScannerState(result);
            nextLexicalUnit(true);
            result.Operand = parseUnaryExpression();
            result.EndPosition = result.Operand.EndPosition;
            return result;
        }

        private ExpressionNode parseCastExpression(bool force) {
            var filename = scanner.Filename;
            var line = scanner.StartLine;
            var column = scanner.StartColumn;
            var disabledWarnings = scanner.CodeErrorManager.DisabledWarnings;
            var startPosition = scanner.StartPosition;
        
            var restorePoint = this.createRestorePoint();
            nextLexicalUnit(true);
            var type = parseType(false);
            if (type == null || lexicalUnit != LexicalUnit.CloseParenthesis || nextLexicalUnit(false) == LexicalUnit.EndOfStream) {
                if (force) {
                    throw error(ParseErrorId.UnexpectedLexicalUnit);
                }
                this.restore(restorePoint);
                return null;
            }
            switch (lexicalUnit) {
            case Keyword:
                switch (scanner.Keyword) {
                case As:
                case Instanceof:
                    if (!force) {
                        this.restore(restorePoint);
                        return null;
                    }
                    break;
                }
                break;

            case Identifier:
            case VerbatimIdentifier:
            case Complement:
            case Not:
            case OpenParenthesis:
            case DoubleLiteral:
            case FloatLiteral:
            case StringLiteral:
            case RealLiteral:
            case CharacterLiteral:
            case DecimalIntegerLiteral:
            case LongLiteral:
            case VerbatimStringLiteral:
                break;

            case ContextualKeyword:
                if (scanner.Keyword == Keyword.From) {
                    var result = new CastExpressionNode { Filename = filename, Line = line, Column = column, DisabledWarnings = disabledWarnings,
                        StartPosition = startPosition, TargetType = type };
                    result.Expression = parseQueryExpression();
                    result.EndPosition = result.Expression.EndPosition;
                    return result;
                }
                break;

            default:
                if (!force) {
                    this.restore(restorePoint);
                    return null;
                }
                break;
            }
            var result = new CastExpressionNode { Filename = filename, Line = line, Column = column, DisabledWarnings = disabledWarnings,
                        StartPosition = startPosition, TargetType = type };
            result.Expression = parseUnaryExpression();
            result.EndPosition = result.Expression.EndPosition;
            return result;
        }
        
        private ExpressionNode parsePrimaryExpression() {
            ExpressionNode result = null;
            switch (lexicalUnit) {
            case CharacterLiteral:
                result = createLiteralExpression(LiteralKind.Character);
                nextLexicalUnit(false);
                break;

            case DecimalIntegerLiteral:
                result = createLiteralExpression(LiteralKind.DecimalInteger);
                nextLexicalUnit(false);
                break;

            case DoubleLiteral:
                result = createLiteralExpression(LiteralKind.Double);
                nextLexicalUnit(false);
                break;

            case FloatLiteral:
                result = createLiteralExpression(LiteralKind.Float);
                nextLexicalUnit(false);
                break;

            case HexadecimalIntegerLiteral:
                result = createLiteralExpression(LiteralKind.HexadecimalInteger);
                nextLexicalUnit(false);
                break;

            case HexadecimalLongLiteral:
                result = createLiteralExpression(LiteralKind.HexadecimalLong);
                nextLexicalUnit(false);
                break;

            case LongLiteral:
                result = createLiteralExpression(LiteralKind.Long);
                nextLexicalUnit(false);
                break;

            case RealLiteral:
                result = createLiteralExpression(LiteralKind.Real);
                nextLexicalUnit(false);
                break;

            case StringLiteral:
                result = createLiteralExpression(LiteralKind.String);
                nextLexicalUnit(false);
                break;

            case VerbatimStringLiteral:
                result = createLiteralExpression(LiteralKind.VerbatimString);
                nextLexicalUnit(false);
                break;

            case VerbatimIdentifier:
            case ContextualKeyword:
            case Identifier: {
                var name = new SimpleNameExpressionNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() };
                result = name;
                setScannerState(result);
                int endPosition = scanner.EndPosition;
                nextLexicalUnit(false);
                result.EndPosition = parseTypeArguments(name.TypeArguments, true, endPosition);
            }
            break;

            case OpenParenthesis: {
                nextLexicalUnit(true);
                result = parseExpression();
                result.Parenthesized = true;
                if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                    throw error(ParseErrorId.CloseParenthesisExpected);
                }
                result.EndPosition = scanner.EndPosition;
                nextLexicalUnit(false);
            }
            break;

            case Keyword:
                switch (scanner.Keyword) {
                case True:
                    result = createLiteralExpression(LiteralKind.True);
                    nextLexicalUnit(false);
                    break;

                case False:
                    result = createLiteralExpression(LiteralKind.False);
                    nextLexicalUnit(false);
                    break;

                case Null:
                    result = createLiteralExpression(LiteralKind.Null);
                    nextLexicalUnit(false);
                    break;

                case Boolean:
                    result = createTypeExpression(TypeReferenceKind.Boolean);
                    nextLexicalUnit(false);
                    break;

                case Byte:
                    result = createTypeExpression(TypeReferenceKind.Byte);
                    nextLexicalUnit(false);
                    break;

                case Char:
                    result = createTypeExpression(TypeReferenceKind.Char);
                    nextLexicalUnit(false);
                    break;

                case Double:
                    result = createTypeExpression(TypeReferenceKind.Double);
                    nextLexicalUnit(false);
                    break;

                case Float:
                    result = createTypeExpression(TypeReferenceKind.Float);
                    nextLexicalUnit(false);
                    break;

                case Int:
                    result = createTypeExpression(TypeReferenceKind.Int);
                    nextLexicalUnit(false);
                    break;

                case Long:
                    result = createTypeExpression(TypeReferenceKind.Long);
                    nextLexicalUnit(false);
                    break;

                case Short:
                    result = createTypeExpression(TypeReferenceKind.Short);
                    nextLexicalUnit(false);
                    break;

                case String:
                    result = createTypeExpression(TypeReferenceKind.String);
                    nextLexicalUnit(false);
                    break;

                case Sizeof:
                    saveScannerState();
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    var sizeofExp = new SizeofExpressionNode();
                    setSavedScannerState(sizeofExp);
                    sizeofExp.Expression = parseExpression();
                    result = sizeofExp;
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    result.EndPosition = scanner.EndPosition;
                    nextLexicalUnit(false);
                    break;
                    
                case Typeof: {
                    saveScannerState();
                    if (nextLexicalUnit(true) != LexicalUnit.OpenParenthesis) {
                        throw error(ParseErrorId.OpenParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                    var typeofExp = new TypeofExpressionNode();
                    setSavedScannerState(typeofExp);
                    if (lexicalUnit == LexicalUnit.Keyword && scanner.Keyword == Keyword.Void) {
                        typeofExp.Type = new PrimitiveTypeReferenceNode(TypeReferenceKind.Void);
                    } else {
                        typeofExp.Type = parseType(true);
                    }
                    result = typeofExp;
                    if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    result.EndPosition = scanner.EndPosition;
                    nextLexicalUnit(false);
                    break;
                }
                case This: {
                    result = new ThisAccessExpressionNode { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;
                }
                case Super: {
                    saveScannerState();
                    result = parseSuperAccessExpression();
                    setSavedScannerState(result);
                    break;
                }
                case New:
                    result = parseNewExpression();
                    break;
                }
                break;
            }
            if (result == null) {
                throw error(ParseErrorId.UnexpectedLexicalUnit);
            }
            for (;;) {
                switch (lexicalUnit) {
                case Dot: {
                    nextLexicalUnit(true);
                    if (!isIdentifier(lexicalUnit)) {
                        throw error(ParseErrorId.IdentifierExpected);
                    }
                    var memberAccess = new MemberAccessExpressionNode {
                        TargetObject = result,
                        Member = new SimpleNameExpressionNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() }
                    };
                    copyScannerState(result, memberAccess);
                    setScannerState(memberAccess.Member);
                    memberAccess.Member.EndPosition = scanner.EndPosition;
                    result = memberAccess;
                    int endPosition = scanner.EndPosition;
                    nextLexicalUnit(false);
                    memberAccess.EndPosition = parseTypeArguments(memberAccess.Member.TypeArguments, true, endPosition);
                }
                break;

                case NullSafeMemberAccess: {
                    nextLexicalUnit(true);
                    if (!isIdentifier(lexicalUnit)) {
                        throw error(ParseErrorId.IdentifierExpected);
                    }
                    var memberAccess = new NullSafeMemberAccessExpressionNode {
                        TargetObject = result,
                        Member = new SimpleNameExpressionNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() }
                    };
                    copyScannerState(result, memberAccess);
                    setScannerState(memberAccess.Member);
                    memberAccess.Member.EndPosition = scanner.EndPosition;
                    result = memberAccess;
                    int endPosition = scanner.EndPosition;
                    nextLexicalUnit(false);
                    memberAccess.EndPosition = parseTypeArguments(memberAccess.Member.TypeArguments, true, endPosition);
                }
                break;

                case OpenParenthesis: {
                    if (result.ExpressionKind == ExpressionKind.Type) {
                        throw error(ParseErrorId.UnexpectedLexicalUnit);
                    }
                    var invocation = new InvocationExpressionNode { TargetObject =  result };
                    setScannerState(invocation);
                    invocation.StartPosition = result.StartPosition;
                    result = invocation;
                    nextLexicalUnit(true);
                    result.EndPosition = parseArguments(invocation.Arguments);
                }
                break;

                case OpenBracket: {
                    switch (result.ExpressionKind) {
                    case Type:
                    case ArrayCreation:
                        throw error(ParseErrorId.UnexpectedLexicalUnit);
                    }
                    nextLexicalUnit(true);
                    var elementAccess = new ElementAccessExpressionNode { TargetObject = result };
                    copyScannerState(result, elementAccess);
                    result = elementAccess;
                    result.EndPosition = parseExpressionList(elementAccess.Indexes);
                }
                break;

                case Increment: {
                    var unary = new UnaryExpressionNode { Operator = UnaryOperator.PostIncrement, Operand = result };
                    copyScannerState(result, unary);
                    result = unary;
                    result.EndPosition = scanner.EndPosition;
                    nextLexicalUnit(false);
                    break;
                }
                case Decrement: {
                    var unary = new UnaryExpressionNode { Operator = UnaryOperator.PostDecrement, Operand = result };
                    copyScannerState(result, unary);
                    result = unary;
                    result.EndPosition = scanner.EndPosition;
                    nextLexicalUnit(false);
                    break;
                }
                default:
                    if (result.ExpressionKind == ExpressionKind.Type) {
                        throw error(ParseErrorId.UnexpectedLexicalUnit);
                    }
                    return result;
                }
            }
        }

        private ExpressionNode parseSuperAccessExpression() {
            ExpressionNode result = null;
            var superAccess = new SuperAccessExpressionNode();
            setScannerState(superAccess);
            superAccess.EndPosition = scanner.EndPosition;
            switch (nextLexicalUnit(true)) {
            case Dot:
                if (!isIdentifier(nextLexicalUnit(true))) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                var memberAccess = new MemberAccessExpressionNode {
                    TargetObject = superAccess,
                    Member = new SimpleNameExpressionNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() }
                };
                copyScannerState(superAccess, memberAccess);
                copyScannerState(superAccess, memberAccess.Member);
                memberAccess.Member.EndPosition = scanner.EndPosition;
                result = memberAccess;
                int endPosition = scanner.EndPosition;
                nextLexicalUnit(false);
                memberAccess.EndPosition = parseTypeArguments(memberAccess.Member.TypeArguments, true, endPosition);
                break;

            case NullSafeMemberAccess:
                if (!isIdentifier(nextLexicalUnit(true))) {
                    throw error(ParseErrorId.IdentifierExpected);
                }
                var nullSafeMemberAccess = new NullSafeMemberAccessExpressionNode {
                    TargetObject = superAccess,
                    Member = new SimpleNameExpressionNode { NameOffset = scanner.StartPosition, NameLength = getLexicalUnitLength() }
                };
                copyScannerState(superAccess, nullSafeMemberAccess);
                copyScannerState(superAccess, nullSafeMemberAccess.Member);
                nullSafeMemberAccess.Member.EndPosition = scanner.EndPosition;
                result = nullSafeMemberAccess;
                int endPosition2 = scanner.EndPosition;
                nextLexicalUnit(false);
                nullSafeMemberAccess.EndPosition = parseTypeArguments(nullSafeMemberAccess.Member.TypeArguments, true, endPosition2);
                break;

			case OpenBracket:
                nextLexicalUnit(true);
                var elementAccess = new ElementAccessExpressionNode { TargetObject = superAccess };
                copyScannerState(superAccess, elementAccess);
                result = elementAccess;
                result.EndPosition = parseExpressionList(elementAccess.Indexes);
                break;

            default:
                throw error(ParseErrorId.DotExpected);
            }
            return result;
        }

        private ExpressionNode parseNewExpression() {
            ExpressionNode result = null;
            saveScannerState();
            switch (nextLexicalUnit(true)) {
            case OpenBracket: {
                int dims = parseDimensions();
                if (lexicalUnit != LexicalUnit.OpenBrace) {
                    throw error(ParseErrorId.OpenBraceExpected);
                }
                nextLexicalUnit(true);
                var arrayCreation = new ArrayCreationExpressionNode { Dimensions = dims, Initializer = parseArrayInitializer() };
                setSavedScannerState(arrayCreation);
                arrayCreation.EndPosition = arrayCreation.Initializer.EndPosition;
                result = arrayCreation;
            }
            break;

            case OpenBrace: {
                nextLexicalUnit(true);
                var objectCreation = new AnonymousObjectCreationExpressionNode();
                setSavedScannerState(objectCreation);
                result = objectCreation;
                while (lexicalUnit != LexicalUnit.CloseBrace) {
                    var memberDeclarator = new MemberInitializerNode();
                    setScannerState(memberDeclarator);
                    if (isIdentifier(lexicalUnit)) {
                        var restorePoint = this.createRestorePoint();
                        int sp = scanner.StartPosition;
                        int len = getLexicalUnitLength();
                        if (nextLexicalUnit(true) == LexicalUnit.Assign) {
                            nextLexicalUnit(true);
                            memberDeclarator.NameOffset = sp;
                            memberDeclarator.NameLength = len;
                            memberDeclarator.Value = parseExpression();
                            goto memberDeclaratorCreated;
                        }
                        this.restore(restorePoint);
                    }
                    memberDeclarator.Value = parsePrimaryExpression();
                    memberDeclarator.EndPosition = memberDeclarator.Value.EndPosition;
                    switch (memberDeclarator.Value.ExpressionKind) {
                    case SimpleName:
                    case MemberAccess:
                        break;

                    default:
                        throw error(ParseErrorId.NameOrMemberAccessExpected);
                    }
                memberDeclaratorCreated:
                    objectCreation.MemberDeclarators.add(memberDeclarator);
                    parseCommaOrCloseBrace();
                }
                result.EndPosition = scanner.EndPosition;
                nextLexicalUnit(false);
            }
            break;
            
            default: {
                var filename = savedFilename;
                int line = savedLine;
                int column = savedColumn;
                var disabledWarnings = savedDisabledWarnings;
                int startPosition = savedStartPosition;
                var type = parseType(true);
                if (type.TypeReferenceKind != TypeReferenceKind.Array) {
                    switch (lexicalUnit) {
                    case OpenBrace:
                        nextLexicalUnit(true);
                        var objectCreation = new ObjectCreationExpressionNode { Filename = filename, Line = line, Column = column,
                            DisabledWarnings = disabledWarnings, Type = type, Initializer = parseObjectOrCollectionInitializer(),
                            StartPosition = startPosition };
                        result = objectCreation;
                        result.EndPosition = objectCreation.Initializer.EndPosition;
                        break;

                    case OpenParenthesis:
                        objectCreation = new ObjectCreationExpressionNode { Filename = filename, Line = line, Column = column,
                            DisabledWarnings = disabledWarnings, Type = type, StartPosition = startPosition };
                        result = objectCreation;
                        nextLexicalUnit(true);
                        result.EndPosition = parseArguments(objectCreation.Arguments);
                        if (lexicalUnit == LexicalUnit.OpenBrace) {
                            nextLexicalUnit(true);
                            objectCreation.Initializer = parseObjectOrCollectionInitializer();
                            objectCreation.EndPosition = objectCreation.Initializer.EndPosition;
                        }
                        break;

                    case OpenBracket:
                        var arrayCreation = new ArrayCreationExpressionNode { Filename = filename, Line = line, Column = column,
                            DisabledWarnings = disabledWarnings, Type = type, StartPosition = startPosition };
                        result = arrayCreation;
                        bool noOpenBracket = false;
                        int endPosition = scanner.EndPosition;
                        while (nextLexicalUnit(true) != LexicalUnit.CloseBracket) {
                            arrayCreation.DimensionExpressions.add(parseExpression());
                            if (lexicalUnit != LexicalUnit.CloseBracket) {
                                throw error(ParseErrorId.CloseBracketExpected);
                            }
	                        endPosition = scanner.EndPosition;
                            if (nextLexicalUnit(false) != LexicalUnit.OpenBracket) {
                                noOpenBracket = true;
                                break;
                            }
                        }
                        if (!noOpenBracket && lexicalUnit == LexicalUnit.CloseBracket) {
	                        endPosition = scanner.EndPosition;
                            if (nextLexicalUnit(false) == LexicalUnit.OpenBracket) {
                                arrayCreation.Dimensions = parseDimensions() + 1;
	                        	endPosition = scanner.EndPosition;
                            } else {
                                arrayCreation.Dimensions = 1;
                            }
                        }
                        if (lexicalUnit == LexicalUnit.OpenBrace) {
                            nextLexicalUnit(true);
                            arrayCreation.Initializer = parseArrayInitializer();
                            result.EndPosition = arrayCreation.Initializer.EndPosition;
                        } else {
                        	result.EndPosition = endPosition;
                        }
                        break;
                    }
                } else if (lexicalUnit == LexicalUnit.OpenBrace) {
                    nextLexicalUnit(true);
                    var arrayCreation = new ArrayCreationExpressionNode { Type = type };
                    setSavedScannerState(arrayCreation);
                    arrayCreation.Initializer = parseArrayInitializer();
                    result = arrayCreation;
                    result.EndPosition = arrayCreation.Initializer.EndPosition;
                }
	            break;
            }
            }
            return result;
        }

        private ArrayInitializerExpressionNode parseArrayInitializer() {
            var result = new ArrayInitializerExpressionNode();
			setSavedScannerState(result);
            while (lexicalUnit != LexicalUnit.CloseBrace) {
                if (lexicalUnit == LexicalUnit.OpenBrace) {
                    nextLexicalUnit(true);
                    result.Values.add(parseArrayInitializer());
                } else {
                    result.Values.add(parseExpression());
                }
                parseCommaOrCloseBrace();
            }
			result.EndPosition = scanner.EndPosition;
            nextLexicalUnit(false);
            return result;
        }

        private InitializerExpressionNode parseObjectOrCollectionInitializer() {
            if (isIdentifier(lexicalUnit)) {
                var restorePoint = this.createRestorePoint();
                int sp = scanner.StartPosition;
                int len = getLexicalUnitLength();
                saveScannerState();
                if (nextLexicalUnit(true) == LexicalUnit.Assign) {
                    var objectInitializer = new ObjectInitializerExpressionNode();
                    setSavedScannerState(objectInitializer);
                    var memberInitializer = new MemberInitializerNode { NameOffset = sp, NameLength = len };
                    setSavedScannerState(memberInitializer);
                    if (nextLexicalUnit(true) == LexicalUnit.OpenBrace) {
                        nextLexicalUnit(true);
                        memberInitializer.Value = parseObjectOrCollectionInitializer();
                    } else {
                        memberInitializer.Value = parseExpression();
                    }
                    memberInitializer.EndPosition = memberInitializer.Value.EndPosition;
                    objectInitializer.MemberInitializers.add(memberInitializer);
                    while (lexicalUnit == LexicalUnit.Comma) {
                        if (!isIdentifier(nextLexicalUnit(true))) {
                            break;
                        }
                        sp = scanner.StartPosition;
                        len = getLexicalUnitLength();
		                saveScannerState();
                        if (nextLexicalUnit(true) != LexicalUnit.Assign) {
                            throw error(ParseErrorId.AssignExpected);
                        }
                        memberInitializer = new MemberInitializerNode { NameOffset = sp, NameLength = len };
	                    setSavedScannerState(memberInitializer);
                        if (nextLexicalUnit(true) == LexicalUnit.OpenBrace) {
                            nextLexicalUnit(true);
                            memberInitializer.Value = parseObjectOrCollectionInitializer();
                        } else {
                            memberInitializer.Value = parseExpression();
                        }
	                    memberInitializer.EndPosition = memberInitializer.Value.EndPosition;
                        objectInitializer.MemberInitializers.add(memberInitializer);
                    }
                    if (lexicalUnit != LexicalUnit.CloseBrace) {
                        throw error(ParseErrorId.CloseBraceExpected);
                    }
                   	objectInitializer.EndPosition = scanner.EndPosition;
                    nextLexicalUnit(false);
                    return objectInitializer;
                }
                this.restore(restorePoint);
            }
            var collectionInitializer = new CollectionInitializerExpressionNode();
            setScannerState(collectionInitializer);
            while (lexicalUnit != LexicalUnit.CloseBrace) {
                if (lexicalUnit == LexicalUnit.OpenBrace) {
                    nextLexicalUnit(true);
                    var values = new ArrayList<ExpressionNode>();
                    while (lexicalUnit != LexicalUnit.CloseBrace) {
                        values.add(parseExpression());
                        parseCommaOrCloseBrace();
                    }
                    nextLexicalUnit(false);
                    collectionInitializer.Values.add(values);
                } else {
                    var value = new ArrayList<ExpressionNode>();
                    value.add(parseExpression());
                    collectionInitializer.Values.add(value);
                }
                parseCommaOrCloseBrace();
            }
            collectionInitializer.EndPosition = scanner.EndPosition;
            nextLexicalUnit(false);
            return collectionInitializer;
        }

        private int parseArguments(List<ExpressionNode> arguments) {
            var first = true;
            while (lexicalUnit != LexicalUnit.CloseParenthesis) {
                if (first) {
                    first = false;
                } else {
                    if (lexicalUnit != LexicalUnit.Comma) {
                        throw error(ParseErrorId.CloseParenthesisExpected);
                    }
                    nextLexicalUnit(true);
                }
                arguments.add(parseExpression());
            }
            int result = scanner.EndPosition;
            nextLexicalUnit(false);
            return result;
        }

        private int parseExpressionList(List<ExpressionNode> expressions) {
            expressions.add(parseExpression());
            while (lexicalUnit != LexicalUnit.CloseBracket) {
                if (lexicalUnit != LexicalUnit.Comma) {
                    throw error(ParseErrorId.CloseBracketExpected);
                }
                nextLexicalUnit(true);
                expressions.add(parseExpression());
            }
            int result = scanner.EndPosition;
            nextLexicalUnit(false);
            return result;
        }

        private ExpressionNode parseAnnotationValue() {
            switch (lexicalUnit) {
            case OpenBracket:
                return parseAnnotationSection();
            case OpenBrace:
                saveScannerState();
                nextLexicalUnit(true);
                return parseAnnotationValueArrayInitializer();
            default:
                return parseConditionalExpression();
            }
        }

        private ArrayInitializerExpressionNode parseAnnotationValueArrayInitializer() {
            var result = new ArrayInitializerExpressionNode();
            setSavedScannerState(result);
            while (lexicalUnit != LexicalUnit.CloseBrace) {
                result.Values.add(parseAnnotationValue());
                parseCommaOrCloseBrace();
            }
            nextLexicalUnit(false);
            return result;
        }

        private AnnotationSectionNode parseAnnotationSection() {
            var result = new AnnotationSectionNode();
            setScannerState(result);
            if (!isIdentifier(nextLexicalUnit(true))) {
                throw error(ParseErrorId.IdentifierExpected);
            }
            do {
                var attribute = new AnnotationCreationNode();
                setScannerState(attribute);
                attribute.Type = parseType(true);
                if (lexicalUnit == LexicalUnit.OpenParenthesis) {
                    if (nextLexicalUnit(true) != LexicalUnit.CloseParenthesis) {
                        var namedValue = false;
                        if (isIdentifier(lexicalUnit)) {
                            var restorePoint = this.createRestorePoint();
                            int sp = scanner.StartPosition;
                            int len = getLexicalUnitLength();
                            saveScannerState();
                            if (nextLexicalUnit(true) == LexicalUnit.Assign) {
                                var objectInitializer = new ObjectInitializerExpressionNode();
                                setSavedScannerState(objectInitializer);
                                var memberInitializer = new MemberInitializerNode();
                                setSavedScannerState(memberInitializer);
                                memberInitializer.NameOffset = sp;
                                memberInitializer.NameLength = len;
                                nextLexicalUnit(true);
                                memberInitializer.Value = parseAnnotationValue();
                                memberInitializer.EndPosition = memberInitializer.Value.EndPosition;
                                objectInitializer.MemberInitializers.add(memberInitializer);
                                while (lexicalUnit == LexicalUnit.Comma) {
                                    if (!isIdentifier(nextLexicalUnit(true))) {
                                        break;
                                    }
                                    sp = scanner.StartPosition;
                                    len = getLexicalUnitLength();
                                    saveScannerState();
                                    if (nextLexicalUnit(true) != LexicalUnit.Assign) {
                                        throw error(ParseErrorId.AssignExpected);
                                    }
                                    memberInitializer = new MemberInitializerNode();
                                    setSavedScannerState(memberInitializer);
                                    memberInitializer.NameOffset = sp;
                                    memberInitializer.NameLength = len;
                                    nextLexicalUnit(true);
                                    memberInitializer.Value = parseAnnotationValue();
	                                memberInitializer.EndPosition = memberInitializer.Value.EndPosition;
                                    objectInitializer.MemberInitializers.add(memberInitializer);
                                    objectInitializer.EndPosition = memberInitializer.EndPosition;
                                }
                                attribute.Initializer = objectInitializer;
                                namedValue = true;
                            }
                            if (!namedValue) {
                                this.restore(restorePoint);
                            }
                        }
                        if (!namedValue) {
                            attribute.Initializer = parseAnnotationValue();
                        }
                        if (lexicalUnit != LexicalUnit.CloseParenthesis) {
                            throw error(ParseErrorId.CloseParenthesisExpected);
                        }
                    }
					attribute.EndPosition = scanner.EndPosition;
                    nextLexicalUnit(true);
                } else {
					attribute.EndPosition = attribute.Type.EndPosition;
				}
                result.Annotations.add(attribute);
            } while (lexicalUnit == LexicalUnit.Comma);
            if (lexicalUnit != LexicalUnit.CloseBracket) {
                throw error(ParseErrorId.CloseBracketExpected);
            }
			result.EndPosition = scanner.EndPosition;
            nextLexicalUnit(false);
            return result;
        }
        
        private TypeReferenceNode parseClassType(bool fail) {
            return parsePackageOrTypeName(fail);
        }

        private TypeReferenceNode parseInterfaceType(bool fail) {
            return parsePackageOrTypeName(fail);
        }

        private TypeReferenceNode parseType(bool fail) {
            return parseType(fail, false);
        }

        private TypeReferenceNode parseType(bool fail, bool allowVoid) {
            var result = parseNonArrayType(fail, allowVoid);
            if (lexicalUnit != LexicalUnit.OpenBracket) {
                return result;
            }
            var restorePoint = createRestorePoint();
            if (nextLexicalUnit(false) != LexicalUnit.CloseBracket) {
                restore(restorePoint);
                return result;
            }
            do {
                var arrayType = new ArrayTypeReferenceNode();
                copyScannerState(result, arrayType);
                arrayType.ElementType = result;
                result = arrayType;
                result.EndPosition = scanner.EndPosition;
                if (nextLexicalUnit(false) != LexicalUnit.OpenBracket) {
                    return result;
                }
            } while (nextLexicalUnit(true) == LexicalUnit.CloseBracket);
            throw error(ParseErrorId.CloseBracketExpected);
        }

        private int parseTypeArguments(List<TypeReferenceNode> typeArguments, bool primaryExpression, int endPosition) {
            if (lexicalUnit != LexicalUnit.LessThan) {
                return endPosition;
            }
            var restorePoint = this.createRestorePoint();
            var hasComma = false;
            nextLexicalUnit(true);
            while (true) {
            	int result;
                switch (lexicalUnit) {
                case QuestionMark:
                    saveScannerState();
                    int end = scanner.EndPosition;
                    if (nextLexicalUnit(true) == LexicalUnit.Colon) {
                        nextLexicalUnit(true);
                        var wildcard = new WildcardTypeReferenceNode(TypeReferenceKind.UpperBoundedWildcard);
                        setSavedScannerState(wildcard);
                        wildcard.Bound = parseType(true);
                        wildcard.EndPosition = wildcard.Bound.EndPosition;
                        typeArguments.add(wildcard);
                    } else {
                        var wildcard = new WildcardTypeReferenceNode(TypeReferenceKind.Wildcard);
                        setSavedScannerState(wildcard);
                        wildcard.EndPosition = end;
                        typeArguments.add(wildcard);
                    }
                    if (lexicalUnit == LexicalUnit.Comma) {
                        hasComma = true;
                        nextLexicalUnit(true);
                    }
                    break;
                    
                case GreaterThan:
                    if (typeArguments.size() == 0) {
                        throw error(ParseErrorId.IdentifierExpected);
                    }
                    result = scanner.EndPosition;
                    nextLexicalUnit(false);
                    return result;

                default:
                    var type = parseType(hasComma);
                    if (type == null) {
                        this.restore(restorePoint);
                        typeArguments.clear();
                        return endPosition;
                    }
                    typeArguments.add(type);
                    switch (lexicalUnit) {
                    case GreaterThan:
                    	result = scanner.EndPosition;
                        if (primaryExpression) {
                            switch (nextLexicalUnit(false)) {
                            case OpenParenthesis:
                            case CloseParenthesis:
                            case CloseBracket:
                            case Colon:
                            case SemiColon:
                            case Comma:
                            case Dot:
                            case QuestionMark:
                            case Equal:
                            case NotEqual:
                            case EndOfStream:
                                return result;

                            default:
                                this.restore(restorePoint);
                                typeArguments.clear();
                                return endPosition;
                            }
                        } else {
                            nextLexicalUnit(false);
                            return result;
                        }

                    case Colon:
                        if (nextLexicalUnit(true) != LexicalUnit.QuestionMark) {
                            throw error(ParseErrorId.WildcardExpected);
                        }
                        var wildcard = new WildcardTypeReferenceNode(TypeReferenceKind.LowerBoundedWildcard);
                        wildcard.Bound = typeArguments[typeArguments.size() - 1];
                        copyScannerState(wildcard.Bound, wildcard);
                        wildcard.EndPosition = scanner.EndPosition;
                        typeArguments.remove(typeArguments.size() - 1);
                        typeArguments.add(wildcard);
                        if (nextLexicalUnit(true) == LexicalUnit.Comma) {
                            hasComma = true;
                            nextLexicalUnit(true);
                        }
                        break;
                        
                    case Comma:
                        hasComma = true;
                        nextLexicalUnit(true);
                        break;

                    default:
                        if (hasComma) {
                            throw error(ParseErrorId.GreaterThanExpected);
                        } else {
                            this.restore(restorePoint);
                            typeArguments.clear();
                            return endPosition;
                        }
                    }
                    break;
                }
            }
        }

        private TypeReferenceNode parsePackageOrTypeName(bool fail) {
            if (!isIdentifier(lexicalUnit)) {
                if (fail) {
                    throw error(ParseErrorId.IdentifierExpected);
                } else {
                    return null;
                }
            }
            TypeReferenceNode result;
            int sp = scanner.StartPosition;
            int len = getLexicalUnitLength();
            var name = new SimpleNameTypeReferenceNode { StartPosition = sp };
            setScannerState(name);
            name.NameOffset = sp;
            name.NameLength = len;
            result = name;
            int endPosition = scanner.EndPosition;
            nextLexicalUnit(false);
            endPosition = parseTypeArguments(name.TypeArguments, false, endPosition);
            while (lexicalUnit == LexicalUnit.Dot) {
				result.EndPosition = scanner.StartPosition;
                nextLexicalUnit(fail);
                if (!isIdentifier(lexicalUnit)) {
                    if (fail) {
                        throw error(ParseErrorId.IdentifierExpected);
                    } else {
                        return null;
                    }
                }
                name = new SimpleNameTypeReferenceNode { StartPosition = scanner.StartPosition, EndPosition = scanner.EndPosition };
                setScannerState(name);
                name.NameOffset = scanner.StartPosition;
                name.NameLength = getLexicalUnitLength();
                var qualifiedType = new QualifiedTypeReferenceNode { EnclosingType = result, SimpleName = name,
                		StartPosition = result.StartPosition };
                copyScannerState(result, qualifiedType);
                result = qualifiedType;
                endPosition = scanner.EndPosition;
                nextLexicalUnit(false);
                endPosition = parseTypeArguments(name.TypeArguments,false, endPosition);
            }
			result.EndPosition = endPosition;
            return result;
        }

        private TypeReferenceNode parseNonArrayType(bool fail, bool allowVoid) {
            TypeReferenceNode result;
            switch (lexicalUnit) {
            case Identifier:
            case ContextualKeyword:
            case VerbatimIdentifier:
                result = parsePackageOrTypeName(fail);
                if (!fail && result == null) {
                    return null;
                }
                break;

            case Keyword:
                switch (scanner.Keyword) {
                case Byte:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.Byte) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                case Char:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.Char) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                case Short:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.Short) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                case Int:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.Int) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                case Long:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.Long) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                case Boolean:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.Boolean) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                case Double:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.Double) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                case String:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.String) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                case Float:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.Float) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                case Void:
                    result = new PrimitiveTypeReferenceNode(TypeReferenceKind.Void) { EndPosition = scanner.EndPosition };
                    setScannerState(result);
                    nextLexicalUnit(false);
                    break;

                default:
                    if (fail) {
                        throw error(ParseErrorId.IdentifierExpected);
                    } else {
                        return null;
                    }
                }
                break;

            default:
                if (fail) {
                    throw error(ParseErrorId.IdentifierExpected);
                } else {
                    return null;
                }
            }
            if (!allowVoid && result.TypeReferenceKind == TypeReferenceKind.Void) {
                if (fail) {
                    throw error(ParseErrorId.UnexpectedVoid);
                } else {
                    return null;
                }
            }
            return result;
        }
        
        private int parseDimensions() {
            int result = 0;
            while (nextLexicalUnit(true) == LexicalUnit.CloseBracket) {
                result++;
                if (nextLexicalUnit(false) != LexicalUnit.OpenBracket) {
                    return result;
                }
            }
            throw error(ParseErrorId.CloseBracketExpected);
        }

        private LiteralExpressionNode createLiteralExpression(LiteralKind literalKind) {
            var result = new LiteralExpressionNode { EndPosition = scanner.EndPosition };
            setScannerState(result);
            result.LiteralKind = literalKind;
            result.ValueOffset = scanner.StartPosition;
            result.ValueLength = getLexicalUnitLength();
            return result;
        }

        private TypeExpressionNode createTypeExpression(TypeReferenceKind typeReferenceKind) {
            var result = new TypeExpressionNode { EndPosition = scanner.EndPosition };
            setScannerState(result);
            var typeReference = new PrimitiveTypeReferenceNode(typeReferenceKind) { EndPosition = scanner.EndPosition };
            setScannerState(typeReference);
            result.TypeReference = typeReference;
            return result;
        }
        
        private void parseCommaOrCloseBrace() {
            switch (lexicalUnit) {
            default:
                throw error(ParseErrorId.CloseBraceExpected);

            case Comma:
                nextLexicalUnit(true);
                break;

            case CloseBrace:
                break;
            }
        }

        private int parseSemiColon(bool nextBefore, bool failAfter) {
            if (nextBefore) {
                nextLexicalUnit(true);
            }
        	int result = scanner.EndPosition;
            if (lexicalUnit != LexicalUnit.SemiColon) {
                if (newLineOccured) {
                    addError(ParseErrorId.SemiColonExpected);
                } else {
                    throw error(ParseErrorId.SemiColonExpected);
                }
            } else {
                nextLexicalUnit(failAfter);
            }
            return result;
        }
        
        private RestorePoint createRestorePoint() {
            return scanner.createRestorePoint();
        }

        private void restore(RestorePoint restorePoint) {
            scanner.restore(restorePoint);
            nextLexicalUnit(true);
        }
        
        private void saveScannerState() {
            savedFilename = scanner.Filename;
            savedLine = scanner.StartLine;
            savedColumn = scanner.StartColumn;
            savedDisabledWarnings = scanner.CodeErrorManager.DisabledWarnings;
			savedStartPosition = scanner.StartPosition;
        }

        private void setScannerState(SyntaxNode node) {
            node.Filename = scanner.Filename;
            node.Line = scanner.StartLine;
            node.Column = scanner.StartColumn;
            node.DisabledWarnings = scanner.CodeErrorManager.DisabledWarnings;
			if (node.StartPosition == -1) {
				node.StartPosition = scanner.StartPosition;
			}
        }

        private void setSavedScannerState(SyntaxNode node) {
            node.Filename = savedFilename;
            node.Line = savedLine;
            node.Column = savedColumn;
            node.DisabledWarnings = savedDisabledWarnings;
			if (node.StartPosition == -1) {
				node.StartPosition = savedStartPosition;
			}
        }

        private static void copyScannerState(SyntaxNode node1, SyntaxNode node2) {
            node2.Filename = node1.Filename;
            node2.Line = node1.Line;
            node2.Column = node1.Column;
            node2.DisabledWarnings = node1.DisabledWarnings;
            node2.StartPosition = node1.StartPosition;
            node2.EndPosition = node1.EndPosition;
        }
        
        private void addError(ParseErrorId id, params Object[] arguments) {
            scanner.addError(id, 0, Resources.getMessage(id, arguments));
        }
        
        private CodeErrorException error(ParseErrorId id, params Object[] arguments) {
            addError(id, arguments);
            return new CodeErrorException();
        }
        
        private int LexicalUnitLength {
            get {
                return scanner.EndPosition - scanner.StartPosition;
            }
        }
        
        private LexicalUnit nextLexicalUnit(bool fail) {
            return nextLexicalUnit(fail, LexicalUnit.EndOfStream);
        }

        private LexicalUnit nextLexicalUnit(bool fail, LexicalUnit expected) {
            newLineOccured = false;
            while (true) {
                switch (lexicalUnit = scanner.nextLexicalUnit()) {
                case NewLine:
                    newLineOccured = true;
                    break;

                case SingleLineComment:
                    if (docCommentEndPosition > 0 && !wasSingleLineDocComment) {
                        docCommentStartPosition = scanner.StartPosition;
                        docCommentEndPosition = 0;
                    }
                    if (this.LexicalUnitLength > 2 && scanner.Text[scanner.StartPosition + 2] == '/') {
                        if (docCommentEndPosition == 0 || !wasSingleLineDocComment) {
                            docCommentStartPosition = scanner.StartPosition;
                        }
                        wasSingleLineDocComment = true;
                        docCommentEndPosition = scanner.EndPosition;
                    } else {
                        docCommentEndPosition = 0;
                    }
                    break;
                    
                case DelimitedComment:
                    if (this.LexicalUnitLength > 4 && scanner.Text[scanner.StartPosition + 2] == '*') {
                        wasSingleLineDocComment = false;
                        docCommentStartPosition = scanner.StartPosition;
                        docCommentEndPosition = scanner.EndPosition;
                    } else {
                        docCommentEndPosition = 0;
                    }
                    break;
                    
                case Whitespace:
                    break;
                    
                default:
                    if (lexicalUnit == LexicalUnit.EndOfStream && fail) {
                        if (expected == LexicalUnit.CloseBrace) {
                            throw error(ParseErrorId.CloseBraceExpected);
                        } else {
                            throw error(ParseErrorId.UnexpectedEndOfStream);
                        }
                    }
                    return lexicalUnit;
                }
            }
        }
        
        private static bool isIdentifier(LexicalUnit lu) {
            switch (lu) {
            case Identifier:
            case VerbatimIdentifier:
            case ContextualKeyword:
                return true;
            default:
                return false;
            }
        }
    }
}
