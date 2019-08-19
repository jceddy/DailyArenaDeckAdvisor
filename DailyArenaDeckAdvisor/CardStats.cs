using DailyArenaDeckAdvisor.Database;

namespace DailyArenaDeckAdvisor
{
	public class CardStats
	{
		public Card Card { get; set; }

		public int DeckCount { get; set; } = 0;
		public double DeckPercentage { get; set; }
		public int TotalCopies { get; set; } = 0;
		public double AverageCopies
		{
			get
			{
				return (double)TotalCopies / DeckCount;
			}
		}
		public int MaxCopies { get; set; } = 0;

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
	}
}
