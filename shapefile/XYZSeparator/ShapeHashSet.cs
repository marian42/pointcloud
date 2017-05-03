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

	public readonly double BucketSize;
	private readonly Dictionary<Bucket, HashSet<ShapePolygon>> data;

	public ShapeHashSet(double bucketSize) {
		this.BucketSize = bucketSize;
		this.data = new Dictionary<Bucket, HashSet<ShapePolygon>>();
	}

	private Bucket getBucket(Vector3 point) {
		return new Bucket((int)Math.Floor(point.x / this.BucketSize), (int)Math.Floor(point.z / this.BucketSize));
	}

	private void add(ShapePolygon shape) {
		for (int x = (int)Math.Floor(shape.BoundingBox.Left / this.BucketSize); x <= (int)Math.Floor(shape.BoundingBox.Right / this.BucketSize); x++) {
			for (int z = (int)Math.Floor(shape.BoundingBox.Top / this.BucketSize); z <= (int)Math.Floor(shape.BoundingBox.Bottom / this.BucketSize); z++) {
				var bucket = new Bucket(x, z);
				if (!this.data.ContainsKey(bucket)) {
					this.data[bucket] = new HashSet<ShapePolygon>();
				}
				if (!this.data[bucket].Contains(shape)) {
					this.data[bucket].Add(shape);
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

	public void Load(string filename) {
		var fileInfo = new System.IO.FileInfo(filename);
		Console.WriteLine("Loading " + fileInfo.Name + "...");
			
		Shapefile shapefile = new Shapefile(filename);
		int count = 0;
		foreach (var shape in shapefile.OfType<ShapePolygon>()) {
			this.add(shape);
			count++;
		}
		Console.WriteLine("Loaded " + count + " shapes in " + this.data.Keys.Count + " buckets.");
	}

	public IEnumerable<ShapePolygon> GetByPoint(Vector3 point) {
		var bucket = this.getBucket(point);
		if (!this.data.ContainsKey(bucket)) {
			return Enumerable.Empty<ShapePolygon>();
		}
		return this.data[bucket];
	}
}
