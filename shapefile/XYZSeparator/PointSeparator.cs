using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catfood.Shapefile;
using System.IO;

namespace XYZSeparator {
	public class PointSeparator {
		private ShapeHashSet shapes;
		private string polygonFolder;
		private const int QUEUE_LENGTH = 400;
		public int HitCount { get; private set; }

		private Dictionary<Polygon, List<Vector3>> currentPoints;
		private Queue<Polygon> queue;

		public PointSeparator(ShapeHashSet shapes, string polygonFolder) {
			this.shapes = shapes;
			this.polygonFolder = polygonFolder;
			this.currentPoints = new Dictionary<Polygon, List<Vector3>>();
			this.queue = new Queue<Polygon>();
			this.HitCount = 0;
		}

		private void dequeueAndSave() {
			var polygon = this.queue.Dequeue();
			var points = this.currentPoints[polygon];
			this.currentPoints.Remove(polygon);
			File.AppendAllLines(this.getPolygonFileName(polygon), points.Select(p => p.ToXYZLine()));
			Console.WriteLine("Wrote " + points.Count + " points to " + polygon.ShapePolygon.GetMetadata("uuid") + ".xyz");
			this.HitCount += points.Count;
		}

		int buildings = 0;

		private void addPoint(Polygon polygon, Vector3 point) {
			if (!this.currentPoints.ContainsKey(polygon)) {
				this.currentPoints[polygon] = new List<Vector3>();
				this.queue.Enqueue(polygon);
				if (this.queue.Count > PointSeparator.QUEUE_LENGTH) {
					this.dequeueAndSave();
				}
				buildings++;
				Console.WriteLine("buildings: " + buildings);
			}
			this.currentPoints[polygon].Add(point);
		}

		public void ProcessXYZFile(string filename) {
			int hits = 0;
			int points = 0;
			var lastUpdate = DateTime.Now;
			foreach (var point in XYZLoader.LoadContinuous(filename)) {
				foreach (var polygon in this.shapes.GetByPoint(point)) {
					if (polygon.Contains(point)) {
						this.addPoint(polygon, point);
						hits++;
					}
				}
				points++;
				if ((DateTime.Now - lastUpdate).TotalSeconds > 1) {
					Console.WriteLine("Processed " + points + " points, " + hits + " hits.");
					lastUpdate = DateTime.Now;
				}
			}
			while (this.queue.Any()) {
				this.dequeueAndSave();
			}
		}

		private string getPolygonFileName(Polygon polygon) {
			return this.polygonFolder + polygon.ShapePolygon.GetMetadata("uuid") + ".xyz";
		}
	}
}
