using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace DailyArenaDeckAdvisor.Database
{
	/// <summary>
	/// Class that represents a set of Magic colors.
	/// </summary>
	[TypeConverter(typeof(Converter))]
	public class CardColors : IComparable<CardColors>, IComparable<int>, IComparable<string>
	{
		/// <summary>
		/// A converter that converts between string and CardColors.
		/// </summary>
		public class Converter : TypeConverter
		{
			/// <summary>
			/// Check whether the converter can convert from a specified source Type.
			/// </summary>
			/// <param name="context">The Type Descriptor Context (not used).</param>
			/// <param name="sourceType">The source Type to check.</param>
			/// <returns>True if this converter can convert from the source Type, false otherwise.</returns>
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
			{
				if (sourceType == typeof(string))
				{
					return true;
				}
				return base.CanConvertFrom(context, sourceType);
			}

			/// <summary>
			/// Convert an object to a CardColors.
			/// </summary>
			/// <param name="context">The Type Descriptor Context (not used).</param>
			/// <param name="culture">The current Culture info (not used).</param>
			/// <param name="value">The object to convert.</param>
			/// <returns>A CardColors object converted from the source value.</returns>
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if (value is string)
				{
					return CardColorFromString(value.ToString());
				}
				return base.ConvertFrom(context, culture, value);
			}

			/// <summary>
			/// Convert an CardColors object to a specified destination Type.
			/// </summary>
			/// <param name="context">The Type Descriptor Context (not used).</param>
			/// <param name="culture">The current Culture info (not used).</param>
			/// <param name="value">The CardColors object to convert.</param>
			/// <param name="destinationType">The destination Type to convert the CardColors object to.</param>
			/// <returns>An object of the destination Type that represents the CardColors object value.</returns>
			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if (destinationType == typeof(string))
				{
					return ((CardColors)value).ToString();
				}
				return base.ConvertTo(context, culture, value, destinationType);
			}
		}

		/// <summary>
		/// Is White one of the colors?
		/// </summary>
		public bool IsWhite { get; private set; }

		/// <summary>
		/// Is Blue one of the colors?
		/// </summary>
		public bool IsBlue { get; private set; }

		/// <summary>
		/// Is Black one of the colors?
		/// </summary>
		public bool IsBlack { get; private set; }

		/// <summary>
		/// Is Red one of the colors?
		/// </summary>
		public bool IsRed { get; private set; }

		/// <summary>
		/// Is Green one of the colors?
		/// </summary>
		public bool IsGreen { get; private set; }

		/// <summary>
		/// An integer representing the "sort order" of the color set.
		/// </summary>
		public int SortOrder { get; private set; }

		/// <summary>
		/// A string representation of the color set.
		/// </summary>
		public string ColorString { get; private set; }

		/// <summary>
		/// Private constructor called by the static CardColorFromString method.
		/// </summary>
		/// <param name="isWhite">Is this a White color combo?</param>
		/// <param name="isBlue">Is this a Blue color combo?</param>
		/// <param name="isBlack">Is this a Black color combo?</param>
		/// <param name="isRed">Is this a Red color combo?</param>
		/// <param name="isGreen">Is this a Green color combo?</param>
		/// <param name="sortOrder">An integer representing the "sort order" of the color set.</param>
		private CardColors(bool isWhite, bool isBlue, bool isBlack, bool isRed, bool isGreen, int sortOrder)
		{
			IsWhite = isWhite;
			IsBlue = isBlue;
			IsBlack = isBlack;
			IsRed = isRed;
			IsGreen = isGreen;
			SortOrder = sortOrder;
			ColorString = string.Format("{0}{1}{2}{3}{4}",
				isWhite ? "W" : string.Empty,
				isBlue ? "U" : string.Empty,
				isBlack ? "B" : string.Empty,
				isRed ? "R" : string.Empty,
				isGreen ? "G" : string.Empty);
		}

		/// <summary>
		/// Check whether this color combo contains another one.
		/// </summary>
		/// <param name="other">The other color combo to check.</param>
		/// <returns>True if this color is the same as the other, or completely contains it; false otherwise.</returns>
		public bool Contains(CardColors other)
		{
			if (other.IsWhite && !IsWhite) return false;
			if (other.IsBlue && !IsBlue) return false;
			if (other.IsBlack && !IsBlack) return false;
			if (other.IsRed && !IsRed) return false;
			if (other.IsGreen && !IsGreen) return false;
			return true;
		}

		/// <summary>
		/// Get a string representation of the color combo.
		/// </summary>
		/// <returns>A string representation of the color combo.</returns>
		public override string ToString()
		{
			return ColorString;
		}

		/// <summary>
		/// Check whether this color combo is equal to another object/value.
		/// </summary>
		/// <param name="obj">The other object/value to compare this color combo to.</param>
		/// <returns>True if this color combo is equal to the specified object/value; false otherwise.</returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			else if (obj is string)
			{
				return CompareTo(obj as string) == 0;
			}
			else if (obj is int)
			{
				return CompareTo((int)obj) == 0;
			}
			else if (obj is CardColors)
			{
				return CompareTo(obj as CardColors) == 0;
			}

			return false;
		}

		/// <summary>
		/// Get this color combo's hash code.
		/// </summary>
		/// <returns>The color combo's sort order.</returns>
		public override int GetHashCode()
		{
			return SortOrder;
		}

		/// <summary>
		/// Static dictionary containing all CardColors objects that have been created.
		/// </summary>
		private static Dictionary<int, CardColors> _cachedColors = new Dictionary<int, CardColors>();

		/// <summary>
		/// Get a CardColors object from a string specification.
		/// </summary>
		/// <param name="colorString">The string specification for the color combo.</param>
		/// <returns>The specified CardColors object.</returns>
		public static CardColors CardColorFromString(string colorString)
		{
			bool isWhite = colorString.Contains("W");
			bool isBlue = colorString.Contains("U");
			bool isBlack = colorString.Contains("B");
			bool isRed = colorString.Contains("R");
			bool isGreen = colorString.Contains("G");

			int sortOrder = 0;
			sortOrder += isWhite ? 1 : 0;
			sortOrder += isBlue ? 2 : 0;
			sortOrder += isBlack ? 4 : 0;
			sortOrder += isRed ? 8 : 0;
			sortOrder += isGreen ? 16 : 0;

			if (!_cachedColors.ContainsKey(sortOrder))
			{
				_cachedColors[sortOrder] = new CardColors(isWhite, isBlue, isBlack, isRed, isGreen, sortOrder);
			}

			return _cachedColors[sortOrder];
		}

		/// <summary>
		/// Compare this CardColors object to another one.
		/// </summary>
		/// <param name="other">The other CardColors object to compare this one to.</param>
		/// <returns>0 if the objects are equal, otherwise a difference value based on the objects' sort orders.</returns>
		public int CompareTo(CardColors other)
		{
			return SortOrder.CompareTo(other.SortOrder);
		}

		/// <summary>
		/// Compare this CardColors object to an integer.
		/// </summary>
		/// <param name="other">The integer to compare this CardColors object to.</param>
		/// <returns>A comparison of this object's sort order with the specified integer.</returns>
		public int CompareTo(int other)
		{
			return SortOrder.CompareTo(other);
		}

		/// <summary>
		/// Compare this CardColors object to an string.
		/// </summary>
		/// <param name="other">The string to compare this CardColors object to.</param>
		/// <returns>A comparison result similar to comparing directly to the CardColors object reprsented by the specified string.</returns>
		public int CompareTo(string other)
		{
			bool isWhite = other.Contains("W");
			bool isBlue = other.Contains("U");
			bool isBlack = other.Contains("B");
			bool isRed = other.Contains("R");
			bool isGreen = other.Contains("G");

			int sortOrder = 0;
			sortOrder += isWhite ? 1 : 0;
			sortOrder += isBlue ? 2 : 0;
			sortOrder += isBlack ? 4 : 0;
			sortOrder += isRed ? 8 : 0;
			sortOrder += isGreen ? 16 : 0;

			return CompareTo(sortOrder);
		}
	}
}
