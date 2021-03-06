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
using org.eclipse.core.resources;

namespace cnatural.eclipse.helpers {

	//
	// Stores mappings between resources and project relative names.
	// 
 	public class ResourceSet<T> where T : IResource {
 		private Map<String, T> resources;
 		private Map<T, String> projectRelativeNames;
 	
 		public ResourceSet() {
 			this.resources = new HashMap<String, T>();
 			this.projectRelativeNames = new HashMap<T, String>();
 		}
 		
 		public void add(T resource) {
 			var name = resource.getProjectRelativePath().toPortableString();
 			resources[name] = resource;
 			projectRelativeNames[resource] = name;
 		}
 		
 		public void addAll(Iterable<T> resources) {
 			foreach (var resource in resources) {
 				add(resource);
 			}
 		}
 		
 		public String getProjectRelativeName(T resource) {
 			return projectRelativeNames[resource];
 		}
 		
 		public T getResource(String name) {
 			return resources[name];
 		}
 		
 		public Iterable<T> getAllResources() {
 			return resources.values();
 		}
 		
 		public Iterable<String> getAllProjectRelativeNames() {
 			return projectRelativeNames.values();
 		}
 	}
}
 