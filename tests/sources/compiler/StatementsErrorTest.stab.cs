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
using org.junit;

namespace cnatural.compiler.test {

	public class StatementsErrorTest : ErrorTest {
		protected override String ResourcesPath {
			get {
				return "StatementsErrorTest";
			}
		}
		
		[Test]
		public void labelOutsideScope() {
			doTest("LabelOutsideScope", new String[]{ 
				"LabelOutsideScope.stab.cs (4, 13) error 273: No such label 'label' within the scope of the goto statement" },
				new String[] {});
		}
		
		[Test]
		public void whileNull() {
			doTest("WhileNull", new String[]{ 
				"WhileNull.stab.cs (3, 16) error 234: Cannot implicitly convert type '<null>' to 'boolean'" },
				new String[] {});
		}
		
		[Test]
		public void definiteAssignment() {
			doTest("DefiniteAssignment", new String[]{ 
				"DefiniteAssignment.stab.cs (3, 9) error 283: Variable 'i' is used but never initialized" },
				new String[] {});
		}
		
		[Test]
		public void definiteAssignment2() {
			doTest("DefiniteAssignment2", new String[]{ 
				"DefiniteAssignment2.stab.cs (4, 13) error 269: Use of unassigned local variable 'i'" },
				new String[] {});
		}
		
		[Test]
		public void definiteAssignment3() {
			doTest("DefiniteAssignment3", new String[]{ 
				"DefiniteAssignment3.stab.cs (6, 13) error 269: Use of unassigned local variable 'i'" },
				new String[] {});
		}
		
		[Test]
		public void definiteAssignment4() {
			doTest("DefiniteAssignment4", new String[]{ 
				"DefiniteAssignment4.stab.cs (10, 20) error 269: Use of unassigned local variable 'i'" },
				new String[] {
				"DefiniteAssignment4.stab.cs (13, 9) warning 289: Unreachable statement" });
		}
		
		[Test]
		public void definiteAssignment5() {
			doTest("DefiniteAssignment5", new String[]{ 
				"DefiniteAssignment5.stab.cs (13, 16) error 269: Use of unassigned local variable 'i'" },
				new String[] {});
		}
		
		[Test]
		public void definiteAssignment6() {
			doTest("DefiniteAssignment6", new String[]{ 
				"DefiniteAssignment6.stab.cs (22, 16) error 269: Use of unassigned local variable 'result'" },
				new String[] {});
		}
		
		[Test]
		public void definiteAssignment7() {
			doTest("DefiniteAssignment7", new String[]{ 
				"DefiniteAssignment7.stab.cs (7, 17) error 269: Use of unassigned local variable 'b'" },
				new String[] {});
		}
        
		[Test]
		public void varArrayInitializer() {
			doTest("VarArrayInitializer", new String[]{ 
				"VarArrayInitializer.stab.cs (3, 9) error 306: Cannot initialize implicitly-typed variable with an array initializer" },
				new String[] {});
		}
        
		[Test]
		public void localWildcard() {
			doTest("LocalWildcard", new String[]{ 
				"LocalWildcard.stab.cs (5, 24) error 324: Cannot create an instance of the type 'java.util.HashSet<?>'" },
				new String[] {});
		}
		
		[Test]
		public void genericExceptionHandler() {
			doTest("GenericExceptionHandler", new String[]{ 
				"GenericExceptionHandler.stab.cs (5, 18) error 326: Cannot use the type parameter 'T' in a catch clause" },
				new String[] {});
		}
		
		[Test]
		public void enumSwitchDuplicateCase() {
			doTest("EnumSwitchDuplicateCase", new String[]{ 
				"EnumSwitchDuplicateCase.stab.cs (8, 9) error 190: The same case already occurs in the 'switch' statement" },
				new String[] {});
		}
		
		[Test]
		public void enumConstantArgument() {
			doTest("EnumConstantArgument", new String[]{ 
				"EnumConstantArgument.stab.cs (2, 5) error 230: Type 'E' does not contain an accessible constructor with the specified arguments" },
				new String[] {});
		}

		[Test]
		public void foreachVarRedefinition() {
			doTest("ForeachVarRedefinition", new String[]{ 
				"ForeachVarRedefinition.stab.cs (4, 9) error 282: A variable named 'i' is already defined in this scope" },
				new String[] {});
		}
		
	}
}
