using System.Windows;
using System.Windows.Controls;

namespace DailyArenaDeckAdvisor
{
	/// <summary>
	/// Class that selects the template used for tab details (switches between templates for Meta Report vs. Archetype).
	/// </summary>
	public class TabTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Select the data template to use for the item being displayed.
		/// </summary>
		/// <param name="item">The item being displayed.</param>
		/// <param name="container">The item's container.</param>
		/// <returns>ArchetypeTabTemplate if the item is an Archetype object, MetaReportTabTemplate otherwise.</returns>
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			Window win = Application.Current.MainWindow;

			if (item is Archetype)
			{
				return win.FindResource("ArchetypeTabTemplate") as DataTemplate;
			}
			else
			{
				return win.FindResource("MetaReportTabTemplate") as DataTemplate;
			}
		}
	}
}
