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

			var startTime = DateTime.Now;
			var shapeHashSet = new ShapeHashSet(100, 2);
			shapeHashSet.Load(shapeFileName);

			PointSeparator separator = new PointSeparator(shapeHashSet, outputFolder);

			foreach (var file in new DirectoryInfo(inputFolder).GetFiles()) {
				if (file.Extension != ".xyz") {
					continue;
				}
				separator.ProcessXYZFile(file.FullName);
			}

			separator.ClearQueue();
			Polygon.SaveAggregatedMetadata(outputFolder);
			Console.WriteLine("Found " + separator.HitCount + " points in " + (int)System.Math.Floor((DateTime.Now - startTime).TotalMinutes) + "m " + (DateTime.Now - startTime).Seconds + "s.");
			
			Console.ReadLine();
		}
	}
}