using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DailyArenaDeckAdvisor.Database
{
	/// <summary>
	/// Class representing a Magic Expansion.
	/// </summary>
	public class Set : IComparable<Set>, IComparable<string>
	{
		/// <summary>
		/// The set's canonical short code.
		/// </summary>
		public string Code { get; private set; }

		/// <summary>
		/// The short code for the set used on Arena (usually an upper-case version of Code, but there are exceptions).
		/// </summary>
		public string ArenaCode { get; private set; }

		/// <summary>
		/// The full name of the set.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Get readonly list containing the names of cards from this set that don't appear in boosters.
		/// </summary>
		public ReadOnlyCollection<string> NotInBooster { get; private set; }

		/// <summary>
		/// Gets a readonly dictionary of rarity counts.
		/// </summary>
		public ReadOnlyDictionary<CardRarity, int> RarityCounts { get; private set; }

		/// <summary>
		/// Gets the total number of cards in the set.
		/// </summary>
		public int TotalCards { get; private set; }

		/// <summary>
		/// Gets the set's hash code (this is a hash of the set's Arena Code string).
		/// </summary>
		public int HashCode { get; private set; }

		/// <summary>
		/// Gets the set's rotation date.
		/// </summary>
		public DateTime Rotation { get; private set; }

		/// <summary>
		/// Gets a flag representing whether the set is "rotation safe" (will not rotate within the next 80 days).
		/// </summary>
		public bool RotationSafe { get; private set; }

		/// <summary>
		/// The Set constructor, called by the static CreateSet method.
		/// </summary>
		/// <param name="name">The set's full name.</param>
		/// <param name="code">The set's cannonical short code.</param>
		/// <param name="arenaCode">The set's short code in Arena.</param>
		/// <param name="notInBooster">A list of names of cards in the set that don't appear in boosters (from Planeswalker decks, etc.)</param>
		/// <param name="totalCards">The total number of cards in the set.</param>
		/// <param name="rarityCounts">The number of cards of each rarity in the set.</param>
		/// <param name="rotation">The date the set rotates out of standard.</param>
		private Set(string name, string code, string arenaCode, List<string> notInBooster, int totalCards, Dictionary<CardRarity, int> rarityCounts, DateTime rotation)
		{
			Code = code;
			ArenaCode = arenaCode;
			Name = name;
			NotInBooster = notInBooster.Select(x => x).ToList().AsReadOnly();
			TotalCards = totalCards;
			RarityCounts = new ReadOnlyDictionary<CardRarity, int>(new Dictionary<CardRarity, int>()
			{
				{ CardRarity.Common, rarityCounts.ContainsKey(CardRarity.Common) ? rarityCounts[CardRarity.Common] : 0 },
				{ CardRarity.Uncommon, rarityCounts.ContainsKey(CardRarity.Uncommon) ? rarityCounts[CardRarity.Uncommon] : 0 },
				{ CardRarity.Rare, rarityCounts.ContainsKey(CardRarity.Rare) ? rarityCounts[CardRarity.Rare] : 0 },
				{ CardRarity.MythicRare, rarityCounts.ContainsKey(CardRarity.MythicRare) ? rarityCounts[CardRarity.MythicRare] : 0 }
			});
			HashCode = arenaCode.GetHashCode();
			Rotation = rotation;
			RotationSafe = Rotation.AddDays(-80) > DateTime.Now;
		}

		/// <summary>
		/// A string representation of the set.
		/// </summary>
		/// <returns>The set's full name.</returns>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Check whether the set is equal to another object.
		/// </summary>
		/// <param name="obj">The object to compare the set to.</param>
		/// <returns>True if the comparison object is equal to the set; false otherwise.</returns>
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
			else if (obj is Set)
			{
				return CompareTo(obj as Set) == 0;
			}

			return false;
		}

		/// <summary>
		/// Get the set's Hash Code.
		/// </summary>
		/// <returns>The set's Hash Code (this is a hash of the set's Arena Code).</returns>
		public override int GetHashCode()
		{
			return HashCode;
		}

		/// <summary>
		/// A static Dictionary containing all of the sets keyed by Name.
		/// </summary>
		private static Dictionary<string, Set> _setsByName = new Dictionary<string, Set>();

		/// <summary>
		/// Static method to create a Set and add it to the Dictionary of Sets.
		/// </summary>
		/// <param name="name">The set's full name.</param>
		/// <param name="code">The set's cannonical short code.</param>
		/// <param name="arenaCode">The set's short code in Arena.</param>
		/// <param name="notInBooster">A list of names of cards in the set that don't appear in boosters (from Planeswalker decks, etc.)</param>
		/// <param name="totalCards">The total number of cards in the set.</param>
		/// <param name="rarityCounts">The number of cards of each rarity in the set.</param>
		/// <param name="rotation">The date the set rotates out of standard.</param>
		/// <returns>The new Set object.</returns>
		public static Set CreateSet(string name, string code, string arenaCode, List<string> notInBooster, int totalCards,
			Dictionary<CardRarity, int> rarityCounts, DateTime rotation)
		{
			if (_setsByName.ContainsKey(name))
			{
				Set set = _setsByName[name];
				if (set.ArenaCode != arenaCode)
				{
					throw new ArgumentException($"Set with Name {name} exists with code {set.ArenaCode}, but {arenaCode} was passed.");
				}
				return set;
			}

			Set newSet = new Set(name, code, arenaCode, notInBooster, totalCards, rarityCounts, rotation);
			_setsByName.Add(name, newSet);
			_recomputeAllSets = true;
			return newSet;
		}

		/// <summary>
		/// Get a Set by Name.
		/// </summary>
		/// <param name="name">The name of the set to get.</param>
		/// <returns>The set specified by the name.</returns>
		public static Set GetSet(string name)
		{
			return _setsByName[name];
		}

		/// <summary>
		/// Compare the Set to another one.
		/// </summary>
		/// <param name="other">The other Set to compare this one to.</param>
		/// <returns>0 if the two Set objects have the same Code, otherwise the result of a comparison of the two Sets' Codes.</returns>
		public int CompareTo(Set other)
		{
			return Code.CompareTo(other.Code);
		}

		/// <summary>
		/// Compare the Set to a string.
		/// </summary>
		/// <param name="other">The string to compare the Set to.</param>
		/// <returns>0 if the Set's Code equals the specified string, otherwise the result of a comparison of the Set's Code to the specified string.</returns>
		public int CompareTo(string other)
		{
			return Code.CompareTo(other);
		}

		/// <summary>
		/// A readonly list of all the sets.
		/// </summary>
		private static ReadOnlyCollection<Set> _allSets;

		/// <summary>
		/// The underlying set list has changed, recompute the readonly list the next time it's accessed.
		/// </summary>
		private static bool _recomputeAllSets = true;

		/// <summary>
		/// Gets a readonly list of all of the Sets;
		/// </summary>
		public static ReadOnlyCollection<Set> AllSets
		{
			get
			{
				if(_recomputeAllSets)
				{
					_allSets = _setsByName.Values.ToList().AsReadOnly();
					_recomputeAllSets = false;
				}
				return _allSets;
			}
		}

		/// <summary>
		/// Clear the list of Sets.
		/// </summary>
		public static void ClearSets()
		{
			_setsByName.Clear();
			_recomputeAllSets = true;
		}
	}
}
