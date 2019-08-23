using System;
using System.IO;

namespace DailyArenaDeckAdvisorUpdater
{
	public static class FileLogger
	{
		public static string FilePath { get; set; }
		public static void Log(string message, params object[] arg)
		{
			using (StreamWriter streamWriter = new StreamWriter(FilePath))
			{
				streamWriter.WriteLine(message, arg);
				streamWriter.Close();
			}
		}
		public static void Log(Exception e, string message, params object[] arg)
		{
			using (StreamWriter streamWriter = new StreamWriter(FilePath))
			{
				streamWriter.WriteLine(message, arg);
				streamWriter.WriteLine("{0}", e.ToString());
				streamWriter.Close();
			}
		}
	}
}
