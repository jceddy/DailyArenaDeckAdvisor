using DailyArena.Common.Core.Bindable;
using DailyArena.Common.Core.Database;
using DailyArena.Common.Core.Utility;
using DailyArena.Common.Database;
using DailyArena.Common.Utility;
using DailyArena.DeckAdvisor.Common;
using DailyArena.DeckAdvisor.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, IDeckAdvisorProgram
	{
		/// <summary>
		/// The list of Archetypes pulled down from the server.
		/// </summary>
		List<Archetype> _archetypes = new List<Archetype>();

		/// <summary>
		/// The current meta report.
		/// </summary>
		MetaReport _report;

		/// <summary>
		/// The player's card inventory; key = Arena Id of a card, value = Quantity of that card owned.
		/// </summary>
		Dictionary<int, int> _playerInventory = new Dictionary<int, int>();

		/// <summary>
		/// The player inventory keyed by Card Name instead of by Arena Id. The Stored Quantity will "max out" at the maximum card copy count for the selected format.
		/// </summary>
		Dictionary<string, int> _playerInventoryCounts = new Dictionary<string, int>();

		/// <summary>
		/// Dictionary that stores "colors" for nonbasic lands in Standard. Used for generating replacement suggestions for nonbasic lands.
		/// </summary>
		Dictionary<string, CardColors> _colorsByLand = new Dictionary<string, CardColors>();

		/// <summary>
		/// List containing names of cards that were banned in Standard.
		/// </summary>
		List<string> _standardBannings = new List<string>();

		/// <summary>
		/// Dictionary that stores set name translations for other languages.
		/// </summary>
		Dictionary<string, Dictionary<string, string>> _setNameTranslations = new Dictionary<string, Dictionary<string, string>>();

		/// <summary>
		/// Dictionary containing counts of wildcards the player owns, keyed by rarity.
		/// </summary>
		Dictionary<CardRarity, int> _wildcardsOwned = new Dictionary<CardRarity, int>();

		/// <summary>
		/// List of decks in the player inventory.
		/// </summary>
		Dictionary<Guid, Archetype> _playerDecks = new Dictionary<Guid, Archetype>();

		/// <summary>
		/// List of objects that will be shown in the Tab control on the GUI.
		/// </summary>
		ObservableCollection<object> _tabObjects = new ObservableCollection<object>();

		/// <summary>
		/// Dictionary mapping each card in the card database to its metagame statistics.
		/// </summary>
		Dictionary<Card, CardStats> _cardStats = new Dictionary<Card, CardStats>();

		/// <summary>
		/// List of deck archetypes after filters are applied.
		/// </summary>
		List<Archetype> _filteredArchetypes;

		/// <summary>
		/// List of deck archetypes, ordered by the estimated "booster cost" of each deck after wildcards, and then by the average "booster cost" disregarding wildcards.
		/// </summary>
		IOrderedEnumerable<Archetype> _orderedArchetypes;

		/// <summary>
		/// A list of names of cards in Arena that allow a player to include any number of copies.
		/// </summary>
		List<string> _anyNumber = new List<string>() { "Persistent Petitioners", "Rat Colony", "Seven Dwarves" };

		/// <summary>
		/// A dictionary mapping basic land names to a Card of that type the player owns.
		/// </summary>
		Dictionary<string, Card> _basicLands = new Dictionary<string, Card>();

		/// <summary>
		/// A dictionary mapping basic land names to color strings.
		/// </summary>
		Dictionary<string, string> _basicLandColors = new Dictionary<string, string>()
		{
			{ "Plains", "W" },
			{ "Island", "U" },
			{ "Swamp", "B" },
			{ "Mountain", "R" },
			{ "Forest", "G" },
			{ "Snow-Covered Plains", "W" },
			{ "Snow-Covered Island", "U" },
			{ "Snow-Covered Swamp", "B" },
			{ "Snow-Covered Mountain", "R" },
			{ "Snow-Covered Forest", "G" }
		};

		/// <summary>
		/// Gets or sets reference to the application logger.
		/// </summary>
		public ILogger Logger { get; set; }

		/// <summary>
		/// Gets the currently running app.
		/// </summary>
		public IDeckAdvisorApp CurrentApp
		{
			get
			{
				return (IDeckAdvisorApp)Application.Current;
			}
		}

		/// <summary>
		/// Gets the application name string to use while logging.
		/// </summary>
		public string ApplicationName { get { return "Main Application"; } }

		/// <summary>
		/// Gets the application name to use while sending Usage Statistics.
		/// </summary>
		public string ApplicationUsageName { get { return "DailyArenaDeckAdvisor"; } }

		/// <summary>
		/// Gets or sets the user's vault progress.
		/// </summary>
		public Bindable<double> VaultProgress { get; private set; } = new Bindable<double>();

		/// <summary>
		/// Gets or sets the selected format being viewed.
		/// </summary>
		public Bindable<string> Format { get; private set; } = new Bindable<string>();

		/// <summary>
		/// Gets or sets the selected deck sort field.
		/// </summary>
		public Bindable<string> Sort { get; private set; } = new Bindable<string>();

		/// <summary>
		/// Gets or sets the selected deck sort direction.
		/// </summary>
		public Bindable<string> SortDir { get; private set; } = new Bindable<string>();

		/// <summary>
		/// Gets or sets the card text filter value.
		/// </summary>
		public Bindable<string> CardText { get; private set; } = new Bindable<string>() { Value = string.Empty };

		/// <summary>
		/// Gets or sets the state of the "Rotation" toggle button.
		/// </summary>
		public BindableBool RotationProof { get; private set; } = new BindableBool();

		/// <summary>
		/// Gets or sets the selected font size for the display.
		/// </summary>
		public Bindable<int> SelectedFontSize { get; private set; } = new Bindable<int>() { Value = 12 };

		/// <summary>
		/// Gets or sets the message to show on the loading screen.
		/// </summary>
		public Bindable<string> LoadingText { get; private set; }

		/// <summary>
		/// Gets or sets the value for the loading progress bar.
		/// </summary>
		public Bindable<int> LoadingValue { get; private set; } = new Bindable<int>();

		/// <summary>
		/// Gets or sets the message to show on the exception screen.
		/// </summary>
		public Bindable<string> ExceptionText { get; private set; } = new Bindable<string>();

		/// <summary>
		/// Gets or sets the Url for an automatically-created Github issue.
		/// </summary>
		public Bindable<string> IssueUrl { get; private set; } = new Bindable<string>();

		/// <summary>
		/// Gets or sets the Bitmap Scaling Mode (can be overridden in App.config for users with funky video card/driver combinations that cause crashing under high-quality scaling.
		/// </summary>
		public BitmapScalingMode BitmapScalingMode { get; private set; }

		/// <summary>
		/// Minimum Arena client version supported by the application.
		/// </summary>
		private ClientVersion _minimumClient = new ClientVersion(1, 1200);

		/// <summary>
		/// Default constructor. Sets the logger and scaling mode, and does GUI initialization stuff.
		/// </summary>
		public MainWindow()
		{
			App application = (App)Application.Current;
			Logger = application.Logger;
			Logger.Debug("Main Window Constructor Called - {0}", ApplicationName);

			this.InitializeProgram();
			WpfInitializer.InitializeDelegates();

			LoadingText = new Bindable<string>() { Value = Properties.Resources.Loading_LoadingCardDatabase };

			BitmapScalingMode = GetBitmapScalingMode();

			InitializeComponent();
			DataContext = this;
		}

		/// <summary>
		/// Get thw lowest of two rarities.
		/// </summary>
		/// <param name="r1">The first rarity to compare.</param>
		/// <param name="r2">The second rarity to compare.</param>
		/// <returns>Whichever of the compared rarities has the lowest "value".</returns>
		private CardRarity LowestRarity(CardRarity r1, CardRarity r2)
		{
			return r1.CompareTo(r2) > 0 ? r2 : r1;
		}

		/// <summary>
		/// Loads a list of names of cards that are banned in Standard.
		/// </summary>
		/// <returns>The updated cache timestamp in a sortable string format.</returns>
		private string LoadStandardBannings()
		{
			Logger.Debug("LoadStandardBannings() Called");
			string cacheTimestamp = "1970-01-01T00:00:00Z";

			if (File.Exists("standard_bannings.json"))
			{
				string json = File.ReadAllText("standard_bannings.json");
				dynamic data = JsonConvert.DeserializeObject(json);

				try
				{
					_standardBannings = data.Bannings.ToObject<List<string>>();

					// set cache timestamp after loading colors object to force a re-download if an exception happens
					cacheTimestamp = data.LastUpdate;
				}
				catch (Exception e)
				{
					Logger.Error(e, "Exception in LoadStandardBannings() - need to re-download");
				}
			}

			Logger.Debug("LoadStandardBannings() Finished - cacheTimestamp={0)", cacheTimestamp);
			return cacheTimestamp;
		}

		/// <summary>
		/// Save data on ards banned in Standard to the local cache file.
		/// </summary>
		/// <param name="lastUpdate">The latest timestamp for the standard lands data that was pulled from the server.</param>
		public void SaveStandardBannings(string lastUpdate)
		{
			Logger.Debug("SaveStandardBannings() Called - lastUpdate={0}", lastUpdate);
			var data = new
			{
				LastUpdate = lastUpdate,
				Bannings = _standardBannings
			};

			string json = JsonConvert.SerializeObject(data);
			File.WriteAllText("standard_bannings.json", json);
			Logger.Debug("SaveStandardBannings() Finished");
		}

		/// <summary>
		/// Populate the _standardBannings dictionary.
		/// </summary>
		private void PopulateStandardBannings()
		{
			Logger.Debug("PopulateStandardBannings() Called");
			string serverUpdateTime = CardDatabase.GetServerTimestamp("StandardBannings");
			string cacheTimestamp = LoadStandardBannings();

			if (string.Compare(serverUpdateTime, cacheTimestamp) > 0)
			{
				var banningsUrl = $"https://clans.dailyarena.net/standard_bannings.json?_c={Guid.NewGuid()}";
				var result = WebUtilities.FetchStringFromUrl(banningsUrl, _standardBannings.Count != 0, out List<WebException> exceptions);

				if (result == null)
				{
					foreach (WebException exception in exceptions)
					{
						Logger.Error(exception, "Exception from FetchStringFromUrl in {method}", "PopulateStandardBannings");
					}
				}
				else
				{
					_standardBannings = JsonConvert.DeserializeObject<List<string>>(result.ToString());

					SaveStandardBannings(serverUpdateTime);
				}
			}
			Logger.Debug("PopulateStandardBannings() Finished");
		}

		/// <summary>
		/// Loads a list of nonbasic lands in Standard, using the local cached if it's up to date, otherwise refreshing the local cache from the server.
		/// </summary>
		/// <returns>The updated cache timestamp in a sortable string format.</returns>
		private string LoadStandardLands()
		{
			Logger.Debug("LoadStandardLands() Called");
			string cacheTimestamp = "1970-01-01T00:00:00Z";

			if (File.Exists("lands.json"))
			{
				string json = File.ReadAllText("lands.json");
				dynamic data = JsonConvert.DeserializeObject(json);

				try
				{
					_colorsByLand = data.ColorsByLand.ToObject<Dictionary<string, CardColors>>();

					// set cache timestamp after loading colors object to force a re-download if an exception happens
					cacheTimestamp = data.LastUpdate;
				}
				catch(Exception e)
				{
					Logger.Error(e, "Exception in LoadStandardLands() - need to re-download");
				}
			}

			Logger.Debug("LoadStandardLands() Finished - cacheTimestamp={0)", cacheTimestamp);
			return cacheTimestamp;
		}

		/// <summary>
		/// Save data on nonbasic lands in Standard to the local cache file.
		/// </summary>
		/// <param name="lastUpdate">The latest timestamp for the standard lands data that was pulled from the server.</param>
		public void SaveStandardLands(string lastUpdate)
		{
			Logger.Debug("SaveStandardLands() Called - lastUpdate={0}", lastUpdate);
			var data = new
			{
				LastUpdate = lastUpdate,
				ColorsByLand = _colorsByLand
			};

			string json = JsonConvert.SerializeObject(data);
			File.WriteAllText("lands.json", json);
			Logger.Debug("SaveStandardLands() Finished");
		}

		/// <summary>
		/// Populate the _colorsByLand dictionary.
		/// </summary>
		private void PopulateColorsByLand()
		{
			Logger.Debug("PopulateColorsByLand() Called");
			string serverUpdateTime = CardDatabase.GetServerTimestamp("StandardLands");
			string cacheTimestamp = LoadStandardLands();

			if (string.Compare(serverUpdateTime, cacheTimestamp) > 0)
			{
				var landsUrl = $"https://clans.dailyarena.net/standard_lands.json?_c={Guid.NewGuid()}";
				var result = WebUtilities.FetchStringFromUrl(landsUrl, _colorsByLand.Count != 0, out List<WebException> exceptions);

				if (result == null)
				{
					foreach (WebException exception in exceptions)
					{
						Logger.Error(exception, "Exception from FetchStringFromUrl in {method}", "PopulateColorsByLand");
					}
				}
				else
				{
					_colorsByLand.Clear();
					dynamic json = JToken.Parse(result.ToString());
					foreach (dynamic lands in json)
					{
						string landName = (string)lands["Name"];
						if (!_colorsByLand.ContainsKey(landName))
						{
							_colorsByLand.Add(landName, CardColors.CardColorFromString((string)lands["Colors"]));
						}
					}

					SaveStandardLands(serverUpdateTime);
				}
			}
			Logger.Debug("PopulateColorsByLand() Finished");
		}

		/// <summary>
		/// Loads a dictionary of set name translations, using the local cached if it's up to date, otherwise refreshing the local cache from the server.
		/// </summary>
		/// <returns>The updated cache timestamp in a sortable string format.</returns>
		private string LoadSetTranslations()
		{
			Logger.Debug("LoadSetTranslations() Called");
			string cacheTimestamp = "1970-01-01T00:00:00Z";

			if (File.Exists("set_name_translations.json"))
			{
				string json = File.ReadAllText("set_name_translations.json");
				dynamic data = JsonConvert.DeserializeObject(json);

				try
				{
					_setNameTranslations = data.Translations.ToObject<Dictionary<string, Dictionary<string, string>>>();

					// set cache timestamp after loading colors object to force a re-download if an exception happens
					cacheTimestamp = data.LastUpdate;
				}
				catch (Exception e)
				{
					Logger.Error(e, "Exception in LoadSetTranslations() - need to re-download");
				}
			}

			Logger.Debug("LoadSetTranslations() Finished - cacheTimestamp={0)", cacheTimestamp);
			return cacheTimestamp;
		}

		/// <summary>
		/// Save the set name translation dictionary to the local cache file.
		/// </summary>
		/// <param name="lastUpdate">The latest timestamp for the set name translation file that was pulled from the server.</param>
		public void SaveSetTranslations(string lastUpdate)
		{
			Logger.Debug("SaveSetTranslations() Called - lastUpdate={0}", lastUpdate);
			var data = new
			{
				LastUpdate = lastUpdate,
				Translations = _setNameTranslations
			};

			string json = JsonConvert.SerializeObject(data);
			File.WriteAllText("set_name_translations.json", json);
			Logger.Debug("SaveSetTranslations() Finished");
		}

		/// <summary>
		/// Populate the _setNameTranslations dictionary.
		/// </summary>
		private void PopulateSetTranslations()
		{
			Logger.Debug("PopulateSetTranslations() Called");
			string serverUpdateTime = CardDatabase.GetServerTimestamp("SetTranslations");
			string cacheTimestamp = LoadSetTranslations();

			if (string.Compare(serverUpdateTime, cacheTimestamp) > 0)
			{
				var setTranslationsUrl = $"https://clans.dailyarena.net/set_name_translations.json?_c={Guid.NewGuid()}";
				var result = WebUtilities.FetchStringFromUrl(setTranslationsUrl, _setNameTranslations.Count != 0, out List<WebException> exceptions);

				if (result == null)
				{
					foreach (WebException exception in exceptions)
					{
						Logger.Error(exception, "Exception from FetchStringFromUrl in {method}", "PopulateSetTranslations");
					}
				}
				else
				{
					dynamic json = JToken.Parse(result.ToString());
					_setNameTranslations = json.ToObject<Dictionary<string, Dictionary<string, string>>>();

					SaveSetTranslations(serverUpdateTime);
				}
			}
			Logger.Debug("PopulateSetTranslations() Finished");
		}

		/// <summary>
		/// Gets or sets a dictionary mapping the Format names shown on the GUI drop-down to the name to use when querying archetype data from the server.
		/// </summary>
		public Dictionary<string, Tuple<string, string>> FormatMappings { get; set; }

		/// <summary>
		/// Reload all of the player data from the Arena real-time logs and recompute all of the app data.
		/// </summary>
		private void ReloadAndCrunchAllData()
		{
			Logger.Debug("ReloadAndCrunchAllData() Called");

			App application = (App)Application.Current;
			DeckFilters filters = application.State.Filters;

			Dispatcher.Invoke(() => { _tabObjects.Clear(); });

			LoadingValue.Value = 60;

			_archetypes.Clear();
			_playerInventory.Clear();
			_playerDecks.Clear();
			_basicLands.Clear();
			_wildcardsOwned.Clear();
			_cardStats.Clear();
			_orderedArchetypes = null;

			LoadingValue.Value = 70;

			int maxInventoryCount = Format.Value == Properties.Resources.Item_Brawl ? 1 : 4;

			LoadingValue.Value = 80;

			Archetype.ClearWildcardsOwned();
			Archetype.ClearCardStats();
			if (Archetype.AnyNumber == null)
			{
				Archetype.AnyNumber = _anyNumber.AsReadOnly();
			}

			LoadingValue.Value = 90;

			foreach (Card card in Card.AllCards)
			{
				_cardStats.Add(card, new CardStats() { Card = card });
			}
			ReadOnlyDictionary<string, List<Card>> cardsByName = Card.CardsByName;
			ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;

			LoadingValue.Value = 100;

			Logger.Debug("Card Database Loaded with {0} Cards", cardsById.Count);

			LoadingText.Value = Properties.Resources.Loading_LoadingDeckArchetypes;
			LoadingValue.Value = 0;

			Logger.Debug("Loading Archetype Data");

			LoadingValue.Value = 25;

			Tuple<string, string> mappedFormat = FormatMappings[Format.Value];
			JToken decksJson = null;
			string serverTimestamp = CardDatabase.GetServerTimestamp($"{mappedFormat.Item2}Decks");
			bool loadDecksFromServer = true;
			if(File.Exists($"{mappedFormat.Item1}_decks.json"))
			{
				Logger.Debug("Cached decks file exists ({mappedFormat}_decks.json)", mappedFormat.Item1);
				string content = File.ReadAllText($"{mappedFormat.Item1}_decks.json");
				using (StringReader contentStringReader = new StringReader(content))
				using (JsonTextReader contentJsonReader = new JsonTextReader(contentStringReader) { DateParseHandling = DateParseHandling.None })
				{
					dynamic json = JToken.ReadFrom(contentJsonReader);
					string decksTimestamp = json["Timestamp"];
					if (!(serverTimestamp == null || string.Compare(serverTimestamp, decksTimestamp) > 0))
					{
						// use cached decks
						Logger.Debug("Cached decks file is up to date, use cached deck data (skip server download)");
						decksJson = json["Decks"];
						loadDecksFromServer = false;
					}
				}
			}
			if (loadDecksFromServer)
			{
				Logger.Debug("Loading deck data from server");

				var archetypesUrl = $"https://clans.dailyarena.net/{mappedFormat.Item1}_decks.json?_c={Guid.NewGuid()}";
				var result = WebUtilities.FetchStringFromUrl(archetypesUrl, decksJson != null, out List<WebException> exceptions);

				if (string.IsNullOrWhiteSpace(result))
				{
					foreach (WebException exception in exceptions)
					{
						Logger.Error(exception, "Exception from FetchStringFromUrl in {method}", "ReloadAndCrunchAllData");
					}
				}
				else
				{
					using (StringReader resultStringReader = new StringReader(result))
					using (JsonTextReader resultJsonReader = new JsonTextReader(resultStringReader) { DateParseHandling = DateParseHandling.None })
					{
						decksJson = JToken.ReadFrom(resultJsonReader);
						Logger.Debug("Writing file {mappedFormat}_decks.json", mappedFormat.Item1);
						File.WriteAllText($"{mappedFormat.Item1}_decks.json", JsonConvert.SerializeObject(
							new
							{
								Timestamp = serverTimestamp,
								Decks = decksJson
							}
						));
					}
				}
			}

			LoadingValue.Value = 50;

			Logger.Debug("Parsing decksJson");
			foreach (dynamic archetype in decksJson)
			{
				bool badDeckDefinition = false;
				string name = archetype["deck_name"];
				Logger.Debug("Deck Name: {name}", name);
				Dictionary<string, int> commandZone = new Dictionary<string, int>();
				string companion = null;
				Dictionary<string, int> mainDeck = new Dictionary<string, int>();
				Dictionary<string, int> sideboard = new Dictionary<string, int>();
				if(RotationProof.Value)
				{
					// check whether there are any cards in the deck that aren't rotation-proof...if so, ignore this deck
					Logger.Debug(@"Doing ""rotation-proof"" check...");
					bool ignoreDeck = false;
					foreach (dynamic card in archetype["deck_list"])
					{
						string cardName = (string)card["name"];
						Logger.Debug("Checking main deck card: {cardName}", cardName);

						try
						{
							if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
							{
								Logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
							if (Format != Properties.Resources.Item_Historic_Bo1 && Format != Properties.Resources.Item_Historic_Bo3 && _standardBannings.Contains(cardName))
							{
								Logger.Debug("{cardName} is banned in standard, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
						}
						catch(KeyNotFoundException e)
						{
							Log.Error(e, "Card not found: {cardName}", cardName);
							cardName = Regex.Split(cardName, " // ")[0].Trim();
							cardName = Regex.Split(cardName, " <")[0].Trim();
							if (!cardsByName.ContainsKey(cardName))
							{
								Logger.Debug("{cardName} is not legal on Arena, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
							else
							{
								if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
								{
									Logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
									ignoreDeck = true;
									break;
								}
								if (Format != Properties.Resources.Item_Historic_Bo1 && Format != Properties.Resources.Item_Historic_Bo3 && _standardBannings.Contains(cardName))
								{
									Logger.Debug("{cardName} is banned in standard, ignore this deck", cardName);
									ignoreDeck = true;
									break;
								}
							}
						}
					}

					if(!ignoreDeck)
					{
						foreach (dynamic card in archetype["sideboard"])
						{
							string cardName = (string)card["name"];

							Logger.Debug("Checking sideboard card: {cardName}", cardName);

							var cardPrintings = cardsByName.Where(x => x.Key.Replace("-", " ") == cardName).Select(y => y.Value).FirstOrDefault();
							if (cardPrintings == null)
							{
								ignoreDeck = true;
								break;
							}
							else
							{
								cardName = cardPrintings[0].Name;
							}

							if (cardPrintings.Count(x => x.Set.RotationSafe) == 0)
							{
								Logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
							if (Format != Properties.Resources.Item_Historic_Bo1 && Format != Properties.Resources.Item_Historic_Bo3 && _standardBannings.Contains(cardName))
							{
								Logger.Debug("{cardName} is banned in standard, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
						}
					}

					if(ignoreDeck)
					{
						continue;
					}
				}
				else if (Format != Properties.Resources.Item_Historic_Bo1 && Format != Properties.Resources.Item_Historic_Bo3)
				{
					// check whether there are any cards in the deck that aren't in standard...if so, ignore this deck
					Logger.Debug(@"Doing Standard legality check...");
					bool ignoreDeck = false;
					foreach (dynamic card in archetype["deck_list"])
					{
						string cardName = (string)card["name"];
						Logger.Debug("Checking main deck card: {cardName}", cardName);

						try
						{
							if (string.IsNullOrWhiteSpace(cardName))
							{
								Logger.Debug("Empty card name found, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
							else if (_standardBannings.Contains(cardName))
							{
								Logger.Debug("{cardName} is banned in standard, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
							else if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
							{
								Logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
						}
						catch (KeyNotFoundException e)
						{
							Log.Error(e, "Card not found: {cardName}", cardName);
							cardName = Regex.Split(cardName, " // ")[0].Trim();
							cardName = Regex.Split(cardName, " <")[0].Trim();
							if (_standardBannings.Contains(cardName))
							{
								Logger.Debug("{cardName} is banned in standard, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
							else if(!cardsByName.ContainsKey(cardName))
							{
								Logger.Debug("Unknown card {cardName} detected, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
							else if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
							{
								Logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
						}
					}

					if (!ignoreDeck)
					{
						foreach (dynamic card in archetype["sideboard"])
						{
							string cardName = (string)card["name"];

							Logger.Debug("Checking sideboard card: {cardName}", cardName);

							if (_standardBannings.Contains(cardName))
							{
								Logger.Debug("{cardName} is banned in standard, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
							else
							{
								List<Card> cardPrintings = null;
								if (cardsByName.ContainsKey(cardName))
								{
									cardPrintings = cardsByName[cardName];
								}
								else
								{
									cardPrintings = cardsByName.Where(x => x.Key.Replace("-", " ") == cardName).Select(y => y.Value).FirstOrDefault();
									if (cardPrintings == null)
									{
										ignoreDeck = true;
										break;
									}
									else
									{
										cardName = cardPrintings[0].Name;
									}
								}

								if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
								{
									Logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
									ignoreDeck = true;
									break;
								}
							}
						}
					}

					if (ignoreDeck)
					{
						continue;
					}
				}

				bool ignoreDeckCheck = false;
				foreach(dynamic card in archetype["deck_list"])
				{
					string cardName = ((string)card["name"]).Trim();
					if(cardName == "Deck")
					{
						continue;
					}
					if(!cardsByName.ContainsKey(cardName))
					{
						cardName = Regex.Split(cardName, " // ")[0].Trim();
						cardName = Regex.Split(cardName, " <")[0].Trim();
						if(!cardsByName.ContainsKey(cardName))
						{
							Logger.Debug("{cardName} does not exist in Arena, ignore this deck", cardName);
							ignoreDeckCheck = true;
							break;
						}
					}
				}
				if(!ignoreDeckCheck)
				{
					foreach (dynamic card in archetype["sideboard"])
					{
						string cardName = ((string)card["name"]).Trim();
						if (!cardsByName.ContainsKey(cardName))
						{
							cardName = Regex.Split(cardName, " // ")[0].Trim();
							cardName = Regex.Split(cardName, " <")[0].Trim();
							if (!cardsByName.ContainsKey(cardName))
							{
								Logger.Debug("{cardName} does not exist in Arena, ignore this deck", cardName);
								ignoreDeckCheck = true;
								break;
							}
						}
					}
				}
				if (ignoreDeckCheck)
				{
					continue;
				}

				foreach (dynamic card in archetype["deck_list"])
				{
					string cardName = ((string)card["name"]).Trim();
					if(cardName == "Deck")
					{
						continue;
					}
					int cardQuantity = (int)card["quantity"];

					Logger.Debug("Processing main deck card: {cardName}, {cardQuantity}", cardName, cardQuantity);
					try
					{
						foreach (Card archetypeCard in cardsByName[cardName])
						{
							CardStats stats = _cardStats[archetypeCard];
							if (!mainDeck.ContainsKey(cardName))
							{
								stats.DeckCount++;
							}
							stats.TotalCopies += cardQuantity;
						}
					}
					catch (KeyNotFoundException e)
					{
						Logger.Error(e, "Card name not found in cardsByName: {cardName}", cardName);
						if (cardName.Contains("//"))
						{
							cardName = Regex.Split(cardName, " // ")[0].Trim();
							foreach (Card archetypeCard in cardsByName[cardName])
							{
								CardStats stats = _cardStats[archetypeCard];
								if (!mainDeck.ContainsKey(cardName))
								{
									stats.DeckCount++;
								}
								stats.TotalCopies += cardQuantity;
							}
						}
					}

					// found some brawl decks on mtggoldfish that illegally have the same
					// entry twice...we'll just ignore those decks here
					if (mainDeck.ContainsKey(cardName))
					{
						Logger.Debug("Bad deck, definition, skipping");
						badDeckDefinition = true;
						break;
					}
					mainDeck.Add(cardName, cardQuantity);
				}
				if (badDeckDefinition)
				{
					continue;
				}
				foreach (dynamic card in archetype["sideboard"])
				{
					string cardName = (string)card["name"];
					int cardQuantity = (int)card["quantity"];

					if(cardQuantity == 0)
					{
						Logger.Debug("Card {cardName} found in sideboard with a quantity of zero, skipping", cardName);
						continue;
					}

					Logger.Debug("Processing sideboard card: {cardName}, {cardQuantity}", cardName, cardQuantity);
					foreach (Card archetypeCard in cardsByName[cardName])
					{
						CardStats stats = _cardStats[archetypeCard];
						if (!mainDeck.ContainsKey(cardName) && !sideboard.ContainsKey(cardName))
						{
							stats.DeckCount++;
						}
						stats.TotalCopies += cardQuantity;
					}
					if (sideboard.ContainsKey(cardName))
					{
						sideboard[cardName] += cardQuantity;
					}
					else
					{
						sideboard.Add(cardName, cardQuantity);
					}
				}
				if (archetype["commandzone"] != null)
				{
					foreach (dynamic card in archetype["commandzone"])
					{
						string cardName = (string)card["name"];
						int cardQuantity = (int)card["quantity"];

						Logger.Debug("Processing commandZone card: {cardName}, {cardQuantity}", cardName, cardQuantity);
						if(cardsByName.ContainsKey(cardName) && cardsByName[cardName][0].ColorIdentity == null)
						{
							Logger.Debug("Commander card has no color identity set, skipping deck");
							badDeckDefinition = true;
							break;
						}
						if(!cardsByName.ContainsKey(cardName))
						{
							Logger.Debug($"Card {cardName} not found, skipping deck");
							badDeckDefinition = true;
							break;
						}
						foreach (Card archetypeCard in cardsByName[cardName])
						{
							CardStats stats = _cardStats[archetypeCard];
							if (!mainDeck.ContainsKey(cardName) && !sideboard.ContainsKey(cardName) && !commandZone.ContainsKey(cardName))
							{
								stats.DeckCount++;
							}
							stats.TotalCopies += cardQuantity;
						}
						if (commandZone.ContainsKey(cardName))
						{
							commandZone[cardName] += cardQuantity;
						}
						else
						{
							commandZone.Add(cardName, cardQuantity);
						}
					}
					if (badDeckDefinition)
					{
						continue;
					}
				}
				if(archetype["companion"] != null)
				{
					companion = (string)archetype["companion"].name;

					Logger.Debug("Processing companion card: {cardName}", companion);
					if (!cardsByName.ContainsKey(companion))
					{
						Logger.Debug($"Card {companion} not found, skipping deck");
						continue;
					}

					if(!sideboard.ContainsKey(companion))
					{
						Logger.Debug($"Sideboard does not contain companion card, skipping deck");
						continue;
					}
				}
				var combinedCounts = mainDeck.Concat(sideboard).Concat(commandZone).GroupBy(x => x.Key).Select(x => new { Name = x.Key, Count = x.Sum(y => y.Value) });
				foreach (var cardCount in combinedCounts)
				{
					foreach (Card archetypeCard in cardsByName[cardCount.Name])
					{
						CardStats stats = _cardStats[archetypeCard];
						stats.MaxCopies = Math.Max(stats.MaxCopies, Math.Min(4, cardCount.Count));
					}
				}
				int win = archetype["win"] == null ? -1 : (int)archetype["win"];
				int loss = -1;
				try
				{
					loss = archetype["loss"] == null ? -1 : (int)archetype["loss"];
				}
				catch(FormatException e)
				{
					Logger.Debug(e, "Format Exception when parsing archetype loss field");
					loss = -1;
				}

				List<Archetype> similarDecks = new List<Archetype>();
				if(archetype["similar_decks"] != null)
				{
					Logger.Debug("Alternate deck configuration found, processing...");
					foreach (dynamic similarDeck in archetype["similar_decks"])
					{
						Dictionary<string, int> similarMainDeck = new Dictionary<string, int>();
						Dictionary<string, int> similarSideboard = new Dictionary<string, int>();
						Dictionary<string, int> similarCommandZone = new Dictionary<string, int>();
						string similarCompanion = null;

						bool ignoreDeck = false;
						Logger.Debug("Validating alternate main deck card list");
						foreach (dynamic card in similarDeck["deck"]["deck_list"])
						{
							string cardName = (string)card["name"];
							int cardQuantity = (int)card["quantity"];

							if (!cardsByName.ContainsKey(cardName))
							{
								ignoreDeck = true;
								break;
							}
						}
						if (ignoreDeck)
						{
							continue;
						}

						foreach (dynamic card in similarDeck["deck"]["deck_list"])
						{
							string cardName = (string)card["name"];
							int cardQuantity = (int)card["quantity"];
							Logger.Debug("Processing alternate main deck card: {cardName}, {cardQuantity}", cardName, cardQuantity);

							foreach (Card archetypeCard in cardsByName[cardName])
							{
								CardStats stats = _cardStats[archetypeCard];
								if (!similarMainDeck.ContainsKey(cardName))
								{
									stats.DeckCount++;
								}
								stats.TotalCopies += cardQuantity;
							}

							similarMainDeck.Add(cardName, cardQuantity);
						}

						foreach (dynamic card in similarDeck["deck"]["sideboard"])
						{
							string cardName = (string)card["name"];
							int cardQuantity = (int)card["quantity"];
							Logger.Debug("Processing alternate sideboard card: {cardName}, {cardQuantity}", cardName, cardQuantity);

							List<Card> cardPrintings = null;
							if(cardsByName.ContainsKey(cardName))
							{
								cardPrintings = cardsByName[cardName];
							}
							else
							{
								cardPrintings = cardsByName.Where(x => x.Key.Replace("-", " ") == cardName).Select(y => y.Value).FirstOrDefault();
								if(cardPrintings == null)
								{
									ignoreDeck = true;
									break;
								}
								else
								{
									cardName = cardPrintings[0].Name;
								}
							}
							foreach (Card archetypeCard in cardPrintings)
							{
								CardStats stats = _cardStats[archetypeCard];
								if (!similarMainDeck.ContainsKey(cardName) && !similarSideboard.ContainsKey(cardName))
								{
									stats.DeckCount++;
								}
								stats.TotalCopies += cardQuantity;
							}

							if (similarSideboard.ContainsKey(cardName))
							{
								similarSideboard[cardName] += cardQuantity;
							}
							else
							{
								similarSideboard.Add(cardName, cardQuantity);
							}
						}

						if (ignoreDeck)
						{
							continue;
						}

						if (similarDeck["deck"]["commandzone"] != null)
						{
							foreach (dynamic card in similarDeck["deck"]["commandzone"])
							{
								string cardName = (string)card["name"];
								int cardQuantity = (int)card["quantity"];
								Logger.Debug("Processing alternate commandZone card: {cardName}, {cardQuantity}", cardName, cardQuantity);

								foreach (Card archetypeCard in cardsByName[cardName])
								{
									CardStats stats = _cardStats[archetypeCard];
									if (!similarMainDeck.ContainsKey(cardName) && !similarSideboard.ContainsKey(cardName) && !similarCommandZone.ContainsKey(cardName))
									{
										stats.DeckCount++;
									}
									stats.TotalCopies += cardQuantity;
								}

								if (similarCommandZone.ContainsKey(cardName))
								{
									similarCommandZone[cardName] += cardQuantity;
								}
								else
								{
									similarCommandZone.Add(cardName, cardQuantity);
								}
							}
						}
						if (similarDeck["deck"]["companion"] != null)
						{
							similarCompanion = (string)similarDeck["deck"]["companion"].name;

							Logger.Debug("Processing companion card: {cardName}", similarCompanion);
							if (!cardsByName.ContainsKey(similarCompanion))
							{
								Logger.Debug($"Card {similarCompanion} not found, skipping deck");
								continue;
							}

							if (!similarSideboard.ContainsKey(similarCompanion))
							{
								Logger.Debug($"Sideboard does not contain companion card, skipping deck");
								continue;
							}
						}
						Archetype similarArchetype = new Archetype(name, similarMainDeck, similarSideboard, similarCommandZone, similarCompanion, RotationProof.Value, win, loss,
							null, setNameTranslations: _setNameTranslations);
						CardStats.UpdateDeckAssociations(similarArchetype);
						similarDecks.Add(similarArchetype);
					}
				}

				Archetype newArchetype = new Archetype(name, mainDeck, sideboard, commandZone, companion, RotationProof.Value, win, loss, similarDecks,
					setNameTranslations: _setNameTranslations);
				CardStats.UpdateDeckAssociations(newArchetype);
				_archetypes.Add(newArchetype);
			}

			LoadingValue.Value = 75;

			Logger.Debug("Initializing Card Stats Objects");
			foreach (Card card in cardsById.Values)
			{
				CardStats stats = _cardStats[card];
				stats.DeckPercentage = (double)stats.DeckCount / (_archetypes.Count + _archetypes.Sum(x => x.SimilarDecks == null ? 0 : x.SimilarDecks.Count()));
			}
			Archetype.CardStats = new ReadOnlyDictionary<Card, CardStats>(_cardStats);

			LoadingValue.Value = 100;

			Logger.Debug("{0} Deck Archetypes Loaded", _archetypes.Count);

			LoadingText.Value = Properties.Resources.Loading_ProcessingCollectionFromLog;
			LoadingValue.Value = 0;

			Logger.Debug("Processing Player Collection");

			var logFolder = GetLogFolderLocation();
			bool playerInventoryFound = false;
			bool playerCardsFound = false;
			using (var fs = new FileStream(logFolder + "\\Player.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				using (var reader = new StreamReader(fs))
				{
					while (true)
					{
						var line = reader.ReadLine();

						if (line == null)
						{
							break;
						}

						if(line.Contains("clientVersion"))
						{
							Regex clientVersionPattern = new Regex(@"\\?""clientVersion\\?"": \\?""([0-9\.]+)\\?""");
							Match match = clientVersionPattern.Match(line);
							if(match.Groups.Count > 1)
							{
								ClientVersion clientVersion = new ClientVersion(match.Groups[1].Value);
								if(clientVersion < _minimumClient)
								{
									LoadingText.Value = Properties.Resources.Message_ClientVersion;
									return;
								}
							}
						}
						else if (line.Contains("PlayerInventory.GetPlayerCardsV3"))
						{
							int jsonStart = line.IndexOf("{");
							int jsonEnd = line.LastIndexOf("}");
							string jsonString = line.Substring(jsonStart, jsonEnd - jsonStart + 1);
							dynamic json = JToken.Parse(jsonString);

							if (json.payload != null)
							{
								Logger.Debug(@"Processing player card inventory...");
								playerCardsFound = true;
								_playerInventory.Clear();
								_basicLands.Clear();
								_playerInventoryCounts.Clear();

								foreach(dynamic info in json.payload)
								{
									int id = int.Parse(info.Name);
									int count = info.Value;

									if (cardsById.ContainsKey(id))
									{
										Card card = cardsById[id];
										Set set = card.Set;
										if ((RotationProof.NotValue || card.RotationSafe) && (card.StandardLegal || Format == Properties.Resources.Item_Historic_Bo1 || Format == Properties.Resources.Item_Historic_Bo3))
										{
											string name = card.Name;
											if (card.Rarity == CardRarity.BasicLand && !_basicLands.ContainsKey(_basicLandColors[card.Name]))
											{
												_basicLands[_basicLandColors[card.Name]] = card;
											}
											if (!_playerInventoryCounts.ContainsKey(name))
											{
												_playerInventoryCounts.Add(name, 0);
											}
											int maxCount = _anyNumber.Contains(name) ? 4 : maxInventoryCount;
											while (count > 0 && _playerInventoryCounts[name] < maxCount)
											{
												if (!_playerInventory.ContainsKey(id))
												{
													_playerInventory.Add(id, 0);
												}
												_playerInventory[id]++;
												_playerInventoryCounts[name]++;
												count--;
											}
										}
									}
									else
									{
										Logger.Debug(@"Unknown card found in player inventory: {arenaId}", id);
									}
								}

								LoadingValue.Value = Math.Max(LoadingValue.Value, 40);
							}
						}
						else if (line.Contains("PlayerInventory.GetPlayerInventory"))
						{
							int jsonStart = line.IndexOf("{");
							int jsonEnd = line.LastIndexOf("}");
							string jsonString = line.Substring(jsonStart, jsonEnd - jsonStart + 1);
							dynamic json = JToken.Parse(jsonString);

							if(json.payload != null)
							{
								Logger.Debug(@"Processing player wildcard inventory...");
								playerInventoryFound = true;
								_wildcardsOwned[CardRarity.Common] = json.payload.wcCommon ?? 0;
								_wildcardsOwned[CardRarity.Uncommon] = json.payload.wcUncommon ?? 0;
								_wildcardsOwned[CardRarity.Rare] = json.payload.wcRare ?? 0;
								_wildcardsOwned[CardRarity.MythicRare] = json.payload.wcMythic ?? 0;
								VaultProgress.Value = json.payload.vaultProgress;

								LoadingValue.Value = Math.Max(LoadingValue.Value, 60);
							}
						}
						else if (line.Contains("Deck.GetDeckListsV3") && !filters.HideFromCollection)
						{
							int jsonStart = line.IndexOf("{");
							int jsonEnd = line.LastIndexOf("}");
							string jsonString = line.Substring(jsonStart, jsonEnd - jsonStart + 1);
							dynamic json = null;
							try
							{
								json = JToken.Parse(jsonString);
							}
							catch(JsonReaderException jsonEx)
							{
								Logger.Debug(jsonEx, "Exception when parsing deck list JSON: {line}", line);
								continue;
							}

							if (json.payload != null)
							{
								Logger.Debug(@"Processing player deck list...");

								foreach (dynamic deck in json.payload)
								{
									string name = deck["name"];
									int[] commandZone = deck["commandZoneGRPIds"] == null ? new int[0] : deck["commandZoneGRPIds"].ToObject<int[]>();
									int companion = deck["companionGRPId"] == null ? 0 : (int)deck["companionGRPId"];
									int[] mainDeck = deck["mainDeck"].ToObject<int[]>();
									int[] sideboard = deck["sideboard"].ToObject<int[]>();
									Guid id = Guid.Parse((string)deck["id"]);
									if (_playerDecks.ContainsKey(id))
									{
										_playerDecks.Remove(id);
									}
									Dictionary<string, int> mainDeckByName = new Dictionary<string, int>();
									Dictionary<string, int> sideboardByName = new Dictionary<string, int>();
									Dictionary<string, int> commandZoneByName = new Dictionary<string, int>();
									string companionName = null;

									if (Format == Properties.Resources.Item_Brawl && commandZone.Length == 0)
									{
										continue;
									}
									else if ((Format == Properties.Resources.Item_Standard || Format == Properties.Resources.Item_Historic_Bo3) && (sideboard.Length == 0 || commandZone.Length > 0))
									{
										continue;
									}
									else if ((Format == Properties.Resources.Item_ArenaStandard || Format == Properties.Resources.Item_Historic_Bo1) && (sideboard.Length > 1 || commandZone.Length > 0))
									{
										continue;
									}

									bool ignoreDeck = false;
									for (int i = 0; i < mainDeck.Length; i += 2)
									{
										if (cardsById.ContainsKey(mainDeck[i]))
										{
											string cardName = cardsById[mainDeck[i]].Name;
											int cardQuantity = mainDeck[i + 1];
											if (mainDeckByName.ContainsKey(cardName))
											{
												mainDeckByName[cardName] += cardQuantity;
											}
											else
											{
												mainDeckByName.Add(cardName, cardQuantity);
											}
										}
										else
										{
											Logger.Debug(@"Unknown card found, ignoring player deck: {arenaId}", mainDeck[i]);
											ignoreDeck = true;
											break;
										}
									}

									if(ignoreDeck)
									{
										continue;
									}

									for (int i = 0; i < sideboard.Length; i += 2)
									{
										if(cardsById.ContainsKey(sideboard[i]))
										{
											string cardName = cardsById[sideboard[i]].Name;
											int cardQuantity = sideboard[i + 1];
											if (sideboardByName.ContainsKey(cardName))
											{
												sideboardByName[cardName] += cardQuantity;
											}
											else
											{
												sideboardByName.Add(cardName, cardQuantity);
											}
										}
										else
										{
											Logger.Debug(@"Unknown sideboard card found, ignoring player deck: {arenaId}", mainDeck[i]);
											ignoreDeck = true;
											break;
										}
									}

									if (ignoreDeck)
									{
										continue;
									}

									for (int i = 0; i < commandZone.Length; i += 2)
									{
										if (cardsById.ContainsKey(commandZone[i]))
										{
											string cardName = cardsById[commandZone[i]].Name;
											int cardQuantity = commandZone[i + 1];
											if (commandZoneByName.ContainsKey(cardName))
											{
												commandZoneByName[cardName] += cardQuantity;
											}
											else
											{
												commandZoneByName.Add(cardName, cardQuantity);
											}
										}
										else
										{
											Logger.Debug(@"Unknown card found, ignoring player deck: {arenaId}", mainDeck[i]);
											ignoreDeck = true;
											break;
										}
									}

									if (ignoreDeck)
									{
										continue;
									}

									if (companion != 0)
									{
										if (cardsById.ContainsKey(companion))
										{
											companionName = cardsById[companion].Name;
											if (!sideboardByName.ContainsKey(companionName))
											{
												Logger.Debug(@"Sideboard doesn't contain companion, ignoring player deck");
												continue;
											}
										}
										else
										{
											Logger.Debug(@"Unknown card found, ignoring player deck: {arenaId}", companion);
											continue;
										}
									}

									if (RotationProof.Value)
									{
										// check whether there are any cards in the deck that aren't rotation-proof...if so, ignore this deck
										Logger.Debug(@"Doing ""rotation-proof"" check...");
										ignoreDeck = false;
										foreach (var card in mainDeckByName)
										{
											string cardName = card.Key;
											Logger.Debug("Checking main deck card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
											{
												Logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
												ignoreDeck = true;
												break;
											}
										}

										if (!ignoreDeck)
										{
											foreach (var card in sideboardByName)
											{
												string cardName = card.Key;

												Logger.Debug("Checking sideboard card: {cardName}", cardName);

												if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
												{
													Logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
													ignoreDeck = true;
													break;
												}
											}
										}

										if (!ignoreDeck)
										{
											foreach (var card in commandZoneByName)
											{
												string cardName = card.Key;

												Logger.Debug("Checking command zone card: {cardName}", cardName);

												if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
												{
													Logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
													ignoreDeck = true;
													break;
												}
											}
										}

										if (ignoreDeck)
										{
											continue;
										}
									}
									else if (Format != Properties.Resources.Item_Historic_Bo1 && Format != Properties.Resources.Item_Historic_Bo3)
									{
										// check whether there are any cards in the deck that aren't in standard...if so, ignore this deck
										Logger.Debug(@"Doing Standard legality check...");
										ignoreDeck = false;
										foreach (var card in mainDeckByName)
										{
											string cardName = card.Key;
											Logger.Debug("Checking main deck card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
											{
												Logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
												ignoreDeck = true;
												break;
											}
										}

										if (!ignoreDeck)
										{
											foreach (var card in sideboardByName)
											{
												string cardName = card.Key;

												Logger.Debug("Checking sideboard card: {cardName}", cardName);

												if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
												{
													Logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
													ignoreDeck = true;
													break;
												}
											}
										}

										if (!ignoreDeck)
										{
											foreach (var card in commandZoneByName)
											{
												string cardName = card.Key;

												Logger.Debug("Checking command zone card: {cardName}", cardName);

												if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
												{
													Logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
													ignoreDeck = true;
													break;
												}
											}
										}

										if (ignoreDeck)
										{
											continue;
										}
									}

									_playerDecks.Add(id, new Archetype(name, mainDeckByName, sideboardByName, commandZoneByName, companionName, RotationProof.Value,
										isPlayerDeck: true, setNameTranslations: _setNameTranslations));
								}

								LoadingValue.Value = Math.Max(LoadingValue.Value, 20);
							}
						}
						else if((line.Contains("Deck.CreateDeckV3") || line.Contains("Deck.UpdateDeckV3")) && !filters.HideFromCollection)
						{
							int jsonStart = line.IndexOf("{");
							int jsonEnd = line.LastIndexOf("}");
							string jsonString = line.Substring(jsonStart, jsonEnd - jsonStart + 1);
							dynamic json = JToken.Parse(jsonString);

							if(json.payload != null)
							{
								Logger.Debug(@"Processing player deck create/update...");
								dynamic deck = json.payload;

								string name = deck["name"];
								int[] commandZone = deck["commandZoneGRPIds"] == null ? new int[0] : deck["commandZoneGRPIds"].ToObject<int[]>();
								int companion = deck["companionGRPId"] == null ? 0 : (int)deck["companionGRPId"];
								int[] mainDeck = deck["mainDeck"].ToObject<int[]>();
								int[] sideboard = deck["sideboard"].ToObject<int[]>();
								Guid id = Guid.Parse((string)deck["id"]);
								if(_playerDecks.ContainsKey(id))
								{
									_playerDecks.Remove(id);
								}
								Dictionary<string, int> mainDeckByName = new Dictionary<string, int>();
								Dictionary<string, int> sideboardByName = new Dictionary<string, int>();
								Dictionary<string, int> commandZoneByName = new Dictionary<string, int>();
								string companionName = null;

								if (Format == Properties.Resources.Item_Brawl && commandZone.Length == 0)
								{
									continue;
								}
								else if ((Format == Properties.Resources.Item_Standard || Format == Properties.Resources.Item_Historic_Bo3) && (sideboard.Length == 0 || commandZone.Length > 0))
								{
									continue;
								}
								else if ((Format == Properties.Resources.Item_ArenaStandard || Format == Properties.Resources.Item_Historic_Bo1) && (sideboard.Length > 1 || commandZone.Length > 0))
								{
									continue;
								}

								var ignoreDeck = false;
								for (int i = 0; i < mainDeck.Length; i += 2)
								{
									if(!cardsById.ContainsKey(mainDeck[i]))
									{
										Logger.Debug(@"Unknown Card found with ArenaId {arenaId}, skipping deck.", mainDeck[i]);
										ignoreDeck = true;
										break;
									}
									string cardName = cardsById[mainDeck[i]].Name;
									int cardQuantity = mainDeck[i + 1];
									if (mainDeckByName.ContainsKey(cardName))
									{
										mainDeckByName[cardName] += cardQuantity;
									}
									else
									{
										mainDeckByName.Add(cardName, cardQuantity);
									}
								}

								if(ignoreDeck)
								{
									continue;
								}

								for (int i = 0; i < sideboard.Length; i += 2)
								{
									string cardName = cardsById[sideboard[i]].Name;
									int cardQuantity = sideboard[i + 1];
									if (sideboardByName.ContainsKey(cardName))
									{
										sideboardByName[cardName] += cardQuantity;
									}
									else
									{
										sideboardByName.Add(cardName, cardQuantity);
									}
								}
								for (int i = 0; i < commandZone.Length; i += 2)
								{
									string cardName = cardsById[commandZone[i]].Name;
									int cardQuantity = commandZone[i + 1];
									if (commandZoneByName.ContainsKey(cardName))
									{
										commandZoneByName[cardName] += cardQuantity;
									}
									else
									{
										commandZoneByName.Add(cardName, cardQuantity);
									}
								}

								if (companion != 0)
								{
									if (cardsById.ContainsKey(companion))
									{
										companionName = cardsById[companion].Name;
										if (!sideboardByName.ContainsKey(companionName))
										{
											Logger.Debug(@"Sideboard doesn't contain companion, ignoring player deck");
											continue;
										}
									}
									else
									{
										Logger.Debug(@"Unknown card found, ignoring player deck: {arenaId}", companion);
										continue;
									}
								}

								if (RotationProof.Value)
								{
									// check whether there are any cards in the deck that aren't rotation-proof...if so, ignore this deck
									Logger.Debug(@"Doing ""rotation-proof"" check...");
									ignoreDeck = false;
									foreach (var card in mainDeckByName)
									{
										string cardName = card.Key;
										Logger.Debug("Checking main deck card: {cardName}", cardName);

										if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
										{
											Logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
											ignoreDeck = true;
											break;
										}
									}

									if (!ignoreDeck)
									{
										foreach (var card in sideboardByName)
										{
											string cardName = card.Key;

											Logger.Debug("Checking sideboard card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
											{
												Logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
												ignoreDeck = true;
												break;
											}
										}
									}

									if (!ignoreDeck)
									{
										foreach (var card in commandZoneByName)
										{
											string cardName = card.Key;

											Logger.Debug("Checking command zone card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
											{
												Logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
												ignoreDeck = true;
												break;
											}
										}
									}

									if (ignoreDeck)
									{
										continue;
									}
								}
								else if (Format != Properties.Resources.Item_Historic_Bo1 && Format != Properties.Resources.Item_Historic_Bo3)
								{
									// check whether there are any cards in the deck that aren't in standard...if so, ignore this deck
									Logger.Debug(@"Doing Standard legality check...");
									ignoreDeck = false;
									foreach (var card in mainDeckByName)
									{
										string cardName = card.Key;
										Logger.Debug("Checking main deck card: {cardName}", cardName);

										if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
										{
											Logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
											ignoreDeck = true;
											break;
										}
									}

									if (!ignoreDeck)
									{
										foreach (var card in sideboardByName)
										{
											string cardName = card.Key;

											Logger.Debug("Checking sideboard card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
											{
												Logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
												ignoreDeck = true;
												break;
											}
										}
									}

									if (!ignoreDeck)
									{
										foreach (var card in commandZoneByName)
										{
											string cardName = card.Key;

											Logger.Debug("Checking command zone card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
											{
												Logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
												ignoreDeck = true;
												break;
											}
										}
									}

									if (ignoreDeck)
									{
										continue;
									}
								}

								_playerDecks.Add(id, new Archetype(name, mainDeckByName, sideboardByName, commandZoneByName, companionName, RotationProof.Value, isPlayerDeck: true,
									setNameTranslations: _setNameTranslations));

								LoadingValue.Value = Math.Max(LoadingValue.Value, 80);
							}
						}
						else if (line.Contains("Deck.DeleteDeck") && !filters.HideFromCollection)
						{
							int jsonStart = line.IndexOf("{");
							int jsonEnd = line.LastIndexOf("}");
							string jsonString = line.Substring(jsonStart, jsonEnd - jsonStart + 1);
							dynamic json = JToken.Parse(jsonString);

							if(json.request != null)
							{
								Logger.Debug(@"Processing player deck delete...");
								dynamic request = JToken.Parse((string)json.request);

								Guid id = Guid.Parse((string)request["params"]["deckId"]);
								if (_playerDecks.ContainsKey(id))
								{
									_playerDecks.Remove(id);
								}
							}
						}
						else if(line.StartsWith("[Accounts - Client] Successfully logged in to account:"))
						{
							Logger.Debug(@"New login detected, clearing player deck list...");
							_playerDecks.Clear();
						}
					}
				}
			}

			if(!(playerInventoryFound && playerCardsFound))
			{
				Logger.Debug("Player Inventory or Cards not found in MTGA log, show an information screen [{playerInventoryFound}, {playerCardsFound}]", playerInventoryFound, playerCardsFound);
				Dispatcher.Invoke(() =>
				{
					LoadingScreen.Visibility = Visibility.Collapsed;
					DetailedLoggingScreen.Visibility = Visibility.Visible;
				});
				return;
			}

			Archetype.WildcardsOwned = new ReadOnlyDictionary<CardRarity, int>(_wildcardsOwned);

			LoadingValue.Value = 100;

			Logger.Debug("Player Collection Loaded with {0} Cards", _playerInventory.Count);

			LoadingText.Value = Properties.Resources.Loading_ComputingDeckSuggestions;
			LoadingValue.Value = 0;

			Logger.Debug("Computing Suggestions");

			foreach (Archetype archetype in _archetypes)
			{
				Logger.Debug("Processing Archetype {0}", archetype.Name);
				Dictionary<string, int> haveCards = new Dictionary<string, int>();
				Dictionary<int, int> mainDeckCards = new Dictionary<int, int>();
				Dictionary<int, int> sideboardCards = new Dictionary<int, int>();
				Dictionary<int, int> commandZoneCards = new Dictionary<int, int>();
				Dictionary<int, int> mainDeckToCollect = new Dictionary<int, int>();
				Dictionary<int, int> sideboardToCollect = new Dictionary<int, int>();
				Dictionary<int, int> commandZoneToCollect = new Dictionary<int, int>();

				var cardsNeededForArchetype = archetype.MainDeck.Concat(archetype.Sideboard).Concat(archetype.CommandZone).
					GroupBy(x => x.Key).Select(y => new { Name = y.Key, Quantity = y.Sum(z => z.Value) });

				Dictionary<int, int> playerInventory = new Dictionary<int, int>(_playerInventory.Select(x => new { Id = x.Key, Count = x.Value }).ToDictionary(x => x.Id, y => y.Count));

				Logger.Debug("Comparing deck list to inventory to determine which cards are still needed");
				foreach (var neededCard in cardsNeededForArchetype)
				{
					int neededForMain = archetype.MainDeck.ContainsKey(neededCard.Name) ? archetype.MainDeck[neededCard.Name] : 0;
					int neededForSideboard = archetype.Sideboard.ContainsKey(neededCard.Name) ? archetype.Sideboard[neededCard.Name] : 0;
					int neededForCommandZone = archetype.CommandZone.ContainsKey(neededCard.Name) ? archetype.CommandZone[neededCard.Name] : 0;

					int neededCards = neededCard.Quantity;
					if (_anyNumber.Contains(neededCard.Name))
					{
						neededCards = Math.Min(neededCards, 4);
					}
					haveCards.Add(neededCard.Name, 0);
					List<Card> printings = cardsByName[neededCard.Name];
					if (neededCards > 0)
					{
						CardRarity rarity = CardRarity.MythicRare;
						foreach (Card printing in printings)
						{
							rarity = LowestRarity(rarity, printing.Rarity);
							int playerOwns = playerInventory.ContainsKey(printing.ArenaId) ? playerInventory[printing.ArenaId] : 0;
							if (printing.Rarity == CardRarity.BasicLand && playerOwns > 0)
							{
								if (neededForMain > 0)
								{
									mainDeckCards.Add(printing.ArenaId, neededForMain);
								}
								if (neededForSideboard > 0)
								{
									sideboardCards.Add(printing.ArenaId, neededForSideboard);
								}
								if(neededForCommandZone > 0)
								{
									commandZoneCards.Add(printing.ArenaId, neededForCommandZone);
								}
								haveCards[printing.Name] += neededCards;
								neededCards = 0;
								neededForMain = 0;
								neededForSideboard = 0;
								neededForCommandZone = 0;
							}
							else if (playerOwns >= neededCards)
							{
								if (neededForMain > 0)
								{
									mainDeckCards.Add(printing.ArenaId, neededForMain);
								}
								if (neededForSideboard > 0)
								{
									sideboardCards.Add(printing.ArenaId, neededForSideboard);
								}
								if(neededForCommandZone > 0)
								{
									commandZoneCards.Add(printing.ArenaId, neededForCommandZone);
								}
								haveCards[printing.Name] += neededCards;
								playerInventory[printing.ArenaId] -= neededCards;
								neededCards = 0;
								neededForMain = 0;
								neededForSideboard = 0;
								neededForCommandZone = 0;
							}
							else if (playerOwns > 0)
							{
								int owned = playerOwns;
								if (neededForMain > 0)
								{
									mainDeckCards.Add(printing.ArenaId, 1);
									owned--;
									neededForMain--;
									while (owned > 0 && neededForMain > 0)
									{
										mainDeckCards[printing.ArenaId]++;
										owned--;
										neededForMain--;
									}
								}
								if (owned > 0 && neededForSideboard > 0)
								{
									sideboardCards.Add(printing.ArenaId, 1);
									owned--;
									neededForSideboard--;
									while (owned > 0 && neededForSideboard > 0)
									{
										sideboardCards[printing.ArenaId]++;
										owned--;
										neededForSideboard--;
									}
								}
								if(owned > 0 && neededForCommandZone > 0)
								{
									commandZoneCards.Add(printing.ArenaId, 1);
									owned--;
									neededForCommandZone--;
									while(owned > 0 && neededForCommandZone > 0)
									{
										commandZoneCards[printing.ArenaId]++;
										owned--;
										neededForCommandZone--;
									}
								}
								haveCards[printing.Name] += playerOwns;
								neededCards -= playerOwns;
								playerInventory[printing.ArenaId] = 0;
							}
							if (neededCards == 0) { break; }
						}
						if (neededForMain > 0)
						{
							foreach (Card printing in printings)
							{
								if (printing.Rarity == rarity)
								{
									mainDeckToCollect.Add(printing.ArenaId, neededForMain);
									break;
								}
							}
						}
						if (neededForSideboard > 0)
						{
							foreach (Card printing in printings)
							{
								if (printing.Rarity == rarity)
								{
									sideboardToCollect.Add(printing.ArenaId, neededForSideboard);
									break;
								}
							}
						}
						if(neededForCommandZone > 0)
						{
							foreach(Card printing in printings)
							{
								if(printing.Rarity == rarity)
								{
									commandZoneToCollect.Add(printing.ArenaId, neededForCommandZone);
									break;
								}
							}
						}
					}
				}

				Logger.Debug("Generating replacement suggestions for missing cards");
				List<Tuple<int, int, int>> suggestedReplacements = GenerateReplacements(archetype, mainDeckToCollect, sideboardToCollect, commandZoneToCollect, playerInventory,
					cardsByName, cardsById, filters);

				Logger.Debug("Updating Archetype objects with suggestions");
				archetype.SuggestedMainDeck = new ReadOnlyDictionary<int, int>(mainDeckCards);
				archetype.SuggestedSideboard = new ReadOnlyDictionary<int, int>(sideboardCards);
				archetype.SuggestedCommandZone = new ReadOnlyDictionary<int, int>(commandZoneCards);
				archetype.MainDeckToCollect = new ReadOnlyDictionary<int, int>(mainDeckToCollect);
				archetype.SideboardToCollect = new ReadOnlyDictionary<int, int>(sideboardToCollect);
				archetype.CommandZoneToCollect = new ReadOnlyDictionary<int, int>(commandZoneToCollect);
				archetype.SuggestedReplacements = suggestedReplacements.AsReadOnly();

				Logger.Debug("Processing Alternate Deck Configurations");
				if(archetype.SimilarDecks != null && archetype.SimilarDecks.Count > 0)
				{
					foreach(Archetype similarArchetype in archetype.SimilarDecks)
					{
						Logger.Debug("Processing Similar Archetype {0}", similarArchetype.Name);
						haveCards = new Dictionary<string, int>();
						mainDeckCards = new Dictionary<int, int>();
						sideboardCards = new Dictionary<int, int>();
						commandZoneCards = new Dictionary<int, int>();
						mainDeckToCollect = new Dictionary<int, int>();
						sideboardToCollect = new Dictionary<int, int>();
						commandZoneToCollect = new Dictionary<int, int>();

						cardsNeededForArchetype = similarArchetype.MainDeck.Concat(similarArchetype.Sideboard).Concat(similarArchetype.CommandZone).
							GroupBy(x => x.Key).Select(y => new { Name = y.Key, Quantity = y.Sum(z => z.Value) });

						playerInventory = new Dictionary<int, int>(_playerInventory.Select(x => new { Id = x.Key, Count = x.Value }).ToDictionary(x => x.Id, y => y.Count));

						Logger.Debug("Comparing deck list to inventory to determine which cards are still needed");
						foreach (var neededCard in cardsNeededForArchetype)
						{
							int neededForMain = similarArchetype.MainDeck.ContainsKey(neededCard.Name) ? similarArchetype.MainDeck[neededCard.Name] : 0;
							int neededForSideboard = similarArchetype.Sideboard.ContainsKey(neededCard.Name) ? similarArchetype.Sideboard[neededCard.Name] : 0;
							int neededForCommandZone = similarArchetype.CommandZone.ContainsKey(neededCard.Name) ? similarArchetype.CommandZone[neededCard.Name] : 0;

							int neededCards = neededCard.Quantity;
							if (_anyNumber.Contains(neededCard.Name))
							{
								neededCards = Math.Min(neededCards, 4);
							}
							haveCards.Add(neededCard.Name, 0);
							List<Card> printings = cardsByName[neededCard.Name];
							if (neededCards > 0)
							{
								CardRarity rarity = CardRarity.MythicRare;
								foreach (Card printing in printings)
								{
									rarity = LowestRarity(rarity, printing.Rarity);
									int playerOwns = playerInventory.ContainsKey(printing.ArenaId) ? playerInventory[printing.ArenaId] : 0;
									if (printing.Rarity == CardRarity.BasicLand && playerOwns > 0)
									{
										if (neededForMain > 0)
										{
											mainDeckCards.Add(printing.ArenaId, neededForMain);
										}
										if (neededForSideboard > 0)
										{
											sideboardCards.Add(printing.ArenaId, neededForSideboard);
										}
										if(neededForCommandZone > 0)
										{
											commandZoneCards.Add(printing.ArenaId, neededForCommandZone);
										}
										haveCards[printing.Name] += neededCards;
										neededCards = 0;
										neededForMain = 0;
										neededForSideboard = 0;
										neededForCommandZone = 0;
									}
									else if (playerOwns >= neededCards)
									{
										if (neededForMain > 0)
										{
											mainDeckCards.Add(printing.ArenaId, neededForMain);
										}
										if (neededForSideboard > 0)
										{
											sideboardCards.Add(printing.ArenaId, neededForSideboard);
										}
										if(neededForCommandZone > 0)
										{
											commandZoneCards.Add(printing.ArenaId, neededForCommandZone);
										}
										haveCards[printing.Name] += neededCards;
										playerInventory[printing.ArenaId] -= neededCards;
										neededCards = 0;
										neededForMain = 0;
										neededForSideboard = 0;
										neededForCommandZone = 0;
									}
									else if (playerOwns > 0)
									{
										int owned = playerOwns;
										if (neededForMain > 0)
										{
											mainDeckCards.Add(printing.ArenaId, 1);
											owned--;
											neededForMain--;
											while (owned > 0 && neededForMain > 0)
											{
												mainDeckCards[printing.ArenaId]++;
												owned--;
												neededForMain--;
											}
										}
										if (owned > 0 && neededForSideboard > 0)
										{
											sideboardCards.Add(printing.ArenaId, 1);
											owned--;
											neededForSideboard--;
											while (owned > 0 && neededForSideboard > 0)
											{
												sideboardCards[printing.ArenaId]++;
												owned--;
												neededForSideboard--;
											}
										}
										if(owned > 0 && neededForCommandZone > 0)
										{
											commandZoneCards.Add(printing.ArenaId, 1);
											owned--;
											neededForCommandZone--;
											while(owned > 0 && neededForCommandZone > 0)
											{
												commandZoneCards[printing.ArenaId]++;
												owned--;
												neededForCommandZone--;
											}
										}
										haveCards[printing.Name] += playerOwns;
										neededCards -= playerOwns;
										playerInventory[printing.ArenaId] = 0;
									}
									if (neededCards == 0) { break; }
								}
								if (neededForMain > 0)
								{
									foreach (Card printing in printings)
									{
										if (printing.Rarity == rarity)
										{
											mainDeckToCollect.Add(printing.ArenaId, neededForMain);
											break;
										}
									}
								}
								if (neededForSideboard > 0)
								{
									foreach (Card printing in printings)
									{
										if (printing.Rarity == rarity)
										{
											sideboardToCollect.Add(printing.ArenaId, neededForSideboard);
											break;
										}
									}
								}
								if(neededForCommandZone > 0)
								{
									foreach (Card printing in printings)
									{
										if (printing.Rarity == rarity)
										{
											commandZoneToCollect.Add(printing.ArenaId, neededForCommandZone);
											break;
										}
									}
								}
							}
						}

						Logger.Debug("Generating replacement suggestions for missing cards");
						suggestedReplacements = GenerateReplacements(similarArchetype, mainDeckToCollect, sideboardToCollect, commandZoneToCollect, playerInventory, cardsByName,
							cardsById, filters);

						Logger.Debug("Updating Archetype objects with suggestions");
						similarArchetype.SuggestedMainDeck = new ReadOnlyDictionary<int, int>(mainDeckCards);
						similarArchetype.SuggestedSideboard = new ReadOnlyDictionary<int, int>(sideboardCards);
						similarArchetype.SuggestedCommandZone = new ReadOnlyDictionary<int, int>(commandZoneCards);
						similarArchetype.MainDeckToCollect = new ReadOnlyDictionary<int, int>(mainDeckToCollect);
						similarArchetype.SideboardToCollect = new ReadOnlyDictionary<int, int>(sideboardToCollect);
						similarArchetype.CommandZoneToCollect = new ReadOnlyDictionary<int, int>(commandZoneToCollect);
						similarArchetype.SuggestedReplacements = suggestedReplacements.AsReadOnly();
					}

					archetype.SortSimilarDecks();
				}
			}

			LoadingValue.Value = 50;

			Logger.Debug("Handling player inventory decks");
			foreach (Archetype playerDeck in _playerDecks.Values)
			{
				Logger.Debug("Processing Player Deck {0}", playerDeck.Name);
				Dictionary<string, int> haveCards = new Dictionary<string, int>();
				Dictionary<int, int> mainDeckCards = new Dictionary<int, int>();
				Dictionary<int, int> sideboardCards = new Dictionary<int, int>();
				Dictionary<int, int> commandZoneCards = new Dictionary<int, int>();
				Dictionary<int, int> mainDeckToCollect = new Dictionary<int, int>();
				Dictionary<int, int> sideboardToCollect = new Dictionary<int, int>();
				Dictionary<int, int> commandZoneToCollect = new Dictionary<int, int>();

				var cardsNeededForArchetype = playerDeck.MainDeck.Concat(playerDeck.Sideboard).Concat(playerDeck.CommandZone).
					GroupBy(x => x.Key).Select(y => new { Name = y.Key, Quantity = y.Sum(z => z.Value) });

				Dictionary<int, int> playerInventory = new Dictionary<int, int>(_playerInventory.Select(x => new { Id = x.Key, Count = x.Value }).ToDictionary(x => x.Id, y => y.Count));

				Logger.Debug("Comparing deck list to inventory to determine which cards are still needed");
				foreach (var neededCard in cardsNeededForArchetype)
				{
					int neededForMain = playerDeck.MainDeck.ContainsKey(neededCard.Name) ? playerDeck.MainDeck[neededCard.Name] : 0;
					int neededForSideboard = playerDeck.Sideboard.ContainsKey(neededCard.Name) ? playerDeck.Sideboard[neededCard.Name] : 0;
					int neededForCommandZone = playerDeck.CommandZone.ContainsKey(neededCard.Name) ? playerDeck.CommandZone[neededCard.Name] : 0;

					int neededCards = neededCard.Quantity;
					if (_anyNumber.Contains(neededCard.Name))
					{
						neededCards = Math.Min(neededCards, 4);
					}
					haveCards.Add(neededCard.Name, 0);
					List<Card> printings = cardsByName[neededCard.Name];
					if (neededCards > 0)
					{
						CardRarity rarity = CardRarity.MythicRare;
						foreach (Card printing in printings)
						{
							rarity = LowestRarity(rarity, printing.Rarity);
							int playerOwns = playerInventory.ContainsKey(printing.ArenaId) ? playerInventory[printing.ArenaId] : 0;
							if (printing.Rarity == CardRarity.BasicLand && playerOwns > 0)
							{
								if (neededForMain > 0)
								{
									mainDeckCards.Add(printing.ArenaId, neededForMain);
								}
								if (neededForSideboard > 0)
								{
									sideboardCards.Add(printing.ArenaId, neededForSideboard);
								}
								if(neededForCommandZone > 0)
								{
									commandZoneCards.Add(printing.ArenaId, neededForCommandZone);
								}
								haveCards[printing.Name] += neededCards;
								neededCards = 0;
								neededForMain = 0;
								neededForSideboard = 0;
								neededForCommandZone = 0;
							}
							else if (playerOwns >= neededCards)
							{
								if (neededForMain > 0)
								{
									mainDeckCards.Add(printing.ArenaId, neededForMain);
								}
								if (neededForSideboard > 0)
								{
									sideboardCards.Add(printing.ArenaId, neededForSideboard);
								}
								if(neededForCommandZone > 0)
								{
									commandZoneCards.Add(printing.ArenaId, neededForCommandZone);
								}
								haveCards[printing.Name] += neededCards;
								playerInventory[printing.ArenaId] -= neededCards;
								neededCards = 0;
								neededForMain = 0;
								neededForSideboard = 0;
								neededForCommandZone = 0;
							}
							else if (playerOwns > 0)
							{
								int owned = playerOwns;
								if (neededForMain > 0)
								{
									mainDeckCards.Add(printing.ArenaId, 1);
									owned--;
									neededForMain--;
									while (owned > 0 && neededForMain > 0)
									{
										mainDeckCards[printing.ArenaId]++;
										owned--;
										neededForMain--;
									}
								}
								if (owned > 0 && neededForSideboard > 0)
								{
									sideboardCards.Add(printing.ArenaId, 1);
									owned--;
									neededForSideboard--;
									while (owned > 0 && neededForSideboard > 0)
									{
										sideboardCards[printing.ArenaId]++;
										owned--;
										neededForSideboard--;
									}
								}
								if(owned > 0 && neededForCommandZone > 0)
								{
									commandZoneCards.Add(printing.ArenaId, 1);
									owned--;
									neededForCommandZone--;
									while(owned > 0 && neededForCommandZone > 0)
									{
										commandZoneCards[printing.ArenaId]++;
										owned--;
										neededForSideboard--;
									}
								}
								haveCards[printing.Name] += playerOwns;
								neededCards -= playerOwns;
								playerInventory[printing.ArenaId] = 0;
							}
							if (neededCards == 0) { break; }
						}
						if (neededForMain > 0)
						{
							foreach (Card printing in printings)
							{
								if (printing.Rarity == rarity)
								{
									mainDeckToCollect.Add(printing.ArenaId, neededForMain);
									break;
								}
							}
						}
						if (neededForSideboard > 0)
						{
							foreach (Card printing in printings)
							{
								if (printing.Rarity == rarity)
								{
									sideboardToCollect.Add(printing.ArenaId, neededForSideboard);
									break;
								}
							}
						}
						if (neededForCommandZone > 0)
						{
							foreach (Card printing in printings)
							{
								if (printing.Rarity == rarity)
								{
									commandZoneToCollect.Add(printing.ArenaId, neededForCommandZone);
									break;
								}
							}
						}
					}
				}

				bool badCommander = false;
				foreach(string commanderName in playerDeck.CommandZone.Keys)
				{
					if(!cardsByName.ContainsKey(commanderName) || cardsByName[commanderName][0].ColorIdentity == null)
					{
						Logger.Debug("Missing Color Identity information for Commander: {commanderName} - skipping player deck {playerDeck}", commanderName, playerDeck.Name);
						badCommander = true;
						break;
					}
				}
				if (badCommander)
				{
					continue;
				}

				Logger.Debug("Generating replacement suggestions for missing cards");
				List<Tuple<int, int, int>> suggestedReplacements = GenerateReplacements(playerDeck, mainDeckToCollect, sideboardToCollect, commandZoneToCollect, playerInventory,
					cardsByName, cardsById, filters);

				Logger.Debug("Updating Player Deck objects with suggestions");
				playerDeck.SuggestedMainDeck = new ReadOnlyDictionary<int, int>(mainDeckCards);
				playerDeck.SuggestedSideboard = new ReadOnlyDictionary<int, int>(sideboardCards);
				playerDeck.SuggestedCommandZone = new ReadOnlyDictionary<int, int>(commandZoneCards);
				playerDeck.MainDeckToCollect = new ReadOnlyDictionary<int, int>(mainDeckToCollect);
				playerDeck.SideboardToCollect = new ReadOnlyDictionary<int, int>(sideboardToCollect);
				playerDeck.CommandZoneToCollect = new ReadOnlyDictionary<int, int>(commandZoneToCollect);
				playerDeck.SuggestedReplacements = suggestedReplacements.AsReadOnly();

				if(playerDeck.BoosterCost > 0)
				{
					// if the player doesn't own all the cards for this deck, add it to the list
					_archetypes.Add(playerDeck);
				}
			}

			LoadingValue.Value = 75;

			Logger.Debug("Sorting Archetypes and generating Meta Report");
			FilterArchetypes(filters);

			LoadingValue.Value = 90;
			SortArchetypes();

			LoadingValue.Value = 100;

			Logger.Debug("Finished Computing Suggestions, Updating GUI");

			Dispatcher.Invoke(() =>
			{
				LoadingScreen.Visibility = Visibility.Collapsed;
				FilterPanel.Visibility = Visibility.Visible;
				DeckTabs.Visibility = Visibility.Visible;
				DeckTabs.ItemsSource = _tabObjects;
				DeckTabs.SelectedIndex = 0;
			});

			Logger.Debug("ReloadAndCrunchAllData() Finished");
		}

		/// <summary>
		/// Apply the archetype filters and store the filtered list in _filteredArchetypes.
		/// </summary>
		/// <param name="filters">The deck filters to apply.</param>
		/// <param name="sort">Whether to sort the list after applying filters (Defaults to false).</param>
		private void FilterArchetypes(DeckFilters filters, bool sort = false)
		{
			Logger.Debug("FilterArchetypes() Called, sort={sort}", sort);
			ReadOnlyDictionary<string, List<Card>> cardsByName = Card.CardsByName;
			ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
			_filteredArchetypes = _archetypes;

			if (filters.HideMissingWildcards)
			{
				_filteredArchetypes = _filteredArchetypes.Where(x => x.TotalWildcardsNeeded == 0).ToList();
			}
			if (filters.HideMissingCards)
			{
				_filteredArchetypes = _filteredArchetypes.Where(x => x.BoosterCost == 0).ToList();
			}
			if (filters.HideIncompleteReplacements)
			{
				_filteredArchetypes = _filteredArchetypes.Where(
					x => x.MainDeckToCollect.Sum(y => y.Value) + x.SideboardToCollect.Sum(y => y.Value) == x.SuggestedReplacements.Sum(y => y.Item3)
				).ToList();
			}
			if(filters.HideScoreThreshold)
			{
				_filteredArchetypes = _filteredArchetypes.Where(x => x.Score >= filters.ScoreThreshold).ToList();
			}
			if (filters.HideGamesRecorded)
			{
				_filteredArchetypes = _filteredArchetypes.Where(x => (x.Win + x.Loss) >= filters.GamesThreshold).ToList();
			}
			if (filters.HideMythic || filters.HideRare || filters.HideUncommon || filters.HideCommon)
			{
				_filteredArchetypes = _filteredArchetypes.Where(
					x =>
						(!filters.HideMythic || RarityCount(x.MainDeck.Concat(x.Sideboard), CardRarity.MythicRare, cardsByName) <= filters.MythicCount) &&
						(!filters.HideRare || RarityCount(x.MainDeck.Concat(x.Sideboard), CardRarity.Rare, cardsByName) <= filters.RareCount) &&
						(!filters.HideUncommon || RarityCount(x.MainDeck.Concat(x.Sideboard), CardRarity.Uncommon, cardsByName) <= filters.UncommonCount) &&
						(!filters.HideCommon || RarityCount(x.MainDeck.Concat(x.Sideboard), CardRarity.Common, cardsByName) <= filters.CommonCount)
				).ToList();
			}
			if(!string.IsNullOrEmpty(CardText.Value))
			{
				_filteredArchetypes = _filteredArchetypes.Where(x => x.HasCardText(CardText.Value)).ToList();
			}

			_report = new MetaReport(cardsByName, _cardStats, cardsById, _playerInventoryCounts, _archetypes, RotationProof.Value, _setNameTranslations);

			if(sort)
			{
				SortArchetypes();
			}
		}

		/// <summary>
		/// Generate replacement suggestions for a deck archetype.
		/// </summary>
		/// <param name="archetype">The deck to generate replacements suggestions for.</param>
		/// <param name="mainDeckToCollect">The main deck cards for the deck that the player hasn't collected yet.</param>
		/// <param name="sideboardToCollect">The sideboard cards for the deck that the player hasn't collected yet.</param>
		/// <param name="commandZoneToCollect">The command zone cards for the deck that the player hasn't collected yet.</param>
		/// <param name="playerInventory">The player's card inventory/collection.</param>
		/// <param name="cardsByName">A dictionary mapping card names to lists of Card objects.</param>
		/// <param name="cardsById">A dictioanry mapping arena ids to Card objects.</param>
		/// <returns>A list of Tuple&lt;int, int, int&gt; with Item1 = card to be replaced, Item2 = suggested replacement, Item3 = number of times to make this replacement</returns>
		private List<Tuple<int, int, int>> GenerateReplacements(Archetype archetype, Dictionary<int, int> mainDeckToCollect, Dictionary<int, int> sideboardToCollect,
			Dictionary<int, int> commandZoneToCollect, Dictionary<int, int> playerInventory, IDictionary<string, List<Card>> cardsByName, IDictionary<int, Card> cardsById,
			DeckFilters filters)
		{
			Logger.Debug("GenerateReplacements() Called for Archetype {archetypeName}", archetype.Name);

			playerInventory = playerInventory.Where(
				x => (!filters.HideCommon || filters.CommonCount > 0 || cardsById[x.Key].Rarity != CardRarity.Common) &&
					(!filters.HideUncommon || filters.UncommonCount > 0 || cardsById[x.Key].Rarity != CardRarity.Uncommon) &&
					(!filters.HideRare || filters.RareCount > 0 || cardsById[x.Key].Rarity != CardRarity.Rare) &&
					(!filters.HideMythic || filters.MythicCount > 0 || cardsById[x.Key].Rarity != CardRarity.MythicRare)
			).ToDictionary(x => x.Key, x => x.Value);

			List<Tuple<int, int, int>> suggestedReplacements = new List<Tuple<int, int, int>>();
			CardColors identity = null;
			if (Format.Value == Properties.Resources.Item_Brawl)
			{
				identity = archetype.CommanderColorIdentity;
			}
			var replacementsToFind = mainDeckToCollect.Concat(sideboardToCollect).Concat(commandZoneToCollect).GroupBy(x => x.Key).
				Select(x => new { Id = x.Key, Count = x.Sum(y => y.Value) });
			foreach (var find in replacementsToFind)
			{
				Logger.Debug("Generating replacements suggestions for card with Arena Id: {0} (need {1})", find.Id, find.Count);
				var cardToReplace = cardsById[find.Id];
				var replacementsNeeded = find.Count;

				if (cardToReplace.Type.Contains("Land"))
				{
					Logger.Debug("Processing Candidates based on Color");
					try
					{
						var candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
							Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && _colorsByLand.ContainsKey(x.Card.Name) && _colorsByLand[x.Card.Name] == _colorsByLand[cardToReplace.Name] && (identity == null || identity.Contains(x.Card.ColorIdentity))).
							OrderByDescending(x => x.Card.Rank + CardStats.GetAssociationModifier(cardToReplace.Name, x.Card.Name, _cardStats));

						foreach (var candidate in candidates)
						{
							if (replacementsNeeded == 0) { break; }
							if (candidate.Quantity > replacementsNeeded)
							{
								suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, replacementsNeeded));
								playerInventory[candidate.Card.ArenaId] -= replacementsNeeded;
								replacementsNeeded = 0;
							}
							else
							{
								suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, candidate.Quantity));
								playerInventory[candidate.Card.ArenaId] = 0;
								replacementsNeeded -= candidate.Quantity;
							}
						}
					}
					catch(KeyNotFoundException e)
					{
						string colorsByLandString = string.Join(Environment.NewLine, _colorsByLand.Select(x => x.Key + ": " + x.Value.ToString()));
						string candidatesString = string.Join(Environment.NewLine, playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
							Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type).Select(x => x.Card.Name));
						throw new KeyNotFoundException($"KeyNotFoundException while Processing Candidates based on Color: cardToReplace={cardToReplace.Name}, _colorsByLand={Environment.NewLine}{colorsByLandString}{Environment.NewLine}candidates={Environment.NewLine}{candidatesString}", e);
					}

					if (replacementsNeeded > 0)
					{
						Logger.Debug("Insufficient Candidates Found, suggesting basic land replacements");
						Random r = new Random();
						// randomizing colors here so we don't always favor colors in WUBRG order
						string[] colors = (_colorsByLand[cardToReplace.Name] == null ?
							new string[] { "W", "U", "B", "R", "G" }.Select(x => new { Sort = r.Next(), Value = x }) :
							_colorsByLand[cardToReplace.Name].ColorString.Select(x => new { Sort = r.Next(), Value = x.ToString() })).
							OrderBy(y => y.Sort).Select(z => z.Value).ToArray();
						Dictionary<string, int> _colorReplacements = new Dictionary<string, int>();
						if (colors.Length > 0)
						{
							int colorIndex = 0;
							while (replacementsNeeded > 0)
							{
								if (!_colorReplacements.ContainsKey(colors[colorIndex]))
								{
									_colorReplacements[colors[colorIndex]] = 0;
								}
								_colorReplacements[colors[colorIndex]]++;
								replacementsNeeded--;
								colorIndex = (colorIndex + 1) % colors.Length;
							}
							suggestedReplacements.AddRange(
								_colorReplacements.Select(x => new Tuple<int, int, int>(cardToReplace.ArenaId, _basicLands[x.Key].ArenaId, x.Value))
							);
						}
					}

					if (replacementsNeeded > 0)
					{
						Logger.Debug("Insufficient Candidates Found, done looking");
					}
				}
				else
				{
					bool isCommander = archetype.CommandZone.ContainsKey(cardToReplace.Name);

					Logger.Debug("Processing Candidates based on Type and Cost");
					var candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
							Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && x.Card.Cost == cardToReplace.Cost && (identity == null || (isCommander && identity == x.Card.ColorIdentity && x.Card.Type.Contains("Legendary")) || (!isCommander && identity.Contains(x.Card.ColorIdentity)))).
							OrderByDescending(x => x.Card.Rank + CardStats.GetAssociationModifier(cardToReplace.Name, x.Card.Name, _cardStats));

					foreach (var candidate in candidates)
					{
						if (replacementsNeeded == 0) { break; }
						if (candidate.Quantity > replacementsNeeded)
						{
							suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, replacementsNeeded));
							playerInventory[candidate.Card.ArenaId] -= replacementsNeeded;
							replacementsNeeded = 0;
						}
						else
						{
							suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, candidate.Quantity));
							playerInventory[candidate.Card.ArenaId] = 0;
							replacementsNeeded -= candidate.Quantity;
						}
					}

					if (replacementsNeeded > 0)
					{
						Logger.Debug("Insufficient Candidates Found, Getting Candidates by Cost");
						candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
							Where(x => x.Quantity > 0 && x.Card.Cost == cardToReplace.Cost && (identity == null || (isCommander && identity == x.Card.ColorIdentity && x.Card.Type.Contains("Legendary")) || (!isCommander && identity.Contains(x.Card.ColorIdentity)))).
							OrderByDescending(x => x.Card.Rank + CardStats.GetAssociationModifier(cardToReplace.Name, x.Card.Name, _cardStats));

						foreach (var candidate in candidates)
						{
							if (replacementsNeeded == 0) { break; }
							if (candidate.Quantity > replacementsNeeded)
							{
								suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, replacementsNeeded));
								playerInventory[candidate.Card.ArenaId] -= replacementsNeeded;
								replacementsNeeded = 0;
							}
							else
							{
								suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, candidate.Quantity));
								playerInventory[candidate.Card.ArenaId] = 0;
								replacementsNeeded -= candidate.Quantity;
							}
						}
					}

					if (replacementsNeeded > 0)
					{
						Logger.Debug("Insufficient Candidates Found, Getting Candidates by Cmc and Color");
						candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
							Where(x => x.Quantity > 0 && x.Card.Cmc == cardToReplace.Cmc && x.Card.Colors == cardToReplace.Colors && (identity == null || (isCommander && identity == x.Card.ColorIdentity && x.Card.Type.Contains("Legendary")) || (!isCommander && identity.Contains(x.Card.ColorIdentity)))).
							OrderByDescending(x => x.Card.Rank + CardStats.GetAssociationModifier(cardToReplace.Name, x.Card.Name, _cardStats));

						foreach (var candidate in candidates)
						{
							if (replacementsNeeded == 0) { break; }
							if (candidate.Quantity > replacementsNeeded)
							{
								suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, replacementsNeeded));
								playerInventory[candidate.Card.ArenaId] -= replacementsNeeded;
								replacementsNeeded = 0;
							}
							else
							{
								suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, candidate.Quantity));
								playerInventory[candidate.Card.ArenaId] = 0;
								replacementsNeeded -= candidate.Quantity;
							}
						}
					}

					if (replacementsNeeded > 0)
					{
						Logger.Debug("Insufficient Candidates Found, Getting Candidates, looking for candidates with lower Cmc");
						int cmcToTest = cardToReplace.Cmc - 1;
						while (replacementsNeeded > 0 && cmcToTest > 0)
						{
							candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
								Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && x.Card.Colors == cardToReplace.Colors && (identity == null || (isCommander && identity == x.Card.ColorIdentity && x.Card.Type.Contains("Legendary")) || (!isCommander && identity.Contains(x.Card.ColorIdentity)))).
								OrderByDescending(x => x.Card.Rank + CardStats.GetAssociationModifier(cardToReplace.Name, x.Card.Name, _cardStats));

							foreach (var candidate in candidates)
							{
								if (replacementsNeeded == 0) { break; }
								if (candidate.Quantity > replacementsNeeded)
								{
									suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, replacementsNeeded));
									playerInventory[candidate.Card.ArenaId] -= replacementsNeeded;
									replacementsNeeded = 0;
								}
								else
								{
									suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, candidate.Quantity));
									playerInventory[candidate.Card.ArenaId] = 0;
									replacementsNeeded -= candidate.Quantity;
								}
							}
							cmcToTest--;
						}
					}

					if (replacementsNeeded > 0)
					{
						Logger.Debug("Insufficient Candidates Found, Getting Candidates, relaxing color requirements and looping through Cmc <= {0}", cardToReplace.Cmc);
						int cmcToTest = cardToReplace.Cmc;
						while (replacementsNeeded > 0 && cmcToTest > 0)
						{
							candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
								Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && cardToReplace.Colors.Contains(x.Card.Colors) && (identity == null || (isCommander && identity == x.Card.ColorIdentity && x.Card.Type.Contains("Legendary")) || (!isCommander && identity.Contains(x.Card.ColorIdentity)))).
								OrderByDescending(x => x.Card.Rank + CardStats.GetAssociationModifier(cardToReplace.Name, x.Card.Name, _cardStats));

							foreach (var candidate in candidates)
							{
								if (replacementsNeeded == 0) { break; }
								if (candidate.Quantity > replacementsNeeded)
								{
									suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, replacementsNeeded));
									playerInventory[candidate.Card.ArenaId] -= replacementsNeeded;
									replacementsNeeded = 0;
								}
								else
								{
									suggestedReplacements.Add(new Tuple<int, int, int>(cardToReplace.ArenaId, candidate.Card.ArenaId, candidate.Quantity));
									playerInventory[candidate.Card.ArenaId] = 0;
									replacementsNeeded -= candidate.Quantity;
								}
							}
							cmcToTest--;
						}
					}

					if (replacementsNeeded > 0)
					{
						Logger.Debug("Insufficient Candidates Found, done looking");
					}
				}
			}

			Logger.Debug("GenerateReplacements() Finished");

			return suggestedReplacements;
		}

		/// <summary>
		/// Get the number of cards from a quantified dictionary of card names of the specified rarity.
		/// </summary>
		/// <param name="cardQuantities">A dictionary mapping card names to quantities.</param>
		/// <param name="rarity">The requested rarity.</param>
		/// <param name="cardsByName">A list mapping card names to a list of card objects.</param>
		/// <returns>The count of cards with the specified rarity.</returns>
		private int RarityCount(IEnumerable<KeyValuePair<string, int>> cardQuantities, CardRarity rarity, ReadOnlyDictionary<string, List<Card>> cardsByName)
		{
			return cardQuantities
				.Where(x => cardsByName[x.Key].Where(y => y.Rarity == rarity).Count() > 0 && cardsByName[x.Key].Where(y => y.Rarity.CompareTo(rarity) < 0).Count() == 0)
				.Sum(z => z.Value);
		}

		/// <summary>
		/// Callback that runs when the main window first loads.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Logger.Debug("Main Window Loaded - {0}", ApplicationName);

			AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
			string assemblyVersion = assemblyName.Version.ToString();
			string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();

			Title = Title + $" - ({assemblyVersion}, {assemblyArchitecture})";

			Logger.Debug("Assembly Version: {0}, Assembly Architecture: {1}", assemblyVersion, assemblyArchitecture);

			this.InitializeState();

			// add propertychanged handlers for various fields
			Format.PropertyChanged += Format_PropertyChanged;
			Sort.PropertyChanged += Sort_PropertyChanged;
			SortDir.PropertyChanged += SortDir_PropertyChanged;
			RotationProof.PropertyChanged += RotationProof_PropertyChanged;
			CardText.PropertyChanged += CardText_PropertyChanged;
			SelectedFontSize.PropertyChanged += SelectedFontSize_PropertyChanged;

			Task loadTask = new Task(() => {
				Logger.Debug("Initializing Card Database");
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
		}

		/// <summary>
		/// Report an Exception to Github.
		/// </summary>
		/// <param name="task">The task that threw the exception.</param>
		/// <param name="method">The method that threw the exception.</param>
		/// <param name="exception">The exception being reported.</param>
		private void ReportException(string task, string method, Exception taskException)
		{
			Logger.Debug("ReportException Called: task={task}, method={method}", task, method);

			ExceptionText.Value = "Unhandled Exception - Sending Data to Server";

			Dispatcher.Invoke(() =>
			{
				LoadingScreen.Visibility = Visibility.Collapsed;
				FilterPanel.Visibility = Visibility.Collapsed;
				DeckTabs.Visibility = Visibility.Collapsed;
				ExceptionScreen.Visibility = Visibility.Visible;
			});

			List<GithubAttachment> attachments = new List<GithubAttachment>();

			var dadaLogFolder = string.Format("{0}Low\\DailyArena\\DailyArenaDeckAdvisor\\logs", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
			var dadaLogFiles = Directory.EnumerateFiles(dadaLogFolder, "log*.txt");
			FileInfo dadaLogFileInfo = dadaLogFiles.Select(x => new FileInfo(x)).OrderByDescending(y => y.LastWriteTimeUtc).First();
			var dadaLogAttachmentName = dadaLogFileInfo.Name;
			string dadaLogContent;
			using (FileStream dadaLogStream = File.Open(dadaLogFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader dadaLogReader = new StreamReader(dadaLogStream))
			{
				dadaLogContent = dadaLogReader.ReadToEndAsync().Result;
			}
			attachments.Add(new GithubAttachment(dadaLogAttachmentName, dadaLogContent));

			var mtgaLogFolder = GetLogFolderLocation();
			string mtgaLogContent = File.ReadAllText(mtgaLogFolder + "\\Player.log");
			attachments.Add(new GithubAttachment("Player.log", mtgaLogContent));

			string nl = Environment.NewLine + Environment.NewLine;

			GithubIssueResponse response = GithubUtilities.CreateNewIssue("daily-arena-deck-advisor", "jceddy", "DailyArenaDeckAdvisor",
				string.Format("Unhandled Exception in {0} ({1} - Main Application)", task, method),
				$"An unhandled exception was detected.{nl}Exception:{nl}{taskException.ToString()}{nl}%{dadaLogAttachmentName}%{nl}%Player.log%",
				new string[] { "automatic" }, ((App)Application.Current).State.Fingerprint, attachments.ToArray(), out string serverResponse, out Exception exception);
			if (serverResponse != null)
			{
				Logger.Debug("Server Response: {serverResponse}", serverResponse);
			}
			if (exception != null)
			{
				Logger.Error(exception, "Exception in {0} ({1} - {2})", "CreateNewIssue", method, ApplicationName);
			}

			if (response == null)
			{
				ExceptionText.Value = Properties.Resources.Message_ErrorSendingData;
			}
			else
			{
				string stateString = response.State == "created" ? Properties.Resources.Message_Created : Properties.Resources.Message_Exists;
				ExceptionText.Value = string.Format("{0} {1} {2}", Properties.Resources.Message_GithubIssue, response.Number, stateString);
				IssueUrl.Value = $"https://github.com/jceddy/DailyArenaDeckAdvisor/issues/{response.Number}";
			}

			Task sleepTask = new Task(() => { Thread.Sleep(10000); });
			sleepTask.Start();
			sleepTask.Wait();

			Dispatcher.Invoke(() => { Close(); });
		}

		/// <summary>
		/// Callback that is triggered when the user selects a different Format from the drop-down on the GUI.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Format_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Logger.Debug("New Format Selected, Format={0}", Format.Value);

			IDeckAdvisorApp application = (IDeckAdvisorApp)Application.Current;
			application.State.LastFormat = Format.Value;
			application.SaveState();

			LoadingText.Value = Properties.Resources.Loading_LoadingCardDatabase;
			LoadingValue.Value = 0;

			Dispatcher.Invoke(() =>
			{
				FilterPanel.Visibility = Visibility.Collapsed;
				DeckTabs.Visibility = Visibility.Collapsed;
				DeckTabs.ItemsSource = null;
				LoadingScreen.Visibility = Visibility.Visible;
			});

			Task loadTask = new Task(() => {
				LoadingValue.Value = 50;
				ReloadAndCrunchAllData();
			});
			loadTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						Logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "loadTask", "Format_PropertyChanged", ApplicationName);

						ReportException("loadTask", "Format_PropertyChanged", t.Exception);
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			loadTask.Start();
		}

		/// <summary>
		/// Method that generates _orderedArchetypes from _archetypes based on format and selected sort options.
		/// </summary>
		private void SortArchetypes()
		{
			Func<Archetype, double> orderFunc = x => 0.0;

			if(Sort.Value == Properties.Resources.Item_BoosterCost)
			{
				orderFunc = x => SortDir.Value == Properties.Resources.Item_Descending ? 0.0 - x.BoosterCostAfterWC : x.BoosterCostAfterWC;
			}
			else if(Sort.Value == Properties.Resources.Item_BoosterCostIgnoringWildcards)
			{
				orderFunc = x => SortDir.Value == Properties.Resources.Item_Descending ? 0.0 - x.BoosterCost : x.BoosterCost;
			}
			else if(Sort.Value == Properties.Resources.Item_BoosterCostIgnoringCollection)
			{
				orderFunc = x => SortDir.Value == Properties.Resources.Item_Descending ? 0.0 - x.TotalBoosterCost : x.TotalBoosterCost;
			}
			else if (Sort.Value == Properties.Resources.Item_DeckScore)
			{
				// default is descending for win rate (highest score on top), most default to ascending (lowest cost on top)
				orderFunc = x => SortDir.Value == Properties.Resources.Item_Ascending ? x.Score : 0.0 - x.Score;
			}
			else if(Sort.Value == Properties.Resources.Item_WinRate)
			{
				// default is descending for win rate (highest win rate on top), most default to ascending (lowest cost on top)
				orderFunc = x => SortDir.Value == Properties.Resources.Item_Ascending ? x.WinRate : 0.0 - x.WinRate;
			}
			else if (Sort.Value == Properties.Resources.Item_MythicRareCount)
			{
				orderFunc = x => SortDir.Value == Properties.Resources.Item_Descending ? 0.0 - x.GetRarityCount(CardRarity.MythicRare) : x.GetRarityCount(CardRarity.MythicRare);
			}
			else if (Sort.Value == Properties.Resources.Item_RareCount)
			{
				orderFunc = x => SortDir.Value == Properties.Resources.Item_Descending ? 0.0 - x.GetRarityCount(CardRarity.Rare) : x.GetRarityCount(CardRarity.Rare);
			}
			else if (Sort.Value == Properties.Resources.Item_UncommonCount)
			{
				orderFunc = x => SortDir.Value == Properties.Resources.Item_Descending ? 0.0 - x.GetRarityCount(CardRarity.Uncommon) : x.GetRarityCount(CardRarity.Uncommon);
			}
			else if (Sort.Value == Properties.Resources.Item_CommonCount)
			{
				orderFunc = x => SortDir.Value == Properties.Resources.Item_Descending ? 0.0 - x.GetRarityCount(CardRarity.Common) : x.GetRarityCount(CardRarity.Common);
			}

			if (Format == Properties.Resources.Item_ArenaStandard)
			{
				// for Arena Standard, use slightly different ordering, favoring deck score over and win rate estimated booster cost
				_orderedArchetypes = _filteredArchetypes.OrderBy(orderFunc).ThenBy(x => x.IsPlayerDeck ? 0 : 1).ThenBy(x => x.BoosterCost == 0 ? 0 : (x.BoosterCostAfterWC == 0 ? 1 : 2)).ThenByDescending(x => x.BoosterCostAfterWC == 0 ? x.Score : x.BoosterCostAfterWC).ThenByDescending(x => x.BoosterCostAfterWC == 0 ? x.WinRate : x.BoosterCostAfterWC).ThenBy(x => x.BoosterCostAfterWC).ThenBy(x => x.BoosterCost);
			}
			else
			{
				_orderedArchetypes = _filteredArchetypes.OrderBy(orderFunc).ThenBy(x => x.IsPlayerDeck ? 0 : 1).ThenBy(x => x.BoosterCostAfterWC).ThenBy(x => x.BoosterCost);
			}

			Dispatcher.Invoke(() =>
			{
				_tabObjects.Clear();
				_tabObjects.Add(_report);

				bool defaultSort = Sort.Value == Properties.Resources.Item_Default;
				bool noWildcardsNeeded = false;
				bool noCardsNeeded = false;
				bool replacementsRequired = false;
				bool playerDecks = false;

				foreach (Archetype archetype in _orderedArchetypes)
				{
					if(defaultSort && !playerDecks && archetype.IsPlayerDeck)
					{
						_tabObjects.Add(new Archetype(Properties.Resources.Tab_PlayerDecks, true, 1, 1));
						playerDecks = true;
					}
					else if(defaultSort && !noCardsNeeded && !archetype.IsPlayerDeck && archetype.BoosterCost == 0)
					{
						_tabObjects.Add(new Archetype(Properties.Resources.Tab_NoCardsNeeded, false, 0, 0));
						noCardsNeeded = true;
					}
					else if(defaultSort && !noWildcardsNeeded && !archetype.IsPlayerDeck && archetype.BoosterCost != 0 && archetype.TotalWildcardsNeeded == 0)
					{
						_tabObjects.Add(new Archetype(Properties.Resources.Tab_NoWildcardsNeeded, false, 1, 0));
						noWildcardsNeeded = true;
					}
					else if(defaultSort && !replacementsRequired && !archetype.IsPlayerDeck && archetype.BoosterCost != 0 && archetype.TotalWildcardsNeeded != 0)
					{
						_tabObjects.Add(new Archetype(Properties.Resources.Tab_ReplacementsRequired, false, 1, 1));
						replacementsRequired = true;
					}

					_tabObjects.Add(archetype);
				}
			});
		}

		/// <summary>
		/// Callback that is triggered when the user selects a different Sort from the drop-down on the GUI.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Sort_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Logger.Debug("New Sort Selected, Sort={0}", Sort.Value);

			App application = (App)Application.Current;
			application.State.LastSort = Sort.Value;
			application.SaveState();

			Task sortTask = new Task(() => {
				SortArchetypes();
			});
			sortTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						Logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "sortTask", "Sort_PropertyChanged", ApplicationName);

						ReportException("sortTask", "Sort_PropertyChanged", t.Exception);
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			sortTask.Start();
		}

		/// <summary>
		/// Callback that is triggered when the user selects a different Sort Dir from the drop-down on the GUI.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void SortDir_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Logger.Debug("New Sort Dir Selected, SortDir={0}", SortDir.Value);

			App application = (App)Application.Current;
			application.State.LastSortDir = SortDir.Value;
			application.SaveState();

			Task sortTask = new Task(() => {
				SortArchetypes();
			});
			sortTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						Logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "sortTask", "SortDir_PropertyChanged", ApplicationName);

						ReportException("sortTask", "SortDir_PropertyChanged", t.Exception);
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			sortTask.Start();
		}

		/// <summary>
		/// Object used to ensure that two instances of filterTask don't run at the same time.
		/// </summary>
		private object _filterTaskLockObject = new object();

		/// <summary>
		/// Apply archetype filters.
		/// </summary>
		/// <param name="filters">Filters to apply.</param>
		public void ApplyFilters(DeckFilters filters)
		{
			Logger.Debug("ApplyFilters() Called");

			Task filterTask = new Task(() => {
				lock (_filterTaskLockObject)
				{
					FilterArchetypes(filters, true);
				}
			});
			filterTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						Logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "filterTask", "ApplyFilters", ApplicationName);

						ReportException("filterTask", "ApplyFilters", t.Exception);
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			filterTask.Start();
		}

		/// <summary>
		/// Callback that is triggered when the user changes the value in the card text filter box.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void CardText_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Logger.Debug("CardText Changed, CardText={0}", CardText.Value);

			App application = (App)Application.Current;
			application.State.CardTextFilter = CardText.Value;
			application.SaveState();

			ApplyFilters(application.State.Filters);
		}

		/// <summary>
		/// When the user hit enter in the text box, update the text value binding.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				TextBox tBox = (TextBox)sender;
				DependencyProperty prop = TextBox.TextProperty;

				BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
				if (binding != null) { binding.UpdateSource(); }
			}
		}

		/// <summary>
		/// Callback that is triggered when the user switches the Rotation toggle on the GUI.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void RotationProof_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Value")
			{
				Logger.Debug("Rotation Proof toggled, RotationProof={0}", RotationProof.Value);

				App application = (App)Application.Current;
				application.State.RotationProof = RotationProof.Value;
				application.SaveState();

				LoadingText.Value = Properties.Resources.Loading_LoadingCardDatabase;
				LoadingValue.Value = 0;

				Dispatcher.Invoke(() =>
				{
					FilterPanel.Visibility = Visibility.Collapsed;
					DeckTabs.Visibility = Visibility.Collapsed;
					DeckTabs.ItemsSource = null;
					LoadingScreen.Visibility = Visibility.Visible;
				});

				Task loadTask = new Task(() => {
					LoadingValue.Value = 50;
					ReloadAndCrunchAllData();
				});
				loadTask.ContinueWith(t =>
					{
						if (t.Exception != null)
						{
							Logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "loadTask", "RotationProof_PropertyChanged", ApplicationName);

							ReportException("loadTask", "RotationProof_PropertyChanged", t.Exception);
						}
					},
					TaskContinuationOptions.OnlyOnFaulted
				);
				loadTask.Start();
			}
		}

		/// <summary>
		/// Callback that is triggered when the user changes the font size selection in the Settings popup.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void SelectedFontSize_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Logger.Debug("New Font Size Selected, FontSize={0}", SelectedFontSize.Value);

			App application = (App)Application.Current;
			application.State.FontSize = SelectedFontSize.Value;
			application.SaveState();
		}

		/// <summary>
		/// Callback that is triggered when the user clicks the "Export Deck" button on the deck details GUI.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Export_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var button = (Button)sender;
				var item = (Archetype)button.DataContext;

				Logger.Debug("Deck Export Clicked, Deck={0}", item.Name);

				Clipboard.SetText(item.ExportList);
				MessageBox.Show($"{item.Name} {Properties.Resources.Message_ExportSuccessful}", "Export");
			}
			catch(Exception ex)
			{
				Logger.Error(ex, "Exception in Export_Clicked");
				if(!(ex is ExternalException || ex is ArgumentNullException))
				{
					throw;
				}
			}
		}

		/// <summary>
		/// Callback that is triggered when the user clicks the "Export w/Replacements" button on the deck details GUI.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void ExportSuggested_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var button = (Button)sender;
				var item = (Archetype)button.DataContext;

				Logger.Debug("Deck Export Suggested Clicked, Deck={0}", item.Name);

				Clipboard.SetText(item.ExportListSuggested);
				MessageBox.Show($"{item.Name} {Properties.Resources.Message_ExportReplacementsSuccessful}", "Export");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Exception in ExportSuggested_Click");
				if (!(ex is ExternalException || ex is ArgumentNullException))
				{
					throw;
				}
			}
		}

		/// <summary>
		/// Callback that is triggered when the user clicks an archetype hyperlink on the Meta Report detail screen, focuses the selected archetype's tab.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			Hyperlink link = (Hyperlink)sender;
			var item = (Archetype)link.DataContext;

			Logger.Debug("Deck Hyperlink Clicked, Deck={0}", item.Name);

			DeckTabs.SelectedItem = item;
		}

		/// <summary>
		/// Callback that is triggered when the user clicks an similar deck hyperlink on the Archetype detail screen, replaces the deck details with the alternate configuration.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void SimilarDeck_Click(object sender, RoutedEventArgs e)
		{
			Hyperlink link = (Hyperlink)sender;
			var item = (Archetype)link.DataContext;

			Logger.Debug("Similar Deck Hyperlink Clicked, Deck={0}", item.Name);

			int selectedIndex = DeckTabs.SelectedIndex;
			_tabObjects[selectedIndex] = item;
			DeckTabs.SelectedIndex = selectedIndex;
		}

		/// <summary>
		/// Callback that is triggered when the user clicks the parent deck hyperlink on the Archetype detail screen, replaces the deck details with the main deck configuration.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void ParentDeck_Click(object sender, RoutedEventArgs e)
		{
			Hyperlink link = (Hyperlink)sender;
			var item = (Archetype)link.DataContext;

			Logger.Debug("Parent Deck Hyperlink Clicked, Deck={0}", item.Name);

			int selectedIndex = DeckTabs.SelectedIndex;
			_tabObjects[selectedIndex] = item.Parent;
			DeckTabs.SelectedIndex = selectedIndex;
		}

		/// <summary>
		/// Refresh the main window contents.
		/// </summary>
		/// <param name="hardRefresh">If true, check server for updates before refreshing.</param>
		public void Refresh(bool hardRefresh)
		{
			Logger.Debug("Refresh({hardRefresh})", hardRefresh);

			LoadingText.Value = Properties.Resources.Loading_LoadingCardDatabase;
			LoadingValue.Value = 0;

			Dispatcher.Invoke(() =>
			{
				FilterPanel.Visibility = Visibility.Collapsed;
				DeckTabs.Visibility = Visibility.Collapsed;
				DeckTabs.ItemsSource = null;
				LoadingScreen.Visibility = Visibility.Visible;
			});

			Task loadTask = new Task(() => {
				if(hardRefresh)
				{
					Logger.Debug("Initializing Card Database");
					CardDatabase.Initialize(false);
					LoadingValue.Value = 20;

					PopulateColorsByLand();
					LoadingValue.Value = 30;

					PopulateStandardBannings();
					LoadingValue.Value = 40;
				}

				LoadingValue.Value = 50;
				ReloadAndCrunchAllData();
			});
			loadTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						Logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "loadTask", "Refresh", ApplicationName);

						ReportException("loadTask", "Refresh", t.Exception);
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			loadTask.Start();
		}

		/// <summary>
		/// Callback that is triggered when the user clicks the refresh button the GUI. (Only re-loads user log information, doesn't check for new data on server.)
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			Logger.Debug("Refresh Clicked");
			Refresh(false);
		}

		/// <summary>
		/// Callback that is triggered when the user clicks the "hard" refresh button the GUI. (Pulls new data from server, if needed.)
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void HardRefresh_Click(object sender, RoutedEventArgs e)
		{
			Logger.Debug("Hard Refresh Clicked");
			Refresh(true);
		}

		/// <summary>
		/// Callback that is triggered when the user clicks the settings button the GUI. Opens the settings popup.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			SettingsDialog settingsDialog = new SettingsDialog()
			{
				Owner = this
			};
			settingsDialog.ShowDialog();
		}

		/// <summary>
		/// Method to query a configuration setting.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns>The setting value.</returns>
		public string GetConfigurationSetting(string key)
		{
			Logger.Debug("GetConfigurationSetting() Called - {key} ({ApplicationName})", key, ApplicationName);

			try
			{
				var appSettings = ConfigurationManager.AppSettings;
				if (appSettings == null)
				{
					return null;
				}
				else
				{
					return appSettings[key];
				}
			}
			catch (ConfigurationErrorsException e)
			{
				Logger.Error(e, "Exception in GetConfigurationSetting()");
				return null;
			}
		}

		/// <summary>
		/// Gets the bitmap scaling mode.
		/// </summary>
		/// <returns>Fant, or whatever override value the user set in App.config.</returns>
		private BitmapScalingMode GetBitmapScalingMode()
		{
			Logger.Debug("GetBitmapScalingMode() Called - {0}", ApplicationName);

			string bitmapScalingMode = GetConfigurationSetting("BitmapScalingMode") ?? "Fant";

			switch (bitmapScalingMode)
			{
				case "Fant":
					Logger.Debug("Using BitmapScalingMode {0}", "Fant");
					return BitmapScalingMode.Fant;
				case "HighQuality":
					Logger.Debug("Using BitmapScalingMode {0}", "HighQuality");
					return BitmapScalingMode.HighQuality;
				case "Linear":
					Logger.Debug("Using BitmapScalingMode {0}", "Linear");
					return BitmapScalingMode.Linear;
				case "LowQuality":
					Logger.Debug("Using BitmapScalingMode {0}", "LowQuality");
					return BitmapScalingMode.LowQuality;
				case "NearestNeighbor":
					Logger.Debug("Using BitmapScalingMode {0}", "NearestNeighbor");
					return BitmapScalingMode.NearestNeighbor;
				default:
					return BitmapScalingMode.Fant;
			}
		}

		/// <summary>
		/// Gets the location of the folder that contains the MTGA real-time log output.
		/// </summary>
		/// <returns>The default location in the current user's AppData, unless there is an override value set in App.config.</returns>
		private string GetLogFolderLocation()
		{
			Logger.Debug("GetLogFolderLocation() Called - {0}", ApplicationName);
			var logFolder = string.Format("{0}Low\\Wizards of the Coast\\MTGA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

			string mtgaLogFolder = GetConfigurationSetting("MTGALogFolder");

			if(mtgaLogFolder != null)
			{
				logFolder = mtgaLogFolder;
			}

			Logger.Debug("Using Log Folder Location: {0}", logFolder);
			return logFolder;
		}

		/// <summary>
		/// Callback that is triggered when the user clicks the filters button the GUI. Opens the filters popup.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void ShowFilters_Click(object sender, RoutedEventArgs e)
		{
			Logger.Debug("ShowFilters_Click() Called - {0}", ApplicationName);
			FiltersDialog filtersDialog = new FiltersDialog()
			{
				Owner = this
			};
			filtersDialog.ShowDialog();
		}

		/// <summary>
		/// Callback that is triggered when the Github issue link is clocked.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Logger.Debug("Hyperlink_RequestNavigate() Called - {0}", ApplicationName);
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}

		/// <summary>
		/// Handle clicks of the Close Button.
		/// </summary>
		/// <param name="sender">The button that was clicked.</param>
		/// <param name="e">The routed event arguments.</param>
		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Logger.Debug("Close_Click() Called - {0}", ApplicationName);
			Close();
		}

		/// <summary>
		/// Method to get a localized string based on the resource name.
		/// </summary>
		/// <param name="name">The resource name to get a localized string for.</param>
		/// <returns>The localized string for the requested resource name.</returns>
		public string GetLocalizedString(string name)
		{
			return Properties.Resources.ResourceManager.GetString(name);
		}
	}
}
