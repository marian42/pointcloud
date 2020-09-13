# About
This is a Unity application to view pointclouds and generate roof meshes.
It allows you to test different algorithms to find roof planes and generate roof meshes in point cloud data from aerial LIDAR scans.

# Preparing point data
The application requires the pointclouds to be separated by buildings.
You can skip this step and use the supplied sample data without any configuration.

The program to separate point cloud data is located in the `shapefile/XYZSeparator` directory.
It is a standalone C# application that doesn't use Unity.

First, download and unpack your pointcloud data, for example from [here](https://www.opengeodata.nrw.de/produkte/geobasis/dom/dom1l/).
You also need building layout data in the shapefile format, which can be downloaded from [here](https://www.opengeodata.nrw.de/produkte/geobasis/lika/alkis_sek/hu_nw/).

Edit the `config.ini` file to include the locations of the supplied data and the desired output directories.
Compile the `XYZSeparator` project and run it.
The program can process about 25GB of .xyz data per hour.

# Map view in Unity
Load the project within the Unity Editor.
Make sure the scene `MapScene` is loaded.
The application can not run standalone, it requires the Unity Editor.
If you use your own data, edit the "Options" object and enter the locations of your files.
If you want to use the sample data, you don't need to change anything.
Click Play in the Unity Editor to display the map.

You can also load .xyz files directly into the map view by selecting File -> Load pointcloud.
For pointclouds loaded that way, meshes can not be generated.

## Controls
Action | Key
--- | ---
Move map | Drag LMB, W, A, S, D
Rotate map | Drag RMB, Q, E, R, F
Zoom | Scroll, +, -
Load buildings | Click MMB, Space
Select building | Left click
Create Mesh | Double click
