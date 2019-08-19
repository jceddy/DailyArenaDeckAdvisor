using System;
using System.ComponentModel;
using System.Globalization;

namespace DailyArenaDeckAdvisor.Database
{
	[TypeConverter(typeof(Converter))]
	public class CardRarity : IComparable<CardRarity>, IComparable<int>, IComparable<string>
	{
		public class Converter : TypeConverter
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
			{
				if(sourceType == typeof(string))
				{
					return true;
				}
				return base.CanConvertFrom(context, sourceType);
			}

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if(value is string)
				{
					return CardRarityFromString(value.ToString());
				}
				return base.ConvertFrom(context, culture, value);
			}

			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if(destinationType == typeof(string))
				{
					return ((CardRarity)value).ToString();
				}
				return base.ConvertTo(context, culture, value, destinationType);
			}
		}

		public int SortOrder { get; private set; }

		public string Name { get; private set; }

		private CardRarity(string name, int sortOrder)
		{
			Name = name;
			SortOrder = sortOrder;
		}

		public override string ToString()
		{
			return Name;
		}

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

		public override int GetHashCode()
		{
			return SortOrder;
		}

		public static CardRarity Token { get; private set; } = new CardRarity("Token", 0);
		public static CardRarity BasicLand { get; private set; } = new CardRarity("Basic Land", 1);
		public static CardRarity Common { get; private set; } = new CardRarity("Common", 2);
		public static CardRarity Uncommon { get; private set; } = new CardRarity("Uncommon", 3);
		public static CardRarity Rare { get; private set; } = new CardRarity("Rare", 4);
		public static CardRarity MythicRare { get; private set; } = new CardRarity("Mythic Rare", 5);

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

		public int CompareTo(CardRarity other)
		{
			return SortOrder.CompareTo(other.SortOrder);
		}

		public int CompareTo(int other)
		{
			return SortOrder.CompareTo(other);
		}

		public int CompareTo(string other)
		{
			return Name.CompareTo(other);
		}
	}
}
