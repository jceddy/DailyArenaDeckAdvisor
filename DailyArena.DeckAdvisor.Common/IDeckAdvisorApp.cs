using DailyArena.Common.Core;
using Serilog;

namespace DailyArena.DeckAdvisor.Common
{
	/// <summary>
	/// Interface implemented by all Deck Advisor Application/Program classes.
	/// </summary>
	public interface IDeckAdvisorApp : IApp
	{
		/// <summary>
		/// The application's Cached State.
		/// </summary>
		CachedState State { get; set; }

		/// <summary>
		/// Logger for first chance exceptions.
		/// </summary>
		ILogger FirstChanceLogger { get; set; }
	}
}
