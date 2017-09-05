using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catfood.Shapefile;
using XYZSeparator;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public class Polygon {
	public readonly PointD[] OffsetPoints;
	public readonly PointD[] Points;

	public RectangleD BoundingBox;
	
	public readonly string Name;
	private readonly int id;

	private static int globalId;

	private Dictionary<string, string> metadata;
	private static List<Dictionary<string, string>> aggregatedMetadata = new List<Dictionary<string, string>>();

	private bool inAggregatedMetadata;

	public Vector3 Center {
		get;
		private set;
	}

	public Polygon(ShapePolygon shapePolygon, int partIndex, double offset) {
		this.Points = shapePolygon.Parts[partIndex];
		this.OffsetPoints = Polygon.GetEnlargedPolygon(this.Points.ToList(), offset).ToArray();
		this.createBoundingBox();
		this.id = Polygon.globalId;
		Polygon.globalId++;
		this.Name = this.id.ToString().PadLeft(8, '0') + "-" + shapePolygon.GetMetadata("uuid");
		this.metadata = new Dictionary<string, string>();
		foreach (var key in shapePolygon.GetMetadataNames()) {
			this.metadata[key] = shapePolygon.GetMetadata(key);
		}
		this.metadata["filename"] = this.Name;
		this.metadata["address"] = Regex.Replace((shapePolygon.GetMetadata("strasse").Replace("STRA▀E", "Straße").ToLower()), @"(^\w)|(\s\w)", m => m.Value.ToUpper()) + " " + shapePolygon.GetMetadata("hausnr");
		this.findCenter();
	}

	private void findCenter() {
		double centerx = double.Parse(this.metadata["schwerp_x"].Replace(',', '.'), CultureInfo.InvariantCulture);
		double centerz = double.Parse(this.metadata["schwerp_y"].Replace(',', '.'), CultureInfo.InvariantCulture);
		this.Center = new Vector3(centerx, 0.0d, centerz);
	}

	private void createBoundingBox() {
		double minX = this.OffsetPoints[0].X;
		double maxX = this.OffsetPoints[0].X;
		double minY = this.OffsetPoints[0].Y;
		double maxY = this.OffsetPoints[0].Y;
		for (int i = 0; i < this.OffsetPoints.Length; i++) {
			if (this.OffsetPoints[i].X < minX) {
				minX = this.OffsetPoints[i].X;
			}
			if (this.OffsetPoints[i].Y < minY) {
				minY = this.OffsetPoints[i].Y;
			}
			if (this.OffsetPoints[i].X > maxX) {
				minX = this.OffsetPoints[i].X;
			}
			if (this.OffsetPoints[i].Y > maxY) {
				minY = this.OffsetPoints[i].Y;
			}
		}
		this.BoundingBox = new RectangleD(minX, minY, maxX, maxY);
	}

	public static IEnumerable<Polygon> ReadShapePolygon(ShapePolygon shapePolygon, double offset) {
		for (int i = 0; i < shapePolygon.Parts.Count; i++) {
			yield return new Polygon(shapePolygon, i, offset);
		}
	}

	// http://csharphelper.com/blog/2016/01/enlarge-a-polygon-in-c/
	// Return points representing an enlarged polygon.
	private static List<PointD> GetEnlargedPolygon(
		List<PointD> old_points, double offset) {
			List<PointD> enlarged_points = new List<PointD>();
		int num_points = old_points.Count;
		if (old_points[0].X == old_points[num_points - 1].X && old_points[0].X == old_points[num_points - 1].X) {
			num_points--;
		}
		for (int j = 0; j < num_points; j++) {
			// Find the new location for point j.
			// Find the points before and after j.
			int i = (j - 1);
			if (i < 0) i += num_points;
			int k = (j + 1) % num_points;

			// Move the points by the offset.
			PointD v1 = new PointD(
				old_points[j].X - old_points[i].X,
				old_points[j].Y - old_points[i].Y).Normalized().Multiply(offset);
			PointD n1 = new PointD(-v1.Y, v1.X);
			PointD pij1 = new PointD(
				(float)(old_points[i].X + n1.X),
				(float)(old_points[i].Y + n1.Y));
			PointD pij2 = new PointD(
				(float)(old_points[j].X + n1.X),
				(float)(old_points[j].Y + n1.Y));

			PointD v2 = new PointD(
				old_points[k].X - old_points[j].X,
				old_points[k].Y - old_points[j].Y).Normalized().Multiply(offset);
			PointD n2 = new PointD(-v2.Y, v2.X);

			PointD pjk1 = new PointD(
				(float)(old_points[j].X + n2.X),
				(float)(old_points[j].Y + n2.Y));
			PointD pjk2 = new PointD(
				(float)(old_points[k].X + n2.X),
				(float)(old_points[k].Y + n2.Y));

			// See where the shifted lines ij and jk intersect.
			bool lines_intersect, segments_intersect;
			PointD poi, close1, close2;
			findIntersection(pij1, pij2, pjk1, pjk2,
				out lines_intersect, out segments_intersect,
				out poi, out close1, out close2);
			if (lines_intersect && !double.IsNaN(poi.X) && !double.IsNaN(poi.Y)) {
				enlarged_points.Add(poi);
			}			
		}

		return enlarged_points;
	}

	// http://csharphelper.com/blog/2014/08/determine-where-two-lines-intersect-in-c/
	// Find the point of intersection between
	// the lines p1 --> p2 and p3 --> p4.
	private static void findIntersection(
		PointD p1, PointD p2, PointD p3, PointD p4,
		out bool lines_intersect, out bool segments_intersect,
		out PointD intersection,
		out PointD close_p1, out PointD close_p2) {
		// Get the segments' parameters.
		double dx12 = p2.X - p1.X;
		double dy12 = p2.Y - p1.Y;
		double dx34 = p4.X - p3.X;
		double dy34 = p4.Y - p3.Y;

		// Solve for t1 and t2
		double denominator = (dy12 * dx34 - dx12 * dy34);

		double t1 =
			((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
				/ denominator;
		if (double.IsInfinity(t1)) {
			// The lines are parallel (or close enough to it).
			lines_intersect = false;
			segments_intersect = false;
			intersection = new PointD(float.NaN, float.NaN);
			close_p1 = new PointD(float.NaN, float.NaN);
			close_p2 = new PointD(float.NaN, float.NaN);
			return;
		}
		lines_intersect = true;

		double t2 =
			((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
				/ -denominator;

		// Find the point of intersection.
		intersection = new PointD(p1.X + dx12 * t1, p1.Y + dy12 * t1);

		// The segments intersect if t1 and t2 are between 0 and 1.
		segments_intersect =
			((t1 >= 0) && (t1 <= 1) &&
			 (t2 >= 0) && (t2 <= 1));

		// Find the closest points on the segments.
		if (t1 < 0) {
			t1 = 0;
		} else if (t1 > 1) {
			t1 = 1;
		}

		if (t2 < 0) {
			t2 = 0;
		} else if (t2 > 1) {
			t2 = 1;
		}

		close_p1 = new PointD(p1.X + dx12 * t1, p1.Y + dy12 * t1);
		close_p2 = new PointD(p3.X + dx34 * t2, p3.Y + dy34 * t2);
	}

	public bool Contains(Vector3 testPoint) {
		// http://stackoverflow.com/a/14998816/895589
		bool result = false;
		int j = this.OffsetPoints.Length - 1;
		for (int i = 0; i < this.OffsetPoints.Count(); i++) {
			if (this.OffsetPoints[i].Y < testPoint.z && this.OffsetPoints[j].Y >= testPoint.z || this.OffsetPoints[j].Y < testPoint.z && this.OffsetPoints[i].Y >= testPoint.z) {
				if (this.OffsetPoints[i].X + (testPoint.z - this.OffsetPoints[i].Y) / (this.OffsetPoints[j].Y - this.OffsetPoints[i].Y) * (this.OffsetPoints[j].X - this.OffsetPoints[i].X) < testPoint.x) {
					result = !result;
				}
			}
			j = i;
		}
		return result;
	}

	public override string ToString() {
		return this.OffsetPoints.Aggregate("", (s, p) => s + string.Format(CultureInfo.InvariantCulture, "({0:0.00} {1:0.00}), ", p.X, p.Y)).Trim();
	}

	public void SavePolygon(string folder) {
		string filename = folder + this.Name + ".xyzshape";
		File.WriteAllLines(filename, this.Points.Select(p => new Vector3(p.X, 0, p.Y)).Select(p => p.ToXYZLine()));
	}

	public void SaveMetadata(string folder) {
		string filename = folder + this.Name + ".json";
		string json = JsonConvert.SerializeObject(this.metadata, Newtonsoft.Json.Formatting.Indented);
		File.WriteAllText(filename, json);

		if (!this.inAggregatedMetadata) {
			Polygon.aggregatedMetadata.Add(this.metadata);
			this.inAggregatedMetadata = true;
		}
	}

	public static void SaveAggregatedMetadata(string filename) {
		Console.WriteLine("Writing metadata...");
		using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename, false)) {
			file.Write("[");
			foreach (var building in Polygon.aggregatedMetadata) {
				string json = JsonConvert.SerializeObject(building, Newtonsoft.Json.Formatting.Indented);
				file.Write(json);
				file.Write(", ");
			}
			file.Write("]");
		}
	}
}

public static class Extension {
	public static double GetLength(this PointD point) {
		return Math.Sqrt(Math.Pow(point.X, 2.0d) + Math.Pow(point.Y, 2.0d));
	}

	public static PointD Normalized(this PointD point) {
		return point.Multiply(1.0 / point.GetLength());
	}

	public static PointD Multiply(this PointD point, double factor) {
		return new PointD(point.X * factor, point.Y * factor);
	}
}
