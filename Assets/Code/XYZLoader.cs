using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class XYZLoader {
	public static Vector3[] LoadFile(string fileName, BuildingMetadata metadata) {
		return File.ReadAllLines(fileName).Select(line => XYZLoader.parseLine(line, metadata.Coordinates)).ToArray();
	}

	public static Vector3[] LoadPointFile(string fileName, BuildingMetadata metadata) {
		double centerX = metadata.Coordinates[0];
		double centerZ = metadata.Coordinates[1];

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

	private static Vector3 parseLine(string line, double[] center) {
		try {
			var points = line.Replace(',', ' ').Split(' ').Where(s => s.Any()).Select(s => double.Parse(s)).ToArray();
			return new Vector3((float)(points[0] - center[0]), (float)(points[2]), (float)(points[1] - center[1]));
		}
		catch (FormatException) {
			throw new Exception("Bad line: " + line);
		}
	}
}
