using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catfood.Shapefile;
using System.Globalization;

namespace XYZSeparator {
    public struct Vector3 {
		public readonly double x;
		public readonly double y;
		public readonly double z;

		public Vector3(double x, double y, double z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public Vector3(PointD point) {
			this.x = (double)point.X;
			this.y = 0.0f;
			this.z = (double)point.Y;
		}

		public string ToXYZLine() {
			return string.Format(CultureInfo.InvariantCulture, "{0:0.00} {1:0.00} {2:0.00}", this.x, this.z, this.y);
		}

		public override string ToString() {
			return string.Format(CultureInfo.InvariantCulture, "({0:0.00} {1:0.00} {2:0.00})", this.x, this.y, this.z);
		}
    }
}
