using System;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Class to Cache state information for the application.
	/// </summary>
	public class CachedState
	{
		/// <summary>
		/// The Last value selected from the Format drop-down.
		/// </summary>
		public string LastFormat { get; set; }

		/// <summary>
		/// The State of the "rotation-proof" toggle button.
		/// </summary>
		public bool RotationProof { get; set; }

		/// <summary>
		/// The selected font size for the application.
		/// </summary>
		public int FontSize { get; set; }

		/// <summary>
		/// Fingerprint for usage stats.
		/// </summary>
		public Guid Fingerprint { get; set; } = Guid.NewGuid();

		/// <summary>
		/// Deck filter values set by the user.
		/// </summary>
		public DeckFilters Filters { get; set; } = new DeckFilters();
	}
}
