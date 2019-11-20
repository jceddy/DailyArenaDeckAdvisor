using DailyArena.DeckAdvisor.Common;
using DailyArena.DeckAdvisor.Common.Extensions;
using DailyArena.DeckAdvisor.Console.Resources;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using S = System;

namespace DailyArena.DeckAdvisor.Console
{
	/// <summary>
	/// The main program for the platform-independent console version of the application.
	/// </summary>
	public class Program : IDeckAdvisorApp, IDeckAdvisorProgram
	{
		/// <summary>
		/// The application's Cached State.
		/// </summary>
		public CachedState State { get; set; } = new CachedState();

		/// <summary>
		/// The application's Logger.
		/// </summary>
		public ILogger Logger { get; set; }

		/// <summary>
		/// Gets the application name string to use while logging.
		/// </summary>
		public string ApplicationName { get { return "Console Application"; } }

		/// <summary>
		/// Gets the application name to use while sending Usage Statistics.
		/// </summary>
		public string ApplicationUsageName { get { return "DailyArenaDeckAdvisorConsole"; } }

		/// <summary>
		/// Logger for first chance exceptions.
		/// </summary>
		public ILogger FirstChanceLogger { get; set; }

		/// <summary>
		/// The Program constructor.
		/// </summary>
		public Program()
		{
			this.InitializeApp("-console", false);
			this.InitializeProgram();
		}

		/// <summary>
		/// Gets or sets the current executing program.
		/// </summary>
		public static Program CurrentProgram { get; private set; }

		/// <summary>
		/// Field to store the configuration from appsettings.json
		/// </summary>
		private IConfigurationRoot _configuration;

		/// <summary>
		/// Gets or sets a dictionary mapping the Format names shown on the GUI drop-down to the name to use when querying archetype data from the server.
		/// </summary>
		public Dictionary<string, Tuple<string, string>> FormatMappings { get; set; }

		/// <summary>
		/// Method to query a configuration setting.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns>The setting value.</returns>
		public string GetConfigurationSetting(string key)
		{
			Logger.Debug("GetConfigurationSetting() Called - {key} ({ApplicationName})", key, ApplicationName);

			if (_configuration == null)
			{
				var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
				Logger.Debug("CodeBase: {exePath}", exePath);

				var appRoot = exePath;
				if(appRoot.Contains("\\"))
				{
					Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
					appRoot = appPathMatcher.Match(exePath).Value;
				}

				Logger.Debug("BasePath: {appRoot}", appRoot);

				var builder = new ConfigurationBuilder()
					.AddJsonFile($"{appRoot}{Path.DirectorySeparatorChar}appsettings.json", optional: true, reloadOnChange: true);
				_configuration = builder.Build();
			}

			return _configuration[key];
		}

		/// <summary>
		/// Method to get a localized string based on the resource name.
		/// </summary>
		/// <param name="name">The resource name to get a localized string for.</param>
		/// <returns>The localized string for the requested resource name.</returns>
		public string GetLocalizedString(string name)
		{
			return Localization.ResourceManager.GetString(name);
		}

		/// <summary>
		/// Gets the currently running app.
		/// </summary>
		public IDeckAdvisorApp CurrentApp
		{
			get
			{
				return CurrentProgram;
			}
		}

		/// <summary>
		/// Method to run the program.
		/// </summary>
		public void Run()
		{
			S.Console.OutputEncoding = Encoding.UTF8;
			S.Console.WriteLine(CurrentProgram.GetLocalizedString("Loading_LoadingCardDatabase"));
			S.Console.WriteLine(Localization.Message_PressToExit);
			S.Console.ReadKey();
		}

		/// <summary>
		/// The execution entry point.
		/// </summary>
		/// <param name="args">Arguments passed on the command line.</param>
		static void Main(string[] args)
		{
			CurrentProgram = new Program();
			CurrentProgram.Run();
		}
	}
}
