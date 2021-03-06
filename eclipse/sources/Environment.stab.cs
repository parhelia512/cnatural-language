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
using org.eclipse.core.resources;
using org.eclipse.core.runtime;
using org.eclipse.jface.preference;
using Image = org.eclipse.swt.graphics.Image;
using org.eclipse.ui.console;
using org.eclipse.ui.editors.text;
using org.eclipse.ui.texteditor;
using org.osgi.framework;
using stab.lang;

namespace cnatural.eclipse {

	//
	// Type-safe manipulation of the icons embedded into the archive of the plug-in.
	//
	public enum Icon {
		Class("class.gif"),
		DefaultField("field_default.gif"),
		DefaultMethod("method_default.gif"),
		DefaultProperty("property_default.gif"),
		Enum("enum.gif"),
		Interface("interface.gif"),
		Jar("jar.gif"),
		Library("library.gif"),
		LocalVariable("local_var.gif"),
		Package("package.gif"),
		PublicField("field_public.gif"),
		PublicMethod("method_public.gif"),
		PublicProperty("property_public.gif"),
		ProtectedField("field_protected.gif"),
		ProtectedMethod("method_protected.gif"),
		ProtectedProperty("property_protected.gif"),
		PrivateField("field_private.gif"),
		PrivateMethod("method_private.gif"),
		PrivateProperty("property_private.gif"),
		Project("project.gif"),
		Source("source.gif");
		
		Icon(String filename) {
			this.Filename = filename;
		}
		
		public String Filename^;
	}
	
	//
	// Gives access to the objects shared across the plug-in.
	//
	public class Environment {
		public static final String PLUGIN_ID = "cnatural.eclipse";
		public static final String NATURE_ID = "cnatural.eclipse.stabnature";
		public static final String BUILDER_ID = "cnatural.eclipse.stabbuilder";

		private static MessageConsoleStream consoleStream;

		public static void addProjectBuildListener(IProjectBuildListener listener) {
			Activator.addProjectBuildListener(listener);
		}
		
		public static void removeProjectBuildListener(IProjectBuildListener listener) {
			Activator.removeProjectBuildListener(listener);
		}

		public static IPreferenceStore PreferenceStore {
			get {
				return Activator.getPreferenceStore();
			}
		}

		public static Bundle Bundle {
			get {
				return Activator.getBundle();
			}
		}

		public static Image getIcon(Icon icon) {
			return Activator.getIcon(icon);
		}

		public static ProjectManager getProjectManager(IResource resource) {
			return Activator.getProjectManager(resource);
		}

		//
		// Gets the path to the libraries embedded into the archive of the plug-in.
		//
		public static String getLibraryPath(String filename) {
			try {
				return FileLocator.toFileURL(FileLocator.find(getBundle(), new Path("/libs/" + filename), null)).getFile();
			} catch (Exception e) {
				Environment.logException(e);
				throw new IllegalArgumentException(e.getMessage());
			}
		}

		public static int getTabWidth() {
			return EditorsUI.getPreferenceStore().getInt(AbstractDecoratedTextEditorPreferenceConstants.EDITOR_TAB_WIDTH);
		}

		public static ILog getLog() {
			return Activator.getLog();
		}

		public static void logException(Throwable t) {
			getLog().log(new Status(IStatus.ERROR, PLUGIN_ID, "Unhandled exception", t));
		}

		[Conditional({ "TRACE" })]
		public static void trace(Object sender, String message) {
			var threadName = getLabel(Thread.currentThread().getName(), 10);
			var senderName = getLabel(sender.getClass().getSimpleName(), 15);
			ConsoleStream.write("[" + threadName + "| " + senderName + "] " + message + "\n");
		}

		private static String getLabel(String name, int max) {
			var length = name.length();
			if (length > max) {
				name = name.substring(0, max - 3) + "...";
			}
			while (length++ < max) {
				name += " ";
			}
			return name;
		}

		private static synchronized MessageConsoleStream ConsoleStream {
			get {
				if (consoleStream == null) {
					var consoleManager = ConsolePlugin.getDefault().getConsoleManager();
					var console = new MessageConsole("Stab Plug-in Execution Traces", null);
					consoleManager.addConsoles(new[] { console });
					consoleStream = console.newMessageStream();
				}
				return consoleStream;
			}
		}

		static void configureProject(IProject project) {
			Activator.configureProject(project);
		}
	
		static void deconfigureProject(IProject project) {
			Activator.deconfigureProject(project);
		}
		
		static void fireProjectBuildEvent(ProjectManager projectManager) {
			Activator.fireProjectBuildEvent(projectManager);
		}
		
		private static Activator Activator {
			get {
				if (cnatural.eclipse.Activator.instance == null) {
					throw new IllegalStateException("No plug-in loaded");
				}
				return cnatural.eclipse.Activator.instance;
			}
		}
	}
}
