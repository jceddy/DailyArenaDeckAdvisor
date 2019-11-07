using DailyArena.Common.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace DailyArena.DeckAdvisor
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

			/// <summary>
			/// The state of the "rotation-proof" toggle.
			/// </summary>
			public bool RotationProof { get; set; }
		}

		/// <summary>
		/// Determines whether the tab for this archetype is clickable.
		/// </summary>
		public bool TabEnabled { get; private set; }

		/// <summary>
		///  Gets or sets a list of alternate deck configurations for the same archetype.
		/// </summary>
		public ReadOnlyCollection<Archetype> SimilarDecks { get; private set; }

		/// <summary>
		/// Gets or sets a visibility modifier for alternate deck configuration information on the GUI.
		/// </summary>
		public Visibility SimilarDecksVisibility { get; private set; }

		/// <summary>
		/// Re-sort the Similar Decks collection.
		/// </summary>
		public void SortSimilarDecks()
		{
			SimilarDecks = SimilarDecks.OrderBy(x => x.BoosterCostAfterWC).ThenBy(x => x.BoosterCost).ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets or sets a link back to the main deck for this deck's archetype.
		/// </summary>
		public Archetype Parent { get; protected set; }

		/// <summary>
		/// Gets or sets a visibility modifier for the Parent deck link on the GUI.
		/// </summary>
		public Visibility ParentVisibility { get; protected set; } = Visibility.Collapsed;

		/// <summary>
		/// Gets the name of the deck archetype.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets whether we favor "rotation-proof" cards.
		/// </summary>
		public bool RotationProof { get; private set; }

		/// <summary>
		/// The number of recorded wins for this archetype.
		/// </summary>
		public int Win { get; private set; }

		/// <summary>
		/// The number of recorded losses for this archetype.
		/// </summary>
		public int Loss { get; private set; }

		/// <summary>
		/// Gets or sets the win rate percentage for this archetype.
		/// </summary>
		public double WinRate { get; private set; }

		/// <summary>
		/// A string version of the win/loss information for this deck, for Xaml binding.
		/// </summary>
		public string WinLossView { get; private set; }

		/// <summary>
		/// Visibility modifier for win/loss information on the GUI.
		/// </summary>
		public Visibility WinLossVisibility { get; private set; }

		/// <summary>
		/// Gets a readonly dictionary containing the names and quantities of cards in the the main deck list.
		/// </summary>
		public ReadOnlyDictionary<string, int> MainDeck { get; private set; }

		/// <summary>
		/// Gets a readonly dictionary containing the names and quantities of cards in the sideboard.
		/// </summary>
		public ReadOnlyDictionary<string, int> Sideboard { get; private set; }

		/// <summary>
		/// Gets a readonly dictionary containing the names and quanitites of cards in the command zone.
		/// </summary>
		public ReadOnlyDictionary<string, int> CommandZone { get; private set; }

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
		/// The Arena Ids and quantities of cards still to be collected for the command zone.
		/// </summary>
		private ReadOnlyDictionary<int, int> _commandZoneToCollect = null;

		/// <summary>
		/// Gets or sets the Arena Ids and quantities of cards still to be collected for the command zone.
		/// </summary>
		public ReadOnlyDictionary<int, int> CommandZoneToCollect
		{
			get
			{
				return _commandZoneToCollect;
			}
			set
			{
				if(_commandZoneToCollect == null)
				{
					_commandZoneToCollect = value;
				}
				else
				{
					throw new InvalidOperationException("CommandZoneToCollect can only be set once.");
				}
			}
		}

		/// <summary>
		/// Gets a unique identifier for the deck archetype.
		/// </summary>
		public long Index { get; private set; } = NextIndex++;

		/// <summary>
		/// Whether commander info should be shown.
		/// </summary>
		public Visibility CommanderVisibility { get; private set; }

		/// <summary>
		/// The commander's color identity (for Brawl).
		/// </summary>
		private CardColors _commanderColorIdentity = null;

		/// <summary>
		/// Gets the commander's color identity (for Brawl).
		/// </summary>
		public CardColors CommanderColorIdentity
		{
			get
			{
				if (_commanderColorIdentity == null)
				{
					ReadOnlyDictionary<string, List<Card>> cardsByName = Card.CardsByName;
					Dictionary<string, bool> colorsInIdentity = new Dictionary<string, bool>()
					{
						{ "W", false },
						{ "U", false },
						{ "B", false },
						{ "R", false },
						{ "G", false }
					};
					foreach(string cardName in CommandZone.Keys)
					{
						CardColors colorIdentity = cardsByName[cardName][0].ColorIdentity;
						colorsInIdentity["W"] = colorsInIdentity["W"] || colorIdentity.IsWhite;
						colorsInIdentity["U"] = colorsInIdentity["U"] || colorIdentity.IsBlue;
						colorsInIdentity["B"] = colorsInIdentity["B"] || colorIdentity.IsBlack;
						colorsInIdentity["R"] = colorsInIdentity["R"] || colorIdentity.IsRed;
						colorsInIdentity["G"] = colorsInIdentity["G"] || colorIdentity.IsGreen;
					}
					string colorString = (colorsInIdentity["W"] ? "W" : string.Empty) +
						(colorsInIdentity["U"] ? "U" : string.Empty) +
						(colorsInIdentity["B"] ? "B" : string.Empty) +
						(colorsInIdentity["R"] ? "R" : string.Empty) +
						(colorsInIdentity["G"] ? "G" : string.Empty);
					_commanderColorIdentity = CardColors.CardColorFromString(colorString);
				}
				return _commanderColorIdentity;
			}
		}

		/// <summary>
		/// Gets a boolean indicating whether this deck was imported from the player's inventory.
		/// </summary>
		public bool IsPlayerDeck { get; private set; }

		/// <summary>
		/// Archetype constructor for disabled "header"tabs.
		/// </summary>
		/// <param name="name">The name of the deck archetype section.</param>
		/// <param name="isPlayerDeck">Boolean indicating whether decks in this section were imported from the player inventory.</param>
		/// <param name="boosterCost">Double indicating whether decks in this section have a booster cost.</param>
		/// <param name="totalWildcardsNeeded">Int indicating whether decks in this section need wildcards.</param>
		/// <remarks>This is a little bit of a cludge, but works well enough that I'll probably just leave it.</remarks>
		public Archetype(string name, bool isPlayerDeck, double boosterCost, int totalWildcardsNeeded)
		{
			TabEnabled = false;
			Name = name;
			IsPlayerDeck = isPlayerDeck;
			_boosterCost = boosterCost;
			_totalWildcardsNeeded = totalWildcardsNeeded;
		}

		/// <summary>
		/// Archetype constructor.
		/// </summary>
		/// <param name="name">The name of the deck archetype.</param>
		/// <param name="mainDeck">The names and quantities of cards in the the main deck list.</param>
		/// <param name="sideboard">The names and quantities of cards in the sideboard.</param>
		/// <param name="commandZone">The names and quanitities of cards in the command zone.</param>
		/// <param name="rotationProof">Whether we favor "rotation-proof" cards.</param>
		/// <param name="win">The number of recorded wins for this archetype (if available).</param>
		/// <param name="loss">The number of recorded losses for this archetype (if available).</param>
		/// <param name="similarDecks">List of alternate deck configurations for this archetype.</param>
		/// <param name="isPlayerDeck">Boolean indicating whether this is a deck that was imported from the player inventory.</param>
		/// <param name="setNameTranslations">A dictionary containing set name translations for various languages.</param>
		public Archetype(string name, Dictionary<string, int> mainDeck, Dictionary<string, int> sideboard, Dictionary<string, int> commandZone, bool rotationProof, int win = -1,
			int loss = -1, List<Archetype> similarDecks = null, bool isPlayerDeck = false, Dictionary<string, Dictionary<string, string>> setNameTranslations = null)
		{
			TabEnabled = true;
			Name = name;
			MainDeck = new ReadOnlyDictionary<string, int>(mainDeck);
			Sideboard = new ReadOnlyDictionary<string, int>(sideboard);
			CommandZone = new ReadOnlyDictionary<string, int>(commandZone);
			RotationProof = rotationProof;
			Win = win;
			Loss = loss;
			WinRate = (double)win / (win + loss);

			if (win == -1 || loss == -1)
			{
				WinLossView = string.Empty;
				WinLossVisibility = Visibility.Collapsed;
			}
			else
			{
				WinLossView = $"{Properties.Resources.Archetype_Win}: {win}, {Properties.Resources.Archetype_Loss}: {loss}, {Properties.Resources.Archetype_Total}: {win + loss} ({WinRate:P})";
				WinLossVisibility = Visibility.Visible;
			}

			if(commandZone.Count > 0)
			{
				CommanderVisibility = Visibility.Visible;
			}
			else
			{
				CommanderVisibility = Visibility.Collapsed;
			}

			if(similarDecks == null || similarDecks.Count == 0)
			{
				SimilarDecksVisibility = Visibility.Collapsed;
			}
			else
			{
				foreach (Archetype similar in similarDecks)
				{
					similar.Parent = this;
					similar.ParentVisibility = Visibility.Visible;
				}

				SimilarDecks = similarDecks.AsReadOnly();
				SimilarDecksVisibility = Visibility.Visible;
			}

			IsPlayerDeck = isPlayerDeck;
			_setNameTranslations = setNameTranslations;
		}

		/// <summary>
		/// The average number of boosters a player would need to open to collect the cards for the deck, starting from an empty collection.
		/// </summary>
		private double _totalBoosterCost = -1;

		/// <summary>
		/// Gets the average number of boosters a player would need to open to collect the cards for the deck, starting from an empty collection.
		/// </summary>
		public double TotalBoosterCost
		{
			get
			{
				if (_totalBoosterCost < 0 && _mainDeckToCollect != null && _sideboardToCollect != null && _commandZoneToCollect != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					_totalBoosterCost = _mainDeckToCollect.Sum(x => cardsById[x.Key].BoosterCost * Math.Min(x.Value, 4)) +
						_sideboardToCollect.Sum(x => cardsById[x.Key].BoosterCost * Math.Min(x.Value, 4)) +
						_commandZoneToCollect.Sum(x => cardsById[x.Key].BoosterCost * Math.Min(x.Value, 4)) +
						_suggestedMainDeck.Sum(x => cardsById[x.Key].BoosterCost * Math.Min(x.Value, 4)) +
						_suggestedSideboard.Sum(x => cardsById[x.Key].BoosterCost * Math.Min(x.Value, 4)) +
						_suggestedCommandZone.Sum(x => cardsById[x.Key].BoosterCost * Math.Min(x.Value, 4));
				}
				return _totalBoosterCost;
			}
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
				if(_boosterCost < 0 && _mainDeckToCollect != null && _sideboardToCollect != null && _suggestedCommandZone != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					_boosterCost = _mainDeckToCollect.Sum(x => cardsById[x.Key].BoosterCost * Math.Min(x.Value, 4)) +
						_sideboardToCollect.Sum(x => cardsById[x.Key].BoosterCost * Math.Min(x.Value, 4)) +
						_commandZoneToCollect.Sum(x => cardsById[x.Key].BoosterCost * Math.Min(x.Value, 4));
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
				if (_boosterCostAfterWC < 0 && _mainDeckToCollect != null && _sideboardToCollect != null && _commandZoneToCollect != null && _wildcardsOwned != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					var cardsToSelect = _mainDeckToCollect.Select(x => new { cardsById[x.Key].Rarity, Card = cardsById[x.Key], Count = Math.Min(x.Value, 4) }).
						Concat(_sideboardToCollect.Select(x => new { cardsById[x.Key].Rarity, Card = cardsById[x.Key], Count = Math.Min(x.Value, 4) })).
						Concat(_commandZoneToCollect.Select(x => new { cardsById[x.Key].Rarity, Card = cardsById[x.Key], Count = Math.Min(x.Value, 4) })).
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
					_wildcardsNeeded = _mainDeckToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = _anyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (_suggestedMainDeck.Where(y => y.Key == x.Key).Count() + _suggestedSideboard.Where(z => z.Key == x.Key).Count() + _suggestedCommandZone.Where(z => z.Key == x.Key).Count()) : x.Value }).
						Concat(_sideboardToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = _anyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (_suggestedMainDeck.Where(y => y.Key == x.Key).Count() + _suggestedSideboard.Where(z => z.Key == x.Key).Count() + _suggestedCommandZone.Where(z => z.Key == x.Key).Count()) : x.Value })).
						Concat(_commandZoneToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = _anyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (_suggestedMainDeck.Where(y => y.Key == x.Key).Count() + _suggestedSideboard.Where(z => z.Key == x.Key).Count() + _suggestedCommandZone.Where(z => z.Key == x.Key).Count()) : x.Value })).
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
					int? wc = WildcardsNeeded?.Sum(x => Math.Max(x.Item2 - _wildcardsOwned[x.Item1], 0));
					if(wc.HasValue)
					{
						_totalWildcardsNeeded = wc.Value;
					}
				}
				return _totalWildcardsNeeded;
			}
		}

		/// <summary>
		/// Total card counts by rarity.
		/// </summary>
		private Dictionary<CardRarity, int> _rarityCounts = null;

		/// <summary>
		/// Get the number of cards of the given rarity in the deck archetype.
		/// </summary>
		/// <param name="rarity">The rarity to count.</param>
		/// <returns>The number of cards of the requested rarity in the deck archetype.</returns>
		public int GetRarityCount(CardRarity rarity)
		{
			if(_rarityCounts == null)
			{
				if(_mainDeckToCollect != null && _sideboardToCollect != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					_rarityCounts = _mainDeckToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = _anyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (_suggestedMainDeck.Where(y => y.Key == x.Key).Count() + _suggestedSideboard.Where(z => z.Key == x.Key).Count() + _suggestedCommandZone.Where(z => z.Key == x.Key).Count()) : x.Value }).
						Concat(_sideboardToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = _anyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (_suggestedMainDeck.Where(y => y.Key == x.Key).Count() + _suggestedSideboard.Where(z => z.Key == x.Key).Count() + _suggestedCommandZone.Where(z => z.Key == x.Key).Count()) : x.Value })).
						Concat(_commandZoneToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = _anyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (_suggestedMainDeck.Where(y => y.Key == x.Key).Count() + _suggestedSideboard.Where(z => z.Key == x.Key).Count() + _suggestedCommandZone.Where(z => z.Key == x.Key).Count()) : x.Value })).
						Concat(_suggestedMainDeck.Select(x => new { cardsById[x.Key].Rarity, Count = Math.Min(x.Value, 4) })).
						Concat(_suggestedSideboard.Select(x => new { cardsById[x.Key].Rarity, Count = Math.Min(x.Value, 4) })).
						Concat(_suggestedCommandZone.Select(x => new { cardsById[x.Key].Rarity, Count = Math.Min(x.Value, 4) })).
						Concat(new List<CardRarity>() { CardRarity.MythicRare, CardRarity.Rare, CardRarity.Uncommon, CardRarity.Common }.Select(x => new { Rarity = x, Count = 0 })).
						GroupBy(x => x.Rarity).
						ToDictionary(x => x.Key, x => x.Sum(y => y.Count));
				}
				else
				{
					return -1;
				}
			}

			return _rarityCounts[rarity];
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
		/// The Arena Ids and quantities of suggested cards to use in the command zone.
		/// </summary>
		private ReadOnlyDictionary<int, int> _suggestedCommandZone = null;

		/// <summary>
		/// Gets or sets the Arena Ids and quantities of suggested cards to use in the command zone.
		/// </summary>
		public ReadOnlyDictionary<int, int> SuggestedCommandZone
		{
			get
			{
				return _suggestedCommandZone;
			}
			set
			{
				if (_suggestedCommandZone == null)
				{
					_suggestedCommandZone = value;
				}
				else
				{
					throw new InvalidOperationException("SuggestedCommandZone can only be set once.");
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
							cardsById[x.Item1].PrintedName, cardsById[x.Item2].PrintedName, x.Item3, cardsById[x.Item1], cardsById[x.Item2])
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
					_mainDeckView = _mainDeckToCollect.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = false, Stats = CardStats[cardsById[x.Key]], RotationProof = RotationProof }).
						Concat(_suggestedMainDeck.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = true, Stats = CardStats[cardsById[x.Key]], RotationProof = RotationProof }))
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
		/// The visibility of the "next booster to collect" section (used to hide that section on the GUI when there is no set to show).
		/// </summary>
		public Visibility PurchaseBoosterVisibility
		{
			get
			{
				return string.IsNullOrWhiteSpace(NextBoosterSetToPurchase) ? Visibility.Collapsed : Visibility.Visible;
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
					_sideboardView = _sideboardToCollect.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = false, Stats = CardStats[cardsById[x.Key]], RotationProof = RotationProof }).
						Concat(_suggestedSideboard.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = true, Stats = CardStats[cardsById[x.Key]], RotationProof = RotationProof }))
						.ToList().AsReadOnly();
				}
				return _sideboardView;
			}
		}

		/// <summary>
		/// A list of objects representing a view into the command zone, for Xaml binding.
		/// </summary>
		private IReadOnlyList<CardView> _commandZoneView = null;

		/// <summary>
		/// Gets a list of objects representing a view into the command zone, for Xaml binding.
		/// </summary>
		public IReadOnlyList<CardView> CommandZoneView
		{
			get
			{
				if(_commandZoneView == null && _commandZoneToCollect != null && _suggestedCommandZone != null)
				{
					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					_commandZoneView = _commandZoneToCollect.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = false, Stats = CardStats[cardsById[x.Key]], RotationProof = RotationProof }).
						Concat(_suggestedCommandZone.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = true, Stats = CardStats[cardsById[x.Key]], RotationProof = RotationProof }))
						.ToList().AsReadOnly();
				}
				return _commandZoneView;
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
					StringBuilder exportList = new StringBuilder();

					ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
					var commandZone = SuggestedCommandZone.Concat(CommandZoneToCollect).GroupBy(x => x.Key).
						Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
						Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");
					var mainDeck = SuggestedMainDeck.Concat(MainDeckToCollect).GroupBy(x => x.Key).
						Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
						Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");
					var sideboard = SuggestedSideboard.Concat(SideboardToCollect).GroupBy(x => x.Key).
						Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
						Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");

					if(commandZone.Count() > 0)
					{
						exportList.AppendFormat("Commander{0}{1}{0}{0}", Environment.NewLine, string.Join(Environment.NewLine, commandZone));
					}
					exportList.AppendFormat("Deck{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, mainDeck));
					if(sideboard.Count() > 0)
					{
						exportList.AppendFormat("{0}{0}Sideboard{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, sideboard));
					}
					_exportList = exportList.ToString();
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
					StringBuilder exportList = new StringBuilder();

					Dictionary<int, int> mainDeckReplacements = new Dictionary<int, int>();
					Dictionary<int, int> sideboardReplacements = new Dictionary<int, int>();
					Dictionary<int, int> commandZoneReplacements = new Dictionary<int, int>();
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
						if(replacementCount > 0 && CommandZoneToCollect.ContainsKey(replacement.Item1))
						{
							if (CommandZoneToCollect[replacement.Item1] >= replacementCount)
							{
								if (commandZoneReplacements.ContainsKey(replacement.Item2))
								{
									commandZoneReplacements[replacement.Item2] += replacementCount;
								}
								else
								{
									commandZoneReplacements.Add(replacement.Item2, replacementCount);
								}
								replacementCount = 0;
							}
							else
							{
								if (commandZoneReplacements.ContainsKey(replacement.Item2))
								{
									commandZoneReplacements[replacement.Item2] += CommandZoneToCollect[replacement.Item1];
								}
								else
								{
									commandZoneReplacements.Add(replacement.Item2, CommandZoneToCollect[replacement.Item1]);
								}
								replacementCount -= CommandZoneToCollect[replacement.Item1];
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
					var commandZone = SuggestedCommandZone.Concat(commandZoneReplacements).GroupBy(x => x.Key).
						Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
						Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");

					if (commandZone.Count() > 0)
					{
						exportList.AppendFormat("Commander{0}{1}{0}{0}", Environment.NewLine, string.Join(Environment.NewLine, commandZone));
					}
					exportList.AppendFormat("Deck{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, mainDeck));
					if (sideboard.Count() > 0)
					{
						exportList.AppendFormat("{0}{0}Sideboard{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, sideboard));
					}
					_exportListSuggested = exportList.ToString();
				}
				return _exportListSuggested;
			}
		}

		/// <summary>
		/// A dictionary containing set name translations for various languages.
		/// </summary>
		private Dictionary<string, Dictionary<string, string>> _setNameTranslations;

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
						Concat(CommandZoneToCollect.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value })).
						Where(x => x.Card.RotationSafe || !RotationProof).
						Select(x => new { SetName = x.Card.Set.Name, BoosterCost = x.Card.BoosterCost * x.Quantity }).
						GroupBy(x => x.SetName).
						Where(x => x.Key != "Arena").
						Select(x => new { SetName = x.Key, BoosterCost = x.Sum(y => y.BoosterCost) }).
						OrderByDescending(x => x.BoosterCost);

					if (orderedSets.Count() > 0)
					{
						_nextBoosterSetToPurchase = orderedSets.First().SetName;
					}
					else
					{
						_nextBoosterSetToPurchase = string.Empty;
					}

					string currentCulture = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
					if (_setNameTranslations.ContainsKey(_nextBoosterSetToPurchase) && _setNameTranslations[_nextBoosterSetToPurchase].ContainsKey(currentCulture))
					{
						_nextBoosterSetToPurchase = _setNameTranslations[_nextBoosterSetToPurchase][currentCulture];
					}
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
		/// Check if this deck archetype contains a card with the specified text in any of its fields (MainDeck, Sideboard, CommandZone, Suggestions)
		/// </summary>
		/// <param name="cardText">The text to search for.</param>
		/// <returns>True if the deck archetype contains the specified card text, false otherwise.</returns>
		public bool HasCardText(string cardText)
		{
			ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;

			try
			{
				if (cardText.StartsWith("/") && cardText.EndsWith("/"))
				{
					Regex cardTextRegex = new Regex(cardText.Substring(1, cardText.Length - 2), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

					return SuggestedMainDeck.Any(x => cardTextRegex.IsMatch(cardsById[x.Key].Name) || cardTextRegex.IsMatch(cardsById[x.Key].PrintedName)) ||
						MainDeckToCollect.Any(x => cardTextRegex.IsMatch(cardsById[x.Key].Name) || cardTextRegex.IsMatch(cardsById[x.Key].PrintedName)) ||
						SuggestedSideboard.Any(x => cardTextRegex.IsMatch(cardsById[x.Key].Name) || cardTextRegex.IsMatch(cardsById[x.Key].PrintedName)) ||
						SideboardToCollect.Any(x => cardTextRegex.IsMatch(cardsById[x.Key].Name) || cardTextRegex.IsMatch(cardsById[x.Key].PrintedName)) ||
						SuggestedCommandZone.Any(x => cardTextRegex.IsMatch(cardsById[x.Key].Name) || cardTextRegex.IsMatch(cardsById[x.Key].PrintedName)) ||
						CommandZoneToCollect.Any(x => cardTextRegex.IsMatch(cardsById[x.Key].Name) || cardTextRegex.IsMatch(cardsById[x.Key].PrintedName)) ||
						SuggestedReplacements.Any(x => cardTextRegex.IsMatch(cardsById[x.Item2].Name) || cardTextRegex.IsMatch(cardsById[x.Item2].PrintedName));
				}
			}
			catch (Exception)
			{
				// if regex fails, just return true
				return true;
			}

			CultureInfo culture = CultureInfo.CurrentCulture;
			CultureInfo english = CultureInfo.GetCultureInfo("en-US");
			return SuggestedMainDeck.Any(x => english.CompareInfo.IndexOf(cardsById[x.Key].Name, cardText, CompareOptions.IgnoreCase) > -1 || culture.CompareInfo.IndexOf(cardsById[x.Key].PrintedName, cardText, CompareOptions.IgnoreCase) > -1) ||
				MainDeckToCollect.Any(x => english.CompareInfo.IndexOf(cardsById[x.Key].Name, cardText, CompareOptions.IgnoreCase) > -1 || culture.CompareInfo.IndexOf(cardsById[x.Key].PrintedName, cardText, CompareOptions.IgnoreCase) > -1) ||
				SuggestedSideboard.Any(x => english.CompareInfo.IndexOf(cardsById[x.Key].Name, cardText, CompareOptions.IgnoreCase) > -1 || culture.CompareInfo.IndexOf(cardsById[x.Key].PrintedName, cardText, CompareOptions.IgnoreCase) > -1) ||
				SideboardToCollect.Any(x => english.CompareInfo.IndexOf(cardsById[x.Key].Name, cardText, CompareOptions.IgnoreCase) > -1 || culture.CompareInfo.IndexOf(cardsById[x.Key].PrintedName, cardText, CompareOptions.IgnoreCase) > -1) ||
				SuggestedCommandZone.Any(x => english.CompareInfo.IndexOf(cardsById[x.Key].Name, cardText, CompareOptions.IgnoreCase) > -1 || culture.CompareInfo.IndexOf(cardsById[x.Key].PrintedName, cardText, CompareOptions.IgnoreCase) > -1) ||
				CommandZoneToCollect.Any(x => english.CompareInfo.IndexOf(cardsById[x.Key].Name, cardText, CompareOptions.IgnoreCase) > -1 || culture.CompareInfo.IndexOf(cardsById[x.Key].PrintedName, cardText, CompareOptions.IgnoreCase) > -1) ||
				SuggestedReplacements.Any(x => english.CompareInfo.IndexOf(cardsById[x.Item2].Name, cardText, CompareOptions.IgnoreCase) > -1 || culture.CompareInfo.IndexOf(cardsById[x.Item2].PrintedName, cardText, CompareOptions.IgnoreCase) > -1);
		}

		/// <summary>
		/// Static field containing the unique index to be used for the next deck archetype object created.
		/// </summary>
		private static long NextIndex { get; set; } = 0;
	}
}
