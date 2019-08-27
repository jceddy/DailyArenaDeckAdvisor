using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace DailyArenaDeckAdvisor
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : Window
	{
		public ObservableCollection<int> FontSizes { get; private set; } = new ObservableCollection<int>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24 };

		public SettingsDialog()
		{
			InitializeComponent();
			DataContext = this;
		}

		private void WikiButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://github.com/jceddy/DailyArenaDeckAdvisor/wiki");
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
