The C♮ Programming Language
===========================

A multi-paradigm programming language designed for the Java and .NET Virtual Machines, starting from the C# syntax. 

Pronounced *"see-natural"*.

A fork from the original stab-language project at https://code.google.com/p/stab-language/, contains all the svn repo except the embedded eclipse plugin (on eclipse/eclipse).

To build
--------

To build the compiler, libraries and tests:

	java -jar bin/build.jar
	
To run the tests (on success the compiler and libraries jar files are moved to the bin folder):

	java -jar bin/tests.jar
	
To clean the temporary files:

	java -jar bin/build.jar clean
