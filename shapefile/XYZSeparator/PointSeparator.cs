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
		private string outputFolder;
		private const int QUEUE_LENGTH = 400;
		public int HitCount { get; private set; }

		private Dictionary<Polygon, List<Vector3>> currentPoints;
		private Queue<Polygon> queue;

		private long hits = 0;
		private long points = 0;
		private int files = 0;
		private int buildings = 0;
		private DateTime lastUpdate = DateTime.Now;

		public PointSeparator(ShapeHashSet shapes, string outputFolder) {
			this.shapes = shapes;
			this.outputFolder = outputFolder;
			this.currentPoints = new Dictionary<Polygon, List<Vector3>>();
			this.queue = new Queue<Polygon>();
			this.HitCount = 0;
		}

		private void dequeueAndSave() {
			var polygon = this.queue.Dequeue();
			var points = this.currentPoints[polygon];
			this.currentPoints.Remove(polygon);
			File.AppendAllLines(this.outputFolder + polygon.GetXYZFilename(), points.Select(p => p.ToXYZLine()));
			polygon.SavePolygon(this.outputFolder);
			polygon.SaveMetadata(this.outputFolder);
			this.HitCount += points.Count;
		}

		private void addPoint(Polygon polygon, Vector3 point) {
			if (!this.currentPoints.ContainsKey(polygon)) {
				this.currentPoints[polygon] = new List<Vector3>();
				this.queue.Enqueue(polygon);
				if (this.queue.Count > PointSeparator.QUEUE_LENGTH) {
					this.dequeueAndSave();
				}
				buildings++;
			}
			this.currentPoints[polygon].Add(point);
		}

		public void ProcessXYZFile(string filename) {
			foreach (var point in XYZLoader.LoadContinuous(filename)) {
				foreach (var polygon in this.shapes.GetByPoint(point)) {
					if (polygon.Contains(point)) {
						this.addPoint(polygon, point);
						hits++;
					}
				}
				points++;
				if (points % 1000 == 0 && (DateTime.Now - lastUpdate).TotalSeconds > 1) {
					Console.WriteLine("Processed " + this.files.ToString().PadLeft(3) + " files, "
						+ this.points.ToString().PadLeft(12) + " points, "
						+ this.hits.ToString().PadLeft(9) + " hits, "
						+ buildings.ToString().PadLeft(6) +  " buildings");
					lastUpdate = DateTime.Now;
				}
			}
			this.files++;
		}

		public void ClearQueue() {
			while (this.queue.Any()) {
				this.dequeueAndSave();
			}
		}
	}
}
