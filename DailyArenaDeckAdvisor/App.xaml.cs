using DailyArena.Common;
using DailyArena.Common.Extensions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Compact;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application, IApp
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

			LogSystemInfo();
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
				if(State.Fingerprint == Guid.Empty)
				{
					State.Fingerprint = Guid.NewGuid();
					SaveState();
				}
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

		/// <summary>
		/// Logs information about the system the application is running on.
		/// </summary>
		public void LogSystemInfo()
		{
			ManagementObjectSearcher videoController = new ManagementObjectSearcher("select * from Win32_VideoController");

			foreach (ManagementObject obj in videoController.Get())
			{
				Logger.Debug("Video Controller Info:\nName:{Name}\nStatus:{Status}\nCaption:{Caption}\nDeviceID:{DeviceID}\nAdapterRAM:{AdapterRAM}\nAdapterDACType:{AdapterDACType}\nMonochrome:{Monochrome}\n" +
					"InstalledDisplayDrivers:{InstalledDisplayDrivers}\nDriverVersion:{DriverVersion}\nVideoProcessor:{VideoProcessor}\nVideoArchitecture:{VideoArchitecture}\nVideoMemoryType:{VideoMemoryType}",
					obj.TryGetProperty("Name"), obj.TryGetProperty("Status"), obj.TryGetProperty("Caption"), obj.TryGetProperty("DeviceID"), SizeSuffix((long)Convert.ToDouble(obj.TryGetProperty("AdapterRAM"))), obj.TryGetProperty("AdapterDACType"), obj.TryGetProperty("Monochrome"),
					obj.TryGetProperty("InstalledDisplayDrivers"), obj.TryGetProperty("DriverVersion"), obj.TryGetProperty("VideoProcessor"), obj.TryGetProperty("VideoArchitecture"), obj.TryGetProperty("VideoMemoryType"));
			}

			ManagementObjectSearcher processor = new ManagementObjectSearcher("select * from Win32_Processor");

			foreach (ManagementObject obj in processor.Get())
			{
				Logger.Debug("Processor Info:\nName:{Name}\nDeviceID:{DeviceID}\nManufacturer:{Manufacturer}\nCurrentClockSpeed:{CurrentClockSpeed}\nCaption:{Caption}\nNumberOfCores:{NumberOfCores}\nNumberOfEnabledCore:{NumberOfEnabledCore}\n" +
					"NumberOfLogicalProcessors:{NumberOfLogicalProcessors}\nArchitecture:{Architecture}\nFamily:{Family}\nProcessorType:{ProcessorType}\nCharacteristics:{Characteristics}\nAddressWidth:{AddressWidth}",
					obj.TryGetProperty("Name"), obj.TryGetProperty("DeviceID"), obj.TryGetProperty("Manufacturer"), obj.TryGetProperty("CurrentClockSpeed"), obj.TryGetProperty("Caption"), obj.TryGetProperty("NumberOfCores"), obj.TryGetProperty("NumberOfEnabledCore"),
					obj.TryGetProperty("NumberOfLogicalProcessors"), obj.TryGetProperty("Architecture"), obj.TryGetProperty("Family"), obj.TryGetProperty("ProcessorType"), obj.TryGetProperty("Characteristics"), obj.TryGetProperty("AddressWidth"));
			}

			ManagementObjectSearcher operatingSystem = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

			foreach (ManagementObject obj in operatingSystem.Get())
			{
				Logger.Debug("Operating System Info:\nCaption:{Caption}\nWindowsDirectory:{WindowsDirectory}\nProductType:{ProductType}\nSerialNumber:{SerialNumber}\nSystemDirectory:{SystemDirectory}\nCountryCode:{CountryCode}\nCurrentTimeZone:{CurrentTimeZone}\n" +
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
	}
}
