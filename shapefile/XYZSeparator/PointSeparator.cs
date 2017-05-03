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

		private Dictionary<ShapePolygon, List<Vector3>> currentPoints;
		private Queue<ShapePolygon> queue;

		public PointSeparator(ShapeHashSet shapes, string polygonFolder) {
			this.shapes = shapes;
			this.polygonFolder = polygonFolder;
			this.currentPoints = new Dictionary<ShapePolygon, List<Vector3>>();
			this.queue = new Queue<ShapePolygon>();
		}

		private void dequeueAndSave() {
			var polygon = this.queue.Dequeue();
			var points = this.currentPoints[polygon];
			this.currentPoints.Remove(polygon);
			File.AppendAllLines(this.getPolygonFileName(polygon), points.Select(p => p.ToXYZLine()));
			Console.WriteLine("Wrote " + points.Count + " points to " + polygon.GetMetadata("uuid") + ".xyz");
		}

		int buildings = 0;

		private void addPoint(ShapePolygon polygon, Vector3 point) {
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
					if (polygon.Parts.Any(part => this.pointInPolygon(part, point))) {
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

		private bool pointInPolygon(PointD[] polygon, Vector3 testPoint) {
			// http://stackoverflow.com/a/14998816/895589
			bool result = false;
			int j = polygon.Length - 1;
			for (int i = 0; i < polygon.Count(); i++) {
				if (polygon[i].Y < testPoint.z && polygon[j].Y >= testPoint.z || polygon[j].Y < testPoint.z && polygon[i].Y >= testPoint.z) {
					if (polygon[i].X + (testPoint.z - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.x) {
						result = !result;
					}
				}
				j = i;
			}
			return result;
		}

		private string getPolygonFileName(ShapePolygon polygon) {
			return this.polygonFolder + polygon.GetMetadata("uuid") + ".xyz";
		}
	}
}
