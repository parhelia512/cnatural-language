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

	public class SwitchStatement : Statement {
		SwitchStatement(Expression expression, SwitchSection[] sections)
			: super(StatementKind.Switch) {
			this.Expression = expression;
            this.Sections = (sections == null) ? Query.empty<SwitchSection>() : Query.asIterable((SwitchSection[])sections.clone());
		}
		
		public Expression Expression^;
		
		public Iterable<SwitchSection> Sections^;
	}

	public class SwitchSection {
		SwitchSection(SwitchLabel[] labels, Statement[] statements) {
            this.Labels = (labels == null) ? Query.empty<SwitchLabel>() : Query.asIterable((SwitchLabel[])labels.clone());
            this.Statements = (statements == null) ? Query.empty<Statement>() : Query.asIterable((Statement[])statements.clone());
		}
		
		public Iterable<SwitchLabel> Labels^;
		
		public Iterable<Statement> Statements^;
	}

	public class SwitchLabel {
		SwitchLabel(String name, int value, bool defaultLabel) {
			this.Name = name;
			this.Value = value;
			this.Default = defaultLabel;
		}
	
		public String Name^;
		
		public int Value^;
		
		public bool Default^;
	}
	
	public class GotoCaseStatement : Statement {
		GotoCaseStatement(SwitchLabel label)
			: super(StatementKind.GotoCase) {
			this.Label = label;
		}
	
		public SwitchLabel Label^;
	}
}
