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
using java.util;

namespace cnatural.syntaxtree {
	
	public enum PackageMemberKind {
		Package,
		Class,
		Interface,
		Delegate
	}
	
	public interface IPackageMember {
		PackageMemberKind PackageMemberKind^;
        int DocumentationOffset;
        int DocumentationLength;
	}
	
	public class PackageBodyNode : SyntaxNode {
		public PackageBodyNode() {
			this.UsingDirectives = new ArrayList<UsingDirectiveNode>();
			this.Members = new ArrayList<IPackageMember>();
		}
		
		public List<UsingDirectiveNode> UsingDirectives^;
		
		public List<IPackageMember> Members^;
	}
	
	public class PackageDeclarationNode : SyntaxNode, IPackageMember {
		public PackageDeclarationNode() {
			this.Annotations = new ArrayList<AnnotationSectionNode>();
			this.Identifiers = new ArrayList<SourceCodePartNode>();
		}
		
		public PackageMemberKind PackageMemberKind {
			get {
				return PackageMemberKind.Package;
			}
		}
		
        public int DocumentationOffset;
		
        public int DocumentationLength;
        
		public PackageBodyNode Body;
		
		public List<AnnotationSectionNode> Annotations^;

		public List<SourceCodePartNode> Identifiers^;
	}
	
	public class SourceCodePartNode : SyntaxNode {
		public SourceCodePartNode() {
		}
		
		public int Offset;
		
		public int Length;
	}
	
}
