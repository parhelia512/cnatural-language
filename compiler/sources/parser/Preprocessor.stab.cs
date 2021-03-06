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
using cnatural.helpers;

namespace cnatural.parser {

    public class PreprocessedText {
        PreprocessedText(char[] text, Iterable<InputSectionPart> inputSectionParts) {
            this.Text = text;
            this.InputSectionParts = inputSectionParts;
        }

        public char[] Text^;

        public Iterable<InputSectionPart> InputSectionParts^;
    }
    
    public class Preprocessor {
        private PreprocessorScanner scanner;
        private PreprocessorLexicalUnit lexicalUnit;
        private int[] warnings;
    
        public Preprocessor(CodeErrorManager codeErrorManager, char[] text) {
            this.scanner = new PreprocessorScanner(codeErrorManager, text);
            this.Symbols = new HashSet<String>();
            this.warnings = new int[16];
        }

        public String Filename {
            get {
                return scanner.Filename;
            }
            set {
                scanner.Filename = value;
            }
        }
        
        public char[] Text {
            get {
                return scanner.Text;
            }
        }
        
        public HashSet<String> Symbols^;

        public final PreprocessedText preprocess() {
            var parts = new ArrayList<InputSectionPart>();
            parseSource(scanner.Position, false, parts, true);
            return new PreprocessedText(this.Text, parts);
        }

        private int parseSource(int position, bool skippedSection, ArrayList<InputSectionPart> parts, bool fail) {
            int line = scanner.Line;
            nextLexicalUnit(skippedSection);
            for (; ; ) {
                switch (lexicalUnit) {
                case EndOfStream:
                    return position;

                case SourceCode:
                    int endPosition = scanner.Position;
                    while (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.SourceCode) {
                        endPosition = scanner.Position;
                    }
                    parts.add(new SourceCodeSectionPart(position, endPosition - position, line));
                    position = endPosition;
                    break;

                case NumberSign:
                    line = scanner.Line + 1;
                    parseOptionalWhitespace(skippedSection);
                    switch (lexicalUnit) {
                    case Define:
                        position = parseDefinition(position, skippedSection, true, parts);
                        break;

                    case Undef:
                        position = parseDefinition(position, skippedSection, false, parts);
                        break;

                    case Region:
                        position = parseRegion(position, skippedSection, parts);
                        break;

                    case Error:
                        position = parseDiagnostic(position, skippedSection, true, parts);
                        break;

                    case Warning:
                        position = parseDiagnostic(position, skippedSection, false, parts);
                        break;

                    case Line:
                        position = parseLine(position, skippedSection, parts);
                        break;

                    case Pragma:
                        position = parsePragma(position, skippedSection, parts);
                        break;

                    case If:
                        position = parseConditional(position, skippedSection, parts);
                        break;

                    default:
                        if (fail) {
                            throw scanner.error(ParseErrorId.MalformedPreprocessorDirective);
                        } else {
                            return position;
                        }
                    }
                    line = scanner.Line - 1;
                    break;

                default:
                    throw scanner.error(ParseErrorId.InternalError);
                }
            }
        }
        
        private int parseDefinition(int position, bool skippedSection, bool define, ArrayList<InputSectionPart> parts) {
            parseWhitespace(skippedSection);
            String symbol;
            int symbolOffset = scanner.Position;
            switch (nextLexicalUnit(skippedSection)) {
            case Symbol:
                symbol = getSymbol(symbolOffset, scanner.Position - symbolOffset);
                break;

            case Define:
                symbol = "define";
                break;

            case Undef:
                symbol = "undef";
                break;

            case Warning:
                symbol = "warning";
                break;

            case Region:
                symbol = "region";
                break;

            case Pragma:
                symbol = "pragma";
                break;

            case Line:
                symbol = "line";
                break;

            case Error:
                symbol = "error";
                break;

            case Endregion:
                symbol = "endregion";
                break;

            case Endif:
                symbol = "endif";
                break;

            case Else:
                symbol = "else";
                break;

            default:
                throw scanner.error(ParseErrorId.PreprocessorSymbolExpected);
            }
            int line = scanner.Line;
            parseNewLine(true);
            parts.add(new DefinitionSectionPart(position, scanner.Position - position, line, define, symbol));
            position = scanner.Position;
            nextLexicalUnit(skippedSection);
            if (!skippedSection) {
                if (define) {
                    Symbols.add(symbol);
                } else {
                    Symbols.remove(symbol);
                }
            }
            return position;
        }

        private int parseRegion(int position, bool skippedSection, ArrayList<InputSectionPart> parts) {
            var smessage = "";
            if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                int messageOffset = scanner.Position;
                scanner.scanMessage();
                smessage = new String(this.Text, messageOffset, scanner.Position - messageOffset);
                nextLexicalUnit(skippedSection);
            }
            checkNewLine();
            var subParts = new ArrayList<InputSectionPart>();
            parseSource(scanner.Position, skippedSection, subParts, false);
            if (lexicalUnit != PreprocessorLexicalUnit.Endregion) {
                throw scanner.error(ParseErrorId.EndregionExpected);
            }
            String emessage = "";
            if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                int messageOffset = scanner.Position;
                scanner.scanMessage();
                emessage = new String(this.Text, messageOffset, scanner.Position - messageOffset);
                nextLexicalUnit(skippedSection);
            }
            int line = scanner.Line;
            checkWhitespaceOrNewLine();
            parts.add(new RegionSectionPart(position, scanner.Position - position, line, subParts, smessage, emessage));
            position = scanner.Position;
            nextLexicalUnit(skippedSection);
            return position;
        }
        
        private int parseDiagnostic(int position, bool skippedSection, bool error, ArrayList<InputSectionPart> parts) {
            if (skippedSection) {
                int p = ignoreDirective(position);
                parts.add(new DiagnosticSectionPart(position, p - position, scanner.Line, error, ""));
                return p;
            }
            var message = "";
            if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                int messageOffset = scanner.Position;
                scanner.scanMessage();
                message = new String(this.Text, messageOffset, scanner.Position - messageOffset);
                nextLexicalUnit(skippedSection);
            }
            int line = scanner.Line;
            checkWhitespaceOrNewLine();
            parts.add(new DiagnosticSectionPart(position, scanner.Position - position, line, error, message));
            position = scanner.Position;
            nextLexicalUnit(skippedSection);
            return position;
        }
        
        private int parseLine(int position, bool skippedSection, ArrayList<InputSectionPart> parts) {
            if (skippedSection) {
                int p = ignoreDirective(position);
                parts.add(new LineSectionPart(position, p - position, scanner.Line, false));
                return p;
            }
            parseWhitespace(skippedSection);
            int arg1Offset = scanner.Position;
            switch (nextLexicalUnit(skippedSection)) {
            case DecimalDigits:
                int line;
                try {
                    line = Integer.parseInt(new String(this.Text, arg1Offset, scanner.Position - arg1Offset));
                } catch (NumberFormatException e) {
                    throw scanner.error(ParseErrorId.InvalidNumber);
                }
                var filename = "";
                if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                    int filenameOffset = scanner.Position;
                    if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.Filename) {
                        filename = new String(this.Text, filenameOffset + 1, scanner.Position - filenameOffset - 2);
                        nextLexicalUnit(skippedSection);
                    }
                }
                parseNewLine(false);
                parts.add(new LineSectionPart(position, scanner.Position - position, line, filename));
                break;

            case Symbol:
                var hidden = false;
                switch (this.Text[arg1Offset]) {
                case 'd':
                    if (!isIdentifier("default", arg1Offset, scanner.Position - arg1Offset)) {
                        throw scanner.error(ParseErrorId.DecimalDigitsExpected);
                    }
                    break;
                case 'h':
                    if (!isIdentifier("hidden", arg1Offset, scanner.Position - arg1Offset)) {
                        throw scanner.error(ParseErrorId.DecimalDigitsExpected);
                    }
                    hidden = true;
                    break;
                default:
                    throw scanner.error(ParseErrorId.MalformedPreprocessorDirective);
                }
                line = scanner.Line;
                parseNewLine(true);
                parts.add(new LineSectionPart(position, scanner.Position - position, line, hidden));
                break;
            default:
                throw scanner.error(ParseErrorId.DecimalDigitsExpected);
            }
            position = scanner.Position;
            nextLexicalUnit(skippedSection);
            return position;
        }

        private int parsePragma(int position, bool skippedSection, ArrayList<InputSectionPart> parts) {
            if (skippedSection) {
                int p = ignoreDirective(position);
                parts.add(new PragmaSectionPart(position, p - position, scanner.Line, false, Query.emptyInt()));
                return p;
            }
            parseWhitespace(skippedSection);
            if (nextLexicalUnit(skippedSection) != PreprocessorLexicalUnit.Warning) {
                throw scanner.error(ParseErrorId.WarningExpected);
            }
            parseWhitespace(skippedSection);
            int actionOffset = scanner.Position;
            if (nextLexicalUnit(skippedSection) != PreprocessorLexicalUnit.Symbol) {
                throw scanner.error(ParseErrorId.MalformedPreprocessorDirective);
            }
            var restore = false;
            switch (this.Text[actionOffset]) {
            case 'd':
                if (!isIdentifier("disable", actionOffset, scanner.Position - actionOffset)) {
                    throw scanner.error(ParseErrorId.MalformedPreprocessorDirective);
                }
                break;
            case 'r':
                if (!isIdentifier("restore", actionOffset, scanner.Position - actionOffset)) {
                    throw scanner.error(ParseErrorId.MalformedPreprocessorDirective);
                }
                restore = true;
                break;
            default:
                throw scanner.error(ParseErrorId.MalformedPreprocessorDirective);
            }
            int nwarnings = 0;
            if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                int numberOffset = scanner.Position;
                if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.DecimalDigits) {
                    int warning;
                    try {
                        warning = Integer.parseInt(new String(this.Text, numberOffset, scanner.Position - numberOffset));
                    } catch (NumberFormatException e) {
                        throw scanner.error(ParseErrorId.InvalidNumber);
                    }
                    warnings[nwarnings++] = warning;
                    do {
                        parseOptionalWhitespace(skippedSection);
                        if (lexicalUnit == PreprocessorLexicalUnit.Comma) {
                            numberOffset = scanner.Position;
                            if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                                numberOffset = scanner.Position;
                                nextLexicalUnit(skippedSection);
                            }
                            if (lexicalUnit != PreprocessorLexicalUnit.DecimalDigits) {
                                throw scanner.error(ParseErrorId.DecimalDigitsExpected);
                            }
                            try {
                                warning = Integer.parseInt(new String(this.Text, numberOffset, scanner.Position - numberOffset));
                            } catch (NumberFormatException e) {
                                throw scanner.error(ParseErrorId.InvalidNumber);
                            }
                            if (nwarnings == sizeof(warnings)) {
                                var t = new int[nwarnings * 2];
                                System.arraycopy(warnings, 0, t, 0, nwarnings);
                                warnings = t;
                            }
                            warnings[nwarnings++] = warning;
                        }
                    } while (lexicalUnit == PreprocessorLexicalUnit.DecimalDigits);
                }
            }
            int line = scanner.Line;
            parseNewLine(false);
            var t = new int[nwarnings];
            System.arraycopy(warnings, 0, t, 0, nwarnings);
            parts.add(new PragmaSectionPart(position, scanner.Position - position, line, restore, Query.asIterable(t)));
            position = scanner.Position;
            nextLexicalUnit(skippedSection);
            return position;
        }

        private int parseConditional(int position, bool skippedSection, ArrayList<InputSectionPart> parts) {
            var sectionFound = false;
            do {
                var lu = lexicalUnit;
                int line = scanner.Line;
                var skip = sectionFound;
                var eval = false;
                if (lu == PreprocessorLexicalUnit.Else) {
                    nextLexicalUnit(skippedSection);
                } else {
                    parseWhitespace(skippedSection);
                    int expressionOffset = scanner.Position;
                    nextLexicalUnit(skippedSection);
                    eval = parseExpression(expressionOffset, skippedSection);
                    skip |= !eval;
                    if (!sectionFound) {
                        sectionFound = eval;
                    }
                }
                skip |= skippedSection;
                parseNewLine(false);
                var subParts = new ArrayList<InputSectionPart>();
                int newOffset = parseSource(scanner.Position, skip, subParts, false);
                switch (lu) {
                    case If:
                        parts.add(new IfSectionPart(position, newOffset - position, line, subParts, skip, eval));
                        break;
                    case Elif:
                        parts.add(new ElifSectionPart(position, newOffset - position, line, subParts, skip, eval));
                        break;
                    case Else:
                        parts.add(new ElseSectionPart(position, newOffset - position, line, subParts, skip));
                        break;
                }
                position = newOffset;
                switch (lexicalUnit) {
                    case Elif:
                    case Else:
                    case Endif:
                        break;
                    default:
                        throw scanner.error(ParseErrorId.EndifExpected);
                }
            } while (lexicalUnit != PreprocessorLexicalUnit.Endif);
			int line = scanner.Line;
            parseNewLine(true);
			parts.add(new EndifSectionPart(position, scanner.Position - position, line));

            position = scanner.Position;
            nextLexicalUnit(skippedSection);
            return position;
        }

        private bool parseExpression(int position, bool skippedSection) {
            return parseOrExpression(position, skippedSection);
        }

        private bool parseOrExpression(int position, bool skippedSection) {
            var leftEval = parseAndExpression(position, skippedSection);
            for (; ; ) {
                if (lexicalUnit == PreprocessorLexicalUnit.WhiteSpace) {
                    position = scanner.Position;
                    nextLexicalUnit(skippedSection);
                }
                bool rightEval = false;
                if (lexicalUnit == PreprocessorLexicalUnit.Or) {
                    position = scanner.Position;
                    if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                        position = scanner.Position;
                        nextLexicalUnit(skippedSection);
                    }
                    rightEval = parseAndExpression(position, skippedSection);
                    leftEval = leftEval || rightEval;
                } else {
                    return leftEval;
                }
            }
        }

        private bool parseAndExpression(int position, bool skippedSection) {
            bool leftEval = parseEqualityExpression(position, skippedSection);
            for (;;) {
                if (lexicalUnit == PreprocessorLexicalUnit.WhiteSpace) {
                    position = scanner.Position;
                    nextLexicalUnit(skippedSection);
                }
                bool rightEval = false;
                if (lexicalUnit == PreprocessorLexicalUnit.And) {
                    position = scanner.Position;
                    if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                        position = scanner.Position;
                        nextLexicalUnit(skippedSection);
                    }
                    rightEval = parseEqualityExpression(position, skippedSection);
                    leftEval = leftEval && rightEval;
                } else {
                    return leftEval;
                }
            }
        }

        private bool parseEqualityExpression(int position, bool skippedSection) {
            var leftEval = parseUnaryExpression(position, skippedSection);
            for (; ; ) {
                if (lexicalUnit == PreprocessorLexicalUnit.WhiteSpace) {
                    position = scanner.Position;
                    nextLexicalUnit(skippedSection);
                }
                bool rightEval = false;
                switch (lexicalUnit) {
                case Equal:
                    position = scanner.Position;
                    if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                        position = scanner.Position;
                        nextLexicalUnit(skippedSection);
                    }
                    rightEval = parseEqualityExpression(position, skippedSection);
                    leftEval = leftEval == rightEval;
                    break;

                case NotEqual:
                    position = scanner.Position;
                    if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                        position = scanner.Position;
                        nextLexicalUnit(skippedSection);
                    }
                    rightEval = parseEqualityExpression(position, skippedSection);
                    leftEval = leftEval != rightEval;
                    break;

                default:
                    return leftEval;
                }
            }
        }
        
        private bool parseUnaryExpression(int position, bool skippedSection) {
            var negate = false;
            while (lexicalUnit == PreprocessorLexicalUnit.Not) {
                negate = !negate;
                position = scanner.Position;
                if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                    position = scanner.Position;
                    nextLexicalUnit(skippedSection);
                }
            }
            bool eval = parsePrimaryExpression(position, skippedSection);
            return (negate) ? !eval : eval;
        }

        private bool parsePrimaryExpression(int position, bool skippedSection) {
            var result = false;
            switch (lexicalUnit) {
            case Symbol:
                String symbol = getSymbol(position, scanner.Position - position);
                if (symbol.equals("true")) {
                    result = true;
                } else if (!symbol.equals("false")) {
                    result = this.Symbols.contains(symbol);
                }
                break;

            case LeftParenthesis:
                position = scanner.Position;
                if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                    position = scanner.Position;
                    nextLexicalUnit(skippedSection);
                }
                result = parseExpression(position, skippedSection);
                if (lexicalUnit == PreprocessorLexicalUnit.WhiteSpace) {
                    nextLexicalUnit(skippedSection);
                }
                if (lexicalUnit != PreprocessorLexicalUnit.RightParenthesis) {
                    throw scanner.error(ParseErrorId.CloseParenthesisExpected);
                }
                break;

            default:
                throw scanner.error(ParseErrorId.PreprocessorSymbolExpected);
            }
            nextLexicalUnit(skippedSection);
            return result;
        }

        private int ignoreDirective(int position) {
            while (true) {
                switch (nextLexicalUnit(true)) {
                    case EndOfStream:
                        return scanner.Position;

                    case NewLine:
                        position = scanner.Position;
                        nextLexicalUnit(true);
                        return position;
                }
            }
        }

        private void parseNewLine(bool advance) {
            if (advance) {
                nextLexicalUnit(false);
            }
            if (lexicalUnit == PreprocessorLexicalUnit.WhiteSpace) {
                nextLexicalUnit(false);
            }
            if (lexicalUnit == PreprocessorLexicalUnit.SingleLineComment) {
                nextLexicalUnit(false);
            }
            checkWhitespaceOrNewLine();
        }

        private void parseOptionalWhitespace(bool skippedSection) {
            if (nextLexicalUnit(skippedSection) == PreprocessorLexicalUnit.WhiteSpace) {
                nextLexicalUnit(skippedSection);
            }
        }

        private void parseWhitespace(bool skippedSection) {
            if (nextLexicalUnit(skippedSection) != PreprocessorLexicalUnit.WhiteSpace) {
                throw scanner.error(ParseErrorId.WhitespaceExpected);
            }
        }

        private void checkWhitespaceOrNewLine() {
            if (lexicalUnit != PreprocessorLexicalUnit.EndOfStream && lexicalUnit != PreprocessorLexicalUnit.NewLine) {
                throw scanner.error(ParseErrorId.NewLineExpected);
            }
        }

        private void checkNewLine() {
            if (lexicalUnit != PreprocessorLexicalUnit.NewLine) {
                throw scanner.error(ParseErrorId.NewLineExpected);
            }
        }
        
        private String getSymbol(int offset, int length) {
            var hasUnicode = false;
            for (int i = 0; i < length; i++) {
                if (this.Text[offset + i] == '\\') {
                    hasUnicode = true;
                    break;
                }
            }
            if (hasUnicode) {
                var sb = new StringBuilder();
                for (int i = 0; i < length; i++) {
                    char c = this.Text[offset + i];
                    if (c == '\\') {
                        i++;
                        sb.append((char)scanUnicodeEscapeSequence(this.Text, offset + i));
                        if (this.Text[offset + i] == 'u') {
                            i += 4;
                        } else {
                            i += 8;
                        }
                    } else {
                        sb.append(c);
                    }
                }
                return sb.toString();
            } else {
                return new String(this.Text, offset, length);
            }
        }

        private int scanUnicodeEscapeSequence(char[] buffer, int offset) {
            var result = 0;
            switch (buffer[offset]) {
                case 'u':
                    for (int i = 1; i < 5; i++) {
                        int value;
                        if ((value = ParserHelper.scanHexDigit(buffer[offset + i])) == -1) {
                            throw scanner.error(ParseErrorId.HexadecimalDigitExpected);
                        }
                        result = result * 16 + value;
                    }
                    return result;
                case 'U':
                    for (int i = 1; i < 9; i++) {
                        int value;
                        if ((value = ParserHelper.scanHexDigit(buffer[offset + i])) == -1) {
                            throw scanner.error(ParseErrorId.HexadecimalDigitExpected);
                        }
                        result = result * 16 + value;
                    }
                    return result;
                default:
                    throw scanner.error(ParseErrorId.InvalidEscapeSequence);
            }
        }
        
        private bool isIdentifier(String identifier, int offset, int length) {
            if (identifier.length() != length) {
                return false;
            }
            for (int i = 0; i < identifier.length(); i++) {
                if (this.Text[offset + i] != identifier[i]) {
                    return false;
                }
            }
            return true;
        }
        
        private PreprocessorLexicalUnit nextLexicalUnit(bool skippedSection) {
            return lexicalUnit = scanner.nextLexicalUnit(skippedSection);
        }
    }
}
