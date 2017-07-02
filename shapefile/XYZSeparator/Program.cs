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
			string inputFolder = "C:/Uni/Bachelorarbeit/git/data/";
			string outputFolder = "C:/Uni/Bachelorarbeit/git/data/buildings/";
			foreach (var file in new DirectoryInfo(outputFolder).GetFiles()) {
				file.Delete();
			}

			var startTime = DateTime.Now;
			string shapeFileName = "C:/Uni/Bachelorarbeit/git/data/Stand_Jan12_Grundrissdaten/Dortmun_24_01_12.shp";
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