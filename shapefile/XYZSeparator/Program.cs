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
			string inputFolder = "E:/pointdata/xyz/";
			string outputFolder = "E:/pointdata/output/";
			string shapeFileName = "E:/pointdata/shape/shapes.shp";
			
			foreach (var file in new DirectoryInfo(outputFolder).GetFiles()) {
				file.Delete();
			}

			var shapeHashSet = new ShapeHashSet(100, 2);
			shapeHashSet.Load(shapeFileName);

			PointSeparator separator = new PointSeparator(shapeHashSet, outputFolder);

			foreach (var file in new DirectoryInfo(inputFolder).GetFiles()) {
				if (file.Extension != ".xyz") {
					continue;
				}
				separator.AddFile(file);
			}

			separator.Run();

			Polygon.SaveAggregatedMetadata(outputFolder);
			Console.ReadLine();
		}
	}
}