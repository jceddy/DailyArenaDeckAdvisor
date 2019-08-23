using System;
using System.ComponentModel;
using System.Globalization;

namespace DailyArenaDeckAdvisor.Database
{
	/// <summary>
	/// Type that represents a Magic Card rarity.
	/// </summary>
	[TypeConverter(typeof(Converter))]
	public class CardRarity : IComparable<CardRarity>, IComparable<int>, IComparable<string>
	{
		/// <summary>
		/// A converter that converts between string and CardRarity.
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
				if(sourceType == typeof(string))
				{
					return true;
				}
				return base.CanConvertFrom(context, sourceType);
			}

			/// <summary>
			/// Convert an object to a CardRarity.
			/// </summary>
			/// <param name="context">The Type Descriptor Context (not used).</param>
			/// <param name="culture">The current Culture info (not used).</param>
			/// <param name="value">The object to convert.</param>
			/// <returns>A CardRarity object converted from the source value.</returns>
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if(value is string)
				{
					return CardRarityFromString(value.ToString());
				}
				return base.ConvertFrom(context, culture, value);
			}

			/// <summary>
			/// Convert an CardRarity object to a specified destination Type.
			/// </summary>
			/// <param name="context">The Type Descriptor Context (not used).</param>
			/// <param name="culture">The current Culture info (not used).</param>
			/// <param name="value">The CardRarity object to convert.</param>
			/// <param name="destinationType">The destination Type to convert the CardRarity object to.</param>
			/// <returns>An object of the destination Type that represents the CardRarity object value.</returns>
			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if(destinationType == typeof(string))
				{
					return ((CardRarity)value).ToString();
				}
				return base.ConvertTo(context, culture, value, destinationType);
			}
		}

		/// <summary>
		/// An integer that specifies the CardRarity's relative value, for sorting and comparisons.
		/// </summary>
		public int SortOrder { get; private set; }

		/// <summary>
		/// The CardRarity's Name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The CardRarity Constructor, called by the static property constructors for predefined rarities.
		/// </summary>
		/// <param name="name">The string Name of the rarity.</param>
		/// <param name="sortOrder">The rarity's relative value.</param>
		private CardRarity(string name, int sortOrder)
		{
			Name = name;
			SortOrder = sortOrder;
		}

		/// <summary>
		/// Get a string representation of the rarity.
		/// </summary>
		/// <returns>The value of the CardRarity's Name property.</returns>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Check whether this rarity is equivalent to another object.
		/// </summary>
		/// <param name="obj">The other object to compare this rarity to.</param>
		/// <returns>True if this rarity is equal to the comparison object; false otherwise.</returns>
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
			else if (obj is CardRarity)
			{
				return CompareTo(obj as CardRarity) == 0;
			}

			return false;
		}

		/// <summary>
		/// Get this rarity's Hash Code.
		/// </summary>
		/// <returns>The value of the rarity's SortOrder property.</returns>
		public override int GetHashCode()
		{
			return SortOrder;
		}

		/// <summary>
		/// Token Rarity
		/// </summary>
		public static CardRarity Token { get; private set; } = new CardRarity("Token", 0);

		/// <summary>
		/// Basic Land Rarity
		/// </summary>
		public static CardRarity BasicLand { get; private set; } = new CardRarity("Basic Land", 1);

		/// <summary>
		/// Common Rarity
		/// </summary>
		public static CardRarity Common { get; private set; } = new CardRarity("Common", 2);

		/// <summary>
		/// Uncommon Rarity
		/// </summary>
		public static CardRarity Uncommon { get; private set; } = new CardRarity("Uncommon", 3);

		/// <summary>
		/// Rare Rarity
		/// </summary>
		public static CardRarity Rare { get; private set; } = new CardRarity("Rare", 4);

		/// <summary>
		/// Mythic Rare Rarity
		/// </summary>
		public static CardRarity MythicRare { get; private set; } = new CardRarity("Mythic Rare", 5);

		/// <summary>
		/// Convert a string to a CardRarity object.
		/// </summary>
		/// <param name="rarity">The string representation of the rarity.</param>
		/// <returns>The CardRarity corresponding to the specified string value.</returns>
		public static CardRarity CardRarityFromString(string rarity)
		{
			switch (rarity)
			{
				case "token":
				case "Token":
					return Token;
				case "land":
				case "Basic Land":
					return BasicLand;
				case "common":
				case "Common":
					return Common;
				case "uncommon":
				case "Uncommon":
					return Uncommon;
				case "rare":
				case "Rare":
					return Rare;
				case "mythic":
				case "Mythic Rare":
					return MythicRare;
				default: throw new ArgumentException($"Invalid Rarity {rarity}", "rarity");
			}
		}

		/// <summary>
		/// Compare this rarity to another CardRarity object.
		/// </summary>
		/// <param name="other">The other CardRarity object to compare this one to.</param>
		/// <returns>0 if the rarities have the same sort order, otherwise the result of a comparison of the two objects' sort orders.</returns>
		public int CompareTo(CardRarity other)
		{
			return SortOrder.CompareTo(other.SortOrder);
		}

		/// <summary>
		/// Compare this rarity to an integer.
		/// </summary>
		/// <param name="other">The integer to compare this rarity to.</param>
		/// <returns>0 if the rarity's SortOrder matches the comparison integer, otherwise the result of a comparison between the SortOrder and the integer.</returns>
		public int CompareTo(int other)
		{
			return SortOrder.CompareTo(other);
		}

		/// <summary>
		/// Compare this rarity to a string.
		/// </summary>
		/// <param name="other">The string to compare this rarity to.</param>
		/// <returns>0 if the comparison string matches the rarity's Name, otherwise the result of a comparison between the string and the rarity's Name.</returns>
		public int CompareTo(string other)
		{
			return Name.CompareTo(other);
		}
	}
}
