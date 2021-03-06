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
using java.io;
using java.util;
using org.eclipse.core.resources;
using org.w3c.dom;
using stab.query;
using cnatural.helpers;

namespace cnatural.eclipse {

	public class ProjectProperties {
		private final static String EMPTY_DOC = @"<?xml version=""1.0"" encoding=""UTF-8""?><stabProperties><libraries/></stabProperties>";
		
		public Iterable<ProjectLibrary> Libraries;
		
		public Iterable<String> PreprocessorSymbols;
		
		public String OutputPath;
		
		public bool load(IFile file) {
			if (file.exists()) {
				try {
					var libraries = new ArrayList<ProjectLibrary>();
					var preprocessorSymbols = new HashSet<String>();
					var outputPath = "bin";
					
					var document = XmlHelper.load(new InputStreamReader(file.getContents()));
					var nodeList = document.getElementsByTagName("library");
					int length = nodeList.getLength();
					for (int i = 0; i < length; i++) {
						var e = (Element)nodeList.item(i);
						var lib = new ProjectLibrary(e.getAttribute("name"));
						var enabled = e.getAttribute("enabled");
						lib.setEnabled(enabled.length() == 0 || !enabled.equals("false"));
						libraries.add(lib);
					}
					nodeList = document.getElementsByTagName("preprocessorSymbols");
					if (nodeList.getLength() == 1) {
						foreach (var s in nodeList.item(0).getTextContent().split(";")) {
							preprocessorSymbols.add(s.trim());
						}
					}
					nodeList = document.getElementsByTagName("outputPath");
					if (nodeList.getLength() == 1) {
						outputPath = nodeList.item(0).getTextContent();
					}
					this.Libraries = libraries;
					this.PreprocessorSymbols = preprocessorSymbols;
					this.OutputPath = outputPath;
					return true;
				} catch (Exception e) {
					Environment.logException(e);
				}
			}
			this.Libraries = Query.empty<ProjectLibrary>();
			this.PreprocessorSymbols = Query.empty<String>();
			return false;
		}
		
		public void save(IFile file) {
			try {
				var document = XmlHelper.load(new StringReader(EMPTY_DOC));
				var libs = (Element)document.getElementsByTagName("libraries").item(0);
				foreach (var lib in this.Libraries) {
					var e = document.createElement("library");
					libs.appendChild(e);
					e.setAttribute("name", lib.Path);
					if (!lib.Enabled) {
						e.setAttribute("enabled", "false");
					}
				}
				if (this.PreprocessorSymbols.any()) {
					var sb = new StringBuilder();
					var first = true;
					foreach (String s in this.PreprocessorSymbols) {
						if (first) {
							first = false;
						} else {
							sb.append(';');
						}
						sb.append(s);
					}
					var e = document.createElement("preprocessorSymbols");
					document.getDocumentElement().appendChild(e);
					e.setTextContent(sb.toString());
				}
				var outputElt = document.createElement("outputPath");
				document.getDocumentElement().appendChild(outputElt);
				outputElt.setTextContent(this.OutputPath);
				
	            var writer = new StringWriter();
				XmlHelper.save(document, writer);
	            var bytes = writer.toString().getBytes("UTF-8");
	            var stream = new ByteArrayInputStream(bytes);
	            if (file.exists()) {
	            	file.setContents(stream, IResource.FORCE, null);
	            } else {
	            	file.create(stream, true, null);
	            }
			} catch (Exception e) {
				Environment.logException(e);
			}
		}
	}
	
	public class ProjectLibrary {
		public ProjectLibrary(String path) {
			this.Path = path;
			this.Enabled = true;
		}

		public String Path^;
		
		public bool Enabled;
	}
}
