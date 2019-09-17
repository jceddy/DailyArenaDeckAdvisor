using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Converter that handles Xaml bindings for images links to null Uris.
	/// </summary>
	public class NullImageConverter : IValueConverter
	{
		/// <summary>
		/// If the Uri is null, unset the balue of the dependency property.
		/// </summary>
		/// <param name="value">The Uri of the image.</param>
		/// <param name="targetType">The type being converted to.</param>
		/// <param name="parameter">A conversion parameter (not used).</param>
		/// <param name="culture">Information on the current selected Culture (not used).</param>
		/// <returns>The image Uri value if it is not null, otherwise DependencyProperty.UnsetValue.</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return DependencyProperty.UnsetValue;
			return value;
		}

		/// <summary>
		/// Convert back from a dependency object to a Uri value.
		/// </summary>
		/// <param name="value">The Uri of the image.</param>
		/// <param name="targetType">The type being converted to.</param>
		/// <param name="parameter">A conversion parameter (not used).</param>
		/// <param name="culture">Information on the current selected Culture (not used).</param>
		/// <returns>An object telling the binding to do nothing (this converter is for one-way bindings).</returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}
