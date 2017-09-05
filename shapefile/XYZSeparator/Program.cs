using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catfood.Shapefile;
using System.Reflection;
using System.Globalization;
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

			var shapeHashSet = new ShapeHashSet(100, config.GetDouble("offset"));
			shapeHashSet.Load(shapeFilename);

			PointSeparator separator = new PointSeparator(shapeHashSet, outputFolder);

			foreach (var file in new DirectoryInfo(inputFolder).GetFiles()) {
				if (file.Extension != ".xyz") {
					continue;
				}
				separator.AddFile(file);
			}

			separator.Run();

			Polygon.SaveAggregatedMetadata(metadataFilename);

			Console.WriteLine("Complete.");
			Console.ReadLine();
		}
	}
}