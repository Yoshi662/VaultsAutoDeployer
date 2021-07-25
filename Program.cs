using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Linq;
using System.Security.Principal;

namespace Updater
{
	public class Program
	{

		static void Main(string[] args)
		{
			//Asks for admin privilege if we don't have it 
			if (!IsAdministrator())
			{
				Console.WriteLine("This program must be running with administrator rights");
				Thread.Sleep(5000);
				Environment.Exit(0);
			}

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
					Log($"[{currentConfig.ServiceName}] - {e.Name} Detected. Waiting for Changes");
					Thread.Sleep(15 * 1000); //15 seg

					using ServiceController serviceController = new(currentConfig.ServiceName);
					ServiceControllerStatus serviceStatus = serviceController.Status;


					try
					{
						//Paramos el servicio
						if (serviceStatus == ServiceControllerStatus.Running)
						{
							Log(currentConfig, $"Stopping Service");
							serviceController.Stop();
						} else
						{
							Log(currentConfig, $"Service is not running");
						}

						//Movemos Archivos
						Log(currentConfig, $"Deleting \"{currentConfig.ProgramFolder}\"");
						Thread.Sleep(1 * 1000);
						EmptyFolder(currentConfig.ProgramFolder, currentConfig);

						Log(currentConfig, $"Copying \"{currentConfig.SupervisedFolder}\" to \"{currentConfig.ProgramFolder}\"");
						CopyFilesRecursively(currentConfig.SupervisedFolder, currentConfig.ProgramFolder, currentConfig);
						Thread.Sleep(1 * 1000);

						Log(currentConfig, $"Deleting \"{currentConfig.SupervisedFolder}\"");
						EmptyFolder(currentConfig.SupervisedFolder, currentConfig, true);

						Log(currentConfig, $" Starting Service");
						serviceController.Start();
						Log(currentConfig, $"Service updated sucessfully");
					}
					catch (Exception ex)
					{
						Log(currentConfig, $"Exception ocurred while updating" +
						$"\n\t\t{ex.Message}");
					}

					static void EmptyFolder(string path, Config config, bool skipProtectedFiles = false)
					{
						DirectoryInfo directoryInfo = new(path);
						foreach (FileInfo file in directoryInfo.GetFiles())
						{
							if (!config.ProtectedFiles.Contains(Path.GetFileName(file.Name)) || skipProtectedFiles)
							{
								file.Delete();
							} else
							{
								Log(config, $"Skipped deletion of {Path.GetFileName(file.Name)}.");
							}
						}
						foreach (DirectoryInfo folder in directoryInfo.GetDirectories())
						{
							folder.Delete(true);
						}
					}

					static void CopyFilesRecursively(string sourcePath, string targetPath, Config config)
					{
						//Now Create all of the directories
						foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
						{
							Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
						}

						//Copy all the files & Replaces any files with the same name
						foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
						{
							//Prevents overwriting the protected files
							if (!config.ProtectedFiles.Contains(Path.GetFileName(newPath)))
							{
								File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
							} else {
								Log(config, $"Skipped protected file {Path.GetFileName(newPath)}.");
							}
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
			Console.WriteLine($"{DateTime.Now:s} - {output}");
		}

		public static void Log(Config config,string output)
		{
			Console.WriteLine($"{DateTime.Now:s} - [{config.ServiceName}] - {output}");
		}

		public static bool IsAdministrator()
		{
			return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
					  .IsInRole(WindowsBuiltInRole.Administrator);
		}
	}
}
