using DailyArenaDeckAdvisor.Database;

namespace DailyArenaDeckAdvisor
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
	}
}
