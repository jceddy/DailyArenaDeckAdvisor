﻿namespace DailyArenaDeckAdvisor
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
	}
}
