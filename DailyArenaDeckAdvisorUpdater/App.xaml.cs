using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DailyArenaDeckAdvisorUpdater
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			string codeBase = Assembly.GetExecutingAssembly().CodeBase;
			UriBuilder uri = new UriBuilder(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
		}
	}
}
