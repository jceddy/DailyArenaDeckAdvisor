using Newtonsoft.Json;
using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Compact;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace DailyArenaDeckAdvisor
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		/// <summary>
		/// The application's Cached State.
		/// </summary>
		public CachedState State { get; private set; } = new CachedState();

		/// <summary>
		/// The application's Logger.
		/// </summary>
		public ILogger Logger { get; private set; }

		/// <summary>
		/// Logger for first chance exceptions.
		/// </summary>
		public ILogger FirstChanceLogger { get; private set; }

		/// <summary>
		/// The application constructor.
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
			Directory.SetCurrentDirectory(dataFolder);

			SelfLog.Enable(msg => Debug.WriteLine(msg));
			SelfLog.Enable(Console.Error);
			Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.
				File(new CompactJsonFormatter(), $"{dataFolder}\\logs\\log.txt",
					rollingInterval: RollingInterval.Hour,
					retainedFileCountLimit: 5,
					fileSizeLimitBytes: 10485760,
					rollOnFileSizeLimit: true).
				CreateLogger();
			FirstChanceLogger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.
				File(new CompactJsonFormatter(), $"{dataFolder}\\logs\\firstChanceExceptions.txt",
					rollingInterval: RollingInterval.Hour,
					retainedFileCountLimit: 2,
					fileSizeLimitBytes: 10485760,
					rollOnFileSizeLimit: true).
				CreateLogger();

			DispatcherUnhandledException += (sender, e) =>
			{
				Logger.Error(e.Exception, "DispatcherUnhandledException");
			};

			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				Logger.Error((Exception)e.ExceptionObject, "UnhandledException");
			};

			AppDomain.CurrentDomain.FirstChanceException += (source, e) =>
			{
				FirstChanceLogger.Debug(e.Exception, "FirstChanceException");
			};

			LoadState();
		}

		/// <summary>
		/// Save the Cached State out to a file.
		/// </summary>
		public void SaveState()
		{
			string stateJson = JsonConvert.SerializeObject(State);
			File.WriteAllText("state.json", stateJson);
		}

		/// <summary>
		/// Load the Cached State from a file, or create a new one.
		/// </summary>
		public void LoadState()
		{
			if(File.Exists("state.json"))
			{
				string stateJson = File.ReadAllText("state.json");
				State = JsonConvert.DeserializeObject<CachedState>(stateJson);
			}
			else
			{
				State = new CachedState();
			}
		}

		/// <summary>
		/// Clear the Cached State and delete the corresponding file if it exists.
		/// </summary>
		public void ClearState()
		{
			State = new CachedState();
			if(File.Exists("state.json"))
			{
				File.Delete("state.json");
			}
		}
	}
}
