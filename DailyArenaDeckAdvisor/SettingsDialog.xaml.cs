using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : Window
	{
		/// <summary>
		/// Xaml-bindable font sizes setting field.
		/// </summary>
		public ObservableCollection<int> FontSizes { get; private set; } = new ObservableCollection<int>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24 };

		/// <summary>
		/// Constructor. Initializes the component and sets the data context.
		/// </summary>
		public SettingsDialog()
		{
			InitializeComponent();
			DataContext = this;
		}

		/// <summary>
		/// Handle clicks of the Github Button.
		/// </summary>
		/// <param name="sender">The button that was clicked.</param>
		/// <param name="e">The routed event arguments.</param>
		private void GithubButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://github.com/jceddy/DailyArenaDeckAdvisor");
		}

		/// <summary>
		/// Handle clicks of the Issues Button.
		/// </summary>
		/// <param name="sender">The button that was clicked.</param>
		/// <param name="e">The routed event arguments.</param>
		private void IssuesButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://github.com/jceddy/DailyArenaDeckAdvisor/issues");
		}

		/// <summary>
		/// Handle clicks of the Wiki Button.
		/// </summary>
		/// <param name="sender">The button that was clicked.</param>
		/// <param name="e">The routed event arguments.</param>
		private void WikiButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://github.com/jceddy/DailyArenaDeckAdvisor/wiki");
		}

		/// <summary>
		/// Handle clicks of the Patreon Button.
		/// </summary>
		/// <param name="sender">The button that was clicked.</param>
		/// <param name="e">The routed event arguments.</param>
		private void PatreonButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://www.patreon.com/DailyArena");
		}

		/// <summary>
		/// Handle clicks of the Close Button.
		/// </summary>
		/// <param name="sender">The button that was clicked.</param>
		/// <param name="e">The routed event arguments.</param>
		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
