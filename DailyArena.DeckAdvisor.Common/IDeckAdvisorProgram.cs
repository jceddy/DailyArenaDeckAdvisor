using DailyArena.Common.Core.Bindable;
using Serilog;
using System;
using System.Collections.Generic;

namespace DailyArena.DeckAdvisor.Common
{
	/// <summary>
	/// Interface implemented by classes the run the core Deck Advisor program functionality.
	/// </summary>
	public interface IDeckAdvisorProgram
	{
		/// <summary>
		/// Gets or sets a reference to the application logger.
		/// </summary>
		ILogger Logger { get; set; }

		/// <summary>
		/// Gets the application name to use while logging.
		/// </summary>
		string ApplicationName { get; }

		/// <summary>
		/// Gets the application name to use while sending Usage Statistics.
		/// </summary>
		string ApplicationUsageName { get; }

		/// <summary>
		/// Implementation-specific method to query a configuration setting value.
		/// </summary>
		/// <returns></returns>
		string GetConfigurationSetting(string key);

		/// <summary>
		/// A dictionary mapping the Format names shown on the GUI drop-down to the name to use when querying archetype data from the server.
		/// </summary>
		Dictionary<string, Tuple<string, string>> FormatMappings { get; set; }

		/// <summary>
		/// Method to get a localized string based on the resource name.
		/// </summary>
		/// <param name="name">The resource name to get a localized string for.</param>
		/// <returns>The localized string for the requested resource name.</returns>
		string GetLocalizedString(string name);

		/// <summary>
		/// Gets the currently running app.
		/// </summary>
		IDeckAdvisorApp CurrentApp { get; }

		/// <summary>
		/// Gets or the selected format being viewed.
		/// </summary>
		Bindable<string> Format { get; }

		/// <summary>
		/// Gets the selected deck sort field.
		/// </summary>
		Bindable<string> Sort { get; }

		/// <summary>
		/// Gets the selected deck sort direction.
		/// </summary>
		Bindable<string> SortDir { get; }

		/// <summary>
		/// Gets the state of the "Rotation" toggle button.
		/// </summary>
		BindableBool RotationProof { get; }

		/// <summary>
		/// Gets the card text filter value.
		/// </summary>
		Bindable<string> CardText { get; }

		/// <summary>
		/// Gets the selected font size for the display.
		/// </summary>
		Bindable<int> SelectedFontSize { get; }
	}
}
