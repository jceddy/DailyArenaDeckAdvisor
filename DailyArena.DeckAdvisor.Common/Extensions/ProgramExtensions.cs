using DailyArena.Common.Core.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DailyArena.DeckAdvisor.Common.Extensions
{
	/// <summary>
	/// Common class extension for Deck Advisor Program classes.
	/// </summary>
	public static class ProgramExtensions
	{
		/// <summary>
		/// Initialization extension for IDeckAdvisorProgram.
		/// </summary>
		/// <param name="app">The program.</param>
		public static void InitializeProgram(this IDeckAdvisorProgram program)
		{
			program.Logger.Debug("InitializeProgram Called - {0}", program.ApplicationName);

			program.SetCulture();

			// these have to happen after SetCulture()
			program.FormatMappings = new Dictionary<string, Tuple<string, string>>()
			{
				{ program.GetLocalizedString("Item_Standard"), new Tuple<string, string>("standard", "Standard") },
				{ program.GetLocalizedString("Item_ArenaStandard"), new Tuple<string, string>("arena_standard", "ArenaStandard") },
				{ program.GetLocalizedString("Item_Brawl"), new Tuple<string, string>("brawl", "Brawl") },
				{ program.GetLocalizedString("Item_Historic_Bo3"), new Tuple<string, string>("historic_bo3", "Historic") },
				{ program.GetLocalizedString("Item_Historic_Bo1"), new Tuple<string, string>("historic_bo1", "Historic") }
			};

			program.SendUsageStats();
		}

		/// <summary>
		/// Sets the current UI culture from app.config if it's set there.
		/// </summary>
		private static void SetCulture(this IDeckAdvisorProgram program)
		{
			program.Logger.Debug("SetCulture() Called - {0}", program.ApplicationName);

			string culture = program.GetConfigurationSetting("UICulture");
			if(culture != null)
			{
				program.Logger.Debug("Setting UI Culture to {culture}", culture);
				Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
			}
			else
			{
				program.Logger.Debug("UICulture not set in configuration settings, Using Default UI Culture");
			}
		}

		/// <summary>
		/// Send usage stats to server.
		/// </summary>
		private static void SendUsageStats(this IDeckAdvisorProgram program)
		{
			new Task(() =>
			{
				AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
				string assemblyVersion = assemblyName.Version.ToString();
				string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();

				NameValueCollection inputs = new NameValueCollection
				{
					{ "application", program.ApplicationUsageName },
					{ "fingerprint", program.CurrentApp.State.Fingerprint.ToString() },
					{ "version", assemblyVersion },
					{ "architecture", assemblyArchitecture }
				};
				string response = WebUtilities.UploadValues("https://clans.dailyarena.net/usage_stats.php", inputs, "POST", true, out List<WebException> exceptions);
				program.Logger.Debug("Usage Statistics Response: {response}", response);
				if (response == null)
				{
					foreach (WebException exception in exceptions)
					{
						program.Logger.Error(exception, "Exception from UploadValues in {method}", "SendUsageStats");
					}
				}
			}).Start();
		}
	}
}
