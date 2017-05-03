using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using XYZSeparator;
using System.Globalization;

public class XYZLoader {
	public static Vector3[] LoadFile(string fileName) {
		return File.ReadAllLines(fileName).Select(line => XYZLoader.parseLine(line)).ToArray();
	}

	public static IEnumerable<Vector3> LoadContinuous(string fileName) {
		var filestream = new System.IO.FileStream(fileName,
										  System.IO.FileMode.Open,
										  System.IO.FileAccess.Read,
										  System.IO.FileShare.ReadWrite);
		var streamReader = new System.IO.StreamReader(filestream, System.Text.Encoding.UTF8, true, 128);
		string line;
		while ((line = streamReader.ReadLine()) != null) {
			yield return parseLine(line);
		}
	}

	private static Vector3 parseLine(string line) {
		try {
			var points = line.Split(',').Where(s => s.Any()).Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
			return new Vector3(points[0], points[2], points[1]);
		}
		catch (FormatException) {
			throw new Exception("Bad line: " + line);
		}
	}
}
