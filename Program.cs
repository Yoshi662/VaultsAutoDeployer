using System;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Threading;

namespace Updater
{
	public class Program
	{
		static void Main(string[] args)
		{
			Config[] configs = Config.LoadConfig("Config.json"); //Load Configurations

			int monitoredFolders = configs.Length;

			Thread[] threads = new Thread[monitoredFolders];
			Log($"Updater Inicialized. Monitored programs: \"{monitoredFolders}\"");
			for (int i = 0; i < monitoredFolders; i++)
			{
				threads[i] = new Thread(() => Update(configs[i]));
				threads[i].Name = configs[i].ServiceName;
				threads[i].Start();
				Thread.Sleep(50);
			}
			Thread.Sleep(Timeout.Infinite);
		}
		/// <summary>
		/// From a <see cref="Config"/> it auto deploys a program 
		/// </summary>
		/// <param name="currentConfig"></param>
		public static void Update(Config currentConfig)
		{
			try
			{
				FileSystemWatcher filewatcher = new(currentConfig.SupervisedFolder);

				filewatcher.Filter = "*.exe";
				filewatcher.Created += Filewatcher_Created;
				filewatcher.EnableRaisingEvents = true;

				Log($"New monitor: [{currentConfig.ServiceName}]");

				while (true)
				{
					_ = filewatcher.WaitForChanged(WatcherChangeTypes.All);
				}
				void Filewatcher_Created(object sender, FileSystemEventArgs e)
				{
					Log($" [{currentConfig.ServiceName}] - {e.Name} Detected. Waiting for Changes");
					Thread.Sleep(15 * 1000); //15 seg

					using ServiceController serviceController = new(currentConfig.ServiceName);
					ServiceControllerStatus serviceStatus = serviceController.Status;

					//Paramos el servicio
					if (serviceStatus == ServiceControllerStatus.Running)
					{
						Log($" [{currentConfig.ServiceName}] - Stopping \"{currentConfig.ServiceName}\"");
						serviceController.Stop();
					} else
					{
						Log($" [{currentConfig.ServiceName}] - \"{currentConfig.ServiceName}\" is not running");
					}

					//Movemos Archivos
					try
					{
						Log($" [{currentConfig.ServiceName}] - Deleting \"{currentConfig.ProgramFolder}\"");
						Thread.Sleep(1 * 1000);
						EmptyFolder(currentConfig.ProgramFolder);

						Log($" [{currentConfig.ServiceName}] - Copying \"{currentConfig.SupervisedFolder}\" to \"{currentConfig.ProgramFolder}\"");
						CopyFilesRecursively(currentConfig.SupervisedFolder, currentConfig.ProgramFolder);
						Thread.Sleep(1 * 1000);

						Log($" [{currentConfig.ServiceName}] - Deleting \"{currentConfig.SupervisedFolder}\"");
						EmptyFolder(currentConfig.SupervisedFolder);

						Log($" [{currentConfig.ServiceName}] - Starting \"{currentConfig.ServiceName}\"");
						serviceController.Start();
						Log($" [{currentConfig.ServiceName}] - Service {currentConfig.ServiceName}. Updated Sucessfully");
					}
					catch (Exception ex)
					{
						Log($" [{currentConfig.ServiceName}] - Exception ocurred while updating" +
						$"\n\t\t{ex.Message}");
					}

					static void EmptyFolder(string path)
					{
						DirectoryInfo directoryInfo = new(path);
						foreach (FileInfo file in directoryInfo.GetFiles())
						{
							file.Delete();
						}
						foreach (DirectoryInfo folder in directoryInfo.GetDirectories())
						{
							folder.Delete(true);
						}
					}

					static void CopyFilesRecursively(string sourcePath, string targetPath)
					{
						//Now Create all of the directories
						foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
						{
							Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
						}

						//Copy all the files & Replaces any files with the same name
						foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
						{
							File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
						}
					}
				}
			}
			catch (Exception e)
			{
				Log(e.ToString());
			}
		}

		public static void Log(string output)
		{
			Console.WriteLine($"[{DateTime.Now:s}] - {output}");
		}
	}
}
