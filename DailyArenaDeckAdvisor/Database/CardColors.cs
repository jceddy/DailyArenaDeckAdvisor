using System;
using System.Collections.Generic;

namespace DailyArenaDeckAdvisor.Database
{
	public class CardColors : IComparable<CardColors>, IComparable<int>, IComparable<string>
	{
		public bool IsWhite { get; private set; }
		public bool IsBlue { get; private set; }
		public bool IsBlack { get; private set; }
		public bool IsRed { get; private set; }
		public bool IsGreen { get; private set; }

		public int SortOrder { get; private set; }

		public string ColorString { get; private set; }

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

		public bool Contains(CardColors other)
		{
			if (other.IsWhite && !IsWhite) return false;
			if (other.IsBlue && !IsBlue) return false;
			if (other.IsBlack && !IsBlack) return false;
			if (other.IsRed && !IsRed) return false;
			if (other.IsGreen && !IsGreen) return false;
			return true;
		}

		public override string ToString()
		{
			return ColorString;
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
			else if (obj is CardColors)
			{
				return CompareTo(obj as CardColors) == 0;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return SortOrder;
		}

		private static Dictionary<int, CardColors> _cachedColors = new Dictionary<int, CardColors>();

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

		public int CompareTo(CardColors other)
		{
			return SortOrder.CompareTo(other.SortOrder);
		}

		public int CompareTo(int other)
		{
			return SortOrder.CompareTo(other);
		}

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
