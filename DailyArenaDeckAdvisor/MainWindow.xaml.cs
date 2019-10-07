using DailyArena.Common.Bindable;
using DailyArena.Common.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// The list of Archetypes pulled down from the server.
		/// </summary>
		List<Archetype> _archetypes = new List<Archetype>();

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
		/// List of deck archetypes, ordered by the estimated "booster cost" of each deck after wildcards, and then byt the average "booster cost" disregarding wildcards.
		/// </summary>
		IOrderedEnumerable<Archetype> _orderedArchetypes;

		/// <summary>
		/// A list of names of cards in Arena that allow a player to include any number of copies.
		/// </summary>
		List<string> _anyNumber = new List<string>() { "Persistent Petitioners", "Rat Colony" };

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
			{ "Forest", "G" }
		};

		/// <summary>
		/// A reference to the application logger.
		/// </summary>
		ILogger _logger;

		/// <summary>
		/// The selected format being viewed.
		/// </summary>
		public Bindable<string> Format { get; private set; } = new Bindable<string>();

		/// <summary>
		/// The state of the "Rotation" toggle button.
		/// </summary>
		public BindableBool RotationProof { get; private set; } = new BindableBool();

		/// <summary>
		/// The selected font size for the display.
		/// </summary>
		public Bindable<int> SelectedFontSize { get; private set; } = new Bindable<int>() { Value = 12 };

		/// <summary>
		/// The message to show on the loading screen.
		/// </summary>
		public Bindable<string> LoadingText { get; private set; }

		/// <summary>
		/// The value for the loading progress bar.
		/// </summary>
		public Bindable<int> LoadingValue { get; private set; } = new Bindable<int>();

		/// <summary>
		/// The Bitmap Scaling Mode (can be overridden in App.config for users with funky video card/driver combinations that cause crashing under high-quality scaling.
		/// </summary>
		public BitmapScalingMode BitmapScalingMode { get; private set; }

		/// <summary>
		/// Default constructor. Sets the logger and scaling mode, and does GUI initialization stuff.
		/// </summary>
		public MainWindow()
		{
			App application = (App)Application.Current;
			_logger = application.Logger;
			_logger.Debug("Main Window Constructor Called - {0}", "Main Application");

			SetCulture();

			// these have to happen after SetCulture()
			_formatMappings = new Dictionary<string, Tuple<string, string>>()
			{
				{ Properties.Resources.Item_Standard, new Tuple<string, string>("standard", "Standard") },
				{ Properties.Resources.Item_ArenaStandard, new Tuple<string, string>("arena_standard", "ArenaStandard") },
				{ Properties.Resources.Item_Brawl, new Tuple<string, string>("brawl", "Brawl") },
				{ Properties.Resources.Item_Historic_Bo3, new Tuple<string, string>("historic_bo3", "Historic") },
				{ Properties.Resources.Item_Historic_Bo1, new Tuple<string, string>("historic_bo1", "Historic") }
			};

			LoadingText = new Bindable<string>() { Value = Properties.Resources.Loading_LoadingCardDatabase };

			BitmapScalingMode = GetBitmapScalingMode();

			InitializeComponent();
			DataContext = this;

			new Task(() => { SendUsageStats(); }).Start();
		}

		/// <summary>
		/// Send usage stats to server.
		/// </summary>
		private void SendUsageStats()
		{
			using (WebClient wc = new WebClient())
			{
				try
				{
					AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
					string assemblyVersion = assemblyName.Version.ToString();
					string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();

					NameValueCollection inputs = new NameValueCollection
					{
						{ "application", "DailyArenaDeckAdvisor" },
						{ "fingerprint", ((App)Application.Current).State.Fingerprint.ToString() },
						{ "version", assemblyVersion },
						{ "architecture", assemblyArchitecture }
					};

					string response = Encoding.UTF8.GetString(wc.UploadValues("https://clans.dailyarena.net/usage_stats.php", "POST", inputs));
					_logger.Debug("Usage Statistics Response: {response}", response);
				}
				catch (WebException e)
				{
					_logger.Error(e, "Exception in SendUsageStats(), ignoring");
				}
			}
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
		/// Loads a list of nonbasic lands in Standard, using the local cached if it's up to date, otherwise refreshing the local cache from the server.
		/// </summary>
		/// <returns>The updated cache timestamp in a sortable string format.</returns>
		private string LoadStandardLands()
		{
			_logger.Debug("LoadStandardLands() Called");
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
					_logger.Error(e, "Exception in LoadStandardLands() - need to re-download");
				}
			}

			_logger.Debug("LoadStandardLands() Finished - cacheTimestamp={0)", cacheTimestamp);
			return cacheTimestamp;
		}

		/// <summary>
		/// Save data on nonbasic lands in Standard to the local cache file.
		/// </summary>
		/// <param name="lastUpdate">The latest timestamp for the standard lands data that was pulled from the server.</param>
		public void SaveStandardLands(string lastUpdate)
		{
			_logger.Debug("SaveStandardLands() Called - lastUpdate={0}", lastUpdate);
			var data = new
			{
				LastUpdate = lastUpdate,
				ColorsByLand = _colorsByLand
			};

			string json = JsonConvert.SerializeObject(data);
			File.WriteAllText("lands.json", json);
			_logger.Debug("SaveStandardLands() Finished");
		}

		/// <summary>
		/// Populate the _colorsByLand dictionary.
		/// </summary>
		private void PopulateColorsByLand()
		{
			_logger.Debug("PopulateColorsByLand() Called");
			string serverUpdateTime = CardDatabase.GetServerTimestamp("StandardLands");
			string cacheTimestamp = LoadStandardLands();

			if (string.Compare(serverUpdateTime, cacheTimestamp) > 0)
			{
				var landsUrl = $"https://clans.dailyarena.net/standard_lands.json?_c={Guid.NewGuid()}";
				var landsRequest = WebRequest.Create(landsUrl);
				landsRequest.Method = "GET";

				try
				{
					using (var landsResponse = landsRequest.GetResponse())
					{
						_colorsByLand.Clear();

						using (Stream landsResponseStream = landsResponse.GetResponseStream())
						using (StreamReader landsResponseReader = new StreamReader(landsResponseStream))
						{
							var result = landsResponseReader.ReadToEnd();
							dynamic json = JToken.Parse(result.ToString());
							foreach (dynamic lands in json)
							{
								string landName = (string)lands["Name"];
								if (!_colorsByLand.ContainsKey(landName))
								{
									_colorsByLand.Add((string)lands["Name"], CardColors.CardColorFromString((string)lands["Colors"]));
								}
							}
						}
					}

					SaveStandardLands(serverUpdateTime);
				}
				catch (WebException) { /* if we didn't find the file, ignore it */ }
			}
			_logger.Debug("PopulateColorsByLand() Finished");
		}

		/// <summary>
		/// Loads a dictionary of set name translations, using the local cached if it's up to date, otherwise refreshing the local cache from the server.
		/// </summary>
		/// <returns>The updated cache timestamp in a sortable string format.</returns>
		private string LoadSetTranslations()
		{
			_logger.Debug("LoadSetTranslations() Called");
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
					_logger.Error(e, "Exception in LoadSetTranslations() - need to re-download");
				}
			}

			_logger.Debug("LoadSetTranslations() Finished - cacheTimestamp={0)", cacheTimestamp);
			return cacheTimestamp;
		}

		/// <summary>
		/// Save the set name translation dictionary to the local cache file.
		/// </summary>
		/// <param name="lastUpdate">The latest timestamp for the set name translation file that was pulled from the server.</param>
		public void SaveSetTranslations(string lastUpdate)
		{
			_logger.Debug("SaveSetTranslations() Called - lastUpdate={0}", lastUpdate);
			var data = new
			{
				LastUpdate = lastUpdate,
				Translations = _setNameTranslations
			};

			string json = JsonConvert.SerializeObject(data);
			File.WriteAllText("set_name_translations.json", json);
			_logger.Debug("SaveSetTranslations() Finished");
		}

		/// <summary>
		/// Populate the _setNameTranslations dictionary.
		/// </summary>
		private void PopulateSetTranslations()
		{
			_logger.Debug("PopulateSetTranslations() Called");
			string serverUpdateTime = CardDatabase.GetServerTimestamp("SetTranslations");
			string cacheTimestamp = LoadSetTranslations();

			if (string.Compare(serverUpdateTime, cacheTimestamp) > 0)
			{
				var setTranslationsUrl = $"https://clans.dailyarena.net/set_name_translations.json?_c={Guid.NewGuid()}";
				var setTranslationsRequest = WebRequest.Create(setTranslationsUrl);
				setTranslationsRequest.Method = "GET";

				try
				{
					using (var setTranslationsResponse = setTranslationsRequest.GetResponse())
					{
						_setNameTranslations.Clear();

						using (Stream setTranslationsResponseStream = setTranslationsResponse.GetResponseStream())
						using (StreamReader setTranslationsResponseReader = new StreamReader(setTranslationsResponseStream))
						{
							var result = setTranslationsResponseReader.ReadToEnd();
							dynamic json = JToken.Parse(result.ToString());
							_setNameTranslations = json.ToObject<Dictionary<string, Dictionary<string, string>>>();
						}
					}

					SaveSetTranslations(serverUpdateTime);
				}
				catch (WebException) { /* if we didn't find the file, ignore it */ }
			}
			_logger.Debug("PopulateSetTranslations() Finished");
		}

		/// <summary>
		/// A dictionary mapping the Format names shown on the GUI drop-down to the name to use when querying archetype data from the server.
		/// </summary>
		private Dictionary<string, Tuple<string, string>> _formatMappings;

		/// <summary>
		/// Reload all of the player data from the Arena real-time logs and recompute all of the app data.
		/// </summary>
		private void ReloadAndCrunchAllData()
		{
			_logger.Debug("ReloadAndCrunchAllData() Called");

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

			_logger.Debug("Card Database Loaded with {0} Cards", cardsById.Count);

			LoadingText.Value = Properties.Resources.Loading_LoadingDeckArchetypes;
			LoadingValue.Value = 0;

			_logger.Debug("Loading Archetype Data");

			LoadingValue.Value = 25;

			Tuple<string, string> mappedFormat = _formatMappings[Format.Value];
			JToken decksJson = null;
			string serverTimestamp = CardDatabase.GetServerTimestamp($"{mappedFormat.Item2}Decks");
			bool loadDecksFromServer = true;
			if(File.Exists($"{mappedFormat.Item1}_decks.json"))
			{
				_logger.Debug("Cached decks file exists ({mappedFormat}_decks.json)", mappedFormat.Item1);
				string content = File.ReadAllText($"{mappedFormat.Item1}_decks.json");
				using (StringReader contentStringReader = new StringReader(content))
				using (JsonTextReader contentJsonReader = new JsonTextReader(contentStringReader) { DateParseHandling = DateParseHandling.None })
				{
					dynamic json = JToken.ReadFrom(contentJsonReader);
					string decksTimestamp = json["Timestamp"];
					if (!(serverTimestamp == null || string.Compare(serverTimestamp, decksTimestamp) > 0))
					{
						// use cached decks
						_logger.Debug("Cached decks file is up to date, use cached deck data (skip server download)");
						decksJson = json["Decks"];
						loadDecksFromServer = false;
					}
				}
			}
			if (loadDecksFromServer)
			{
				_logger.Debug("Loading deck data from server");
				var archetypesUrl = $"https://clans.dailyarena.net/{mappedFormat.Item1}_decks.json?_c={Guid.NewGuid()}";
				var archetypesRequest = WebRequest.Create(archetypesUrl);
				archetypesRequest.Method = "GET";

				try
				{
					using (var archetypesResponse = archetypesRequest.GetResponse())
					{
						using (Stream archetypesResponseStream = archetypesResponse.GetResponseStream())
						using (StreamReader archetypesResponseReader = new StreamReader(archetypesResponseStream))
						{
							var result = archetypesResponseReader.ReadToEnd();
							using (StringReader resultStringReader = new StringReader(result))
							using (JsonTextReader resultJsonReader = new JsonTextReader(resultStringReader) { DateParseHandling = DateParseHandling.None })
							{
								decksJson = JToken.ReadFrom(resultJsonReader);
								_logger.Debug("Writing file {mappedFormat}_decks.json", mappedFormat.Item1);
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
				}
				catch (WebException e)
				{
					_logger.Error(e, "Web Exception while downloading decks file for {mappedFormat}", mappedFormat.Item1);
				}
			}

			LoadingValue.Value = 50;

			_logger.Debug("Parsing decksJson");
			foreach (dynamic archetype in decksJson)
			{
				bool badDeckDefinition = false;
				string name = archetype["deck_name"];
				_logger.Debug("Deck Name: {name}", name);
				Dictionary<string, int> mainDeck = new Dictionary<string, int>();
				Dictionary<string, int> sideboard = new Dictionary<string, int>();
				if(RotationProof.Value)
				{
					// check whether there are any cards in the deck that aren't rotation-proof...if so, ignore this deck
					_logger.Debug(@"Doing ""rotation-proof"" check...");
					bool ignoreDeck = false;
					foreach (dynamic card in archetype["deck_list"])
					{
						string cardName = (string)card["name"];
						_logger.Debug("Checking main deck card: {cardName}", cardName);

						try
						{
							if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
							{
								_logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
						}
						catch(KeyNotFoundException e)
						{
							Log.Error(e, "Card not found: {cardName}", cardName);
							cardName = Regex.Split(cardName, " // ")[0];
							if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
							{
								_logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
						}
					}

					if(!ignoreDeck)
					{
						foreach (dynamic card in archetype["sideboard"])
						{
							string cardName = (string)card["name"];

							_logger.Debug("Checking sideboard card: {cardName}", cardName);

							if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
							{
								_logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
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
					_logger.Debug(@"Doing Standard legality check...");
					bool ignoreDeck = false;
					foreach (dynamic card in archetype["deck_list"])
					{
						string cardName = (string)card["name"];
						_logger.Debug("Checking main deck card: {cardName}", cardName);

						try
						{
							if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
							{
								_logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
								ignoreDeck = true;
								break;
							}
						}
						catch (KeyNotFoundException e)
						{
							Log.Error(e, "Card not found: {cardName}", cardName);
							cardName = Regex.Split(cardName, " // ")[0];
							if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
							{
								_logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
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

							_logger.Debug("Checking sideboard card: {cardName}", cardName);

							if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
							{
								_logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
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
				foreach (dynamic card in archetype["deck_list"])
				{
					string cardName = (string)card["name"];
					int cardQuantity = (int)card["quantity"];

					_logger.Debug("Processing main deck card: {cardName}, {cardQuantity}", cardName, cardQuantity);
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
						_logger.Error(e, "Card name not found in cardsByName: {cardName}", cardName);
						if (cardName.Contains("//"))
						{
							cardName = Regex.Split(cardName, " // ")[0];
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
						_logger.Debug("Bad deck, definition, skipping");
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

					_logger.Debug("Processing sideboard card: {cardName}, {cardQuantity}", cardName, cardQuantity);
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
				var combinedCounts = mainDeck.Concat(sideboard).GroupBy(x => x.Key).Select(x => new { Name = x.Key, Count = x.Sum(y => y.Value) });
				foreach (var cardCount in combinedCounts)
				{
					foreach (Card archetypeCard in cardsByName[cardCount.Name])
					{
						CardStats stats = _cardStats[archetypeCard];
						stats.MaxCopies = Math.Max(stats.MaxCopies, Math.Min(4, cardCount.Count));
					}
				}
				int win = archetype["win"] == null ? -1 : (int)archetype["win"];
				int loss = archetype["loss"] == null ? -1 : (int)archetype["loss"];

				List<Archetype> similarDecks = new List<Archetype>();
				if(archetype["similar_decks"] != null)
				{
					_logger.Debug("Alternate deck configuration found, processing...");
					foreach (dynamic similarDeck in archetype["similar_decks"])
					{
						Dictionary<string, int> similarMainDeck = new Dictionary<string, int>();
						Dictionary<string, int> similarSideboard = new Dictionary<string, int>();

						foreach (dynamic card in similarDeck["deck"]["deck_list"])
						{
							string cardName = (string)card["name"];
							int cardQuantity = (int)card["quantity"];
							_logger.Debug("Processing alternate main deck card: {cardName}, {cardQuantity}", cardName, cardQuantity);

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
							_logger.Debug("Processing alternate sideboard card: {cardName}, {cardQuantity}", cardName, cardQuantity);

							foreach (Card archetypeCard in cardsByName[cardName])
							{
								CardStats stats = _cardStats[archetypeCard];
								if (!similarMainDeck.ContainsKey(cardName))
								{
									stats.DeckCount++;
								}
								stats.TotalCopies += cardQuantity;
							}

							similarSideboard.Add(cardName, cardQuantity);
						}
						Archetype similarArchetype = new Archetype(name, similarMainDeck, similarSideboard, RotationProof.Value, win, loss,
							Format.Value == Properties.Resources.Item_Brawl, null, setNameTranslations: _setNameTranslations);
						CardStats.UpdateDeckAssociations(similarArchetype);
						similarDecks.Add(similarArchetype);
					}
				}

				Archetype newArchetype = new Archetype(name, mainDeck, sideboard, RotationProof.Value, win, loss, Format.Value == Properties.Resources.Item_Brawl, similarDecks,
					setNameTranslations: _setNameTranslations);
				CardStats.UpdateDeckAssociations(newArchetype);
				_archetypes.Add(newArchetype);
			}

			LoadingValue.Value = 75;

			_logger.Debug("Initializing Card Stats Objects");
			foreach (Card card in cardsById.Values)
			{
				CardStats stats = _cardStats[card];
				stats.DeckPercentage = (double)stats.DeckCount / _archetypes.Count;
			}
			Archetype.CardStats = new ReadOnlyDictionary<Card, CardStats>(_cardStats);

			LoadingValue.Value = 100;

			_logger.Debug("{0} Deck Archetypes Loaded", _archetypes.Count);

			LoadingText.Value = Properties.Resources.Loading_ProcessingCollectionFromLog;
			LoadingValue.Value = 0;

			_logger.Debug("Processing Player Collection");

			var logFolder = GetLogFolderLocation();
			using (var fs = new FileStream(logFolder + "\\output_log.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
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

						if (line.Contains("PlayerInventory.GetPlayerCardsV3"))
						{
							_playerInventory.Clear();
							_basicLands.Clear();
							_playerInventoryCounts.Clear();
							line = reader.ReadLine();
							while (line != "}")
							{
								if (line.Contains("jsonrpc") || line.Contains("params"))
								{
									break;
								}
								if (!string.IsNullOrWhiteSpace(line) && line != "{")
								{
									var lineInfo = line.Replace(",", "").Replace("\"", "").Trim().Split(':');
									int id = int.Parse(lineInfo[0]);
									int count = int.Parse(lineInfo[1]);
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
								line = reader.ReadLine();
							}

							if (!(line.Contains("jsonrpc") || line.Contains("params")))
							{
								LoadingValue.Value = Math.Max(LoadingValue.Value, 40);
							}
						}
						else if (line.Contains("PlayerInventory.GetPlayerInventory"))
						{
							_wildcardsOwned[CardRarity.Common] = 0;
							_wildcardsOwned[CardRarity.Uncommon] = 0;
							_wildcardsOwned[CardRarity.Rare] = 0;
							_wildcardsOwned[CardRarity.MythicRare] = 0;
							line = reader.ReadLine();
							while (line != "}")
							{
								if (line.Contains("jsonrpc") || line.Contains("params"))
								{
									break;
								}
								if (!string.IsNullOrWhiteSpace(line) && line != "{")
								{
									var lineInfo = line.Replace(",", "").Replace("\"", "").Trim().Split(':');
									string id = lineInfo[0];
									if (id.StartsWith("wc"))
									{
										int count = int.Parse(lineInfo[1]);
										switch (id)
										{
											case "wcCommon":
												_wildcardsOwned[CardRarity.Common] = count;
												break;
											case "wcUncommon":
												_wildcardsOwned[CardRarity.Uncommon] = count;
												break;
											case "wcRare":
												_wildcardsOwned[CardRarity.Rare] = count;
												break;
											case "wcMythic":
												_wildcardsOwned[CardRarity.MythicRare] = count;
												break;
											default:
												break;
										}
									}
								}
								line = reader.ReadLine();
							}

							if (!(line.Contains("jsonrpc") || line.Contains("params")))
							{
								LoadingValue.Value = Math.Max(LoadingValue.Value, 60);
							}
						}
						else if (line.Contains("Deck.GetDeckListsV3"))
						{
							//_playerDecks.Clear();
							StringBuilder deckListJson = new StringBuilder();
							line = reader.ReadLine();
							while (line != "]")
							{
								if (line.Contains("jsonrpc") || line.Contains("params"))
								{
									break;
								}
								deckListJson.AppendLine(line);
								line = reader.ReadLine();
							}
							if (line == "]")
							{
								deckListJson.AppendLine(line);
								dynamic json = JToken.Parse(deckListJson.ToString());
								foreach (dynamic deck in json)
								{
									string name = deck["name"];
									int commanderId = deck["commandZoneGRPId"];
									int[] mainDeck = deck["mainDeck"].ToObject<int[]>();
									int[] sideboard = deck["sideboard"].ToObject<int[]>();
									Guid id = Guid.Parse((string)deck["id"]);
									if (_playerDecks.ContainsKey(id))
									{
										_playerDecks.Remove(id);
									}
									Dictionary<string, int> mainDeckByName = new Dictionary<string, int>();
									Dictionary<string, int> sideboardByName = new Dictionary<string, int>();

									if (commanderId != 0)
									{
										name = cardsById[commanderId].Name;
										if (Format != Properties.Resources.Item_Brawl)
										{
											continue;
										}
									}
									else if (Format == Properties.Resources.Item_Brawl)
									{
										continue;
									}
									else if((Format == Properties.Resources.Item_Standard || Format == Properties.Resources.Item_Historic_Bo3) && sideboard.Length == 0)
									{
										continue;
									}
									else if((Format == Properties.Resources.Item_ArenaStandard || Format == Properties.Resources.Item_Historic_Bo1) && sideboard.Length > 0)
									{
										continue;
									}

									for (int i = 0; i < mainDeck.Length; i += 2)
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

									if (RotationProof.Value)
									{
										// check whether there are any cards in the deck that aren't rotation-proof...if so, ignore this deck
										_logger.Debug(@"Doing ""rotation-proof"" check...");
										bool ignoreDeck = false;
										foreach (var card in mainDeckByName)
										{
											string cardName = card.Key;
											_logger.Debug("Checking main deck card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
											{
												_logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
												ignoreDeck = true;
												break;
											}
										}

										if (!ignoreDeck)
										{
											foreach (var card in sideboardByName)
											{
												string cardName = card.Key;

												_logger.Debug("Checking sideboard card: {cardName}", cardName);

												if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
												{
													_logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
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
										_logger.Debug(@"Doing Standard legality check...");
										bool ignoreDeck = false;
										foreach (var card in mainDeckByName)
										{
											string cardName = card.Key;
											_logger.Debug("Checking main deck card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
											{
												_logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
												ignoreDeck = true;
												break;
											}
										}

										if (!ignoreDeck)
										{
											foreach (var card in sideboardByName)
											{
												string cardName = card.Key;

												_logger.Debug("Checking sideboard card: {cardName}", cardName);

												if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
												{
													_logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
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

									_playerDecks.Add(id, new Archetype(name, mainDeckByName, sideboardByName, RotationProof.Value, 
										isBrawl: Format == Properties.Resources.Item_Brawl, isPlayerDeck: true, setNameTranslations: _setNameTranslations));
								}
							}

							if (!(line.Contains("jsonrpc") || line.Contains("params")))
							{
								LoadingValue.Value = Math.Max(LoadingValue.Value, 20);
							}
						}
						else if(line.Contains("Deck.CreateDeckV3") || line.Contains("Deck.UpdateDeckV3"))
						{
							StringBuilder deckListJson = new StringBuilder();
							line = reader.ReadLine();
							while (line != "}")
							{
								if (line.Contains("jsonrpc") || line.Contains("params"))
								{
									break;
								}
								deckListJson.AppendLine(line);
								line = reader.ReadLine();
							}
							if (line == "}")
							{
								deckListJson.AppendLine(line);
								dynamic deck = JToken.Parse(deckListJson.ToString());

								string name = deck["name"];
								int commanderId = deck["commandZoneGRPId"];
								int[] mainDeck = deck["mainDeck"].ToObject<int[]>();
								int[] sideboard = deck["sideboard"].ToObject<int[]>();
								Guid id = Guid.Parse((string)deck["id"]);
								if(_playerDecks.ContainsKey(id))
								{
									_playerDecks.Remove(id);
								}
								Dictionary<string, int> mainDeckByName = new Dictionary<string, int>();
								Dictionary<string, int> sideboardByName = new Dictionary<string, int>();

								if (commanderId != 0)
								{
									name = cardsById[commanderId].Name;
									if (Format != Properties.Resources.Item_Brawl)
									{
										continue;
									}
								}
								else if (Format == Properties.Resources.Item_Brawl)
								{
									continue;
								}
								else if ((Format == Properties.Resources.Item_Standard || Format == Properties.Resources.Item_Historic_Bo3) && sideboard.Length == 0)
								{
									continue;
								}
								else if ((Format == Properties.Resources.Item_ArenaStandard || Format == Properties.Resources.Item_Historic_Bo1) && sideboard.Length > 0)
								{
									continue;
								}

								for (int i = 0; i < mainDeck.Length; i += 2)
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

								if (RotationProof.Value)
								{
									// check whether there are any cards in the deck that aren't rotation-proof...if so, ignore this deck
									_logger.Debug(@"Doing ""rotation-proof"" check...");
									bool ignoreDeck = false;
									foreach (var card in mainDeckByName)
									{
										string cardName = card.Key;
										_logger.Debug("Checking main deck card: {cardName}", cardName);

										if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
										{
											_logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
											ignoreDeck = true;
											break;
										}
									}

									if (!ignoreDeck)
									{
										foreach (var card in sideboardByName)
										{
											string cardName = card.Key;

											_logger.Debug("Checking sideboard card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.RotationSafe) == 0)
											{
												_logger.Debug("{cardName} is rotating soon, ignore this deck", cardName);
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
									_logger.Debug(@"Doing Standard legality check...");
									bool ignoreDeck = false;
									foreach (var card in mainDeckByName)
									{
										string cardName = card.Key;
										_logger.Debug("Checking main deck card: {cardName}", cardName);

										if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
										{
											_logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
											ignoreDeck = true;
											break;
										}
									}

									if (!ignoreDeck)
									{
										foreach (var card in sideboardByName)
										{
											string cardName = card.Key;

											_logger.Debug("Checking sideboard card: {cardName}", cardName);

											if (cardsByName[cardName].Count(x => x.Set.StandardLegal) == 0)
											{
												_logger.Debug("{cardName} is not standard legal, ignore this deck", cardName);
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

								_playerDecks.Add(id, new Archetype(name, mainDeckByName, sideboardByName, RotationProof.Value, isBrawl: Format == Properties.Resources.Item_Brawl,
									isPlayerDeck: true, setNameTranslations: _setNameTranslations));
							}

							if (!(line.Contains("jsonrpc") || line.Contains("params")))
							{
								LoadingValue.Value = Math.Max(LoadingValue.Value, 80);
							}
						}
						else if (line.Contains("Deck.DeleteDeck"))
						{
							StringBuilder deckListJson = new StringBuilder();
							line = reader.ReadLine();
							if(line != "{")
							{
								break;
							}
							while (line != "}")
							{
								deckListJson.AppendLine(line);
								line = reader.ReadLine();
							}

							deckListJson.AppendLine(line);
							dynamic deck = JToken.Parse(deckListJson.ToString());

							Guid id = Guid.Parse((string)deck["params"]["deckId"]);
							if (_playerDecks.ContainsKey(id))
							{
								_playerDecks.Remove(id);
							}
						}
					}
				}
			}
			Archetype.WildcardsOwned = new ReadOnlyDictionary<CardRarity, int>(_wildcardsOwned);

			LoadingValue.Value = 100;

			_logger.Debug("Player Collection Loaded with {0} Cards", _playerInventory.Count);

			LoadingText.Value = Properties.Resources.Loading_ComputingDeckSuggestions;
			LoadingValue.Value = 0;

			_logger.Debug("Computing Suggestions");

			foreach (Archetype archetype in _archetypes)
			{
				_logger.Debug("Processing Archetype {0}", archetype.Name);
				Dictionary<string, int> haveCards = new Dictionary<string, int>();
				Dictionary<int, int> mainDeckCards = new Dictionary<int, int>();
				Dictionary<int, int> sideboardCards = new Dictionary<int, int>();
				Dictionary<int, int> mainDeckToCollect = new Dictionary<int, int>();
				Dictionary<int, int> sideboardToCollect = new Dictionary<int, int>();

				var leftJoin =
					from main in archetype.MainDeck
					join side in archetype.Sideboard on main.Key equals side.Key into temp
					from side in temp.DefaultIfEmpty()
					select new
					{
						Name = main.Key,
						Quantity = main.Value + side.Value
					};

				var rightJoin =
					from side in archetype.Sideboard
					join main in archetype.MainDeck on side.Key equals main.Key into temp
					from main in temp.DefaultIfEmpty()
					select new
					{
						Name = side.Key,
						Quantity = side.Value + main.Value
					};

				var cardsNeededForArchetype = leftJoin.Union(rightJoin);

				Dictionary<int, int> playerInventory = new Dictionary<int, int>(_playerInventory.Select(x => new { Id = x.Key, Count = x.Value }).ToDictionary(x => x.Id, y => y.Count));

				_logger.Debug("Comparing deck list to inventory to determine which cards are still needed");
				foreach (var neededCard in cardsNeededForArchetype)
				{
					int neededForMain = archetype.MainDeck.ContainsKey(neededCard.Name) ? archetype.MainDeck[neededCard.Name] : 0;
					int neededForSideboard = archetype.Sideboard.ContainsKey(neededCard.Name) ? archetype.Sideboard[neededCard.Name] : 0;

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
								haveCards[printing.Name] += neededCards;
								neededCards = 0;
								neededForMain = 0;
								neededForSideboard = 0;
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
								haveCards[printing.Name] += neededCards;
								playerInventory[printing.ArenaId] -= neededCards;
								neededCards = 0;
								neededForMain = 0;
								neededForSideboard = 0;
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
					}
				}

				_logger.Debug("Generating replacement suggestions for missing cards");
				List<Tuple<int, int, int>> suggestedReplacements = new List<Tuple<int, int, int>>();
				CardColors identity = null;
				if(Format.Value == Properties.Resources.Item_Brawl)
				{
					identity = cardsByName[archetype.CommanderName].First().ColorIdentity;
				}
				var replacementsToFind = mainDeckToCollect.Concat(sideboardToCollect).GroupBy(x => x.Key).Select(x => new { Id = x.Key, Count = x.Sum(y => y.Value) });
				foreach (var find in replacementsToFind)
				{
					_logger.Debug("Generating replacements suggestions for card with Arena Id: {0} (need {1})", find.Id, find.Count);
					var cardToReplace = cardsById[find.Id];
					var replacementsNeeded = find.Count;

					if (cardToReplace.Type.Contains("Land"))
					{
						_logger.Debug("Processing Candidates based on Color");
						var candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
							Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && _colorsByLand[x.Card.Name] == _colorsByLand[cardToReplace.Name] && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, suggesting basic land replacements");
							Random r = new Random();
							// randomizing colors here so we don't always favor colors in WUBRG order
							string[] colors = _colorsByLand[cardToReplace.Name].ColorString.Select(x => new { Sort = r.Next(), Value = x.ToString() }).
								OrderBy(y => y.Sort).Select(z => z.Value).ToArray();
							Dictionary<string, int> _colorReplacements = new Dictionary<string, int>();
							if (colors.Length > 0)
							{
								int colorIndex = 0;
								while(replacementsNeeded > 0)
								{
									if(!_colorReplacements.ContainsKey(colors[colorIndex]))
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
							_logger.Debug("Insufficient Candidates Found, done looking");
						}
					}
					else
					{
						_logger.Debug("Processing Candidates based on Type and Cost");
						var candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
								Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && x.Card.Cost == cardToReplace.Cost && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, Getting Candidates by Cost");
							candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
								Where(x => x.Quantity > 0 && x.Card.Cost == cardToReplace.Cost && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, Getting Candidates by Cmc and Color");
							candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
								Where(x => x.Quantity > 0 && x.Card.Cmc == cardToReplace.Cmc && x.Card.Colors == cardToReplace.Colors && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, Getting Candidates, looking for candidates with lower Cmc");
							int cmcToTest = cardToReplace.Cmc - 1;
							while (replacementsNeeded > 0 && cmcToTest > 0)
							{
								candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
									Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && x.Card.Colors == cardToReplace.Colors && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, Getting Candidates, relaxing color requirments and looping through Cmc <= {0}", cardToReplace.Cmc);
							int cmcToTest = cardToReplace.Cmc;
							while (replacementsNeeded > 0 && cmcToTest > 0)
							{
								candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
									Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && cardToReplace.Colors.Contains(x.Card.Colors) && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, done looking");
						}
					}
				}

				_logger.Debug("Updating Archetype objects with suggestions");
				archetype.SuggestedMainDeck = new ReadOnlyDictionary<int, int>(mainDeckCards);
				archetype.SuggestedSideboard = new ReadOnlyDictionary<int, int>(sideboardCards);
				archetype.MainDeckToCollect = new ReadOnlyDictionary<int, int>(mainDeckToCollect);
				archetype.SideboardToCollect = new ReadOnlyDictionary<int, int>(sideboardToCollect);
				archetype.SuggestedReplacements = suggestedReplacements.AsReadOnly();

				_logger.Debug("Processing Alternate Deck Configurations");
				if(archetype.SimilarDecks != null && archetype.SimilarDecks.Count > 0)
				{
					foreach(Archetype similarArchetype in archetype.SimilarDecks)
					{
						_logger.Debug("Processing Similar Archetype {0}", similarArchetype.Name);
						haveCards = new Dictionary<string, int>();
						mainDeckCards = new Dictionary<int, int>();
						sideboardCards = new Dictionary<int, int>();
						mainDeckToCollect = new Dictionary<int, int>();
						sideboardToCollect = new Dictionary<int, int>();

						leftJoin =
							from main in similarArchetype.MainDeck
							join side in similarArchetype.Sideboard on main.Key equals side.Key into temp
							from side in temp.DefaultIfEmpty()
							select new
							{
								Name = main.Key,
								Quantity = main.Value + side.Value
							};

						rightJoin =
							from side in similarArchetype.Sideboard
							join main in similarArchetype.MainDeck on side.Key equals main.Key into temp
							from main in temp.DefaultIfEmpty()
							select new
							{
								Name = side.Key,
								Quantity = side.Value + main.Value
							};

						cardsNeededForArchetype = leftJoin.Union(rightJoin);

						playerInventory = new Dictionary<int, int>(_playerInventory.Select(x => new { Id = x.Key, Count = x.Value }).ToDictionary(x => x.Id, y => y.Count));

						_logger.Debug("Comparing deck list to inventory to determine which cards are still needed");
						foreach (var neededCard in cardsNeededForArchetype)
						{
							int neededForMain = similarArchetype.MainDeck.ContainsKey(neededCard.Name) ? similarArchetype.MainDeck[neededCard.Name] : 0;
							int neededForSideboard = similarArchetype.Sideboard.ContainsKey(neededCard.Name) ? similarArchetype.Sideboard[neededCard.Name] : 0;

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
										haveCards[printing.Name] += neededCards;
										neededCards = 0;
										neededForMain = 0;
										neededForSideboard = 0;
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
										haveCards[printing.Name] += neededCards;
										playerInventory[printing.ArenaId] -= neededCards;
										neededCards = 0;
										neededForMain = 0;
										neededForSideboard = 0;
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
							}
						}

						_logger.Debug("Generating replacement suggestions for missing cards");
						suggestedReplacements = new List<Tuple<int, int, int>>();
						identity = null;
						if (Format.Value == Properties.Resources.Item_Brawl)
						{
							identity = cardsByName[similarArchetype.CommanderName].First().ColorIdentity;
						}
						replacementsToFind = mainDeckToCollect.Concat(sideboardToCollect).GroupBy(x => x.Key).Select(x => new { Id = x.Key, Count = x.Sum(y => y.Value) });
						foreach (var find in replacementsToFind)
						{
							_logger.Debug("Generating replacements suggestions for card with Arena Id: {0} (need {1})", find.Id, find.Count);
							var cardToReplace = cardsById[find.Id];
							var replacementsNeeded = find.Count;

							if (cardToReplace.Type.Contains("Land"))
							{
								_logger.Debug("Processing Candidates based on Color");
								var candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
									Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && _colorsByLand[x.Card.Name] == _colorsByLand[cardToReplace.Name] && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
									_logger.Debug("Insufficient Candidates Found, suggesting basic land replacements");
									Random r = new Random();
									// randomizing colors here so we don't always favor colors in WUBRG order
									string[] colors = _colorsByLand[cardToReplace.Name].ColorString.Select(x => new { Sort = r.Next(), Value = x.ToString() }).
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
									_logger.Debug("Insufficient Candidates Found, done looking");
								}
							}
							else
							{
								_logger.Debug("Processing Candidates based on Type and Cost");
								var candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
										Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && x.Card.Cost == cardToReplace.Cost && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
									_logger.Debug("Insufficient Candidates Found, Getting Candidates by Cost");
									candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
										Where(x => x.Quantity > 0 && x.Card.Cost == cardToReplace.Cost && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
									_logger.Debug("Insufficient Candidates Found, Getting Candidates by Cmc and Color");
									candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
										Where(x => x.Quantity > 0 && x.Card.Cmc == cardToReplace.Cmc && x.Card.Colors == cardToReplace.Colors && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
									_logger.Debug("Insufficient Candidates Found, Getting Candidates, looking for candidates with lower Cmc");
									int cmcToTest = cardToReplace.Cmc - 1;
									while (replacementsNeeded > 0 && cmcToTest > 0)
									{
										candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
											Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && x.Card.Colors == cardToReplace.Colors && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
									_logger.Debug("Insufficient Candidates Found, Getting Candidates, relaxing color requirments and looping through Cmc <= {0}", cardToReplace.Cmc);
									int cmcToTest = cardToReplace.Cmc;
									while (replacementsNeeded > 0 && cmcToTest > 0)
									{
										candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
											Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && cardToReplace.Colors.Contains(x.Card.Colors) && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
									_logger.Debug("Insufficient Candidates Found, done looking");
								}
							}
						}

						_logger.Debug("Updating Archetype objects with suggestions");
						similarArchetype.SuggestedMainDeck = new ReadOnlyDictionary<int, int>(mainDeckCards);
						similarArchetype.SuggestedSideboard = new ReadOnlyDictionary<int, int>(sideboardCards);
						similarArchetype.MainDeckToCollect = new ReadOnlyDictionary<int, int>(mainDeckToCollect);
						similarArchetype.SideboardToCollect = new ReadOnlyDictionary<int, int>(sideboardToCollect);
						similarArchetype.SuggestedReplacements = suggestedReplacements.AsReadOnly();
					}

					archetype.SortSimilarDecks();
				}
			}

			LoadingValue.Value = 50;

			_logger.Debug("Handling player inventory decks");
			foreach (Archetype playerDeck in _playerDecks.Values)
			{
				_logger.Debug("Processing Player Deck {0}", playerDeck.Name);
				Dictionary<string, int> haveCards = new Dictionary<string, int>();
				Dictionary<int, int> mainDeckCards = new Dictionary<int, int>();
				Dictionary<int, int> sideboardCards = new Dictionary<int, int>();
				Dictionary<int, int> mainDeckToCollect = new Dictionary<int, int>();
				Dictionary<int, int> sideboardToCollect = new Dictionary<int, int>();

				var leftJoin =
					from main in playerDeck.MainDeck
					join side in playerDeck.Sideboard on main.Key equals side.Key into temp
					from side in temp.DefaultIfEmpty()
					select new
					{
						Name = main.Key,
						Quantity = main.Value + side.Value
					};

				var rightJoin =
					from side in playerDeck.Sideboard
					join main in playerDeck.MainDeck on side.Key equals main.Key into temp
					from main in temp.DefaultIfEmpty()
					select new
					{
						Name = side.Key,
						Quantity = side.Value + main.Value
					};

				var cardsNeededForArchetype = leftJoin.Union(rightJoin);

				Dictionary<int, int> playerInventory = new Dictionary<int, int>(_playerInventory.Select(x => new { Id = x.Key, Count = x.Value }).ToDictionary(x => x.Id, y => y.Count));

				_logger.Debug("Comparing deck list to inventory to determine which cards are still needed");
				foreach (var neededCard in cardsNeededForArchetype)
				{
					int neededForMain = playerDeck.MainDeck.ContainsKey(neededCard.Name) ? playerDeck.MainDeck[neededCard.Name] : 0;
					int neededForSideboard = playerDeck.Sideboard.ContainsKey(neededCard.Name) ? playerDeck.Sideboard[neededCard.Name] : 0;

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
								haveCards[printing.Name] += neededCards;
								neededCards = 0;
								neededForMain = 0;
								neededForSideboard = 0;
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
								haveCards[printing.Name] += neededCards;
								playerInventory[printing.ArenaId] -= neededCards;
								neededCards = 0;
								neededForMain = 0;
								neededForSideboard = 0;
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
					}
				}

				_logger.Debug("Generating replacement suggestions for missing cards");
				List<Tuple<int, int, int>> suggestedReplacements = new List<Tuple<int, int, int>>();
				CardColors identity = null;
				if (Format.Value == Properties.Resources.Item_Brawl)
				{
					identity = cardsByName[playerDeck.CommanderName].First().ColorIdentity;
				}
				var replacementsToFind = mainDeckToCollect.Concat(sideboardToCollect).GroupBy(x => x.Key).Select(x => new { Id = x.Key, Count = x.Sum(y => y.Value) });
				foreach (var find in replacementsToFind)
				{
					_logger.Debug("Generating replacements suggestions for card with Arena Id: {0} (need {1})", find.Id, find.Count);
					var cardToReplace = cardsById[find.Id];
					var replacementsNeeded = find.Count;

					if (cardToReplace.Type.Contains("Land"))
					{
						_logger.Debug("Processing Candidates based on Color");
						var candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
							Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && _colorsByLand[x.Card.Name] == _colorsByLand[cardToReplace.Name] && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, suggesting basic land replacements");
							Random r = new Random();
							// randomizing colors here so we don't always favor colors in WUBRG order
							string[] colors = _colorsByLand[cardToReplace.Name].ColorString.Select(x => new { Sort = r.Next(), Value = x.ToString() }).
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
							_logger.Debug("Insufficient Candidates Found, done looking");
						}
					}
					else
					{
						_logger.Debug("Processing Candidates based on Type and Cost");
						var candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
								Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && x.Card.Cost == cardToReplace.Cost && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, Getting Candidates by Cost");
							candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
								Where(x => x.Quantity > 0 && x.Card.Cost == cardToReplace.Cost && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, Getting Candidates by Cmc and Color");
							candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
								Where(x => x.Quantity > 0 && x.Card.Cmc == cardToReplace.Cmc && x.Card.Colors == cardToReplace.Colors && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, Getting Candidates, looking for candidates with lower Cmc");
							int cmcToTest = cardToReplace.Cmc - 1;
							while (replacementsNeeded > 0 && cmcToTest > 0)
							{
								candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
									Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && x.Card.Colors == cardToReplace.Colors && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, Getting Candidates, relaxing color requirments and looping through Cmc <= {0}", cardToReplace.Cmc);
							int cmcToTest = cardToReplace.Cmc;
							while (replacementsNeeded > 0 && cmcToTest > 0)
							{
								candidates = playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
									Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && cardToReplace.Colors.Contains(x.Card.Colors) && (identity == null || identity.Contains(x.Card.ColorIdentity))).
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
							_logger.Debug("Insufficient Candidates Found, done looking");
						}
					}
				}

				_logger.Debug("Updating Player Deck objects with suggestions");
				playerDeck.SuggestedMainDeck = new ReadOnlyDictionary<int, int>(mainDeckCards);
				playerDeck.SuggestedSideboard = new ReadOnlyDictionary<int, int>(sideboardCards);
				playerDeck.MainDeckToCollect = new ReadOnlyDictionary<int, int>(mainDeckToCollect);
				playerDeck.SideboardToCollect = new ReadOnlyDictionary<int, int>(sideboardToCollect);
				playerDeck.SuggestedReplacements = suggestedReplacements.AsReadOnly();

				if(playerDeck.BoosterCost > 0)
				{
					// if the player doesn't own all the cards for this deck, add it to the list
					_archetypes.Add(playerDeck);
				}
			}

			LoadingValue.Value = 75;

			_logger.Debug("Sorting Archetypes and generating Meta Report");
			if (Format == Properties.Resources.Item_ArenaStandard)
			{
				// for Arena Standard, use slightly different ordering, favoring win rate over estimated booster cost
				_orderedArchetypes = _archetypes.OrderBy(x => x.IsPlayerDeck ? 0 : 1).ThenBy(x => x.BoosterCost == 0 ? 0 : (x.BoosterCostAfterWC == 0 ? 1 : 2)).ThenByDescending(x => x.BoosterCostAfterWC == 0 ? x.WinRate : x.BoosterCostAfterWC).ThenBy(x => x.BoosterCostAfterWC).ThenBy(x => x.BoosterCost);
			}
			else
			{
				_orderedArchetypes = _archetypes.OrderBy(x => x.IsPlayerDeck ? 0 : 1).ThenBy(x => x.BoosterCostAfterWC).ThenBy(x => x.BoosterCost);
			}
			MetaReport report = new MetaReport(cardsByName, _cardStats, cardsById, _playerInventoryCounts, _archetypes, RotationProof.Value, _setNameTranslations);

			LoadingValue.Value = 90;

			Dispatcher.Invoke(() =>
			{
				_tabObjects.Add(report);
				foreach (Archetype archetype in _orderedArchetypes)
				{
					_tabObjects.Add(archetype);
				}
			});

			LoadingValue.Value = 100;

			_logger.Debug("Finished Computing Suggestions, Updating GUI");

			Dispatcher.Invoke(() =>
			{
				LoadingScreen.Visibility = Visibility.Collapsed;
				FilterPanel.Visibility = Visibility.Visible;
				DeckTabs.Visibility = Visibility.Visible;
				DeckTabs.ItemsSource = _tabObjects;
				DeckTabs.SelectedIndex = 0;
			});

			_logger.Debug("ReloadAndCrunchAllData() Finished");
		}

		/// <summary>
		/// Callback that runs when the main window first loads.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_logger.Debug("Main Window Loaded - {0}", "Main Application");

			AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
			string assemblyVersion = assemblyName.Version.ToString();
			string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();

			Title = Title + $" - ({assemblyVersion}, {assemblyArchitecture})";

			App application = (App)Application.Current;
			Format.Value = application.State.LastFormat;
			if(string.IsNullOrWhiteSpace(Format.Value) || !_formatMappings.ContainsKey(Format.Value))
			{
				Format.Value = Properties.Resources.Item_Standard;
				application.State.LastFormat = Format.Value;
				application.SaveState();
			}
			Format.PropertyChanged += Format_PropertyChanged;

			RotationProof.Value = application.State.RotationProof;
			RotationProof.PropertyChanged += RotationProof_PropertyChanged;

			int fontSize = application.State.FontSize;
			if (fontSize < 8 || fontSize > 24)
			{
				SelectedFontSize.Value = 12;
				application.State.FontSize = SelectedFontSize.Value;
				application.SaveState();
			}
			else
			{
				SelectedFontSize.Value = fontSize;
			}
			SelectedFontSize.PropertyChanged += SelectedFontSize_PropertyChanged;

			Task loadTask = new Task(() => {
				_logger.Debug("Initializing Card Database");
				CardDatabase.Initialize(false);
				LoadingValue.Value = 20;

				PopulateColorsByLand();
				LoadingValue.Value = 35;

				PopulateSetTranslations();
				LoadingValue.Value = 50;

				ReloadAndCrunchAllData();
			});
			loadTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						_logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "loadTask", "Window_Loaded", "Main Application");
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			loadTask.Start();
		}

		/// <summary>
		/// Callback that is triggered when the user selects a different Format from the drop-down on the GUI.
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Format_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			_logger.Debug("New Format Selected, Format={0}", Format.Value);

			App application = (App)Application.Current;
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
						_logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "loadTask", "Format_PropertyChanged", "Main Application");
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			loadTask.Start();
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
				_logger.Debug("Rotation Proof toggled, RotationProof={0}", RotationProof.Value);

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
							_logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "loadTask", "RotationProof_PropertyChanged", "Main Application");
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
			_logger.Debug("New Font Size Selected, FontSize={0}", SelectedFontSize.Value);

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

				_logger.Debug("Deck Export Clicked, Deck={0}", item.Name);

				Clipboard.SetText(item.ExportList);
				MessageBox.Show($"{item.Name} Export Successful", "Export");
			}
			catch(Exception ex)
			{
				_logger.Error(ex, "Exception in Export_Clicked");
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

				_logger.Debug("Deck Export Suggested Clicked, Deck={0}", item.Name);

				Clipboard.SetText(item.ExportListSuggested);
				MessageBox.Show($"{item.Name} Export (with Replacements) Successful", "Export");
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Exception in ExportSuggested_Click");
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

			_logger.Debug("Deck Hyperlink Clicked, Deck={0}", item.Name);

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

			_logger.Debug("Similar Deck Hyperlink Clicked, Deck={0}", item.Name);

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

			_logger.Debug("Parent Deck Hyperlink Clicked, Deck={0}", item.Name);

			int selectedIndex = DeckTabs.SelectedIndex;
			_tabObjects[selectedIndex] = item.Parent;
			DeckTabs.SelectedIndex = selectedIndex;
		}

		/// <summary>
		/// Callback that is triggered when the user clicks the refresh button the GUI. (Only re-loads user log information, doesn't check for new data on server.)
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			_logger.Debug("Refresh Clicked");

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
						_logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "loadTask", "Refresh_Click", "Main Application");
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			loadTask.Start();
		}

		/// <summary>
		/// Callback that is triggered when the user clicks the "hard" refresh button the GUI. (Pulls new data from server, if needed.)
		/// </summary>
		/// <param name="sender">The object that triggered the callback.</param>
		/// <param name="e">Arguments regarding the event that triggered the callback.</param>
		private void HardRefresh_Click(object sender, RoutedEventArgs e)
		{
			_logger.Debug("Hard Refresh Clicked");

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
				_logger.Debug("Initializing Card Database");
				CardDatabase.Initialize(false);
				LoadingValue.Value = 20;

				PopulateColorsByLand();
				LoadingValue.Value = 35;

				PopulateColorsByLand();
				LoadingValue.Value = 50;

				ReloadAndCrunchAllData();
			});
			loadTask.ContinueWith(t =>
				{
					if (t.Exception != null)
					{
						_logger.Error(t.Exception, "Exception in {0} ({1} - {2})", "loadTask", "HardRefresh_Click", "Main Application");
					}
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
			loadTask.Start();
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
		/// Gets the bitmap scaling mode.
		/// </summary>
		/// <returns>Fant, or whatever override value the user set in App.config.</returns>
		private BitmapScalingMode GetBitmapScalingMode()
		{
			_logger.Debug("GetBitmapScalingMode() Called - {0}", "Main Application");

			try
			{
				var appSettings = ConfigurationManager.AppSettings;
				if (appSettings == null)
				{
					_logger.Debug("No AppSettings Found, Using Fant");
					return BitmapScalingMode.Fant;
				}
				string bitmapScalingMode = appSettings["BitmapScalingMode"] ?? "Fant";
				switch (bitmapScalingMode)
				{
					case "Fant":
						_logger.Debug("Using BitmapScalingMode {0}", "Fant");
						return BitmapScalingMode.Fant;
					case "HighQuality":
						_logger.Debug("Using BitmapScalingMode {0}", "HighQuality");
						return BitmapScalingMode.HighQuality;
					case "Linear":
						_logger.Debug("Using BitmapScalingMode {0}", "Linear");
						return BitmapScalingMode.Linear;
					case "LowQuality":
						_logger.Debug("Using BitmapScalingMode {0}", "LowQuality");
						return BitmapScalingMode.LowQuality;
					case "NearestNeighbor":
						_logger.Debug("Using BitmapScalingMode {0}", "NearestNeighbor");
						return BitmapScalingMode.NearestNeighbor;
					default:
						return BitmapScalingMode.Fant;
				}
			}
			catch (ConfigurationErrorsException e)
			{
				_logger.Error(e, "Exception in GetBitmapScalingMode(), Using Fant");
				return BitmapScalingMode.Fant;
			}
		}

		/// <summary>
		/// Gets the location of the folder that contains the MTGA real-time log output.
		/// </summary>
		/// <returns>The default location in the current user's AppData, unless there is an override value set in App.config.</returns>
		private string GetLogFolderLocation()
		{
			_logger.Debug("GetLogFolderLocation() Called - {0}", "Main Application");
			var logFolder = string.Format("{0}Low\\Wizards of the Coast\\MTGA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

			try
			{
				var appSettings = ConfigurationManager.AppSettings;
				if (appSettings == null)
				{
					_logger.Debug("No AppSettings Found, Using Default Log Folder Location: {0}", logFolder);
					return logFolder;
				}
				logFolder = appSettings["MTGALogFolder"] ?? logFolder;
			}
			catch (ConfigurationErrorsException e)
			{
				_logger.Error(e, "Exception in GetLogFolderLocation(), Using Default Log Folder Location: {0}", logFolder);
				return logFolder;
			}

			_logger.Debug("Using Log Folder Location: {0}", logFolder);
			return logFolder;
		}

		/// <summary>
		/// Sets the current UI culture from app.config if it's set there.
		/// </summary>
		private void SetCulture()
		{
			_logger.Debug("SetCulture() Called - {0}", "Main Application");

			try
			{
				var appSettings = ConfigurationManager.AppSettings;
				if (appSettings == null)
				{
					_logger.Debug("No AppSettings Found, Using Default UI Culture");
				}
				else {
					string culture = appSettings["UICulture"];
					if(culture != null)
					{
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
	}
}
