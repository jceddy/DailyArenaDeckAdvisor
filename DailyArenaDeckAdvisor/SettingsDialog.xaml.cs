using System.Diagnostics;
using System.Windows;

namespace DailyArenaDeckAdvisor
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : Window
	{
		public SettingsDialog()
		{
			InitializeComponent();
		}

		private void GithubButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://github.com/jceddy/DailyArenaDeckAdvisor");
		}

		private void PatreonButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://www.patreon.com/DailyArena");
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
