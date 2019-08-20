using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace DailyArenaDeckAdvisorUpdater
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		ILogger _logger;

		public MainWindow()
		{
			App application = (App)Application.Current;
			_logger = application.Logger;
			_logger.Debug("Main Window Constructor Called - {0}", "Updater");

			InitializeComponent();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			_logger.Debug("Window Closed, Shutting Down - {0}", "Updater");
			Application.Current.Shutdown();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_logger.Debug("Main Window Loaded - {0}", "Updater");

			Task updateTask = new Task(() =>
			{
				var updaterPath = string.Format("{0}Low\\DailyArena\\DailyArenaDeckAdvisor", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
				var zipFile = $"{updaterPath}\\DailyArenaDeckAdvisor.zip";
				AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
				string assemblyVersion = assemblyName.Version.ToString();
				string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();
				_logger.Debug("Assembly Version: {0}, Assembly Architecture: {1}", assemblyVersion, assemblyArchitecture);
				using (WebClient client = new WebClient())
				{
					_logger.Debug("Downloading Updater Zip File");
					string ver = Guid.NewGuid().ToString();
					if (assemblyArchitecture == "X86")
					{
						client.DownloadFile($"https://clans.dailyarena.net/download/advisor/x86/DailyArenaDeckAdvisor.zip?ver={ver}", zipFile);
					}
					else
					{
						client.DownloadFile($"https://clans.dailyarena.net/download/advisor/x64/DailyArenaDeckAdvisor.zip?ver={ver}", zipFile);
					}

					_logger.Debug("Extracting Updater Zip Entries");
					using (ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Read))
					{
						var entries = archive.Entries.Where(x => x.Name != "DailyArenaDeckAdvisorUpdater.exe");
						foreach (var entry in entries)
						{
							entry.ExtractToFile(entry.Name, true);
						}
					}

					_logger.Debug("Starting Launcher");
					using (Process advisorApp = new Process())
					{
						advisorApp.StartInfo.FileName = "DailyArenaDeckAdvisorLauncher.exe";
						advisorApp.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
						advisorApp.Start();
					}

					_logger.Debug("Closing Updater Window");
					Dispatcher.Invoke(() => { Close(); });
				}
			});
			updateTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						_logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "updateTask", "Window_Loaded", "Updater");
						Dispatcher.Invoke(() => {
							var result = MessageBox.Show(t.Exception.InnerException.ToString(), "Updater Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
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
