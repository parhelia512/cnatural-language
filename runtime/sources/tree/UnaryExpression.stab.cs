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

namespace stab.tree {

    public enum UnaryOperator {
		As,
        Cast,
        Complement,
        Minus,
        Not,
        Plus,
        PostDecrement,
        PostIncrement,
        PreDecrement,
        PreIncrement,
        Sizeof
    }

    public class UnaryExpression : Expression {
        UnaryExpression(Class<?> type, UnaryOperator operator, Expression operand)
            : super(ExpressionKind.Unary, type) {
            this.Operator = operator;
            this.Operand = operand;
        }
        
        public UnaryOperator Operator^;
        
        public Expression Operand^;
    }
}