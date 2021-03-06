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
using cnatural.helpers;

namespace cnatural.parser {
    
    public enum ParseErrorId {
        InternalError,
        ErrorDiagnostic,
        WarningDiagnostic,
        HexadecimalDigitExpected,
        InvalidEscapeSequence,
        UnclosedDelimitedComment,
        InvalidSourceCodeChar,
        DecimalDigitsExpected,
        MalformedHexadecimalNumber,
        UnclosedChar,
        MalformedChar,
        UnclosedString,
        UnclosedVerbatimString,
        InvalidPreprocessorChar,
        UnclosedFilename,
        MisplacedNumberSign,
        MalformedPreprocessorDirective,
        PreprocessorSymbolExpected,
        EndregionExpected,
        InvalidNumber,
        NewLineExpected,
        WhitespaceExpected,
        OpenParenthesisExpected,
        CloseParenthesisExpected,
        EndifExpected,
        WarningExpected,
        IdentifierExpected,
        PackageWithModifiers,
        OpenBraceExpected,
        CloseBraceExpected,
        ClassInterfaceEnumExpected,
        ClassInterfaceEnumDelegateExpected,
        NoPackageMembers,
        DuplicateModifier,
        UnexpectedEndOfStream,
        SemiColonExpected,
        OpenBracketExpected,
        CloseBracketExpected,
        UnexpectedVoid,
        GreaterThanExpected,
        WildcardExpected,
        AssignExpected,
        CommaExpected,
        NameOrMemberAccessExpected,
        DotExpected,
        UnexpectedLexicalUnit,
        ColonExpected,
        SelectOrGroupExpected,
        ByExpected,
        InExpected,
        OnExpected,
        EqualsExpected,
        WhileExpected,
        FinallyExpected,
        ReturnOrBreakExpected,
        SimpleNameExpected,
        SuperOrThisExpected,
        TypeExpected,
        ArrayTypeExpected,
        DuplicateAccessor,
        AccessorExpected,
        UnexpectedVariableDeclaration,
        UnexpectedLabeledStatement,
    }
    
    class Resources {
        private static ResourceManager resourceManager;
        static Resources() {
            ResourceLocale = Locale.getDefault();
        }

        static ResourceManager ResourceManager {
            get {
                if (resourceManager == null) {
                    resourceManager = new ResourceManager(typeof(Resources), "messages");
                }
                return resourceManager;
            }
        }
        
        static Locale ResourceLocale {
            get;
            set;
        }
        
        static String getMessage(ParseErrorId id, params Object[] arguments) {
            return ResourceManager.getMessage(ResourceLocale, id.toString(), arguments);
        }
    }
}
