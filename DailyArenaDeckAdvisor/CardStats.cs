using DailyArena.Common.Core.Database;
using DailyArena.Common.Database;
using System.Collections.Generic;
using System.Linq;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Class that represent non-static meta statistics computed for a Card.
	/// </summary>
	public class CardStats
	{
		/// <summary>
		/// Gets or sets the Card these stats are linked to.
		/// </summary>
		public Card Card { get; set; }

		/// <summary>
		/// Gets or sets the number of decks the card is contained in.
		/// </summary>
		public int DeckCount { get; set; } = 0;

		/// <summary>
		/// Gets or sets the overall percentage of decks the card is contained in.
		/// </summary>
		public double DeckPercentage { get; set; }

		/// <summary>
		/// Gets or sets the total copies of the card played in all decks.
		/// </summary>
		public int TotalCopies { get; set; } = 0;

		/// <summary>
		/// Gets the average number of copies of the card played over all decks that play it.
		/// </summary>
		public double AverageCopies
		{
			get
			{
				if(DeckCount == 0)
				{
					return 0;
				}
				return (double)TotalCopies / DeckCount;
			}
		}

		/// <summary>
		/// Gets or sets the maximim number of copies of the card played in any given deck.
		/// </summary>
		public int MaxCopies { get; set; } = 0;

		/// <summary>
		/// Gets a formatted string view of the card's meta statistics, useful for Xaml binding.
		/// </summary>
		public string MetaStatsView
		{
			get
			{
				if (Card.Rarity == CardRarity.BasicLand)
				{
					return string.Empty;
				}
				return string.Format("({0:0%}, {1:0.0})", DeckPercentage, AverageCopies);
			}
		}

		/// <summary>
		/// Dictionary that keeps track of deck associations between cards.
		/// </summary>
		private static Dictionary<string, Dictionary<string, int>> _associationStats = new Dictionary<string, Dictionary<string, int>>();

		/// <summary>
		/// Cache of pre-computed association modfiers;
		/// </summary>
		private static Dictionary<string, Dictionary<string, double>> _associationModifiers = new Dictionary<string, Dictionary<string, double>>();

		/// <summary>
		/// Get an "association modifier" score from 0-1 that reflects the percentage of decks this card is in with the supplied card.
		/// </summary>
		/// <param name="card1">The first card to get an association modifier for.</param>
		/// <param name="card2">The second card to get an association modifier for.</param>
		/// <param name="cardStats">Object containing deck statistics for all cards.</param>
		/// <returns>The association modifier for the specified card.</returns>
		public static double GetAssociationModifier(string card1, string card2, Dictionary<Card, CardStats> cardStats)
		{
			if (card1 == card2)
			{
				return 1.0;
			}
			else {
				if (_associationModifiers.ContainsKey(card1) && _associationModifiers[card1].ContainsKey(card2))
				{
					return _associationModifiers[card1][card2];
				}
				else
				{
					double modifier = 0.0;

					if (_associationStats.ContainsKey(card1) && _associationStats[card1].ContainsKey(card2))
					{
						modifier = (double)_associationStats[card1][card2] / cardStats.Where(x => x.Key.Name == card1).First().Value.DeckCount;
					}

					if(!_associationModifiers.ContainsKey(card1))
					{
						_associationModifiers[card1] = new Dictionary<string, double>();
					}
					_associationModifiers[card1][card1] = modifier;
					return modifier;
				}
			}
		}

		/// <summary>
		/// Update all deck association data for cards in a given archetype.
		/// </summary>
		/// <param name="archetype"></param>
		public static void UpdateDeckAssociations(Archetype archetype)
		{
			List<string> allCardNames = archetype.MainDeck.Select(x => x.Key).Union(archetype.Sideboard.Select(x => x.Key)).ToList();
			
			for(int i = 0; i < allCardNames.Count; i++)
			{
				for(int j = i + 1; j < allCardNames.Count; j++)
				{
					string card1 = allCardNames[i];
					string card2 = allCardNames[j];

					if(!_associationStats.ContainsKey(card1))
					{
						_associationStats[card1] = new Dictionary<string, int>();
					}
					if (!_associationStats.ContainsKey(card2))
					{
						_associationStats[card2] = new Dictionary<string, int>();
					}

					if(_associationStats[card1].ContainsKey(card2))
					{
						_associationStats[card1][card2]++;
					}
					else
					{
						_associationStats[card1][card2] = 1;
					}

					if (_associationStats[card2].ContainsKey(card1))
					{
						_associationStats[card2][card1]++;
					}
					else
					{
						_associationStats[card2][card1] = 1;
					}
				}
			}
		}
	}
}
