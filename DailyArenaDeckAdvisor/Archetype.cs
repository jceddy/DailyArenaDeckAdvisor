using DailyArenaDeckAdvisor.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace DailyArenaDeckAdvisor
{
	/// <summary>
	/// Class that represents a Magic deck archetype.
	/// </summary>
	public class Archetype
	{
		/// <summary>
		/// Class that represents a "view" version of a card, used to simplify Xaml object binding.
		/// </summary>
		public class CardView
		{
			/// <summary>
			/// The Card object represented by the view.
			/// </summary>
			public Card Card { get; set; }

			/// <summary>
			/// The quanitity of the Card contained by the archetype property the view was generated for.
			/// </summary>
			public int Quantity { get; set; }

			/// <summary>
			/// Whether this view reprents a Card that the player owns.
			/// </summary>
			public bool Collected { get; set; }

			/// <summary>
			/// An objest that contains meta statistics for the card represented by this view.
			/// </summary>
			public CardStats Stats { get; set; }
		}

		/// <summary>
		/// Gets the name of the deck archetype.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets a readonly dictionary containing the names and quantities of cards in the the main deck list.
		/// </summary>
		public ReadOnlyDictionary<string, int> MainDeck { get; private set; }

		/// <summary>
		/// Gets readonly dictionary containing the names and quantities of cards in the sideboard.
		/// </summary>
		public ReadOnlyDictionary<string, int> Sideboard { get; private set; }

		/// <summary>
		/// The Arena Ids and quantities of cards still to be collected for the main deck list.
		/// </summary>
		private ReadOnlyDictionary<int, int> _mainDeckToCollect = null;

		/// <summary>
		/// Gets or sets the Arena Ids and quantities of cards still to be collected for the main deck list.
		/// </summary>
		public ReadOnlyDictionary<int, int> MainDeckToCollect
		{
			get
			{
				return _mainDeckToCollect;
			}
			set
			{
				if(_mainDeckToCollect == null)
				{
					_mainDeckToCollect = value;
				}
				else
				{
					throw new InvalidOperationException("MainDeckToCollect can only be set once.");
				}
			}
		}

		/// <summary>
		/// The Arena Ids and quantities of cards still to be collected for the sideboard.
		/// </summary>
		private ReadOnlyDictionary<int, int> _sideboardToCollect = null;

		/// <summary>
		/// Gets or sets the Arena Ids and quantities of cards still to be collected for the sideboard.
		/// </summary>
		public ReadOnlyDictionary<int, int> SideboardToCollect
		{
			get
			{
				return _sideboardToCollect;
			}
			set
			{
				if (_sideboardToCollect == null)
				{
					_sideboardToCollect = value;
				}
				else
				{
					throw new InvalidOperationException("SideboardToCollect can only be set once.");
				}
			}
		}

		/// <summary>
		/// Gets a unique identifier for the deck archetype.
		/// </summary>
		public long Index { get; private set; } = NextIndex++;

		/// <summary>
		/// Archetype constructor.
		/// </summary>
		/// <param name="name">The name of the deck archetype.</param>
		/// <param name="mainDeck">The names and quantities of cards in the the main deck list.</param>
		/// <param name="sideboard">The names and quantities of cards in the sideboard.</param>
		public Archetype(string name, Dictionary<string, int> mainDeck, Dictionary<string, int> sideboard)
		{
			Name = name;
			MainDeck = new ReadOnlyDictionary<string, int>(mainDeck);
			Sideboard = new ReadOnlyDictionary<string, int>(sideboard);
		}

		/// <summary>
		/// The average number of boosters the player will need to open to collect the cards for the deck (assuming they don't spend wildcards).
		/// </summary>
		private double _boosterCost = -1;

		/// <summary>
		/// Gets the average number of boosters the player will need to open to collect the cards for the deck (assuming they don't spend wildcards).
		/// </summary>
		public double BoosterCost
		{
			get
			{
				if(_boosterCost < 0 && _mainDeckToCollect != null && _sideboardToCollect != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					_boosterCost = _mainDeckToCollect.Sum(x => cardsById[x.Key].BoosterCost * x.Value) +
						_sideboardToCollect.Sum(x => cardsById[x.Key].BoosterCost * x.Value);
				}
				return _boosterCost;
			}
		}

		/// <summary>
		/// The average number of boosters the player will need to open to collect the cards or wildcards needed to build the deck.
		/// </summary>
		private double _boosterCostAfterWC = -1;

		/// <summary>
		/// Gets the average number of boosters the player will need to open to collect the cards or wildcards needed to build the deck.
		/// </summary>
		public double BoosterCostAfterWC
		{
			get
			{
				if (_boosterCostAfterWC < 0 && _mainDeckToCollect != null && _sideboardToCollect != null && _wildcardsOwned != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					var cardsToSelect = _mainDeckToCollect.Select(x => new { cardsById[x.Key].Rarity, Card = cardsById[x.Key], Count = x.Value }).
						Concat(_sideboardToCollect.Select(x => new { cardsById[x.Key].Rarity, Card = cardsById[x.Key], Count = x.Value })).
						GroupBy(x => x.Rarity).
						Select(x => new { Rarity = x.Key, Cards = x.SelectMany(y => Enumerable.Repeat(y.Card, y.Count)).OrderBy(z => z.BoosterCost).ToList() }).
						ToDictionary(x => x.Rarity, y => y.Cards);

					List<Card> finalListOfCardsToCollect = new List<Card>();

					int common = _wildcardsOwned[CardRarity.Common];
					var commonCards = cardsToSelect.ContainsKey(CardRarity.Common) ? cardsToSelect[CardRarity.Common] : new List<Card>();
					for (int i = 0; i < commonCards.Count; i++)
					{
						if (common == 0)
						{
							finalListOfCardsToCollect.AddRange(commonCards.GetRange(i, commonCards.Count - i));
							break;
						}
						common--;
					}

					int uncommon = _wildcardsOwned[CardRarity.Uncommon];
					var uncommonCards = cardsToSelect.ContainsKey(CardRarity.Uncommon) ? cardsToSelect[CardRarity.Uncommon] : new List<Card>();
					for (int i = 0; i < uncommonCards.Count; i++)
					{
						if (uncommon == 0)
						{
							finalListOfCardsToCollect.AddRange(uncommonCards.GetRange(i, uncommonCards.Count - i));
							break;
						}
						uncommon--;
					}

					int rare = _wildcardsOwned[CardRarity.Rare];
					var rareCards = cardsToSelect.ContainsKey(CardRarity.Rare) ? cardsToSelect[CardRarity.Rare] : new List<Card>();
					for (int i = 0; i < rareCards.Count; i++)
					{
						if (rare == 0)
						{
							finalListOfCardsToCollect.AddRange(rareCards.GetRange(i, rareCards.Count - i));
							break;
						}
						rare--;
					}

					int mythic = _wildcardsOwned[CardRarity.MythicRare];
					var mythicCards = cardsToSelect.ContainsKey(CardRarity.MythicRare) ? cardsToSelect[CardRarity.MythicRare] : new List<Card>();
					for (int i = 0; i < mythicCards.Count; i++)
					{
						if (mythic == 0)
						{
							finalListOfCardsToCollect.AddRange(mythicCards.GetRange(i, mythicCards.Count - i));
							break;
						}
						mythic--;
					}

					_boosterCostAfterWC = finalListOfCardsToCollect.Sum(x => x.BoosterCost);
				}
				return _boosterCostAfterWC;
			}
		}

		/// <summary>
		/// A list of the number of wildcards of each rarity needed to complete the deck, along with the number of each rarity the player already owns.
		/// </summary>
		private IReadOnlyList<Tuple<CardRarity, int, int>> _wildcardsNeeded = null;

		/// <summary>
		/// Gets a list of the number of wildcards of each rarity needed to complete the deck, along with the number of each rarity the player already owns.
		/// </summary>
		public IReadOnlyList<Tuple<CardRarity, int, int>> WildcardsNeeded
		{
			get
			{
				if (_wildcardsNeeded == null && _mainDeckToCollect != null && _sideboardToCollect != null && _wildcardsOwned != null &&
					_suggestedMainDeck != null && _suggestedSideboard != null && _anyNumber != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					_wildcardsNeeded = _mainDeckToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = _anyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (_suggestedMainDeck.Where(y => y.Key == x.Key).Count() + _suggestedSideboard.Where(z => z.Key == x.Key).Count()) : x.Value }).
						Concat(_sideboardToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = _anyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (_suggestedMainDeck.Where(y => y.Key == x.Key).Count() + _suggestedSideboard.Where(z => z.Key == x.Key).Count()) : x.Value })).
						GroupBy(x => x.Rarity).
						Select(x => new Tuple<CardRarity, int, int>(x.Key, x.Sum(y => y.Count), _wildcardsOwned[x.Key])).
						OrderByDescending(x => x.Item1).ToList();
				}
				return _wildcardsNeeded;
			}
		}

		/// <summary>
		/// The total number of wildcards needed to complete the deck.
		/// </summary>
		private int _totalWildcardsNeeded = -1;

		/// <summary>
		/// Gets the total number of wildcards needed to complete the deck.
		/// </summary>
		public int TotalWildcardsNeeded
		{
			get
			{
				if (_totalWildcardsNeeded < 0)
				{
					int? wc = WildcardsNeeded?.Sum(x => x.Item2);
					if(wc.HasValue)
					{
						_totalWildcardsNeeded = wc.Value;
					}
				}
				return _totalWildcardsNeeded;
			}
		}

		/// <summary>
		/// The Arena Ids and quantities of suggested cards to use in the main deck list.
		/// </summary>
		private ReadOnlyDictionary<int, int> _suggestedMainDeck = null;

		/// <summary>
		/// Gets or sets the Arena Ids and quantities of suggested cards to use in the main deck list.
		/// </summary>
		public ReadOnlyDictionary<int, int> SuggestedMainDeck
		{
			get
			{
				return _suggestedMainDeck;
			}
			set
			{
				if (_suggestedMainDeck == null)
				{
					_suggestedMainDeck = value;
				}
				else
				{
					throw new InvalidOperationException("SuggestedMainDeck can only be set once.");
				}
			}
		}

		/// <summary>
		/// The Arena Ids and quantities of suggested cards to use in the sideboard.
		/// </summary>
		private ReadOnlyDictionary<int, int> _suggestedSideboard = null;

		/// <summary>
		/// Gets or sets the Arena Ids and quantities of suggested cards to use in the sideboard.
		/// </summary>
		public ReadOnlyDictionary<int, int> SuggestedSideboard
		{
			get
			{
				return _suggestedSideboard;
			}
			set
			{
				if (_suggestedSideboard == null)
				{
					_suggestedSideboard = value;
				}
				else
				{
					throw new InvalidOperationException("SuggestedSideboard can only be set once.");
				}
			}
		}

		/// <summary>
		/// A list of suggested cards to replaced missing ones.
		/// Item1 => Arena Id of the missing card.
		/// Item2 => Arena Id of the suggested replacement card.
		/// Item3 => Quantity of cards to replace this way.
		/// </summary>
		private IReadOnlyList<Tuple<int, int, int>> _suggestedReplacements = null;

		/// <summary>
		/// Gets or sets a list of suggested cards to replaced missing ones.
		/// Item1 => Arena Id of the missing card.
		/// Item2 => Arena Id of the suggested replacement card.
		/// Item3 => Quantity of cards to replace this way.
		/// </summary>
		public IReadOnlyList<Tuple<int, int, int>> SuggestedReplacements
		{
			get
			{
				return _suggestedReplacements;
			}
			set
			{
				if (_suggestedReplacements == null)
				{
					_suggestedReplacements = value;
				}
				else
				{
					throw new InvalidOperationException("SuggestedReplacements can only be set once.");
				}
			}
		}

		/// <summary>
		/// A list of suggested cards to replaced missing ones with links to Card objects for use in Xaml binding.
		/// Item1 => Arena Id of the missing card.
		/// Item2 => Arena Id of the suggested replacement card.
		/// Item3 => Quantity of cards to replace this way.
		/// Item4 => The missing Card.
		/// Item5 => The suggested replacement card.
		/// </summary>
		private IReadOnlyList<Tuple<string, string, int, Card, Card>> _suggestedReplacementsView = null;

		/// <summary>
		/// Gets a list of suggested cards to replaced missing ones with links to Card objects for use in Xaml binding.
		/// Item1 => Arena Id of the missing card.
		/// Item2 => Arena Id of the suggested replacement card.
		/// Item3 => Quantity of cards to replace this way.
		/// Item4 => The missing Card.
		/// Item5 => The suggested replacement card.
		/// </summary>
		public IReadOnlyList<Tuple<string, string, int, Card, Card>> SuggestedReplacementsView
		{
			get
			{
				if (_suggestedReplacementsView == null && _suggestedReplacements != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					_suggestedReplacementsView = _suggestedReplacements.
						Select(x => new Tuple<string, string, int, Card, Card>(
							cardsById[x.Item1].Name, cardsById[x.Item2].Name, x.Item3, cardsById[x.Item1], cardsById[x.Item2])
						).ToList().AsReadOnly();
				}
				return _suggestedReplacementsView;
			}
		}

		/// <summary>
		/// A list of objects representing a view into the main deck list, for Xaml binding.
		/// </summary>
		private IReadOnlyList<CardView> _mainDeckView = null;

		/// <summary>
		/// Gets a list of objects representing a view into the main deck list, for Xaml binding.
		/// </summary>
		public IReadOnlyList<CardView> MainDeckView
		{
			get
			{
				if (_mainDeckView == null && _mainDeckToCollect != null && _suggestedMainDeck != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					_mainDeckView = _mainDeckToCollect.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = false, Stats = CardStats[cardsById[x.Key]] }).
						Concat(_suggestedMainDeck.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = true, Stats = CardStats[cardsById[x.Key]] }))
						.ToList().AsReadOnly();
				}
				return _mainDeckView;
			}
		}

		/// <summary>
		/// The visibility of the sideboard (used to hide the sideboards section on the GUI when there are no cards to show).
		/// </summary>
		public Visibility SideboardVisibility
		{
			get
			{
				return (SideboardToCollect.Count + SuggestedSideboard.Count) == 0 ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		/// <summary>
		/// The visibility of the "cards to collect" section (used to hide that section on the GUI when there are no cards to show).
		/// </summary>
		public Visibility CollectVisibility
		{
			get
			{
				return (SideboardToCollect.Count + MainDeckToCollect.Count) == 0 ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		/// <summary>
		/// A list of objects representing a view into the sideboard, for Xaml binding.
		/// </summary>
		private IReadOnlyList<CardView> _sideboardView = null;

		/// <summary>
		/// Gets a list of objects representing a view into the sideboard, for Xaml binding.
		/// </summary>
		public IReadOnlyList<CardView> SideboardView
		{
			get
			{
				if (_sideboardView == null && _sideboardToCollect != null && _suggestedSideboard != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					_sideboardView = _sideboardToCollect.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = false, Stats = CardStats[cardsById[x.Key]] }).
						Concat(_suggestedSideboard.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = true, Stats = CardStats[cardsById[x.Key]] }))
						.ToList().AsReadOnly();
				}
				return _sideboardView;
			}
		}

		/// <summary>
		/// The string containing the deck in Arena import/export format.
		/// </summary>
		private string _exportList = null;

		/// <summary>
		/// Gets the string containing the deck in Arena import/export format.
		/// </summary>
		public string ExportList
		{
			get
			{
				if (_exportList == null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					var mainDeck = SuggestedMainDeck.Concat(MainDeckToCollect).GroupBy(x => x.Key).
						Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
						Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");
					var sideboard = SuggestedSideboard.Concat(SideboardToCollect).GroupBy(x => x.Key).
						Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
						Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");

					_exportList = string.Format("{0}{1}{1}{2}", string.Join(Environment.NewLine, mainDeck), Environment.NewLine, string.Join(Environment.NewLine, sideboard));
				}
				return _exportList;
			}
		}

		/// <summary>
		/// The string containing the deck (wisth substitutions) in Arena import/export format.
		/// </summary>
		private string _exportListSuggested = null;

		/// <summary>
		/// Gets the string containing the deck (wisth substitutions) in Arena import/export format.
		/// </summary>
		public string ExportListSuggested
		{
			get
			{
				if (_exportListSuggested == null)
				{
					Dictionary<int, int> mainDeckReplacements = new Dictionary<int, int>();
					Dictionary<int, int> sideboardReplacements = new Dictionary<int, int>();
					foreach (Tuple<int, int, int> replacement in SuggestedReplacements)
					{
						int replacementCount = replacement.Item3;
						if (MainDeckToCollect.ContainsKey(replacement.Item1))
						{
							if (MainDeckToCollect[replacement.Item1] >= replacementCount)
							{
								if (mainDeckReplacements.ContainsKey(replacement.Item2))
								{
									mainDeckReplacements[replacement.Item2] += replacementCount;
								}
								else
								{
									mainDeckReplacements.Add(replacement.Item2, replacementCount);
								}
								replacementCount = 0;
							}
							else
							{
								if (mainDeckReplacements.ContainsKey(replacement.Item2))
								{
									mainDeckReplacements[replacement.Item2] += MainDeckToCollect[replacement.Item1];
								}
								else
								{
									mainDeckReplacements.Add(replacement.Item2, MainDeckToCollect[replacement.Item1]);
								}
								replacementCount -= MainDeckToCollect[replacement.Item1];
							}
						}
						if (replacementCount > 0)
						{
							if (sideboardReplacements.ContainsKey(replacement.Item2))
							{
								sideboardReplacements[replacement.Item2] += replacementCount;
							}
							else
							{
								sideboardReplacements.Add(replacement.Item2, replacementCount);
							}
						}
					}

					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					var mainDeck = SuggestedMainDeck.Concat(mainDeckReplacements).GroupBy(x => x.Key).
						Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
						Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");
					var sideboard = SuggestedSideboard.Concat(sideboardReplacements).GroupBy(x => x.Key).
						Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
						Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");

					_exportListSuggested = string.Format("{0}{1}{1}{2}", string.Join(Environment.NewLine, mainDeck), Environment.NewLine, string.Join(Environment.NewLine, sideboard));
				}
				return _exportListSuggested;
			}
		}

		/// <summary>
		/// The name of the next suggested set to purchase a booster from to complete the deck.
		/// </summary>
		private string _nextBoosterSetToPurchase = null;

		/// <summary>
		/// Gets the name of the next suggested set to purchase a booster from to complete the deck.
		/// </summary>
		public string NextBoosterSetToPurchase
		{
			get
			{
				if (_nextBoosterSetToPurchase == null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					var orderedSets = MainDeckToCollect.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
						Concat(SideboardToCollect.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value })).
						Select(x => new { SetName = x.Card.Set.Name, BoosterCost = x.Card.BoosterCost * x.Quantity }).
						GroupBy(x => x.SetName).
						Select(x => new { SetName = x.Key, BoosterCost = x.Sum(y => y.BoosterCost) }).
						OrderByDescending(x => x.BoosterCost);

					if (orderedSets.Count() > 0)
					{
						_nextBoosterSetToPurchase = orderedSets.First().SetName;
					}

					_nextBoosterSetToPurchase = string.Empty;
				}
				return _nextBoosterSetToPurchase;
			}
		}

		/// <summary>
		/// The number of wildcards of each rarity the player owns.
		/// </summary>
		private static ReadOnlyDictionary<CardRarity, int> _wildcardsOwned = null;

		/// <summary>
		/// Gets or sets the number of wildcards of each rarity the player owns.
		/// </summary>
		public static ReadOnlyDictionary<CardRarity, int> WildcardsOwned
		{
			get
			{
				return _wildcardsOwned;
			}
			set
			{
				if (_wildcardsOwned == null)
				{
					_wildcardsOwned = value;
				}
				else
				{
					throw new InvalidOperationException("WildcardsOwned can only be set once.");
				}
			}
		}

		/// <summary>
		/// Clear the list of wildcards the player owns (if refresh is clicked, etc.);
		/// </summary>
		public static void ClearWildcardsOwned()
		{
			_wildcardsOwned = null;
		}

		/// <summary>
		/// A dictionary mapping card objects to their meta stats.
		/// </summary>
		private static ReadOnlyDictionary<Card, CardStats> _cardStats = null;

		/// <summary>
		/// Gets or sets a dictionary mapping card objects to their meta stats.
		/// </summary>
		public static ReadOnlyDictionary<Card, CardStats> CardStats
		{
			get
			{
				return _cardStats;
			}
			set
			{
				if (_cardStats == null)
				{
					_cardStats = value;
				}
				else
				{
					throw new InvalidOperationException("CardStats can only be set once.");
				}
			}
		}

		/// <summary>
		/// Clear the dictionary mapping card objects to their meta stats.
		/// </summary>
		public static void ClearCardStats()
		{
			_cardStats = null;
		}

		/// <summary>
		/// A list of names of non-basic land cards that can show up in any number.
		/// </summary>
		private static IReadOnlyList<string> _anyNumber = null;

		/// <summary>
		/// Gets or sets a list of names of non-basic land cards that can show up in any number.
		/// </summary>
		public static IReadOnlyList<string> AnyNumber
		{
			get
			{
				return _anyNumber;
			}
			set
			{
				if (_anyNumber == null)
				{
					_anyNumber = value;
				}
				else
				{
					throw new InvalidOperationException("AnyNumber can only be set once.");
				}
			}
		}

		/// <summary>
		/// Static field containing the unique index to be used for the next deck archetype object created.
		/// </summary>
		private static long NextIndex { get; set; } = 0;
	}
}
