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

	public class ObjectModelErrorTest : ErrorTest {
		protected override String ResourcesPath {
			get {
				return "ObjectModelErrorTest";
			}
		}
		
		[Test]
		public void multipleBaseClasses() {
			doTest("MultipleBaseClasses", new String[]{ 
		"MultipleBaseClasses.stab.cs (1, 36) error 287: Class 'C' cannot have multiple base classes: 'java.lang.String' and 'java.lang.Object'" },
				new String[] {});
		}
		
		[Test]
		public void sameNameClasses() {
			doTest("SameNameClasses", new String[]{ "SameNameClasses.stab.cs (4, 14) error 169: A type 'C' is already defined" },
				new String[] {});
		}
		
		[Test]
		public void baseMissingDefaultConstructor() {
			doTest("BaseMissingDefaultConstructor", new String[]{
				"BaseMissingDefaultConstructor.stab.cs (6, 14) error 174: 'Base' does not contain a constructor without argument" },
				new String[] {});
		}
		
		[Test]
		public void constructorTypo() {
			doTest("ConstructorTypo", new String[]{ "ConstructorTypo.stab.cs (2, 12) error 220: Method 'X' must have a return type" },
				new String[] {});
		}
		
		[Test]
		public void staticInvocation() {
			doTest("StaticInvocation", new String[]{
				"StaticInvocation.stab.cs (6, 9) error 327: A static member 'test' cannot be resolved in this context" },
				new String[] {});
		}
		
		[Test]
		public void staticThisAccess() {
			doTest("StaticThisAccess", new String[]{ "StaticThisAccess.stab.cs (5, 9) error 262: Keyword 'this' is not valid in a static context" },
				new String[] {});
		}
		
		[Test]
		public void classInInterfaceList() {
			doTest("ClassInInterfaceList", new String[]{
				"ClassInInterfaceList.stab.cs (1, 26) error 180: Interface 'Iface' cannot have a base class: 'java.lang.Object'" },
				new String[] {});
		}
		
		[Test]
		public void unexpectedModifiers() {
			doTest("UnexpectedModifiers",
				new String[]{ "UnexpectedModifiers.stab.cs (1, 25) error 274: Modifier 'protected' cannot be used here",
							  "UnexpectedModifiers.stab.cs (1, 25) error 274: Modifier 'private' cannot be used here" },
				new String[] {});
		}
		
		[Test]
		public void methodOverride() {
			doTest("MethodOverride", new String[]{
				"MethodOverride.stab.cs (2, 26) error 239: No method 'm()' found to override in base types of 'C'" },
				new String[] {});
		}
		
		[Test]
		public void sameNameGenericClasses() {
			doTest("SameNameGenericClasses", new String[]{ "SameNameGenericClasses.stab.cs (4, 18) error 169: A type 'C' is already defined" },
				new String[] {});
		}
		
		[Test]
		public void finalBaseClass() {
			doTest("FinalBaseClass", new String[]{ "FinalBaseClass.stab.cs (4, 18) error 194: 'B': cannot derive from final class 'A'" },
				new String[] {});
		}
		
		[Test]
		public void unimplementedAbstractMethod() {
			doTest("UnimplementedAbstractMethod", new String[]{
				"UnimplementedAbstractMethod.stab.cs (5, 14) error 161: 'D' does not implement inherited abstract method 'C.m()'" },
				new String[] {});
		}
		
		[Test]
		public void circularBaseTypeDependency() {
			doTest("CircularBaseTypeDependency", new String[]{
				"CircularBaseTypeDependency.stab.cs (1, 7) error 179: Circular base type dependency involving 'B' and 'D'" },
				new String[] {});
		}
		
		[Test]
		public void overridePrivate() {
			doTest("OverridePrivate", new String[]{
				"OverridePrivate.stab.cs (7, 26) error 239: No method 'm()' found to override in base types of 'D'" },
				new String[] {});
		}
		
		[Test]
		public void overridePackagePrivate() {
			doTest("OverridePackagePrivate", new String[]{
				"OverridePackagePrivate.stab.cs (10, 30) error 239: No method 'm()' found to override in base types of 'p2.D'" },
				new String[] {});
		}
		
		[Test]
		public void outerClassMethod() {
			doTest("OuterClassMethod", new String[]{
				"OuterClassMethod.stab.cs (7, 13) error 276: The name 'm1' cannot be resolved in this context" },
				new String[] {});
		}
		
		[Test]
		public void finalizeOverriding() {
			doTest("FinalizeOverriding", new String[]{
				"FinalizeOverriding.stab.cs (2, 26) error 197: A class cannot declare a 'finalize' method. Use the destructor syntax instead" },
				new String[] {});
		}
		
		[Test]
		public void covariance() {
			doTest("Covariance", new String[]{
		"Covariance.stab.cs (10, 28) error 208: 'CovarianceAux.copy()' has a return type incompatible with the return type of 'Covariance.copy()'" },
				new String[] {});
		}
		
		[Test]
		public void covariance2() {
			doTest("Covariance2", new String[]{
		"Covariance2.stab.cs (8, 19) error 208: 'CovarianceAux.copy()' has a return type incompatible with the return type of 'Covariance.copy()'" },
				new String[] {});
		}
		
		[Test]
		public void extendsTypeVariable() {
			doTest("ExtendsTypeVariable", new String[]{
				"ExtendsTypeVariable.stab.cs (1, 14) error 186: Cannot derive from 'T' because it is a type variable" },
				new String[] {});
		}
		
		[Test]
		public void inconsistentAccessibility() {
			doTest("InconsistentAccessibility", new String[]{
				"InconsistentAccessibility.stab.cs (7, 14) error 210: Type 'C' is less accessible than method 'm'" },
				new String[] {});
		}
		
		[Test]
		public void nestedProtectedMember() {
			doTest("NestedProtectedMember", new String[]{
				"NestedProtectedMember.stab.cs (10, 21) error 231: Type 'b.B.C' does not contain an accessible 'm' member" },
				new String[] {});
		}
		
		[Test]
		public void nestedStaticAccess() {
			doTest("NestedStaticAccess", new String[]{
				"NestedStaticAccess.stab.cs (5, 15) error 231: Type 'C' does not contain an accessible 'm' member" },
				new String[] {});
		}
		
		[Test]
		public void staticClassConstructor() {
			doTest("StaticClassConstructor", new String[]{
				"StaticClassConstructor.stab.cs (2, 12) error 315: Static classes cannot have instance constructors" },
				new String[] {});
		}
		
		[Test]
		public void staticClassConstructor2() {
			doTest("StaticClassConstructor2", new String[]{
				"StaticClassConstructor2.stab.cs (5, 12) error 315: Static classes cannot have instance constructors" },
				new String[] {});
		}
		
		[Test]
		public void ambiguousExtensionMethods() {
			doTest("AmbiguousExtensionMethods", new String[]{
				"AmbiguousExtensionMethods.stab.cs (18, 20) error 237: Cannot apply invocation to any method, delegate or extension method" },
				new String[] {});
		}
		
		[Test]
		public void deprecatedMethodOverride() {
			doTest("DeprecatedMethodOverride", new String[] {},
				new String[] { "DeprecatedMethodOverride.stab.cs (10, 26) warning 317: 'B.m()' is overriding a deprecated method of type 'A'" });
		}
		
		[Test]
		public void deprecatedMethodUsage() {
			doTest("DeprecatedMethodUsage", new String[] {},
				new String[] { "DeprecatedMethodUsage.stab.cs (12, 12) warning 318: Usage of deprecated method 'A.m()'" });
		}
		
		[Test]
		public void baseClassAfterInterface() {
			doTest("BaseClassAfterInterface", new String[] {
				"BaseClassAfterInterface.stab.cs (1, 20) error 323: Base class 'B' must come before any interfaces" },
				new String[] {});
		}
		
		[Test]
		public void thisAccessInInitializer() {
			doTest("ThisAccessInInitializer", new String[] {
				"ThisAccessInInitializer.stab.cs (6, 16) error 262: Keyword 'this' is not valid in a static context"},
				new String[] {});
		}
		
		[Test]
		public void superAccessInInitializer() {
			doTest("SuperAccessInInitializer", new String[] {
				"SuperAccessInInitializer.stab.cs (6, 22) error 261: Keyword 'super' is not valid in a static context"},
				new String[] {});
		}
		
		[Test]
		public void genericFinalMethodOverride() {
			doTest("GenericFinalMethodOverride", new String[] {
				"GenericFinalMethodOverride.stab.cs (7, 17) error 204: 'D<T>.m(T)' cannot override the non-virtual method in 'C<T>'"},
				new String[] {});
		}
		
		[Test]
		public void unknownFieldType() {
			doTest("UnknownFieldType", new String[] {
				"UnknownFieldType.stab.cs (2, 5) error 278: The name 'inty' cannot be resolved to a type"},
				new String[] {});
		}
		
		[Test]
		public void baseMissingConstructor() {
			doTest("BaseMissingConstructor", new String[]{
				"BaseMissingConstructor.stab.cs (4, 12) error 325: 'java.lang.Object' does not contain a constructor that takes 1 arguments" },
				new String[] {});
		}
		
		[Test]
		public void wildcardConstraint() {
			doTest("WildcardConstraint", new String[]{
				"WildcardConstraint.stab.cs (7, 22) error 329: The method 'WildcardConstraintArgument<T>.method(WildcardConstraintArgument<? : java.lang.Number>)' is not applicable for the arguments (WildcardConstraintArgument<java.lang.String>)" },
				new String[] {});
		}
		
		[Test]
		public void wildcardAssignment() {
			doTest("WildcardAssignment", new String[]{
				"WildcardAssignment.stab.cs (6, 50) error 234: Cannot implicitly convert type 'java.util.Enumeration<? : java.util.zip.ZipEntry>' to 'java.util.Enumeration<java.util.zip.ZipEntry>'" },
				new String[] {});
		}
		
		[Test]
		public void wildcardLowerConstraint() {
			doTest("WildcardLowerConstraint", new String[]{
				"WildcardLowerConstraint.stab.cs (7, 22) error 329: The method 'WildcardLowerConstraintArgument<T>.method(WildcardLowerConstraintArgument<java.lang.Number : ?>)' is not applicable for the arguments (WildcardLowerConstraintArgument<java.lang.Integer>)" },
				new String[] {});
		}
		
		[Test]
		public void nonStaticField() {
			doTest("NonStaticField", new String[]{
				"NonStaticField.stab.cs (5, 13) error 327: A static member 'field' cannot be resolved in this context" },
				new String[] {});
		}
		
		[Test]
		public void privateAccessor() {
			doTest("PrivateAccessor", new String[]{
				"PrivateAccessor.stab.cs (10, 17) error 231: Type 'C' does not contain an accessible 'Prop' member" },
				new String[] {});
		}
		
		[Test]
		public void privateAccessor2() {
			doTest("PrivateAccessor2", new String[]{
				"PrivateAccessor2.stab.cs (11, 9) error 254: A read only property cannot be assigned to" },
				new String[] {});
		}
		
		[Test]
		public void covariance3() {
			doTest("Covariance3", new String[]{
				"Covariance3.stab.cs (8, 9) error 208: 'I2.m()' has a return type incompatible with the return type of 'I1.m()'" },
				new String[] {});
		}
		
		[Test]
		public void covariance4() {
			doTest("Covariance4", new String[]{
				"Covariance4.stab.cs (10, 18) error 239: No method 'm()' found to override in base types of 'C2'" },
				new String[] {});
		}
		
		[Test]
		public void propertyCovariance() {
			doTest("PropertyCovariance", new String[]{
				"PropertyCovariance.stab.cs (2, 23) error 208: 'B.Prop' has a return type incompatible with the return type of 'A.Prop'" },
				new String[] {});
		}
		
		[Test]
		public void indexerCovariance() {
			doTest("IndexerCovariance", new String[]{
				"IndexerCovariance.stab.cs (2, 23) error 208: 'B.this[]' has a return type incompatible with the return type of 'A.this[]'" },
				new String[] {});
		}
		
		[Test]
		public void interfaceMethod() {
			doTest("InterfaceMethod", new String[]{
				"InterfaceMethod.stab.cs (5, 22) error 255: 'C.iterator()' reduce the visibility of the method in 'java.lang.Iterable<java.lang.String>'" },
				new String[] {});
		}
		
		[Test]
		public void fieldInitializerLambda() {
			doTest("FieldInitializerLambda", new String[]{
				"FieldInitializerLambda.stab.cs (4, 24) error 332: Lambda expressions cannot be used inside field initializers" },
				new String[] {});
		}
		
		[Test]
		public void abstractMethod() {
			doTest("AbstractMethod", new String[]{
				"AbstractMethod.stab.cs (2, 25) error 333: 'm()' is abstract but it is contained in non-abstract class 'C'" },
				new String[] {});
		}
		
		[Test]
		public void lessAccessibleBaseClass() {
			doTest("LessAccessibleBaseClass", new String[]{
				"LessAccessibleBaseClass.stab.cs (4, 18) error 334: Base class 'A' is less accessible than 'B'" },
				new String[] {});
		}
		
		[Test]
		public void nestedLessAccessibleBaseClass() {
			doTest("NestedLessAccessibleBaseClass", new String[]{
				"NestedLessAccessibleBaseClass.stab.cs (5, 15) error 334: Base class 'C.A' is less accessible than 'C.B'" },
				new String[] {});
		}
	}
}
