using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DailyArenaDeckAdvisorUpdater
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			var dataFolder = string.Format("{0}Low\\DailyArena\\DailyArenaDeckAdvisor", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
			if (!Directory.Exists(dataFolder))
			{
				Directory.CreateDirectory(dataFolder);
			}
			if (!Directory.Exists($"{dataFolder}\\logs"))
			{
				Directory.CreateDirectory($"{dataFolder}\\logs");
			}

			FileLogger.FilePath = $"{dataFolder}\\logs\\updater.txt";
			if(File.Exists(FileLogger.FilePath))
			{
				File.Delete(FileLogger.FilePath);
			}

			string codeBase = Assembly.GetExecutingAssembly().CodeBase;
			UriBuilder uri = new UriBuilder(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
		}
	}
}
