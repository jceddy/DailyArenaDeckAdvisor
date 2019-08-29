using System.Windows;
using System.Windows.Controls;

namespace DailyArenaDeckAdvisor
{
	/// <summary>
	/// Class to select a Xaml template for the archetype tab detail view, depending on whether it's showing a deck, or a meta report.
	/// </summary>
	public class ContentTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Select the template to use depending on the item type.
		/// </summary>
		/// <param name="item">The item being shown in the detail view.</param>
		/// <param name="container">The container the view is in (not used).</param>
		/// <returns>ArchetypeContentTemplate if the item is an Archetype, otherwise MetaReportContentTemplate.</returns>
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			Window win = Application.Current.MainWindow;

			if (item is Archetype)
			{
				return win.FindResource("ArchetypeContentTemplate") as DataTemplate;
			}
			else
			{
				return win.FindResource("MetaReportContentTemplate") as DataTemplate;
			}
		}
	}
}
