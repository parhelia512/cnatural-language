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

namespace cnatural.eclipse.helpers {

	//
	// To store the dependencies between types and files.
	//
	public class DependencyInfo {
		private Map<String, Set<String>> referencedTypes;
		private Map<String, Set<String>> referencingTypes;
		private Map<String, Set<String>> typeLocations;
		private Map<String, Set<String>> fileContents;

		public DependencyInfo() {
			this.referencedTypes = new HashMap<String, Set<String>>();
			this.referencingTypes = new HashMap<String, Set<String>>();
			this.typeLocations = new HashMap<String, Set<String>>();
			this.fileContents = new HashMap<String, Set<String>>();
		}
		
		public void addTypeToTypeRelation(String referencingType, String referencedType) {
			var referencing = referencingTypes.get(referencedType);
			if (referencing == null) {
				referencing = new HashSet<String>();
				referencingTypes[referencedType] = referencing;
			}
			referencing.add(referencingType);
			var referenced = referencedTypes.get(referencingType);
			if (referenced == null) {
				referenced = new HashSet<String>();
				referencedTypes[referencingType] = referenced;
			}
			referenced.add(referencedType);
		}
		
		public void addFileToTypeRelation(String fileName, String typeName) {
			var contents = fileContents.get(fileName);
			if (contents == null) {
				contents = new HashSet<String>();
				fileContents[fileName] = contents;
			}
			contents.add(typeName);
			var locations = typeLocations.get(typeName);
			if (locations == null) {
				locations = new HashSet<String>();
				typeLocations[typeName] = locations;
			}
			locations.add(fileName);
		}

		public Iterable<String> getTypeLocations(String typeName) {
			return typeLocations[typeName] ?? Query.empty<String>();
		}
	
		public Iterable<String> getFileContents(String fileName) {
			return fileContents[fileName] ?? Query.empty<String>();
		}
	
		public Iterable<String> getReferencingTypes(String referencedType) {
			return referencingTypes[referencedType] ?? Query.empty<String>();
		}
	
		public Iterable<String> getReferencedTypes(String fromType) {
			return referencedTypes[fromType] ?? Query.empty<String>();
		}

		public Iterable<String> getAllReferencingTypes(Iterable<String> typeNames) {
			var result = new HashSet<String>();
			foreach (var t in typeNames) {
				visitReferencingTypes(t, result);
			}
			return result;
		}

		public Iterable<String> getAllReferencingFiles(Iterable<String> fileNames) {
			int nfiles;
			var result = fileNames;
			result = getAllReferencingTypes(result.selectMany(p => getFileContents(p))).selectMany(p => getTypeLocations(p)).toSet();
			do {
				nfiles = result.count();
				result = getAllReferencingTypes(result.selectMany(p => getFileContents(p))).selectMany(p => getTypeLocations(p)).toSet();
			} while (result.count() > nfiles);
			return result.union(fileNames);
		}
		
		public Iterable<String> getAllReferencedTypes(Iterable<String> typeNames) {
			var result = new HashSet<String>();
			foreach (var t in typeNames) {
				visitReferencedTypes(t, result);
			}
			return result;
		}

		public Iterable<String> getAllReferencedFiles(Iterable<String> fileNames) {
			int nfiles;
			var result = fileNames;
			result = getAllReferencedTypes(result.selectMany(p => getFileContents(p))).selectMany(p => getTypeLocations(p)).toSet();
			do {
				nfiles = result.count();
				result = getAllReferencedTypes(result.selectMany(p => getFileContents(p))).selectMany(p => getTypeLocations(p)).toSet();
			} while (result.count() > nfiles);
			return result.union(fileNames);
		}

		private void visitReferencingTypes(String typeName, Set<String> result) {
			if (result.add(typeName)) {
				foreach (var s in getReferencingTypes(typeName)) {
					visitReferencingTypes(s, result);
				}
			}
		}
		
		private void visitReferencedTypes(String typeName, Set<String> result) {
			if (result.add(typeName)) {
				foreach (var s in getReferencedTypes(typeName)) {
					visitReferencedTypes(s, result);
				}
			}
		}
	}
}
