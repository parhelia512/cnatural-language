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
using java.io;
using java.lang;
using java.util;
using cnatural.compiler;
using cnatural.helpers;

namespace cnatural.build {

	public class Application {
	
		private static C♮ language = new C♮();

		public static void main(string[] args) {
			System.exit(new Application().run(args));
		}
		
		public int run(params string[] arguments) {
			if (sizeof(arguments) > 0) {
				System.out.print("Targets ");
				foreach(string command in arguments) {
					System.out.print(command);
					System.out.print(" ");
				}
				System.out.println("...");

				foreach(string command in arguments){
					int code;
					if ((code = doIt(command)) != 0) {
						return code;
					}
				}
				return 0;
			}
			return buildAll();
		}

		private int doIt(string command)
		{
			switch (command) {
			case "clean":
				clean();
				return 0;
			case "compiler":
				return buildCompiler();
			case "runtime":
				return buildRuntimeLibrary();
			case "eclipse":
				return buildEclipse();
			case "build":
				return buildBuild();
			case "tests":
				return buildTests();
			case "install":
				int code;
				if ((code = buildAll()) != 0) {
					return code;
				}
				return runTests();
			}
			System.out.println("Error: Unrecognized command '"+ command +"'");
			return 100;
		}
		
		private int buildAll() {
			System.out.println("Building all...");
			int code;
			if ((code = buildAnnotatedLibrary()) != 0) {
				return code;
			}
			if ((code = buildCompiler()) != 0) {
				return code;
			}
			if ((code = buildRuntimeLibrary()) != 0) {
				return code;
			}
			if ((code = buildTests()) != 0) {
				return code;
			}
			//if ((code = buildEclipse()) != 0) {
			//	return code;
			//}
			return buildBuild();
		}
		
		private cnatural.compiler.Application Compiler {
			get { return new cnatural.compiler.Application(); }
		}
		
		private int buildAnnotatedLibrary() {
			System.out.println();
			System.out.println("Annotated Library");
			System.out.println("--------------------------");
			
			var args = new ArrayList<string>();
			args.add("-cp:bin/stabrt.jar");
			args.add("-out:annotated/stabal.jar");
			addSourceFiles(new File("annotated/sources"), args);
			
			return Compiler.runWith(args);
		}

		private int buildCompiler() {
			System.out.println();
			System.out.println("Compiler");
			System.out.println("--------------------------");

			var args = new ArrayList<string>();
			args.add("-al:bin/stabal.jar");
			args.add("-cp:bin/stabrt.jar;bin/asm-3.3.jar");
			args.add("-resources:compiler/resources");
			args.add("-manifest:compiler/MANIFEST.MF");
			args.add("-out:compiler/stabc.jar");
			addSourceFiles(new File("compiler/sources"), args);
			
			return Compiler.runWith(args);
		}

		private int buildRuntimeLibrary() {
			System.out.println();
			System.out.println("Runtime Library");
			System.out.println("--------------------------");
			
			var args = new ArrayList<string>();
			args.add("-al:bin/stabal.jar");
			args.add("-doc:runtime/doc.xml");
			args.add("-out:runtime/stabrt.jar");
			addSourceFiles(new File("runtime/sources"), args);
			
			return Compiler.runWith(args);
		}

		private int buildTests() {
			System.out.println();
			System.out.println("Tests");
			System.out.println("--------------------------");
			
			var args = new ArrayList<string>();
			args.add("-al:bin/stabal.jar");
			args.add("-cp:bin/stabrt.jar;bin/junit-4.8.1.jar;bin/asm-3.3.jar;compiler/stabc.jar");
			args.add("-out:tests/tests.jar");
			addSourceFiles(new File("tests/sources"), args);
			
			int code = Compiler.runWith(args);
			if (code == 0) {
				System.out.println();
				System.out.println("Tests Launcher");
				args = new ArrayList<string>();
				args.add("-al:bin/stabal.jar");
				args.add("-cp:bin/stabrt.jar;bin/junit-4.8.1.jar");
				args.add("-manifest:tools/tests/MANIFEST.MF");
				args.add("-out:bin/tests.jar");
				addSourceFiles(new File("tools/tests"), args);
				
				code = Compiler.runWith(args);
			}
			return code;
		}

		private int buildEclipse() {
			System.out.println();
			System.out.println("Eclipse Plugin");
			System.out.println("--------------------------");

			var args = new ArrayList<string>();
			args.add("-al:bin/stabal.jar");
			args.add("-resources:eclipse/resources");

			var tmpManifest = new File("eclipse/MANIFEST.MF.TMP");
			string version = "1.0.0";
			using (var reader = new BufferedReader(new FileReader("eclipse/MANIFEST.MF"))) {
				using (var writer = new BufferedWriter(new FileWriter(tmpManifest))) {
					string line;
					while ((line = reader.readLine()) != null) {
						if (line.startsWith("Bundle-Version:")) {
							int idx = line.indexOf(".qualifier");
							var sb = new StringBuilder();
							sb.append(line.substring("Bundle-Version:".length(), idx + 1).trim());
							writer.write(line.substring(0, idx + 1));
							var qualifier = string.format("%1$tY%1$tm%1$td%1$tH%1$tM", GregorianCalendar.getInstance());
							sb.append(qualifier);
							writer.write(qualifier);
							version = sb.toString();
						} else {
							writer.write(line);
						}
						writer.newLine();
					}
				}
			}
			
			args.add("-manifest:eclipse/MANIFEST.MF.TMP");
			args.add("-out:eclipse/stab.tools.eclipse_" + version + ".jar");
			args.add("-define:TRACE");
			
			var pluginDir = new File("eclipse/eclipse/plugins");
			var files = pluginDir.listFiles();
			var sb = new StringBuilder();
			sb.append("-cp:bin/stabrt.jar;bin/stabc.jar;bin/asm-3.3.jar");
			foreach (var file in files) {
				if (!file.isDirectory() && file.getName().endsWith(".jar")) {
					sb.append(';');
					sb.append(file.getAbsolutePath());
				}
			}
			args.add(sb.toString());
			addSourceFiles(new File("eclipse/sources"), args);
			
			int result = Compiler.runWith(args);
			tmpManifest.delete();
			return result;
		}

		private int buildBuild() {
			System.out.println();
			System.out.println("Build Application");
			System.out.println("--------------------------");

			var args = new ArrayList<string>();
			args.add("-al:bin/stabal.jar");
			args.add("-cp:bin/stabrt.jar;bin/asm-3.3.jar;bin/stabc.jar;bin/tests.jar");
			args.add("-manifest:tools/build/MANIFEST.MF");
			args.add("-out:bin/build.jar");
			addSourceFiles(new File("tools/build"), args);
			
			return Compiler.runWith(args);
		}
		
		private int runTests()
		{
			System.out.println();
			System.out.println("Run Tests");
			System.out.println("--------------------------");
			
			return new cnatural.tests.Application().run();
		}

		private void addSourceFiles(File dir, List<string> args) {
			var files = dir.listFiles();
			if (files != null) {
				foreach (var file in files) {
					if (file.isDirectory()) {
						addSourceFiles(file, args);
					} else {
						string filename = file.getName();
						if (filename.endsWith(".stab") || filename.endsWith(".stab.cs") || filename.endsWith(".cnatural.cs")) {
							args.add(file.getAbsolutePath());
						}
					}
				}
			}
		}
		
		private void clean() {
			System.out.println("Cleaning...");
		
			deleteFile("annotated/stabal.jar");
			deleteFile("compiler/stabc.jar");
			deleteFile("runtime/stabrt.jar");
			deleteFile("runtime/doc.xml");
			deleteFile("tests/tests.jar");
			
			foreach (var f in new File("eclipse").listFiles()) {
				if (f.getName().startsWith("stab.tools.eclipse_")) {
					f.delete();
				}
			}
			
			deleteDir("tests/resources/ExpressionsTest/generated");
			deleteDir("tests/resources/LibraryTest/generated");
			deleteDir("tests/resources/IntegrationTest/generated");
			deleteDir("tests/resources/ObjectModelTest/generated");
			deleteDir("tests/resources/ParseCompilationUnitTest/generated");
			deleteDir("tests/resources/ParseExpressionTest/generated");
			deleteDir("tests/resources/PreprocessorTest/generated");
			deleteDir("tests/resources/StatementsTest/generated");
			deleteDir("tests/resources/TypeBuilderTest/stab");
			deleteDir("tests/resources/TypeSystemTest/generated");
			deleteDir("tests/resources/QueryTranslatorTest/generated");
			deleteDir("tests/resources/ExpressionTreesTest/generated");
		}

		private static void deleteFile(string path) {
			var f = new File(path);
			if (f.exists()) {
				if (!new File(path).delete()) {
					System.out.println("Failed to delete " + path);
				}
			}
		}
		
		private static void deleteDir(string dir) {
			var d = new File(dir);
			if (d.exists()) {
				if (!deleteDir(d)) {
					System.out.println("Failed to delete " + dir);
				}
			}
		}
		
		private static bool deleteDir(File dir) {
		    if (dir.isDirectory()) {
				var files = dir.listFiles();
				if (files != null) {
					foreach (var f in files) {
						if (!deleteDir(f)) {
							return false;
						}
					}
				}
	    	}
		    return dir.delete();
		}
	}
}
