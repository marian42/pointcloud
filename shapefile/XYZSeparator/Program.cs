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
			string polygonFolder = "C:/Uni/Bachelorarbeit/git/data/buildings/";
			foreach (var file in new DirectoryInfo(polygonFolder).GetFiles()) {
				file.Delete();
			}

			var startTime = DateTime.Now;
			string pointCouldFileName = "C:/Uni/Bachelorarbeit/git/data/dom1l-fp_32391_5713_1_nw.xyz";			
			string shapeFileName = "C:/Uni/Bachelorarbeit/git/data/Stand_Jan12_Grundrissdaten/Dortmun_24_01_12.shp";
			var shapeHashSet = new ShapeHashSet(100, 2);
			shapeHashSet.Load(shapeFileName);

			PointSeparator separator = new PointSeparator(shapeHashSet, polygonFolder);
			separator.ProcessXYZFile(pointCouldFileName);
			Console.WriteLine("Found " + separator.HitCount + " points in " + (DateTime.Now - startTime).TotalSeconds + " seconds.");
			
			Console.ReadLine();
		}
	}
}