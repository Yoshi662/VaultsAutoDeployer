using System;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Threading;

namespace Updater
{
	class Program
	{/* Workflow
	  *		Detect files in a certain folder
	  *		Wait a minute
	  *		Stop Service (Admin perms required)
	  *		Copy files to certain directory
	  *		Start Service
	  *		Delete Original Files
	  *		
	  *	Config File (json)
	  *		String ServiceName
	  *		String ProgramFiles
	  *		String Supervised_Folder
	  */
		static void Main(string[] args)
		{
			try
			{
				Config currentConfig = Config.LoadConfig("config.json");

				FileSystemWatcher filewatcher = new(currentConfig.SupervisedFolder);

				filewatcher.Filter = "*.exe";
				filewatcher.Created += Filewatcher_Created;
				filewatcher.EnableRaisingEvents = true;

				Log($"Updater Inicialized. Current Project [{currentConfig.ServiceName}]");

				while (true)
				{
					_ = filewatcher.WaitForChanged(WatcherChangeTypes.All);
				}
				void Filewatcher_Created(object sender, FileSystemEventArgs e)
				{
					Log($"{e.Name} Detected. Waiting for Changes");
					Thread.Sleep(15 * 1000);

					using ServiceController serviceController = new(currentConfig.ServiceName);
					ServiceControllerStatus serviceStatus = serviceController.Status;

					//Paramos el servicio
					if (serviceStatus == ServiceControllerStatus.Running)
					{
						Log($"Stopping \"{currentConfig.ServiceName}\"");
						serviceController.Stop();
					} else {
						Log($"\"{currentConfig.ServiceName}\" is not running");
					}

					//Movemos Archivos
					try
					{
						Log($"Deleting \"{currentConfig.ProgramFolder}\"");
						Thread.Sleep(1 * 1000);
						EmptyFolder(currentConfig.ProgramFolder);

						Log($"Copying \"{currentConfig.SupervisedFolder}\" to \"{currentConfig.ProgramFolder}\"");
						CopyFilesRecursively(currentConfig.SupervisedFolder, currentConfig.ProgramFolder);
						Thread.Sleep(1 * 1000);

						Log($"Deleting \"{currentConfig.SupervisedFolder}\"");
						EmptyFolder(currentConfig.SupervisedFolder);

						Log($"Starting \"{currentConfig.ServiceName}\"");
						serviceController.Start();
						Log($"Service {currentConfig.ServiceName}. Updated Sucessfully");
					}
					catch (Exception ex)
					{
						Log($"Exception ocurred while updating" +
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
				Thread.Sleep(15 * 1000);
			}
		}
		
		public static void Log(string output){
			Console.WriteLine($"[{DateTime.Now:s}] - {output}");
		}
	}
}
