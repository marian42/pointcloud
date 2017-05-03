using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catfood.Shapefile;
using System.Reflection;
using System.Globalization;

namespace XYZSeparator {
	class Program {
		public static void Main(string[] args) {
			string pointCouldFileName = "C:/Uni/Bachelorarbeit/git/data/dom1l-fp_32391_5713_1_nw.xyz";			
			string shapeFileName = "C:/Uni/Bachelorarbeit/git/data/Stand_Jan12_Grundrissdaten/Dortmun_24_01_12.shp";
			var shapeHashSet = new ShapeHashSet(200);
			shapeHashSet.Load(shapeFileName);

			PointSeparator separator = new PointSeparator(shapeHashSet, "C:/Uni/Bachelorarbeit/git/data/buildings/");
			separator.ProcessXYZFile(pointCouldFileName);
			
			Console.ReadLine();
		}
	}
}
