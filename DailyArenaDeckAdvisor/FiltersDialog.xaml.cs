using DailyArena.DeckAdvisor.Common;
using DailyArena.DeckAdvisor.Common.Extensions;
using System.Windows;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class FiltersDialog : Window
	{
		/// <summary>
		/// Gets or sets the temporary filters object that belongs to this window.
		/// </summary>
		public DeckFilters Filters { get; private set; }

		/// <summary>
		/// Constructor. Initializes the component and sets the data context.
		/// </summary>
		public FiltersDialog()
		{
			App application = (App)Application.Current;
			Filters = application.State.Filters.Clone();

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
			IDeckAdvisorApp application = (IDeckAdvisorApp)Application.Current;
			if (application.State.Filters != Filters)
			{
				application.State.Filters.SetAllFields(Filters);
				application.SaveState();
				((MainWindow)Owner).ApplyFilters(Filters);
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
			if (application.State.Filters != Filters)
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
