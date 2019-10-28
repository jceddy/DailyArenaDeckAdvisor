using DailyArena.Common.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Class that represents a "Meta Report" for a given format.
	/// </summary>
	public class MetaReport
	{
		/// <summary>
		/// One entry in the meta report. Contains a reference to a card and verious "meta statistics" linked to that card.
		/// </summary>
		public class MetaReportEntry
		{
			/// <summary>
			/// The name of the card this entry is for.
			/// </summary>
			public string Name { get; private set; }

			/// <summary>
			/// The percentage of deck archetypes that include this card.
			/// </summary>
			public double DeckPercentage { get; private set; }

			/// <summary>
			/// The number of copies of the card the player needs to collect to have the average number of copies of this card played.
			/// </summary>
			public double CopiesNeededForAverage { get; private set; }

			/// <summary>
			/// The number of copies of the card the player needs to collect to be able to build any "meta" deck that plays it.
			/// </summary>
			public int TotalCopiesNeeded { get; private set; }

			/// <summary>
			/// The card's "meta dominance" score. This is basically CopiesNeededForAverage * DeckPercentage.
			/// </summary>
			public double Dominance { get; private set; }

			/// <summary>
			/// A string representation of the card's "meta statistics", for easy Xaml binding.
			/// </summary>
			public string MetaStats { get; private set; }

			/// <summary>
			/// A reference to the card that this entry is for.
			/// </summary>
			public Card Card { get; private set; }

			/// <summary>
			/// Constructor for meta report entries.
			/// </summary>
			/// <param name="name">The name of the card this entry is for.</param>
			/// <param name="deckPercentage">The percentage of deck archetypes that include this card.</param>
			/// <param name="copiesNeededForAverage">The number of copies of the card the player needs to collect to have the average number of copies of this card played.</param>
			/// <param name="totalCopiesNeeded">The number of copies of the card the player needs to collect to be able to build any "meta" deck that plays it.</param>
			/// <param name="dominance">The card's "meta dominance" score. This is basically CopiesNeededForAverage * DeckPercentage.</param>
			/// <param name="metaStats">A string representation of the card's "meta statistics", for easy Xaml binding.</param>
			/// <param name="card">A reference to the card that this entry is for.</param>
			public MetaReportEntry(string name, double deckPercentage, double copiesNeededForAverage, int totalCopiesNeeded, double dominance, string metaStats, Card card)
			{
				Name = name;
				DeckPercentage = deckPercentage;
				CopiesNeededForAverage = copiesNeededForAverage;
				TotalCopiesNeeded = totalCopiesNeeded;
				Dominance = dominance;
				MetaStats = metaStats;
				Card = card;
			}
		}

		/// <summary>
		/// Get whether a tab for this item is enabled (always true for MetaReport).
		/// </summary>
		public bool TabEnabled { get; private set; } = true;

		/// <summary>
		/// Gets a list of all of the entries in the report.
		/// </summary>
		public List<MetaReportEntry> ReportEntries { get; private set; }

		/// <summary>
		/// A dictionary containing set name translations for various languages.
		/// </summary>
		private Dictionary<string, Dictionary<string, string>> _setNameTranslations;

		/// <summary>
		/// The name of the next suggested set to purchase a booster for, for building toward the meta.
		/// </summary>
		private string _nextBoosterSetToPurchase;

		/// <summary>
		/// Gets the name of the next suggested set to purchase a booster for, for building toward the meta.
		/// </summary>
		public string NextBoosterSetToPurchase
		{
			get
			{
				if(_nextBoosterSetToPurchase != null)
				{
					return _nextBoosterSetToPurchase;
				}

				var orderedSets = ReportEntries.SelectMany(x =>
						_cardsByName[x.Name].Select(y => new
						{
							Card = y,
							Quantity = x.TotalCopiesNeeded
						})
					).
					Select(x => new { SetName = x.Card.Set.Name, BoosterCost = x.Card.BoosterCost * x.Quantity }).
					GroupBy(x => x.SetName).
					Select(x => new { SetName = x.Key, BoosterCost = x.Sum(y => y.BoosterCost) }).
					OrderByDescending(x => x.BoosterCost);

				if (orderedSets.Count() > 0)
				{
					string setName = orderedSets.First().SetName;
					string currentCulture = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
					if(_setNameTranslations.ContainsKey(setName) && _setNameTranslations[setName].ContainsKey(currentCulture))
					{
						_nextBoosterSetToPurchase = _setNameTranslations[setName][currentCulture];
						return _nextBoosterSetToPurchase;
					}
					else
					{
						_nextBoosterSetToPurchase = setName;
						return _nextBoosterSetToPurchase;
					}
				}

				return string.Empty;
			}
		}

		/// <summary>
		/// Gets a list of the top ten deck archetypes to focus on building in order to collect the top meta cards efficiently.
		/// </summary>
		public List<Archetype> TopDecksToBuild { get; private set; }

		/// <summary>
		/// A dictionary containing lists of card printings keyed by card name.
		/// </summary>
		private ReadOnlyDictionary<string, List<Card>> _cardsByName;

		/// <summary>
		/// A dictionary mapping card objects to their meta-statistics.
		/// </summary>
		private Dictionary<Card, CardStats> _cardStats;

		/// <summary>
		/// A list of all of the entries in the report.
		/// </summary>
		private List<MetaReportEntry> _allReportEntries;

		/// <summary>
		/// Constructor to generate a meta-report for a selected format.
		/// </summary>
		/// <param name="cardsByName">A dictionary containing lists of card printings keyed by card name.</param>
		/// <param name="cardStats">A dictionary mapping card objects to their meta-statistics.</param>
		/// <param name="cardsById">A dictioanry mapping Arena Ids to card objects.</param>
		/// <param name="playerInventoryCounts">A dictionary containing card counts from the player's inventory, keyed by card name.</param>
		/// <param name="archetypes">A list of deck archetypes for the format being computed.</param>
		/// <param name="rotationProof">A boolean determining whether the user has the "Rotation" toggle turned on.</param>
		/// <param name="setNameTranslations">A dictionary containing set name translations for various languages.</param>
		public MetaReport(ReadOnlyDictionary<string, List<Card>> cardsByName, Dictionary<Card, CardStats> cardStats, ReadOnlyDictionary<int, Card> cardsById,
			Dictionary<string, int> playerInventoryCounts, List<Archetype> archetypes, bool rotationProof, Dictionary<string, Dictionary<string, string>> setNameTranslations)
		{
			_cardsByName = cardsByName;
			_cardStats = cardStats;
			_allReportEntries = cardsByName.Where(x => x.Value[0].Rarity != CardRarity.BasicLand && (!rotationProof || x.Value[0].Set.RotationSafe)).Select(x => new MetaReportEntry(
				name: x.Key,
				deckPercentage: _cardStats[x.Value[0]].DeckPercentage,
				copiesNeededForAverage: _cardStats[x.Value[0]].AverageCopies - Math.Min(playerInventoryCounts.ContainsKey(x.Key) ? playerInventoryCounts[x.Key] : 0, _cardStats[x.Value[0]].AverageCopies),
				totalCopiesNeeded: _cardStats[x.Value[0]].MaxCopies - Math.Min(playerInventoryCounts.ContainsKey(x.Key) ? playerInventoryCounts[x.Key] : 0, _cardStats[x.Value[0]].MaxCopies),
				dominance: _cardStats[x.Value[0]].DeckPercentage * (_cardStats[x.Value[0]].AverageCopies - Math.Min(playerInventoryCounts.ContainsKey(x.Key) ? playerInventoryCounts[x.Key] : 0, _cardStats[x.Value[0]].AverageCopies)),
				metaStats: _cardStats[x.Value[0]].MetaStatsView,
				card: x.Value[0]
			)).Where(x => x.TotalCopiesNeeded > 0).ToList();

			ReportEntries = new List<MetaReportEntry>(
				_allReportEntries.OrderByDescending(x => x.Dominance).Take(98)
			);

			TopDecksToBuild = new List<Archetype>(
				archetypes.Select(x => new
				{
					Archetype = x,
					Dominance = x.MainDeckToCollect.Concat(x.SideboardToCollect).Where(a => _allReportEntries.Where(b => b.Name == cardsById[a.Key].Name).Count() > 0).
						Sum(y => _allReportEntries.Where(z => z.Name == cardsById[y.Key].Name).FirstOrDefault().Dominance * y.Value)
				}).OrderByDescending(x => x.Dominance).Select(x => x.Archetype).Take(10)
			);

			_setNameTranslations = setNameTranslations;
		}
	}
}
