using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using XYZSeparator;
using Catfood.Shapefile;

public class ShapeHashSet {

	private class Bucket {
		public readonly int X;
		public readonly int Z;

		public Bucket(int x, int z) {
			this.X = x;
			this.Z = z;
		}

		public override int GetHashCode() {
			return this.X * 10000 + this.Z;
		}

		public override bool Equals(object obj) {
			if (!(obj is Bucket)) {
				return false;
			}
			Bucket bucket = obj as Bucket;
			return bucket.X == this.X && bucket.Z == this.Z;
		}
	}

	private readonly double bucketSize;
	private readonly double offset;
	private readonly string namePrefix;
	private readonly Dictionary<Bucket, HashSet<Polygon>> data;
	private readonly List<Polygon> polygons;

	public ShapeHashSet(double bucketSize, double offset, string namePrefix) {
		this.bucketSize = bucketSize;
		this.offset = offset;
		this.data = new Dictionary<Bucket, HashSet<Polygon>>();
		this.polygons = new List<Polygon>();
		this.namePrefix = namePrefix;
	}

	private Bucket getBucket(Vector3 point) {
		return new Bucket((int)Math.Floor(point.x / this.bucketSize), (int)Math.Floor(point.z / this.bucketSize));
	}

	private void add(Polygon polygon) {
		this.polygons.Add(polygon);
		for (int x = (int)Math.Floor(polygon.BoundingBox.Left / this.bucketSize); x <= (int)Math.Floor(polygon.BoundingBox.Right / this.bucketSize); x++) {
			for (int z = (int)Math.Floor(polygon.BoundingBox.Top / this.bucketSize); z <= (int)Math.Floor(polygon.BoundingBox.Bottom / this.bucketSize); z++) {
				var bucket = new Bucket(x, z);
				if (!this.data.ContainsKey(bucket)) {
					this.data[bucket] = new HashSet<Polygon>();
				}
				if (!this.data[bucket].Contains(polygon)) {
					this.data[bucket].Add(polygon);
				}
			}
		}
	}

	public void PrintStats() {
		int N = 30;
		int[] counts = new int[N];
		foreach (var bucket in this.data.Keys) {
			if (this.data[bucket].Count < N) {
				counts[data[bucket].Count]++;
			}
		}
		for (int i = 0; i < N; i++) {
			Console.WriteLine(i.ToString().PadLeft(3) + ": " + counts[i] + " buckets.");
		}
	}

	public void Load(string filename, RectangleD boundingBox) {
		var fileInfo = new System.IO.FileInfo(filename);
		Console.WriteLine("Loading " + fileInfo.Name + "...");
			
		Shapefile shapefile = new Shapefile(filename);
		foreach (var shape in shapefile.OfType<ShapePolygon>()) {
			foreach (var polygon in Polygon.ReadShapePolygon(shape, this.offset, this.namePrefix)) {
				if (boundingBox.Left < polygon.Center.x && boundingBox.Right > polygon.Center.x && boundingBox.Top > polygon.Center.z && boundingBox.Bottom < polygon.Center.z) {
					this.add(polygon);
				}
			}
		}
		Console.WriteLine("Loaded " + this.polygons.Count() + " polygons in " + this.data.Keys.Count + " buckets.");
	}

	public IEnumerable<Polygon> GetByPoint(Vector3 point) {
		var bucket = this.getBucket(point);
		if (!this.data.ContainsKey(bucket)) {
			return Enumerable.Empty<Polygon>();
		}
		return this.data[bucket];
	}

	public IEnumerable<Polygon> GetPolygons() {
		return this.polygons;
	}
}
