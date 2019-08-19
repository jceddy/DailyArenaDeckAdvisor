using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DailyArenaDeckAdvisor.Database
{
	public class Set : IComparable<Set>, IComparable<string>
	{
		public string Code { get; private set; }

		public string ArenaCode { get; private set; }

		public string Name { get; private set; }

		private List<string> _notInBooster = new List<string>();
		public ReadOnlyCollection<string> NotInBooster
		{
			get
			{
				return _notInBooster.AsReadOnly();
			}
		}

		private readonly Dictionary<CardRarity, int> _rarityCounts = new Dictionary<CardRarity, int>()
		{
			{ CardRarity.Common, 0 },
			{ CardRarity.Uncommon, 0 },
			{ CardRarity.Rare, 0 },
			{ CardRarity.MythicRare, 0 }
		};

		public ReadOnlyDictionary<CardRarity, int> RarityCounts
		{
			get
			{
				return new ReadOnlyDictionary<CardRarity, int>(_rarityCounts);
			}
		}

		public int TotalCards { get; private set; }

		public int HashCode { get; private set; }

		private Set(string name, string code, string arenaCode, List<string> notInBooster, int totalCards, Dictionary<CardRarity, int> rarityCounts)
		{
			Code = code;
			ArenaCode = arenaCode;
			Name = name;
			_notInBooster.AddRange(notInBooster);
			TotalCards = totalCards;
			foreach(var pair in rarityCounts)
			{
				_rarityCounts[pair.Key] = pair.Value;
			}
			HashCode = arenaCode.GetHashCode();
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
			else if (obj is Set)
			{
				return CompareTo(obj as Set) == 0;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return HashCode;
		}

		private static Dictionary<string, Set> _setsByName = new Dictionary<string, Set>();

		public static Set CreateSet(string name, string code, string arenaCode, List<string> notInBooster, int totalCards,
			Dictionary<CardRarity, int> rarityCounts)
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

			Set newSet = new Set(name, code, arenaCode, notInBooster, totalCards, rarityCounts);
			_setsByName.Add(name, newSet);
			return newSet;
		}

		public static Set GetSet(string name)
		{
			return _setsByName[name];
		}

		public int CompareTo(Set other)
		{
			return Code.CompareTo(other.Code);
		}

		public int CompareTo(string other)
		{
			return Code.CompareTo(other);
		}

		public static ReadOnlyCollection<Set> AllSets
		{
			get
			{
				return _setsByName.Values.ToList().AsReadOnly();
			}
		}

		public static void ClearSets()
		{
			_setsByName.Clear();
		}
	}
}
