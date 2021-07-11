using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Updater
{
	public class Config
	{
		public Config(string serviceName, string programFolder, string supervisedFolder)
		{
			ServiceName = serviceName;
			ProgramFolder = programFolder;
			SupervisedFolder = supervisedFolder;
		}

		public string ServiceName { get; private set; }
		public string ProgramFolder { get; private set; }
		public string SupervisedFolder { get; private set; }

		/// <summary>
		/// Gets the configuration from a JSON file
		/// </summary>
		/// <param name="filepath">The Absolute path to the file</param>
		public static Config LoadConfig(string filepath)
		{
			string rawjson = File.ReadAllText(filepath);
			Config Config = JsonSerializer.Deserialize<Config>(rawjson);
			return Config;
		}


		public static void SaveConfig(Config config, string filepath){
			string rawjson = JsonSerializer.Serialize(config);
			File.WriteAllText(filepath, rawjson);
		}
	}
}
