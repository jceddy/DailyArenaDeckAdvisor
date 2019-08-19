using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DailyArenaDeckAdvisorLauncher
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
				AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
				string assemblyVersion = assemblyName.Version.ToString();
				string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();
				using (WebClient client = new WebClient())
				{
					string ver = Guid.NewGuid().ToString();

					var updaterFileLocation = string.Format("{0}Low\\DailyArena\\DailyArenaDeckAdvisor\\DailyArenaDeckAdvisor.zip", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
					if (File.Exists(updaterFileLocation))
					{
						for (int i = 0; i < 20; i++)
						{
							try
							{
								File.Delete("DailyArenaDeckAdvisorUpdater.exe");
								break;
							}
							catch (Exception)
							{
								Thread.Sleep(100); // give updater up to 2 seconds or so to close
							}
						}

						using (ZipArchive archive = ZipFile.Open(updaterFileLocation, ZipArchiveMode.Read))
						{
							var entries = archive.Entries.Where(x => x.Name == "DailyArenaDeckAdvisorUpdater.exe");
							foreach (var entry in entries)
							{
								entry.ExtractToFile(entry.Name, true);
							}
						}

						File.Delete(updaterFileLocation);
					}

					string s = client.DownloadString($"https://clans.dailyarena.net/download/advisor/version.json?ver={ver}");
					dynamic versionObj = JsonConvert.DeserializeObject(s);
					string version = versionObj.version;

					if (version == assemblyVersion)
					{
						using (Process advisorApp = new Process())
						{
							advisorApp.StartInfo.FileName = "DailyArenaDeckAdvisor.exe";
							advisorApp.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
							advisorApp.Start();
						}
					}
					else
					{
						using (Process advisorApp = new Process())
						{
							advisorApp.StartInfo.FileName = "DailyArenaDeckAdvisorUpdater.exe";
							advisorApp.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
							advisorApp.Start();
						}
					}

					Dispatcher.Invoke(() => { Close(); });
				}
			});
			updateTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
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
