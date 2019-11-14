using System.Configuration;
using System.Globalization;
using System.Threading;

namespace DailyArena.DeckAdvisor.Common.Extensions
{
	/// <summary>
	/// Common class extension for Deck Advisor Program classes.
	/// </summary>
	public static class ProgramExtensions
	{
		/// <summary>
		/// Initialization extension for IDeckAdvisorProgram.
		/// </summary>
		/// <param name="app">The program.</param>
		public static void InitializeProgram(this IDeckAdvisorProgram program)
		{
			program.Logger.Debug("InitializeProgram Called - {0}", program.ApplicationName);

			program.SetCulture();
		}

		/// <summary>
		/// Sets the current UI culture from app.config if it's set there.
		/// </summary>
		private static void SetCulture(this IDeckAdvisorProgram program)
		{
			program.Logger.Debug("SetCulture() Called - {0}", program.ApplicationName);

			string culture = program.GetConfigurationSetting("UICulture");
			if(culture != null)
			{
				program.Logger.Debug("Setting UI Culture to {culture}", culture);
				Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
			}
			else
			{
				program.Logger.Debug("UICulture not set in configuration settings, Using Default UI Culture");
			}
		}
	}
}
