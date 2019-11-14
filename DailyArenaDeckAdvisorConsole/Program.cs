using DailyArena.DeckAdvisor.Common;
using DailyArena.DeckAdvisor.Common.Extensions;
using DailyArena.DeckAdvisor.Console.Resources;
using Microsoft.Extensions.Configuration;
using Serilog;
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
		/// The current executing program.
		/// </summary>
		public static Program CurrentProgram { get; private set; }

		/// <summary>
		/// Field to store the configuration from appsettings.json
		/// </summary>
		private IConfigurationRoot _configuration;

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
				Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
				var appRoot = appPathMatcher.Match(exePath).Value;

				var builder = new ConfigurationBuilder()
					.SetBasePath(appRoot)
					.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
				_configuration = builder.Build();
			}

			return _configuration[key];
		}

		/// <summary>
		/// The execution entry point.
		/// </summary>
		/// <param name="args">Arguments passed on the command line.</param>
		static void Main(string[] args)
		{
			CurrentProgram = new Program();
			S.Console.OutputEncoding = Encoding.UTF8;
			S.Console.WriteLine(Localization.Message_PressToExit);
			S.Console.ReadKey();
		}
	}
}
