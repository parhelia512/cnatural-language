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
#undef TRACE
 
using java.io;
using java.lang;
using java.util;
using org.eclipse.core.resources;
using org.eclipse.core.runtime;
using stab.reflection;
using cnatural.compiler;
using StabCompiler = cnatural.compiler.Compiler;
using cnatural.helpers;
using cnatural.parser;
using cnatural.syntaxtree;
using stab.query;

namespace cnatural.eclipse.helpers {

	public class SourceCompilerParameters {
		public SourceCompilerParameters() {
			this.AllFiles = new ResourceSet<IFile>();
			this.PreprocessorSymbols = new HashSet<String>();
			this.GenerateClassFiles = true;
			this.FilesToCompile = new HashSet<String>();
		}
	
		public bool FullBuild;
		
		public bool ProgressiveBuild;
		
		public bool GenerateClassFiles;
		
		public ResourceSet<IFile> AllFiles^;

		public Set<String> FilesToCompile^;
		
		public Set<String> PreprocessorSymbols^;
		
		public String[] ClassPath;
		
		public Library TypeSystem;
		
		public String EditedFileName;
		
		public char[] EditedFileText;
		
		public DependencyInfo DependencyInfo;
	}

	public class SourceCompilerResults {
		public SourceCompilerResults() {
			this.ClassFiles = new HashMap<String, byte[]>();
			this.CodeErrors = new ArrayList<CodeError>();
			this.CompiledFiles = new ArrayList<String>();
			this.CompilationUnits = new HashMap<String, CompilationUnitNode>();
		}
	
		public bool Failed;
		
		public Map<String, byte[]> ClassFiles^;
		
		public List<CodeError> CodeErrors^;
		
		public List<String> CompiledFiles^;
		
		public String MissingType;
		
		public DependencyInfo DependencyInfo;
		
		public Library TypeSystem;
		
		public Library AnnotatedTypeSystem;
		
		public Map<String, CompilationUnitNode> CompilationUnits^;
	}

	public class SourceCompiler {
		private SourceCompilerParameters parameters;
		private char[] buffer;

		public SourceCompiler() {
			buffer = new char[0x1000];
		}
	
		public SourceCompilerResults compile(SourceCompilerParameters parameters, IProgressMonitor monitor) {
			this.parameters = parameters;
			if (parameters.FullBuild) {
				return compileCore(monitor);
			}
			
			try {
				monitor.beginTask("", 5);
				
				var results = compileCore(new SubProgressMonitor(monitor, 2));
				if (results.Failed) {
					if (results.MissingType == null) {
						Environment.trace(this, "Errors: all dependent types rebuild required");
						// Try to build with all the dependencies
						parameters.ProgressiveBuild = false;
						results = compileCore(new SubProgressMonitor(monitor, 3));
					} else {
						Environment.trace(this, "Missing type '" + results.MissingType + "': full rebuild");
						parameters.FullBuild = true;
						results = compileCore(new SubProgressMonitor(monitor, 3));
					}
				} else {
					// Check for type structural modifications
					var oldDependencyInfo = parameters.DependencyInfo;
					var newDependencyInfo = results.DependencyInfo;
					var oldTypes = results.CompiledFiles.selectMany(p => oldDependencyInfo.getFileContents(p)).toList();
					var newTypes = results.CompiledFiles.selectMany(p => newDependencyInfo.getFileContents(p)).toList();
					var typesToRebuild = Query.empty<String>();
					foreach (var type in oldTypes.except(newTypes)) {
						typesToRebuild = typesToRebuild.concat(oldDependencyInfo.getReferencingTypes(type));
					}
					foreach (var type in oldTypes.intersect(newTypes)) {
						if (!JvmTypeSystemHelper.isStructurallyEqual(JvmTypeSystemHelper.getType(parameters.TypeSystem, type),
									JvmTypeSystemHelper.getType(results.TypeSystem, type))) {
							Environment.trace(this, "Structurally changed: " + type);
							typesToRebuild = typesToRebuild.concat(oldDependencyInfo.getReferencingTypes(type));
						}
					}
					var filesToRebuild = typesToRebuild.selectMany(p => oldDependencyInfo.getTypeLocations(p));
					filesToRebuild = filesToRebuild.distinct().except(results.CompiledFiles).toList();
					monitor.worked(1);
					
					if (filesToRebuild.any()) {
						Environment.trace(this, "Structural changes: referencing types rebuild required (" + filesToRebuild.count() + " files)");

						parameters.ProgressiveBuild = false;
						results = compileCore(new SubProgressMonitor(monitor, 3));
						
						/* The compilation units are not using the current type system
						
						// If some types have changed, rebuild the referencing types
						var oldResult = results;
						this.parameters = new SourceCompilerParameters();
						foreach (var f in filesToRebuild) {
							this.parameters.getFilesToCompile().add(f);
						}
						this.parameters.AllFiles.addAll(parameters.AllFiles.getAllResources());
						this.parameters.DependencyInfo = newDependencyInfo;
						this.parameters.ClassPath = parameters.ClassPath;
						this.parameters.TypeSystem = results.TypeSystem;
						
						results = compileCore(new SubProgressMonitor(monitor, 2));
						
						foreach (var e in oldResult.CompilationUnits.entrySet()) {
							results.CompilationUnits[e.Key] = e.Value;
						}
						foreach (var e in oldResult.ClassFiles.entrySet()) {
							results.ClassFiles[e.Key] = e.Value;
						}
						results.CompiledFiles.addAll(oldResult.CompiledFiles);
						*/
					} else {
						monitor.worked(2);
					}
				}
				return results;
			} finally {
				monitor.done();
			}
		}
		
		private SourceCompilerResults compileCore(IProgressMonitor monitor) {
			var t0 = System.nanoTime();
			
			var results = new SourceCompilerResults();
			var hasErrors = false;
			var errorManager = new CodeErrorManager();
			var allFiles = parameters.AllFiles;
			Iterable<IFile> filesToCompile = null;
			
			try {
				monitor.beginTask("", 11);

				var deletedFiles = parameters.FilesToCompile
						.select(p => allFiles.getResource(p))
						.where(p => p == null || !p.exists())
						.select(p => allFiles.getProjectRelativeName(p)).toSet();
				var typesToCopy = Query.empty<TypeInfo>();

				// Get the files to compile
				if (parameters.FullBuild) {
					filesToCompile = allFiles.getAllResources().where(p => p.exists()).toList();
				} else {
					bool filteringDone = false;
					var referencingFiles = parameters.getDependencyInfo().getAllReferencingFiles(parameters.getFilesToCompile());
					if (parameters.ProgressiveBuild && deletedFiles.isEmpty()) {
						var referencedFiles = parameters.getDependencyInfo().getAllReferencedFiles(parameters.getFilesToCompile());
						referencedFiles = referencedFiles.except(parameters.getFilesToCompile());
						referencedFiles = referencedFiles.intersect(referencingFiles);
						
						// Progressive build only if referenced and referencing files do not intersect
						if (!referencedFiles.any()) {
							filesToCompile = parameters.FilesToCompile.select(p => allFiles.getResource(p)).where(p => p.exists()).toList();
							filteringDone = true;
						}
					}
					if (!filteringDone) {
						// Incremental build with dependencies
						filesToCompile = referencingFiles.select(p => allFiles.getResource(p)).where(p => p.exists()).toList();
					}
					
					var filesToKeep = allFiles.getAllProjectRelativeNames().except(referencingFiles);
					typesToCopy = filesToKeep.selectMany(p => parameters.DependencyInfo.getFileContents(p))
							.where(p => p.indexOf('$') == -1).select(p => parameters.TypeSystem.getType(p));
							
					Environment.trace(this, "keeping " + filesToKeep.count() + " files");
					Environment.trace(this, "ignoring " +
							(allFiles.getAllResources().count() - filesToCompile.count() - filesToKeep.count()) + " files");
				}
				Environment.trace(this, "compiling " + filesToCompile.count() + " files");
				monitor.worked(1);
				if (monitor.isCanceled()) {
					throw new InterruptedException();
				}

				var compilationUnits = new HashMap<String, CompilationUnitNode>();
				
				// Parsing
				foreach (var file in filesToCompile) {
					var text = getText(file);
					if (text != null) {
						parse(file, text, errorManager, compilationUnits);
					}
				}
				monitor.worked(1);
				if (monitor.isCanceled()) {
					throw new InterruptedException();
				}
				
				// Compiling
				var t1 = System.nanoTime();
				
				var typeSystem = new Library(parameters.ClassPath);
				JvmTypeSystemHelper.cloneTypes(typesToCopy, typeSystem);
				
				var annotatedTypeSystem = new Library(new[] { Environment.getLibraryPath("stabal.jar") }, typeSystem);
				
				var cparams = new CompilerParameters();
				cparams.TypeSystem = typeSystem;
				cparams.AnnotatedTypeSystem = annotatedTypeSystem;
				cparams.GenerateClassFiles = parameters.GenerateClassFiles;
				cparams.ProgressTracker = new CompilationProgressTracker(monitor);
				
				var cunits = compilationUnits.values().toArray(new CompilationUnitNode[compilationUnits.size()]);
				var cresults = new StabCompiler().compileFromCompilationUnits(cparams, cunits);
				
				Environment.trace(this, "compilation of " + sizeof(cunits) + " files done in " + ((System.nanoTime() - t1) / 1e6) + "ms");

				foreach (var error in cresults.Errors) {
					if (error.Level == 0) {
						hasErrors = true;
					}
					results.CodeErrors.add(error);
					Environment.trace(this, "error (" + error.Line + ", " + error.Column + ") " + error.Filename + ": " + error.Message);
				}

				if (!hasErrors) {
					var dependencyInfo = new DependencyInfo();
					results.DependencyInfo = dependencyInfo;
					var allTypes = new HashSet<String>();
	
					// Copy informations from unbuilt files
					if (parameters.DependencyInfo != null) {
						var unbuiltFiles = allFiles.getAllProjectRelativeNames();
						unbuiltFiles = unbuiltFiles.except(filesToCompile.select(p => allFiles.getProjectRelativeName(p)));
						unbuiltFiles = unbuiltFiles.except(deletedFiles);
						foreach (var file in unbuiltFiles) {
							foreach (var type in parameters.DependencyInfo.getFileContents(file)) {
								allTypes.add(type);
								dependencyInfo.addFileToTypeRelation(file, type);
								foreach (var refType in parameters.DependencyInfo.getReferencedTypes(type)) {
									dependencyInfo.addTypeToTypeRelation(type, refType);
								}
							}
						}
					}
					
					// Collect the types and update the dependencies.
					var typeMembers = new HashMap<IFile, Iterable<TypeMemberNode>>();
					foreach (var file in filesToCompile) {
						var fileName = allFiles.getProjectRelativeName(file);
						var compilationUnit = compilationUnits[fileName];
						if (compilationUnit == null) {
							continue;
						}
						var members = SyntaxTreeHelper.getTypeMembers(compilationUnit);
						typeMembers.put(file, members);
						foreach (var member in members) {
							var typeName = member.getUserData(typeof(TypeInfo)).FullName;
							dependencyInfo.addFileToTypeRelation(fileName, typeName);
							allTypes.add(typeName);
						}
					}

					if (parameters.DependencyInfo != null) {
						// Copy the types ignored by this compilation
						var missingTypes = new HashSet<TypeInfo>();
						foreach (var t in allTypes.where(p => p.indexOf('$') == -1 && !typeSystem.typeExists(p))) {
							if (hasErrors = !parameters.DependencyInfo.getReferencedTypes(t).all(p => allTypes.contains(p))) {
								Environment.trace(this, "Incremental build failed: a type was deleted");
								break;
							}
							missingTypes.add(parameters.TypeSystem.getType(t));
						}
						if (!hasErrors) {
							JvmTypeSystemHelper.cloneTypes(missingTypes, typeSystem);
						}
					}
					
					if (!hasErrors) {
						// Compute the dependencies in the compiled files
						foreach (var member in filesToCompile.select(p => typeMembers[p]).where(p => p != null).selectMany(p => p)) {
							foreach (var t in SyntaxTreeHelper.getTypeMemberDependencies(member)
									.intersect(allTypes.select(p => JvmTypeSystemHelper.getType(typeSystem, p)))) {
								dependencyInfo.addTypeToTypeRelation(member.getUserData(typeof(TypeInfo)).FullName, t.FullName);
							}
						}
	
						results.TypeSystem = typeSystem;
						results.AnnotatedTypeSystem = annotatedTypeSystem;
						foreach (var e in compilationUnits.entrySet()) {
							results.CompilationUnits[e.Key] = e.Value;
						}
						foreach (var e in cresults.ClassFiles.entrySet()) {
							results.ClassFiles[e.Key] = e.Value;
						}
					}
				}
				monitor.worked(1);
			} catch (CodeErrorException e) {
				monitor.worked(10);
			} catch (TypeLoadException e) {
				results.MissingType = e.TypeName;
				hasErrors = true;
				monitor.worked(6);
			} finally {
				monitor.done();
			}

			foreach (var file in filesToCompile) {
				results.CompiledFiles.add(allFiles.getProjectRelativeName(file));
			}
			foreach (var error in errorManager.Errors) {
				if (error.Level == 0) {
					hasErrors = true;
				}
				results.CodeErrors.add(error);
				Environment.trace(this, "error (" + error.Line + ", " + error.Column + ") " + error.Filename + ": " + error.Message);
			}
			results.Failed = hasErrors;
			
			Environment.trace(this, "compilation done in " + ((System.nanoTime() - t0) / 1e6) + "ms");
			return results;
		}
		
		private char[] getText(IFile file) {
			char[] text;
			if (parameters.AllFiles.getProjectRelativeName(file).equals(parameters.EditedFileName)) {
				text = parameters.EditedFileText;
			} else {
				using (var reader = new InputStreamReader(file.getContents(), file.getCharset())) {
					var sb = new StringBuilder();
					int read;
					while ((read = reader.read(buffer)) != -1) {
						sb.append(buffer, 0, read);
					}
					text = new char[sb.length()];
					sb.getChars(0, sizeof(text), text, 0);
				}
			}
			if (sizeof(text) > 0) {
				if (text[sizeof(text) - 1] == '\u001a') {
					text[sizeof(text) - 1] = ' ';
				}
			}
			return text;
		}
		
		private void parse(IFile file, char[] text, CodeErrorManager errorManager, Map<String, CompilationUnitNode> compilationUnits) {
			var filename = parameters.AllFiles.getProjectRelativeName(file);
			
			var preprocessor = new Preprocessor(errorManager, text);
			preprocessor.setFilename(filename);
			preprocessor.Symbols.addAll(parameters.PreprocessorSymbols);
			var preprocessedText = preprocessor.preprocess();
			
			var parser = new Parser();
			var scanner = new PreprocessedTextScanner(errorManager, preprocessedText);
			scanner.setFilename(filename);
			scanner.setTabWidth(Environment.getTabWidth());
			
			var compilationUnit = parser.parseCompilationUnit(scanner);
			if (compilationUnit != null) {
				compilationUnit.setSymbols(preprocessor.Symbols);
				compilationUnits[filename] = compilationUnit;
			}
		}
		
		class CompilationProgressTracker : ICompilationProgressTracker {
			private int ticks = 0;
			private IProgressMonitor monitor;
			
			CompilationProgressTracker(IProgressMonitor monitor) {
				this.monitor = monitor;
			}
			
			public void compilationStageFinished(CompilationStage stage) {
				if (monitor.isCanceled()) {
					throw new InterruptedException();
				}
				monitor.worked(1);
				ticks++;
			}
			public void compilationFinished() {
				if (monitor.isCanceled()) {
					throw new InterruptedException();
				}
				monitor.worked(7 - ticks);
			}
		}
	}
}
