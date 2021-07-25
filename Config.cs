using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Updater
{
	public class Config
	{
		public Config(string serviceName, string programFolder, string supervisedFolder, string[] protectedFiles)
		{
			ServiceName = serviceName;
			ProgramFolder = programFolder;
			SupervisedFolder = supervisedFolder;
			ProtectedFiles = protectedFiles;
		}

		public string ServiceName { get; private set; }
		public string ProgramFolder { get; private set; }
		public string SupervisedFolder { get; private set; }
		public string[] ProtectedFiles { get; private set; }

		public static Config[] LoadConfig(string filepath)
		{
			string rawjson = File.ReadAllText(filepath);
			Config[] Config = JsonSerializer.Deserialize<Config[]>(rawjson);
			return Config;
		}

		public static void SaveConfig(Config[] config, string filepath){
			string rawjson = JsonSerializer.Serialize(config);
			File.WriteAllText(filepath, rawjson);
		}
	}
}
