using System;
using System.Linq;
using Catfood.Shapefile;
using System.IO;

namespace XYZSeparator {
	class Program {
		public static void Main(string[] args) {
			var config = new ConfigReader("config.ini");

			string inputFolder = config.Get("inputFolder");
			if (!inputFolder.EndsWith("/") && !inputFolder.EndsWith("\\")) {
				inputFolder += "/";
			}
			string outputFolder = config.Get("outputFolder");
			if (!outputFolder.EndsWith("/") && !outputFolder.EndsWith("\\")) {
				outputFolder += "/";
			}
			string shapeFilename = config.Get("shapefile");
			string metadataFilename = config.Get("metadataFile");

			if (config.GetBool("clearOutputFolder")) {
				Console.WriteLine("Clearing output folder...");
				foreach (var file in new DirectoryInfo(outputFolder).GetFiles()) {
					file.Delete();
				}
			}

			var boundingBox = new RectangleD(config.GetDouble("minX"), config.GetDouble("maxY"), config.GetDouble("maxX"), config.GetDouble("minY"));

			var shapeHashSet = new ShapeHashSet(100, config.GetDouble("offset"), config.Get("namePrefix"));
			shapeHashSet.Load(shapeFilename, boundingBox);

			PointSeparator separator = new PointSeparator(shapeHashSet, outputFolder);

			foreach (var file in new DirectoryInfo(inputFolder).GetFiles()) {
				if (file.Extension != ".xyz") {
					continue;
				}
				separator.AddFile(file);
			}

			separator.Run();

			Polygon.SaveAllMetadata(metadataFilename, shapeHashSet.GetPolygons().Where(p => p.ContainsPoints));

			Console.WriteLine("Complete.");
			Console.ReadLine();
		}
	}
}