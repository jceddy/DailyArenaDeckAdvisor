using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Compact;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DailyArenaDeckAdvisorLauncher
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		/// <summary>
		/// The logger for the launcher app.
		/// </summary>
		public ILogger Logger { get; private set; }

		/// <summary>
		/// Default constructer, sets up the data folder, current directory, and logging.
		/// </summary>
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

			SelfLog.Enable(msg => Debug.WriteLine(msg));
			SelfLog.Enable(Console.Error);
			Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.
				File(new CompactJsonFormatter(), $"{dataFolder}\\logs\\log.txt",
					rollingInterval: RollingInterval.Hour,
					retainedFileCountLimit: 5,
					fileSizeLimitBytes: 10485760,
					rollOnFileSizeLimit: true).
				CreateLogger();

			string codeBase = Assembly.GetExecutingAssembly().CodeBase;
			UriBuilder uri = new UriBuilder(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
		}
	}
}
