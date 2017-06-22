using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class XYZLoader {
	public const double ReferenceX = 391812;
	public const double ReferenceY = 5713741;
	public const double ReferenceZ = 80;

	public static Vector3[] LoadFile(string fileName) {
		return File.ReadAllLines(fileName).Select(line => XYZLoader.parseLine(line)).ToArray();
	}

	private static Vector3 parseLine(string line) {
		try {
			var points = line.Replace(',', ' ').Split(' ').Where(s => s.Any()).Select(s => double.Parse(s)).ToArray();
			return new Vector3((float)(points[0] - XYZLoader.ReferenceX), (float)(points[2] - XYZLoader.ReferenceZ), (float)(points[1] - XYZLoader.ReferenceY));
		}
		catch (FormatException) {
			throw new Exception("Bad line: " + line);
		}		
	}
}
