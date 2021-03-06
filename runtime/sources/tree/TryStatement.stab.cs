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
 
namespace stab.tree {

	public class TryStatement : Statement {
		TryStatement(BlockStatement body, CatchClause[] catchClauses, BlockStatement finallyBlock)
			: super(StatementKind.Try) {
			this.Body = body;
			this.CatchClauses = (catchClauses == null) ? Query.empty<CatchClause>() : Query.asIterable((CatchClause[])catchClauses.clone());
			this.Finally = finallyBlock;
		}
		
		public BlockStatement Body^;

		public Iterable<CatchClause> CatchClauses^;
		
		public BlockStatement Finally^;
	}
	
	public class CatchClause {
		CatchClause(VariableExpression variable, BlockStatement body) {
			this.Variable = variable;
			this.Body = body;
		}
		
		public VariableExpression Variable^;
		
		public BlockStatement Body^;
	}
}
