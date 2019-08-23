using DailyArenaDeckAdvisor.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DailyArenaDeckAdvisor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		List<Archetype> _archetypes = new List<Archetype>();
		Dictionary<int, int> _playerInventory = new Dictionary<int, int>();
		Dictionary<string, int> _playerInventoryCounts = new Dictionary<string, int>();
		Dictionary<string, string> _colorsByLand = new Dictionary<string, string>();
		Dictionary<CardRarity, int> _wildcardsOwned = new Dictionary<CardRarity, int>();
		List<object> _tabObjects = new List<object>();
		Dictionary<Card, CardStats> _cardStats = new Dictionary<Card, CardStats>();

		IOrderedEnumerable<Archetype> _orderedArchetypes;
		List<string> _anyNumber = new List<string>() { "Persistent Petitioners", "Rat Colony" };

		ILogger _logger;

		public BindableString Format { get; private set; } = new BindableString();

		public MainWindow()
		{
			App application = (App)Application.Current;
			_logger = application.Logger;
			_logger.Debug("Main Window Constructor Called - {0}", "Main Application");

			InitializeComponent();
			DataContext = this;
		}

		private CardRarity LowestRarity(CardRarity r1, CardRarity r2)
		{
			return r1.CompareTo(r2) > 0 ? r2 : r1;
		}

		private string LoadStandardLands()
		{
			_logger.Debug("LoadStandardLands() Called");
			string cacheTimestamp = "1970-01-01T00:00:00Z";

			if (File.Exists("lands.json"))
			{
				string json = File.ReadAllText("lands.json");
				dynamic data = JsonConvert.DeserializeObject(json);

				cacheTimestamp = data.LastUpdate;
				_colorsByLand = data.ColorsByLand.ToObject<Dictionary<string, string>>();
			}

			_logger.Debug("LoadStandardLands() Finished - cacheTimestamp={0)", cacheTimestamp);
			return cacheTimestamp;
		}

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

		private void PopulateColorsByLand()
		{
			_logger.Debug("PopulateColorsByLand() Called");
			string serverUpdateTime = CardDatabase.GetServerTimestamp("StandardLands");
			string cacheTimestamp = LoadStandardLands();

			if (string.Compare(serverUpdateTime, cacheTimestamp) > 0)
			{
				_colorsByLand.Clear();

				var landsUrl = $"https://clans.dailyarena.net/standard_lands.php?_c={Guid.NewGuid()}";
				var landsRequest = WebRequest.Create(landsUrl);
				landsRequest.Method = "GET";

				using (var landsResponse = landsRequest.GetResponse())
				{
					using (Stream landsResponseStream = landsResponse.GetResponseStream())
					using (StreamReader landsResponseReader = new StreamReader(landsResponseStream))
					{
						var result = landsResponseReader.ReadToEnd();
						dynamic json = JToken.Parse(result.ToString());
						foreach (dynamic lands in json)
						{
							_colorsByLand.Add((string)lands["Name"], (string)lands["Colors"]);
						}
					}
				}

				SaveStandardLands(serverUpdateTime);
			}
			_logger.Debug("PopulateColorsByLand() Finished");
		}

		private Dictionary<string, string> _formatMappings = new Dictionary<string, string>()
		{
			{ "Standard", "standard" },
			{ "Arena Standard", "arena_standard" },
			{ "Brawl", "brawl" }
		};

		private void ReloadAndCrunchAllData()
		{
			_logger.Debug("ReloadAndCrunchAllData() Called");

			_archetypes.Clear();
			_playerInventory.Clear();
			_wildcardsOwned.Clear();
			_tabObjects.Clear();
			_cardStats.Clear();
			_orderedArchetypes = null;

			int maxInventoryCount = Format.Value == "Brawl" ? 1 : 4;

			Archetype.ClearWildcardsOwned();
			Archetype.ClearCardStats();
			if (Archetype.AnyNumber == null)
			{
				Archetype.AnyNumber = _anyNumber.AsReadOnly();
			}

			foreach (Card card in Card.AllCards)
			{
				_cardStats.Add(card, new CardStats() { Card = card });
			}
			ReadOnlyDictionary<string, List<Card>> cardsByName = Card.CardsByName;
			ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;

			_logger.Debug("Card Database Loaded with {0} Cards", cardsById.Count);

			Dispatcher.Invoke(() =>
			{
				LoadingDatabase.Visibility = Visibility.Collapsed;
				LoadingArchetypes.Visibility = Visibility.Visible;
			});

			_logger.Debug("Loading Archetype Data");

			string mappedFormat = _formatMappings[Format.Value];
			JToken decksJson = null;
			string serverTimestamp = CardDatabase.GetServerTimestamp($"{Format.Value.Replace(" ", string.Empty)}Decks");
			bool loadDecksFromServer = true;
			if(File.Exists($"{mappedFormat}_decks.json"))
			{
				string content = File.ReadAllText($"{mappedFormat}_decks.json");
				using (StringReader contentStringReader = new StringReader(content))
				using (JsonTextReader contentJsonReader = new JsonTextReader(contentStringReader) { DateParseHandling = DateParseHandling.None })
				{
					dynamic json = JToken.ReadFrom(contentJsonReader);
					string decksTimestamp = json["Timestamp"];
					if (!(serverTimestamp == null || string.Compare(serverTimestamp, decksTimestamp) > 0))
					{
						// use cached decks
						decksJson = json["Decks"];
						loadDecksFromServer = false;
					}
				}
			}
			if (loadDecksFromServer)
			{
				var archetypesUrl = $"https://clans.dailyarena.net/{mappedFormat}_decks.json?_c={Guid.NewGuid()}";
				var archetypesRequest = WebRequest.Create(archetypesUrl);
				archetypesRequest.Method = "GET";

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
							File.WriteAllText($"{mappedFormat}_decks.json", JsonConvert.SerializeObject(
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

			_logger.Debug("Parsing decksJson");
			foreach (dynamic archetype in decksJson)
			{
				bool badDeckDefinition = false;
				string name = archetype["deck_name"];
				Dictionary<string, int> mainDeck = new Dictionary<string, int>();
				Dictionary<string, int> sideboard = new Dictionary<string, int>();
				foreach (dynamic card in archetype["deck_list"])
				{
					string cardName = (string)card["name"];
					int cardQuantity = (int)card["quantity"];

					foreach (Card archetypeCard in cardsByName[cardName])
					{
						CardStats stats = _cardStats[archetypeCard];
						if (!mainDeck.ContainsKey(cardName))
						{
							stats.DeckCount++;
						}
						stats.TotalCopies += cardQuantity;
					}

					// found some brawl decks on mtggoldfish that illegally have the same
					// entry twice...we'll just ignore those decks here
					if (mainDeck.ContainsKey(cardName))
					{
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

					foreach (Card archetypeCard in cardsByName[cardName])
					{
						CardStats stats = _cardStats[archetypeCard];
						if (!mainDeck.ContainsKey(cardName) && !sideboard.ContainsKey(cardName))
						{
							stats.DeckCount++;
						}
						stats.TotalCopies += cardQuantity;
					}
					sideboard.Add(cardName, cardQuantity);
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

				_archetypes.Add(new Archetype(name, mainDeck, sideboard));
			}

			_logger.Debug("Initializing Card Stats Objects");
			foreach (Card card in cardsById.Values)
			{
				CardStats stats = _cardStats[card];
				stats.DeckPercentage = (double)stats.DeckCount / _archetypes.Count;
			}
			Archetype.CardStats = new ReadOnlyDictionary<Card, CardStats>(_cardStats);

			_logger.Debug("{0} Deck Archetypes Loaded", _archetypes.Count);

			Dispatcher.Invoke(() =>
			{
				LoadingArchetypes.Visibility = Visibility.Collapsed;
				ProcessingCollection.Visibility = Visibility.Visible;
			});

			_logger.Debug("Processing Player Collection");

			var logFolder = string.Format("{0}Low\\Wizards of the Coast\\MTGA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
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
									string name = cardsById[id].Name;
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
								line = reader.ReadLine();
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
						}
					}
				}
			}
			Archetype.WildcardsOwned = new ReadOnlyDictionary<CardRarity, int>(_wildcardsOwned);

			_logger.Debug("Player Collection Loaded with {0} Cards", _playerInventory.Count);

			Dispatcher.Invoke(() =>
			{
				ProcessingCollection.Visibility = Visibility.Collapsed;
				ComputingSuggestions.Visibility = Visibility.Visible;
			});

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
				var replacementsToFind = mainDeckToCollect.Concat(sideboardToCollect).GroupBy(x => x.Key).Select(x => new { Id = x.Key, Count = x.Sum(y => y.Value) });
				foreach (var find in replacementsToFind)
				{
					_logger.Debug("Generating replacements suggestions for card with Arena Id: {0} (need {1})", find.Id, find.Count);
					var cardToReplace = cardsById[find.Id];
					var replacementsNeeded = find.Count;

					_logger.Debug("Processing Candidates based on Type and Cost (or Color for lands)");
					var candidates = cardToReplace.Type.Contains("Land") ?
						playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
							Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && _colorsByLand[x.Card.Name] == _colorsByLand[cardToReplace.Name]).
							OrderByDescending(x => x.Card.Rank) :
						playerInventory.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
							Where(x => x.Quantity > 0 && x.Card.Type == cardToReplace.Type && x.Card.Cost == cardToReplace.Cost).
							OrderByDescending(x => x.Card.Rank);

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
							Where(x => x.Quantity > 0 && x.Card.Cost == cardToReplace.Cost).
							OrderByDescending(x => x.Card.Rank);

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
							Where(x => x.Quantity > 0 && x.Card.Cmc == cardToReplace.Cmc && x.Card.Colors == cardToReplace.Colors).
							OrderByDescending(x => x.Card.Rank);

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
								Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && x.Card.Colors == cardToReplace.Colors).
								OrderByDescending(x => x.Card.Rank);

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
								Where(x => x.Quantity > 0 && x.Card.Cmc == cmcToTest && cardToReplace.Colors.Contains(x.Card.Colors)).
								OrderByDescending(x => x.Card.Rank);

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

					if(replacementsNeeded > 0)
					{
						_logger.Debug("Insufficient Candidates Found, done looking");
					}
				}

				_logger.Debug("Updating Archetype objects with suggestions");
				archetype.SuggestedMainDeck = new ReadOnlyDictionary<int, int>(mainDeckCards);
				archetype.SuggestedSideboard = new ReadOnlyDictionary<int, int>(sideboardCards);
				archetype.MainDeckToCollect = new ReadOnlyDictionary<int, int>(mainDeckToCollect);
				archetype.SideboardToCollect = new ReadOnlyDictionary<int, int>(sideboardToCollect);
				archetype.SuggestedReplacements = suggestedReplacements.AsReadOnly();
			}

			_logger.Debug("Sorting Archetypes and generating Meta Report");
			_orderedArchetypes = _archetypes.OrderBy(x => x.BoosterCostAfterWC).ThenBy(x => x.BoosterCost);
			MetaReport report = new MetaReport(cardsByName, _cardStats, cardsById, _playerInventoryCounts, _archetypes);
			_tabObjects.Add(report);
			_tabObjects.AddRange(_orderedArchetypes);

			_logger.Debug("Finished Computing Suggestions, Updating GUI");

			Dispatcher.Invoke(() =>
			{
				ComputingSuggestions.Visibility = Visibility.Collapsed;
				FilterPanel.Visibility = Visibility.Visible;
				DeckTabs.Visibility = Visibility.Visible;
				DeckTabs.ItemsSource = _tabObjects;
				DeckTabs.SelectedIndex = 0;
			});

			_logger.Debug("ReloadAndCrunchAllData() Finished");
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_logger.Debug("Main Window Loaded - {0}", "Main Application");

			AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
			string assemblyVersion = assemblyName.Version.ToString();
			string assemblyArchitecture = assemblyName.ProcessorArchitecture.ToString();

			Title = Title + $" - ({assemblyVersion}, {assemblyArchitecture})";

			App application = (App)Application.Current;
			Format.Value = application.State.LastFormat;
			if(string.IsNullOrWhiteSpace(Format.Value))
			{
				Format.Value = "Standard";
				application.State.LastFormat = Format.Value;
				application.SaveState();
			}
			Format.PropertyChanged += Format_PropertyChanged;

			Task loadTask = new Task(() => {
				_logger.Debug("Initializing Card Database");
				CardDatabase.Initialize();

				PopulateColorsByLand();

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

		private void Format_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			_logger.Debug("New Format Selected, Format={0}", Format.Value);

			App application = (App)Application.Current;
			application.State.LastFormat = Format.Value;
			application.SaveState();

			Dispatcher.Invoke(() =>
			{
				FilterPanel.Visibility = Visibility.Collapsed;
				DeckTabs.Visibility = Visibility.Collapsed;
				DeckTabs.ItemsSource = null;
				LoadingDatabase.Visibility = Visibility.Visible;
			});

			Task loadTask = new Task(() => { ReloadAndCrunchAllData(); });
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

		private void Export_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var item = (Archetype)button.DataContext;

			_logger.Debug("Deck Export Clicked, Deck={0}", item.Name);

			Clipboard.SetText(item.ExportList);
			MessageBox.Show($"{item.Name} Export Successful", "Export");
		}

		private void ExportSuggested_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var item = (Archetype)button.DataContext;

			_logger.Debug("Deck Export Suggested Clicked, Deck={0}", item.Name);

			Clipboard.SetText(item.ExportListSuggested);
			MessageBox.Show($"{item.Name} Export (with Replacements) Successful", "Export");
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			Hyperlink link = (Hyperlink)sender;
			var item = (Archetype)link.DataContext;

			_logger.Debug("Deck Hyperlink Clicked, Deck={0}", item.Name);

			DeckTabs.SelectedItem = item;
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			_logger.Debug("Refresh Clicked");

			Dispatcher.Invoke(() =>
			{
				FilterPanel.Visibility = Visibility.Collapsed;
				DeckTabs.Visibility = Visibility.Collapsed;
				DeckTabs.ItemsSource = null;
				LoadingDatabase.Visibility = Visibility.Visible;
			});

			Task loadTask = new Task(() => { ReloadAndCrunchAllData(); });
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

		private void HardRefresh_Click(object sender, RoutedEventArgs e)
		{
			_logger.Debug("Hard Refresh Clicked");

			Dispatcher.Invoke(() =>
			{
				FilterPanel.Visibility = Visibility.Collapsed;
				DeckTabs.Visibility = Visibility.Collapsed;
				DeckTabs.ItemsSource = null;
				LoadingDatabase.Visibility = Visibility.Visible;
			});

			Task loadTask = new Task(() => {
				_logger.Debug("Initializing Card Database");
				CardDatabase.Initialize();

				PopulateColorsByLand();

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
	}
}
