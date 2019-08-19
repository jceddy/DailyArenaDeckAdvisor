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
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Task updateTask = new Task(() =>
			{
				var updaterPath = string.Format("{0}Low\\DailyArena\\DailyArenaDeckAdvisor", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
				if(!Directory.Exists(updaterPath))
				{
					Directory.CreateDirectory(updaterPath);
				}
				var zipFile = $"{updaterPath}\\DailyArenaDeckAdvisor.zip";
				AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
				string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();
				using (WebClient client = new WebClient())
				{
					string ver = Guid.NewGuid().ToString();
					if (assemblyArchitecture == "X86")
					{
						client.DownloadFile($"https://clans.dailyarena.net/download/advisor/x86/DailyArenaDeckAdvisor.zip?ver={ver}", zipFile);
					}
					else
					{
						client.DownloadFile($"https://clans.dailyarena.net/download/advisor/x64/DailyArenaDeckAdvisor.zip?ver={ver}", zipFile);
					}

					using (ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Read))
					{
						var entries = archive.Entries.Where(x => x.Name != "DailyArenaDeckAdvisorUpdater.exe");
						foreach (var entry in entries)
						{
							entry.ExtractToFile(entry.Name, true);
						}
					}

					using (Process advisorApp = new Process())
					{
						advisorApp.StartInfo.FileName = "DailyArenaDeckAdvisorLauncher.exe";
						advisorApp.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
						advisorApp.Start();
					}

					Dispatcher.Invoke(() => { Close(); });
				}
			});
			updateTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
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
