using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class XYZLoader {
	public struct Vector3d {
		public readonly double x;
		public readonly double y;
		public readonly double z;

		public Vector3d(double x, double y, double z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	public static IEnumerable<Vector3d> LoadXYZFile(string fileName) {
		using (var filestream = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite)) {
			using (var streamReader = new System.IO.StreamReader(filestream, System.Text.Encoding.UTF8, true, 128)) {
				while (!streamReader.EndOfStream) {
					var line = streamReader.ReadLine();
					yield return XYZLoader.parseLine(line);
				}
			}
		}
	}

	public static Vector3[] LoadPointFile(string fileName, BuildingMetadata metadata) {
		List<Vector3> result = new List<Vector3>();

		using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read)) {
			using (var reader = new BinaryReader(fileStream)) {
				while (fileStream.Position != fileStream.Length) {
					short x = reader.ReadInt16();
					short y = reader.ReadInt16();
					short z = reader.ReadInt16();

					result.Add(new Vector3(
						(float)((double)x / 100.0d),
						(float)((double)y / 100.0d),
						(float)((double)z / 100.0d)));
				}
			}
		}

		return result.ToArray();
	}

	private static Vector3d parseLine(string line) {
		try {
			var points = line.Replace(',', ' ').Split(' ').Where(s => s.Any()).Select(s => double.Parse(s)).ToArray();
			return new Vector3d(points[0], points[2], points[1]);
		}
		catch (FormatException) {
			throw new Exception("Bad line: " + line);
		}
	}
}
