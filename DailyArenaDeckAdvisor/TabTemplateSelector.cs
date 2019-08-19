using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DailyArenaDeckAdvisor
{
	public class TabTemplateSelector : DataTemplateSelector
	{
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
