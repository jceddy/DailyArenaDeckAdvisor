using System;
using System.IO;

namespace DailyArenaDeckAdvisorUpdater
{
	/// <summary>
	/// Lightweight file logger for the updater.
	/// </summary>
	/// <remarks>
	/// Can't use the full logger class here because the updater may have to update it.
	/// </remarks>
	public static class FileLogger
	{
		/// <summary>
		/// The path of the log file to write to.
		/// </summary>
		public static string FilePath { get; set; }

		/// <summary>
		/// Write a debug message to the log file.
		/// </summary>
		/// <param name="message">The message to write.</param>
		/// <param name="arg">Arguments to include with the message.</param>
		public static void Log(string message, params object[] arg)
		{
			using (StreamWriter streamWriter = new StreamWriter(FilePath, true))
			{
				streamWriter.WriteLine(message, arg);
			}
		}

		/// <summary>
		/// Write an exception message to the log file.
		/// </summary>
		/// <param name="e">The exception to log.</param>
		/// <param name="message">The messate to write.</param>
		/// <param name="arg">Arguments to include wiht the message.</param>
		public static void Log(Exception e, string message, params object[] arg)
		{
			using (StreamWriter streamWriter = new StreamWriter(FilePath, true))
			{
				streamWriter.WriteLine(message, arg);
				streamWriter.WriteLine("{0}", e.ToString());
			}
		}
	}
}
