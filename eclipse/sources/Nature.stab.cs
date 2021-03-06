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
using org.eclipse.jface.action;
using org.eclipse.jface.viewers;
using org.eclipse.ui;
using cnatural.eclipse.helpers;

namespace cnatural.eclipse {

	//
	// The nature for the Stab language
	//
	public class Nature : IProjectNature {
		private IProject project;
	
		public Nature() {
		}
		
		public IProject getProject() {
			return project;
		}
	
		public void setProject(IProject project) {
			this.project = project;
		}
	
		public void configure() {
			var description = project.getDescription();
			var commands = description.getBuildSpec();
			var newCommands = new ICommand[sizeof(commands) + 1];
			System.arraycopy(commands, 0, newCommands, 0, sizeof(commands));
			var buildCommand = description.newCommand();
			buildCommand.setBuilderName(Environment.BUILDER_ID);
			newCommands[sizeof(commands)] = buildCommand;
			description.setBuildSpec(newCommands);
			project.setDescription(description, IProject.FORCE, null);
			Environment.configureProject(project);
		}
	
		public void deconfigure() {
			var description = project.getDescription();
			var commands = description.getBuildSpec();
			var newCommands = new ICommand[sizeof(commands) - 1];
			for (int i = 0, j = 0; i < sizeof(commands); i++, j++) {
				if (Environment.BUILDER_ID.equals(commands[i].getBuilderName())) {
					i++;
				} else {
					newCommands[j] = commands[i];
				}
			}
			description.setBuildSpec(newCommands);
			project.setDescription(description, IProject.FORCE, null);
			Environment.deconfigureProject(project);
		}
	}
	
	//
	// The action used to add/remove Stab support to/from a project.
	//
	public class ToggleNatureAction : IObjectActionDelegate {
		private ISelection selection;
	
		public ToggleNatureAction() {
		}
		
		public void selectionChanged(IAction action, ISelection selection) {
			this.selection = selection;
		}
	
		public void setActivePart(IAction action, IWorkbenchPart targetPart) {
		}
		
		public void run(IAction action) {
			var project = EclipseHelper.getProjectFromSelection(selection);
			if (project != null) {
				try {
					var description = project.getDescription();
					var natureIds = description.getNatureIds();
					String[] newNatureIds;
					if (project.hasNature(Environment.NATURE_ID)) {
						newNatureIds = new String[sizeof(natureIds) - 1];
						for (int i = 0, j = 0; i < sizeof(natureIds); i++, j++) {
							if (natureIds[i].equals(Environment.NATURE_ID)) {
								i++;
							} else {
								newNatureIds[j] = natureIds[i];
							}
						}
					} else {
						newNatureIds = new String[sizeof(natureIds) + 1];
						newNatureIds[0] = Environment.NATURE_ID;
						System.arraycopy(natureIds, 0, newNatureIds, 1, sizeof(natureIds));
					}
					description.setNatureIds(newNatureIds);
					project.setDescription(description, IProject.FORCE, null);
				} catch (CoreException e) {
					Environment.logException(e);
				}
			}
		}
	}
}
