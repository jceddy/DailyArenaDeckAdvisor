using Serilog;

namespace DailyArena.DeckAdvisor.Common
{
	/// <summary>
	/// Interface implemented by classes the run the core Deck Advisor program functionality.
	/// </summary>
	public interface IDeckAdvisorProgram
	{
		/// <summary>
		/// A reference to the application logger.
		/// </summary>
		ILogger Logger { get; set; }

		/// <summary>
		/// The application name to use while logging.
		/// </summary>
		string ApplicationName { get; }

		/// <summary>
		/// Implementation-specific method to query a configuration setting value.
		/// </summary>
		/// <returns></returns>
		string GetConfigurationSetting(string key);
	}
}
