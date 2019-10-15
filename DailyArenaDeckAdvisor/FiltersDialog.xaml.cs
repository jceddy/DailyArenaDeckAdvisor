using System.Windows;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class FiltersDialog : Window
	{
		/// <summary>
		/// The temporary filters object that belongs to this window.
		/// </summary>
		private DeckFilters _filters;

		/// <summary>
		/// Constructor. Initializes the component and sets the data context.
		/// </summary>
		public FiltersDialog()
		{
			App application = (App)Application.Current;
			_filters = application.State.Filters.Clone();

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
			App application = (App)Application.Current;
			if (application.State.Filters != _filters)
			{
				application.State.Filters.SetAllFields(_filters);
				application.SaveState();
				((MainWindow)Owner).Refresh(false);
			}
			Close();
		}

		/// <summary>
		/// Handle clicks of the Close Button.
		/// </summary>
		/// <param name="sender">The button that was clicked.</param>
		/// <param name="e">The routed event arguments.</param>
		private void Close_Click(object sender, RoutedEventArgs e)
		{
			bool close = true;
			App application = (App)Application.Current;
			if (application.State.Filters != _filters)
			{
				MessageBoxResult messageBoxResult = MessageBox.Show(Properties.Resources.Message_CloseConfirmation, Properties.Resources.Title_CloseConfirmation, MessageBoxButton.YesNo);
				if(messageBoxResult == MessageBoxResult.No)
				{
					close = false;
				}
			}
			if (close)
			{
				Close();
			}
		}
	}
}
