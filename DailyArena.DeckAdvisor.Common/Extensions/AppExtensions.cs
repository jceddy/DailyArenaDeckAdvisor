using DailyArena.Common.Core.Extensions;
using DailyArena.Common.Core.Utility;
using Newtonsoft.Json;
using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Compact;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace DailyArena.DeckAdvisor.Common.Extensions
{
	/// <summary>
	/// Common class extension for Deck Advisor App classes.
	/// </summary>
	public static class AppExtensions
	{
		/// <summary>
		/// Save the Cached State out to a file.
		/// </summary>
		/// <param name="app">The application object.</param>
		public static void SaveState(this IDeckAdvisorApp app)
		{
			string stateJson = JsonConvert.SerializeObject(app.State);
			File.WriteAllText("state.json", stateJson);
		}

		/// <summary>
		/// Load the Cached State from a file, or create a new one.
		/// </summary>
		/// <param name="app">The application object.</param>
		public static void LoadState(this IDeckAdvisorApp app)
		{
			if (File.Exists("state.json"))
			{
				string stateJson = File.ReadAllText("state.json");
				app.State = JsonConvert.DeserializeObject<CachedState>(stateJson);

				bool saveState = false;
				if (app.State.Fingerprint == Guid.Empty)
				{
					app.State.Fingerprint = Guid.NewGuid();
					saveState = true;
				}
				if (app.State.Filters == null)
				{
					app.State.Filters = new DeckFilters();
					saveState = true;
				}
				if (saveState)
				{
					app.SaveState();
				}
			}
			else
			{
				app.State = new CachedState();
			}
		}

		/// <summary>
		/// Clear the Cached State and delete the corresponding file if it exists.
		/// </summary>
		/// <param name="app">The application object.</param>
		public static void ClearState(this IDeckAdvisorApp app)
		{
			app.State = new CachedState();
			if (File.Exists("state.json"))
			{
				File.Delete("state.json");
			}
		}

		/// <summary>
		/// Logs information about the system the application is running on.
		/// </summary>
		/// <param name="app">The application object.</param>
		public static void LogSystemInfo(this IDeckAdvisorApp app)
		{
			ManagementObjectSearcher videoController = new ManagementObjectSearcher("select * from Win32_VideoController");

			foreach (ManagementObject obj in videoController.Get())
			{
				app.Logger.Debug("Video Controller Info:\nName:{Name}\nStatus:{Status}\nCaption:{Caption}\nDeviceID:{DeviceID}\nAdapterRAM:{AdapterRAM}\nAdapterDACType:{AdapterDACType}\nMonochrome:{Monochrome}\n" +
					"InstalledDisplayDrivers:{InstalledDisplayDrivers}\nDriverVersion:{DriverVersion}\nVideoProcessor:{VideoProcessor}\nVideoArchitecture:{VideoArchitecture}\nVideoMemoryType:{VideoMemoryType}",
					obj.TryGetProperty("Name"), obj.TryGetProperty("Status"), obj.TryGetProperty("Caption"), obj.TryGetProperty("DeviceID"), SizeSuffix((long)Convert.ToDouble(obj.TryGetProperty("AdapterRAM"))), obj.TryGetProperty("AdapterDACType"), obj.TryGetProperty("Monochrome"),
					obj.TryGetProperty("InstalledDisplayDrivers"), obj.TryGetProperty("DriverVersion"), obj.TryGetProperty("VideoProcessor"), obj.TryGetProperty("VideoArchitecture"), obj.TryGetProperty("VideoMemoryType"));
			}

			ManagementObjectSearcher processor = new ManagementObjectSearcher("select * from Win32_Processor");

			foreach (ManagementObject obj in processor.Get())
			{
				app.Logger.Debug("Processor Info:\nName:{Name}\nDeviceID:{DeviceID}\nManufacturer:{Manufacturer}\nCurrentClockSpeed:{CurrentClockSpeed}\nCaption:{Caption}\nNumberOfCores:{NumberOfCores}\nNumberOfEnabledCore:{NumberOfEnabledCore}\n" +
					"NumberOfLogicalProcessors:{NumberOfLogicalProcessors}\nArchitecture:{Architecture}\nFamily:{Family}\nProcessorType:{ProcessorType}\nCharacteristics:{Characteristics}\nAddressWidth:{AddressWidth}",
					obj.TryGetProperty("Name"), obj.TryGetProperty("DeviceID"), obj.TryGetProperty("Manufacturer"), obj.TryGetProperty("CurrentClockSpeed"), obj.TryGetProperty("Caption"), obj.TryGetProperty("NumberOfCores"), obj.TryGetProperty("NumberOfEnabledCore"),
					obj.TryGetProperty("NumberOfLogicalProcessors"), obj.TryGetProperty("Architecture"), obj.TryGetProperty("Family"), obj.TryGetProperty("ProcessorType"), obj.TryGetProperty("Characteristics"), obj.TryGetProperty("AddressWidth"));
			}

			ManagementObjectSearcher operatingSystem = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

			foreach (ManagementObject obj in operatingSystem.Get())
			{
				app.Logger.Debug("Operating System Info:\nCaption:{Caption}\nWindowsDirectory:{WindowsDirectory}\nProductType:{ProductType}\nSerialNumber:{SerialNumber}\nSystemDirectory:{SystemDirectory}\nCountryCode:{CountryCode}\nCurrentTimeZone:{CurrentTimeZone}\n" +
					"EncryptionLevel:{EncryptionLevel}\nOSType:{OSType}\nVersion:{Version}",
					obj.TryGetProperty("Caption"), obj.TryGetProperty("WindowsDirectory"), obj.TryGetProperty("ProductType"), obj.TryGetProperty("SerialNumber"), obj.TryGetProperty("SystemDirectory"), obj.TryGetProperty("CountryCode"), obj.TryGetProperty("CurrentTimeZone"),
					obj.TryGetProperty("EncryptionLevel"), obj.TryGetProperty("OSType"), obj.TryGetProperty("Version"));
			}
		}

		/// <summary>
		/// Size suffixes for human-readable RAM strings.
		/// </summary>
		static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

		/// <summary>
		/// Convert RAM value to human-readable format.
		/// </summary>
		/// <param name="value">The bare value.</param>
		/// <returns>The passed value as a human-readable stirng.</returns>
		static string SizeSuffix(long value)
		{
			if (value < 0) { return "-" + SizeSuffix(-value); }
			if (value == 0) { return "0.0 bytes"; }

			int mag = (int)Math.Log(value, 1024);
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
		}

		/// <summary>
		/// Initialization extension for IDeckAdvisorApp.
		/// </summary>
		/// <param name="app">The app.</param>
		/// <param name="postfix">The postfix for log files.</param>
		/// <param name="logSystemInfo">Whether to log system info (don't do this in the .NET Core application)</param>
		public static void InitializeApp(this IDeckAdvisorApp app, string postfix = "", bool logSystemInfo = true)
		{
			ApplicationUtilities.CurrentApp = app;

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
			app.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.
				File(new CompactJsonFormatter(), $"{dataFolder}\\logs\\log{postfix}.txt",
					rollingInterval: RollingInterval.Hour,
					retainedFileCountLimit: 5,
					fileSizeLimitBytes: 10485760,
					rollOnFileSizeLimit: true,
					shared: true).
				CreateLogger();
			app.FirstChanceLogger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.
				File(new CompactJsonFormatter(), $"{dataFolder}\\logs\\firstChanceExceptions{postfix}.txt",
					rollingInterval: RollingInterval.Hour,
					retainedFileCountLimit: 2,
					fileSizeLimitBytes: 10485760,
					rollOnFileSizeLimit: true,
					shared: true).
				CreateLogger();

			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				app.Logger.Error((Exception)e.ExceptionObject, "UnhandledException");
			};

			AppDomain.CurrentDomain.FirstChanceException += (source, e) =>
			{
				app.FirstChanceLogger.Debug(e.Exception, "FirstChanceException");
			};

			if (logSystemInfo)
			{
				// only log system info when running on Windows
				app.LogSystemInfo();
			}
			app.LoadState();
		}
	}
}
