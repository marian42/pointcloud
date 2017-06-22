using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class PointHashSet {

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

	public readonly float BucketSize;
	private readonly Dictionary<Bucket, HashSet<Vector3>> data;
	private readonly Dictionary<Bucket, HashSet<int>> indices;
	private readonly Dictionary<Bucket, float> groundHeight;
	private Vector3[] points;

	public PointHashSet(float bucketSize, Vector3[] points) {
		this.BucketSize = bucketSize;
		this.data = new Dictionary<Bucket, HashSet<Vector3>>();
		this.indices = new Dictionary<Bucket, HashSet<int>>();
		this.groundHeight = new Dictionary<Bucket, float>();
		this.points = points;

		for (int i = 0; i < this.points.Length; i++) {
			this.add(this.points[i], i);
		}
	}

	private Bucket getBucket(Vector3 point) {
		return new Bucket(Mathf.FloorToInt(point.x / this.BucketSize), Mathf.FloorToInt(point.z / this.BucketSize));
	}

	private void add(Vector3 point, int index) {
		var bucket = this.getBucket(point);
		if (!this.data.ContainsKey(bucket)) {
			this.data[bucket] = new HashSet<Vector3>();
			this.indices[bucket] = new HashSet<int>();
		}
		this.data[bucket].Add(point);
		this.indices[bucket].Add(index);
		if (!this.groundHeight.ContainsKey(bucket) || this.groundHeight[bucket] > point.y) {
			this.groundHeight[bucket] = point.y;
		}
	}

	public IEnumerable<Vector3> GetPointsInRange(Vector3 point, float radius, bool strict) {
		var lowerCorner = this.getBucket(new Vector3(point.x - radius, point.y, point.z - radius));
		var upperCorner = this.getBucket(new Vector3(point.x + radius, point.y, point.z + radius));
		float radiusSquared = Mathf.Pow(radius, 2.0f);

		for (int x = lowerCorner.X; x <= upperCorner.X; x++) {
			for (int z = lowerCorner.Z; z <= upperCorner.Z; z++) {
				var bucket = new Bucket(x, z);
				if (!this.data.ContainsKey(bucket)) {
					continue;
				}
				foreach (var pointInRadius in this.data[bucket]) {
					if (!strict || Mathf.Pow(pointInRadius.x - point.x, 2.0f) + Mathf.Pow(pointInRadius.z - point.z, 2.0f) < radiusSquared) {
						yield return pointInRadius;
					}
				}
			}
		}
	}

	public int GetIndex(Vector3 point) {
		var bucket = this.getBucket(point);
		return this.indices[bucket].FirstOrDefault(i => this.points[i] == point);
	}

	public float GetGroundHeight(Vector3 point) {
		return this.groundHeight[this.getBucket(point)];
	}

	public IEnumerable<IEnumerable<Vector3>> Cluster(float maxDistance, int minPointCount) {
		HashSet<Vector3> completed = new HashSet<Vector3>();

		foreach (var seedPoint in this.points) {
			if (completed.Contains(seedPoint)) {
				continue;
			}
			completed.Add(seedPoint);
			var cluster = new HashSet<Vector3>();
			cluster.Add(seedPoint);

			var toVisit = new Queue<Vector3>();
			toVisit.Enqueue(seedPoint);

			while (toVisit.Any()) {
				var next = toVisit.Dequeue();
				foreach (var inRange in this.GetPointsInRange(next, maxDistance, false).Where(p => !cluster.Contains(p))) {
					cluster.Add(inRange);
					completed.Add(inRange);
					toVisit.Enqueue(inRange);
				}
			}
			if (cluster.Count >= minPointCount) {
				yield return cluster;
			}
		}
	}
}
