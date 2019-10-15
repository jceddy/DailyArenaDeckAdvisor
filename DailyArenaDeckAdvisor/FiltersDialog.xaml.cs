using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class FiltersDialog : Window
	{
		/// <summary>
		/// Constructor. Initializes the component and sets the data context.
		/// </summary>
		public FiltersDialog()
		{
			InitializeComponent();
			DataContext = this;
		}

		/// <summary>
		/// Handle clicks of the Apply Button.
		/// </summary>
		/// <param name="sender">The button that was clicked.</param>
		/// <param name="e">The routed event arguments.</param>
		private void Apply_Click(object sender, RoutedEventArgs e)
		{
			/* TODO */
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
