using DailyArena.Common.Core.Database;
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
			program.Logger.Debug("InitializeProgram() Called - {0}", program.ApplicationName);

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
		/// <param name="app">The program.</param>
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

			program.Logger.Debug("Current UI Culture: {CurrentUICulture}", Thread.CurrentThread.CurrentUICulture);
		}

		/// <summary>
		/// Send usage stats to server.
		/// </summary>
		/// <param name="app">The program.</param>
		private static void SendUsageStats(this IDeckAdvisorProgram program)
		{
			program.Logger.Debug("SendUsageStats() Called - {0}", program.ApplicationName);

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

		/// <summary>
		/// Initialize various state variables.
		/// </summary>
		/// <param name="app">The program.</param>
		public static void InitializeState(this IDeckAdvisorProgram program)
		{
			program.Logger.Debug("InitializeState() Called - {0}", program.ApplicationName);

			bool saveState = false;
			program.Format.Value = program.CurrentApp.State.LastFormat;
			if (string.IsNullOrWhiteSpace(program.Format.Value) || !program.FormatMappings.ContainsKey(program.Format.Value))
			{
				program.Format.Value = program.GetLocalizedString("Item_Standard");
				program.CurrentApp.State.LastFormat = program.Format.Value;
				saveState = true;
			}

			program.Sort.Value = program.CurrentApp.State.LastSort;
			List<string> sortStrings = new List<string>()
			{
				program.GetLocalizedString("Item_Default"),
				program.GetLocalizedString("Item_BoosterCost"),
				program.GetLocalizedString("Item_BoosterCostIgnoringWildcards"),
				program.GetLocalizedString("Item_BoosterCostIgnoringCollection"),
				program.GetLocalizedString("Item_DeckScore"),
				program.GetLocalizedString("Item_WinRate"),
				program.GetLocalizedString("Item_MythicRareCount"),
				program.GetLocalizedString("Item_RareCount"),
				program.GetLocalizedString("Item_UncommonCount"),
				program.GetLocalizedString("Item_CommonCount")
			};
			if (string.IsNullOrWhiteSpace(program.Sort.Value) || !sortStrings.Contains(program.Sort.Value))
			{
				program.Sort.Value = program.GetLocalizedString("Item_Default");
				program.CurrentApp.State.LastSort = program.Sort.Value;
				saveState = true;
			}

			program.SortDir.Value = program.CurrentApp.State.LastSortDir;
			List<string> sortDirStrings = new List<string>()
			{
				program.GetLocalizedString("Item_Default"),
				program.GetLocalizedString("Item_Ascending"),
				program.GetLocalizedString("Item_Descending")
			};
			if (string.IsNullOrWhiteSpace(program.SortDir.Value) || !sortDirStrings.Contains(program.SortDir.Value))
			{
				program.SortDir.Value = program.GetLocalizedString("Item_Default");
				program.CurrentApp.State.LastSortDir = program.SortDir.Value;
				saveState = true;
			}

			program.RotationProof.Value = program.CurrentApp.State.RotationProof;
			program.CardText.Value = program.CurrentApp.State.CardTextFilter;

			int fontSize = program.CurrentApp.State.FontSize;
			if (fontSize < 8 || fontSize > 24)
			{
				program.SelectedFontSize.Value = 12;
				program.CurrentApp.State.FontSize = program.SelectedFontSize.Value;
				saveState = true;
			}
			else
			{
				program.SelectedFontSize.Value = fontSize;
			}

			if (saveState)
			{
				program.CurrentApp.SaveState();
			}
		}

		/// <summary>
		/// Initialize the card database, and populate objects that are loaded from the back-end server.
		/// </summary>
		/// <param name="app">The program.</param>
		/*public static Task InitializeDatabaseAndPopulate(this IDeckAdvisorProgram program)
		{
			program.Logger.Debug("InitializeDatabaseAndPopulate() Called - {0}", program.ApplicationName);

			Task loadTask = new Task(() => {
				program.Logger.Debug("Initializing Card Database");
				CardDatabase.Initialize(false);
				LoadingValue.Value = 20;

				PopulateColorsByLand();
				LoadingValue.Value = 30;

				PopulateStandardBannings();
				LoadingValue.Value = 40;

				PopulateSetTranslations();
				LoadingValue.Value = 50;

				ReloadAndCrunchAllData();
			});
			loadTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						Logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "loadTask", "Window_Loaded", ApplicationName);

						ReportException("loadTask", "Window_Loaded", t.Exception);
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			loadTask.Start();

			return loadTask;
		}*/
	}
}
