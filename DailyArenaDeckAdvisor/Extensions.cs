using Serilog;
using System.Management;
using System.Windows;

namespace DailyArenaDeckAdvisor
{
	/// <summary>
	/// Class that contains static extension methods for other classes.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// The logger object.
		/// </summary>
		static ILogger _logger;

		/// <summary>
		/// Static constructor, stores a reference to the application's logger.
		/// </summary>
		static Extensions()
		{
			App application = (App)Application.Current;
			_logger = application.Logger;
		}

		/// <summary>
		/// Tries to get the value of a ManagementObject property by name.
		/// </summary>
		/// <param name="wmiObj">The object that is being queried.</param>
		/// <param name="propertyName">The name of the property to get.</param>
		/// <returns>The value of the requested property, if it exists; null otherwise.</returns>
		public static object TryGetProperty(this ManagementObject wmiObj, string propertyName)
		{
			object retval;
			try
			{
				retval = wmiObj.GetPropertyValue(propertyName);
			}
			catch (ManagementException ex)
			{
				_logger.Error(ex, "ManagementObject doesn't contain property {0}", propertyName);
				retval = null;
			}
			return retval;
		}
	}
}
