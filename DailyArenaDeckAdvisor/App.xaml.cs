using Newtonsoft.Json;
using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Compact;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DailyArenaDeckAdvisor
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public CachedState State { get; private set; } = new CachedState();
		public ILogger Logger { get; private set; }

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

			LoadState();
		}

		public void SaveState()
		{
			string stateJson = JsonConvert.SerializeObject(State);
			File.WriteAllText("state.json", stateJson);
		}

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
