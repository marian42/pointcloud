using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace XYZSeparator {
	class ConfigReader {
		private Dictionary<String, String> data;

		public ConfigReader(String filename) {
			this.data = new Dictionary<string, string>();
			foreach (var line in File.ReadAllLines(filename)) {
				var trimmedLine = line.Trim();
				if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith("#") || !trimmedLine.Contains("=")) {
					continue;
				}
				var key = line.Substring(0, line.IndexOf("="));
				var value = line.Substring(line.IndexOf("=") + 1);

				this.data.Add(key.Trim().ToLower(), value.Trim());
			}
		}

		public string Get(string key) {
			return this.data[key.ToLower()];
		}

		public bool GetBool(string key) {
			var value = this.data[key.ToLower()].ToLower();
			return value == "1" || value == "true";
		}

		public double GetDouble(string key) {
			return double.Parse(this.data[key.ToLower()], CultureInfo.InvariantCulture);
		}
	}
}
