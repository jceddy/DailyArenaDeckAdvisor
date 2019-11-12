using System;

namespace DailyArena.DeckAdvisor.Common
{
	/// <summary>
	/// Class to Cache state information for the application.
	/// </summary>
	public class CachedState
	{
		/// <summary>
		/// Gets or sets the last value selected from the Format drop-down.
		/// </summary>
		public string LastFormat { get; set; }

		/// <summary>
		/// Gets or sets the last value selcted by the deck Sort drop-down.
		/// </summary>
		public string LastSort { get; set; }

		/// <summary>
		/// Gets or sets the last value selected by the deck Sort Dir drop-down.
		/// </summary>
		public string LastSortDir { get; set; }

		/// <summary>
		/// Gets or sets the State of the "rotation-proof" toggle button.
		/// </summary>
		public bool RotationProof { get; set; }

		/// <summary>
		/// Gets or sets the selected font size for the application.
		/// </summary>
		public int FontSize { get; set; }

		/// <summary>
		/// Gets or sets the fingerprint for usage stats.
		/// </summary>
		public Guid Fingerprint { get; set; } = Guid.NewGuid();

		/// <summary>
		/// Gets or sets the deck filter values set by the user.
		/// </summary>
		public DeckFilters Filters { get; set; } = new DeckFilters();

		/// <summary>
		/// Gets or sets the card text filter value.
		/// </summary>
		public string CardTextFilter { get; set; } = string.Empty;
	}
}
