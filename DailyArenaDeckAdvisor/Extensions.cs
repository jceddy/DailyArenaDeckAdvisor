using Serilog;
using System.Management;
using System.Windows;

namespace DailyArenaDeckAdvisor
{
	public static class Extensions
	{
		static ILogger _logger;

		static Extensions()
		{
			App application = (App)Application.Current;
			_logger = application.Logger;
		}

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
