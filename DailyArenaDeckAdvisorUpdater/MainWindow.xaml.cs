﻿using System;
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

namespace DailyArenaDeckAdvisorUpdater
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// Default constructer, initializes the GUI components.
		/// </summary>
		public MainWindow()
		{
			App application = (App)Application.Current;
			FileLogger.Log("Main Window Constructor Called - {0}", "Updater");

			SetCulture();
			InitializeComponent();
		}

		/// <summary>
		/// Sets the current UI culture from DailyArenaDeckAdvisor.exe.config if it's set there.
		/// </summary>
		private void SetCulture()
		{
			FileLogger.Log("SetCulture() Called - {0}", "Launcher");

			try
			{
				var appSettings = ConfigurationManager.OpenExeConfiguration("DailyArenaDeckAdvisor.exe");
				if (appSettings == null)
				{
					FileLogger.Log("No AppSettings Found, Using Default UI Culture");
				}
				else
				{
					var cultureSetting = appSettings.AppSettings.Settings["UICulture"];
					if (cultureSetting != null)
					{
						var culture = cultureSetting.Value;
						FileLogger.Log("Setting UI Culture to {0}", culture);
						Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
					}
					else
					{
						FileLogger.Log("UICulture not set in AppSettings, Using Default UI Culture");
					}
				}
			}
			catch (ConfigurationErrorsException e)
			{
				FileLogger.Log(e, "Exception in SetCulture(), Using Default UI Culture");
			}
		}

		/// <summary>
		/// Event handler that is called when the window closes. Shuts down the executable.
		/// </summary>
		/// <param name="sender">The Window.</param>
		/// <param name="e">The event arguments.</param>
		private void Window_Closed(object sender, EventArgs e)
		{
			FileLogger.Log("Window Closed, Shutting Down - {0}", "Updater");
			Application.Current.Shutdown();
		}

		/// <summary>
		/// Event handler that is called when the window is loaded. Fires off updater activities.
		/// </summary>
		/// <param name="sender">The Window.</param>
		/// <param name="e">The routed event arguments.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			FileLogger.Log("Main Window Loaded - {0}", "Updater");

			Task updateTask = new Task(() =>
			{
				var updaterPath = string.Format("{0}Low\\DailyArena\\DailyArenaDeckAdvisor", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
				var zipFile = $"{updaterPath}\\DailyArenaDeckAdvisor.zip";
				AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
				string assemblyVersion = assemblyName.Version.ToString();
				string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();
				FileLogger.Log("Assembly Version: {0}, Assembly Architecture: {1}", assemblyVersion, assemblyArchitecture);
				using (WebClient client = new WebClient())
				{
					FileLogger.Log("Downloading Updater Zip File");
					string ver = Guid.NewGuid().ToString();
					try
					{
						if (assemblyArchitecture == "X86")
						{
							client.DownloadFile($"https://clans.dailyarena.net/download/advisor/x86/DailyArenaDeckAdvisor.zip?ver={ver}", zipFile);
						}
						else
						{
							client.DownloadFile($"https://clans.dailyarena.net/download/advisor/x64/DailyArenaDeckAdvisor.zip?ver={ver}", zipFile);
						}

						FileLogger.Log("Extracting Updater Zip Entries");
						using (ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Read))
						{
							var entries = archive.Entries.Where(x => !x.Name.StartsWith("DailyArenaDeckAdvisorUpdater"));
							foreach (var entry in entries)
							{
								if (entry.FullName.EndsWith("/"))
								{
									Directory.CreateDirectory(entry.FullName);
								}
								else
								{
									entry.ExtractToFile(entry.FullName, true);
								}
							}
						}
					}
					catch (WebException we)
					{
						FileLogger.Log(we, "WebException in {0}, {1} - {2}", "updateTask", "Re-Running Launcher", "Updater");
					}

					FileLogger.Log("Starting Launcher");
					using (Process advisorApp = new Process())
					{
						advisorApp.StartInfo.FileName = "DailyArenaDeckAdvisorLauncher.exe";
						advisorApp.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
						advisorApp.Start();
					}

					FileLogger.Log("Closing Updater Window");
					Dispatcher.Invoke(() => { Close(); });
				}
			});
			updateTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						FileLogger.Log(t.Exception, "Exception in {0} ({1} - {2})", "updateTask", "Window_Loaded", "Updater");
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
