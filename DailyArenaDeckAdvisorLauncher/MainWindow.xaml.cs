using Newtonsoft.Json;
using Serilog;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DailyArenaDeckAdvisorLauncher
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// The logger for the launcher app.
		/// </summary>
		ILogger _logger;

		/// <summary>
		/// Default constructer, saves a reference to the application logger and initializes the GUI components.
		/// </summary>
		public MainWindow()
		{
			App application = (App)Application.Current;
			_logger = application.Logger;
			_logger.Debug("Main Window Constructor Called - {0}", "Launcher");

			SetCulture();
			InitializeComponent();
		}

		/// <summary>
		/// Event handler that is called when the window closes. Shuts down the executable.
		/// </summary>
		/// <param name="sender">The Window.</param>
		/// <param name="e">The event arguments.</param>
		private void Window_Closed(object sender, EventArgs e)
		{
			_logger.Debug("Window Closed, Shutting Down - {0}", "Launcher");
			Application.Current.Shutdown();
		}

		/// <summary>
		/// Sets the current UI culture from DailyArenaDeckAdvisor.exe.config if it's set there.
		/// </summary>
		private void SetCulture()
		{
			_logger.Debug("SetCulture() Called - {0}", "Launcher");

			try
			{
				var appSettings = ConfigurationManager.OpenExeConfiguration("DailyArenaDeckAdvisor.exe");
				if (appSettings == null)
				{
					_logger.Debug("No AppSettings Found, Using Default UI Culture");
				}
				else
				{
					var cultureSetting = appSettings.AppSettings.Settings["UICulture"];
					if (cultureSetting != null)
					{
						var culture = cultureSetting.Value;
						_logger.Debug("Setting UI Culture to {culture}", culture);
						Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
					}
					else
					{
						_logger.Debug("UICulture not set in AppSettings, Using Default UI Culture");
					}
				}
			}
			catch (ConfigurationErrorsException e)
			{
				_logger.Error(e, "Exception in SetCulture(), Using Default UI Culture");
			}
		}

		/// <summary>
		/// Event handler that is called when the window is loaded. Fires off launcher activities.
		/// </summary>
		/// <param name="sender">The Window.</param>
		/// <param name="e">The routed event arguments.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_logger.Debug("Main Window Loaded - {0}", "Launcher");

			Task updateTask = new Task(() =>
			{
				_logger.Debug("Checking for Updates");
				AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
				string assemblyVersion = assemblyName.Version.ToString();
				string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();
				_logger.Debug("Assembly Version: {0}, Assembly Architecture: {1}", assemblyVersion, assemblyArchitecture);
				bool forceUpdate = false;
				using (WebClient client = new WebClient())
				{
					string ver = Guid.NewGuid().ToString();

					var updaterFileLocation = string.Format("{0}Low\\DailyArena\\DailyArenaDeckAdvisor\\DailyArenaDeckAdvisor.zip", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
					if (File.Exists(updaterFileLocation))
					{
						_logger.Debug("Updater Zip Found, Updating Updater Executable");
						bool deleteSuccess = false;
						for (int i = 0; i < 30; i++)
						{
							try
							{
								File.Delete("DailyArenaDeckAdvisorUpdater.exe");
								foreach(string path in Directory.EnumerateDirectories(Directory.GetCurrentDirectory()))
								{
									foreach(string filePath in Directory.EnumerateFiles(path))
									{
										if(filePath.EndsWith("DailyArenaDeckAdvisorUpdater.resources.dll"))
										{
											File.Delete(path);
										}
									}
								}
								deleteSuccess = true;
								break;
							}
							catch (Exception)
							{
								Thread.Sleep(200); // give updater up to 6 seconds or so to close
							}
						}
						if(deleteSuccess)
						{
							_logger.Debug("Successfully Deleted DailyArenaDeckAdvisorUpdater.exe, updating from Zip");

							using (ZipArchive archive = ZipFile.Open(updaterFileLocation, ZipArchiveMode.Read))
							{
								var entries = archive.Entries.Where(x => x.Name.StartsWith("DailyArenaDeckAdvisorUpdater"));
								foreach (var entry in entries)
								{
									entry.ExtractToFile(entry.Name, true);
								}
							}
						}
						else
						{
							_logger.Debug("Failed to Delete DailyArenaDeckAdvisorUpdater.exe");
							forceUpdate = true;
						}

						_logger.Debug("Deleting Updater Zip");
						File.Delete(updaterFileLocation);
					}

					if(forceUpdate)
					{
						_logger.Debug("Detected a Problem with the previous update, forcing a new one");
					}

					try
					{
						string s = client.DownloadString($"https://clans.dailyarena.net/download/advisor/version.json?ver={ver}");
						dynamic versionObj = JsonConvert.DeserializeObject(s);
						string version = versionObj.version;
						_logger.Debug("Latest Version: {0}", version);

						if ((version == assemblyVersion) && !forceUpdate)
						{
							_logger.Debug("Starting Main Application");
							using (Process advisorApp = new Process())
							{
								advisorApp.StartInfo.FileName = "DailyArenaDeckAdvisor.exe";
								advisorApp.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
								advisorApp.Start();
							}
						}
						else
						{
							_logger.Debug("Starting Updater");
							using (Process advisorApp = new Process())
							{
								advisorApp.StartInfo.FileName = "DailyArenaDeckAdvisorUpdater.exe";
								advisorApp.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
								advisorApp.Start();
							}
						}
					}
					catch(WebException we)
					{
						_logger.Error(we, "WebException in {0}, {1} - {2}", "updateTask", "Starting Main Application", "Launcher");
						using (Process advisorApp = new Process())
						{
							advisorApp.StartInfo.FileName = "DailyArenaDeckAdvisor.exe";
							advisorApp.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
							advisorApp.Start();
						}
					}

					_logger.Debug("Closing Launcher Window");
					Dispatcher.Invoke(() => { Close(); });
				}
			});
			updateTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						_logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "updateTask", "Window_Loaded", "Launcher");
						Dispatcher.Invoke(() => {
							var result = MessageBox.Show(t.Exception.InnerException.ToString(), "Launcher Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
							Close();
						});
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			updateTask.Start();
		}
	}
}
