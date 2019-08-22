using DailyArenaDeckAdvisor.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyArenaDeckAdvisor
{
	public class MetaReport
	{
		public class MetaReportEntry
		{
			public string Name { get; set; }
			public double DeckPercentage { get; set; }
			public double CopiesNeededForAverage { get; set; }
			public int TotalCopiesNeeded { get; set; }
			public double Dominance { get; set; }
			public string MetaStats { get; set; }
			public Card Card { get; set; }
		}

		public List<MetaReportEntry> ReportEntries { get; private set; }

		public string NextBoosterSetToPurchase
		{
			get
			{
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
					return orderedSets.First().SetName;
				}

				return string.Empty;
			}
		}

		public List<Archetype> TopDecksToBuild { get; private set; }

		private ReadOnlyDictionary<string, List<Card>> _cardsByName;
		private Dictionary<Card, CardStats> _cardStats;
		private List<MetaReportEntry> _allReportEntries;

		public MetaReport(ReadOnlyDictionary<string, List<Card>> cardsByName, Dictionary<Card, CardStats> cardStats, ReadOnlyDictionary<int, Card> cardsById,
			Dictionary<string, int> playerInventoryCounts, List<Archetype> archetypes)
		{
			_cardsByName = cardsByName;
			_cardStats = cardStats;
			_allReportEntries = cardsByName.Where(x => x.Value[0].Rarity != CardRarity.BasicLand).Select(x => new MetaReportEntry()
			{
				Name = x.Key,
				DeckPercentage = _cardStats[x.Value[0]].DeckPercentage,
				CopiesNeededForAverage = _cardStats[x.Value[0]].AverageCopies - Math.Min(playerInventoryCounts.ContainsKey(x.Key) ? playerInventoryCounts[x.Key] : 0, _cardStats[x.Value[0]].AverageCopies),
				TotalCopiesNeeded = _cardStats[x.Value[0]].MaxCopies - Math.Min(playerInventoryCounts.ContainsKey(x.Key) ? playerInventoryCounts[x.Key] : 0, _cardStats[x.Value[0]].MaxCopies),
				Dominance = _cardStats[x.Value[0]].DeckPercentage * (_cardStats[x.Value[0]].AverageCopies - Math.Min(playerInventoryCounts.ContainsKey(x.Key) ? playerInventoryCounts[x.Key] : 0, _cardStats[x.Value[0]].AverageCopies)),
				MetaStats = _cardStats[x.Value[0]].MetaStatsView,
				Card = x.Value[0]
			}).Where(x => x.TotalCopiesNeeded > 0).ToList();

			ReportEntries = new List<MetaReportEntry>(
				_allReportEntries.OrderByDescending(x => x.Dominance).Take(70)
			);

			TopDecksToBuild = new List<Archetype>(
				archetypes.Select(x => new
				{
					Archetype = x,
					Dominance = x.MainDeckToCollect.Concat(x.SideboardToCollect).Where(a => _allReportEntries.Where(b => b.Name == cardsById[a.Key].Name).Count() > 0).
						Sum(y => _allReportEntries.Where(z => z.Name == cardsById[y.Key].Name).FirstOrDefault().Dominance * y.Value)
				}).OrderByDescending(x => x.Dominance).Select(x => x.Archetype).Take(10)
			);
		}
	}
}
