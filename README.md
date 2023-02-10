# boid-simulation
Interactive simulation of flocking behavior made in Unity. The simulation runs on the CPU and is fully multithreaded.

## Opening the Project
The project has to be cloned since it uses Git LFS - ZIP download won't contain binary files.
After cloning the project, it has to be opened with Unity 2022.2.5f1.
When opening it for the first time a TMP Importer window will appear. Please import both TMP Essentials and TMP Examples & Extras.

## Modes
The simulation has two modes: Simulation and Gameplay.
In the Simulation mode you can explore the simulation, create attractors and change swarm size.
In the Gameplay mode you play as an anglerfish and try to lure other fish and eat them.
You can switch between them by pressing M.

## Assets Folder Structure
/Audio 		- audio files from CC0 libraries
/Materials 	- Unity materials
/Models 	- 3D models created by me in Blender
/Prefabs 	- Unity prefabs and prefab variants
/Scenes 	- Unity scenes and scene data
/Scripts 	- C# files
	/Gameplay 	- components for playing with the simulation
	/Patterns 	- implemented design patterns
	/Simulation - code responsible for running the Boid simulation
		/Core 			- core code responsible for managing the simulation and scheduling Jobs
		/Interactive 	- interactive parts of the simulation e.g., attractors
		/Jobs 			- Unity Jobs for multithreaded calculations
		/Utils 			- helper classes for running and managing the simulation
	/Utils 		- general utility code
/Settings 	- rendering and post-processing settings