using DailyArenaDeckAdvisor.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace DailyArenaDeckAdvisor
{
	public class Archetype
	{
		public class CardView
		{
			public Card Card { get; set; }
			public int Quantity { get; set; }
			public bool Collected { get; set; }
			public CardStats Stats { get; set; }
		}

		public string Name { get; private set; }
		public Dictionary<string, int> MainDeck { get; private set; }
		public Dictionary<string, int> Sideboard { get; private set; }
		public Dictionary<int, int> MainDeckToCollect { get; set; }
		public Dictionary<int, int> SideboardToCollect { get; set; }

		public int Index { get; private set; } = NextIndex++;

		public Archetype(string name, Dictionary<string, int> mainDeck, Dictionary<string, int> sideboard)
		{
			Name = name;
			MainDeck = mainDeck;
			Sideboard = sideboard;
		}

		public double BoosterCost
		{
			get
			{
				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				return MainDeckToCollect.Sum(x => cardsById[x.Key].BoosterCost * x.Value) +
					SideboardToCollect.Sum(x => cardsById[x.Key].BoosterCost * x.Value);
			}
		}

		public double BoosterCostAfterWC
		{
			get
			{
				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				var cardsToSelect = MainDeckToCollect.Select(x => new { cardsById[x.Key].Rarity, Card = cardsById[x.Key], Count = x.Value }).
					Concat(SideboardToCollect.Select(x => new { cardsById[x.Key].Rarity, Card = cardsById[x.Key], Count = x.Value })).
					GroupBy(x => x.Rarity).
					Select(x => new { Rarity = x.Key, Cards = x.SelectMany(y => Enumerable.Repeat(y.Card, y.Count)).OrderBy(z => z.BoosterCost).ToList() }).
					ToDictionary(x => x.Rarity, y => y.Cards);

				List<Card> finalListOfCardsToCollect = new List<Card>();

				int common = WildcardsOwned[CardRarity.Common];
				var commonCards = cardsToSelect.ContainsKey(CardRarity.Common) ? cardsToSelect[CardRarity.Common] : new List<Card>();
				for (int i = 0; i < commonCards.Count; i++)
				{
					if(common == 0)
					{
						finalListOfCardsToCollect.AddRange(commonCards.GetRange(i, commonCards.Count - i));
						break;
					}
					common--;
				}

				int uncommon = WildcardsOwned[CardRarity.Uncommon];
				var uncommonCards = cardsToSelect.ContainsKey(CardRarity.Uncommon) ? cardsToSelect[CardRarity.Uncommon] : new List<Card>();
				for (int i = 0; i < uncommonCards.Count; i++)
				{
					if (uncommon == 0)
					{
						finalListOfCardsToCollect.AddRange(uncommonCards.GetRange(i, uncommonCards.Count - i));
						break;
					}
					uncommon--;
				}

				int rare = WildcardsOwned[CardRarity.Rare];
				var rareCards = cardsToSelect.ContainsKey(CardRarity.Rare) ? cardsToSelect[CardRarity.Rare] : new List<Card>();
				for (int i = 0; i < rareCards.Count; i++)
				{
					if (rare == 0)
					{
						finalListOfCardsToCollect.AddRange(rareCards.GetRange(i, rareCards.Count - i));
						break;
					}
					rare--;
				}

				int mythic = WildcardsOwned[CardRarity.MythicRare];
				var mythicCards = cardsToSelect.ContainsKey(CardRarity.MythicRare) ? cardsToSelect[CardRarity.MythicRare] : new List<Card>();
				for (int i = 0; i < mythicCards.Count; i++)
				{
					if (mythic == 0)
					{
						finalListOfCardsToCollect.AddRange(mythicCards.GetRange(i, mythicCards.Count - i));
						break;
					}
					mythic--;
				}

				return finalListOfCardsToCollect.Sum(x => x.BoosterCost);
			}
		}

		public List<Tuple<CardRarity, int, int>> WildcardsNeeded
		{
			get
			{
				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				return MainDeckToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = AnyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (SuggestedMainDeck.Where(y => y.Key == x.Key).Count() + SuggestedSideboard.Where(z => z.Key == x.Key).Count()) : x.Value }).
					Concat(SideboardToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = AnyNumber.Contains(cardsById[x.Key].Name) ? Math.Min(x.Value, 4) - (SuggestedMainDeck.Where(y => y.Key == x.Key).Count() + SuggestedSideboard.Where(z => z.Key == x.Key).Count()) : x.Value })).
					GroupBy(x => x.Rarity).
					Select(x => new Tuple<CardRarity, int, int>(x.Key, x.Sum(y => y.Count), WildcardsOwned[x.Key])).
					OrderByDescending(x => x.Item1).ToList();
			}
		}

		public int TotalWildcardsNeeded
		{
			get
			{
				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				return MainDeckToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = x.Value }).
					Concat(SideboardToCollect.Select(x => new { cardsById[x.Key].Rarity, Count = x.Value })).
					GroupBy(x => x.Rarity).
					Select(x => new { Rarity = x.Key, Count = x.Sum(y => y.Count) }).
					Sum(x => x.Count - Math.Min(WildcardsOwned[x.Rarity], x.Count));
			}
		}

		public Dictionary<int, int> SuggestedMainDeck { get; set; }
		public Dictionary<int, int> SuggestedSideboard { get; set; }

		public List<Tuple<int, int, int>> SuggestedReplacements { get; set; }

		public List<Tuple<string, string, int>> SuggestedReplacementsView
		{
			get
			{
				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				return SuggestedReplacements.
					Select(x => new Tuple<string, string, int>(cardsById[x.Item1].Name, cardsById[x.Item2].Name, x.Item3)).ToList();
			}
		}

		public List<CardView> MainDeckView
		{
			get
			{
				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				return MainDeckToCollect.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = false, Stats = CardStats[cardsById[x.Key]] }).
					Concat(SuggestedMainDeck.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = true, Stats = CardStats[cardsById[x.Key]] }))
					.ToList();
			}
		}

		public Visibility SideboardVisibility
		{
			get
			{
				return (SideboardToCollect.Count + SuggestedSideboard.Count) == 0 ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		public Visibility CollectVisibility
		{
			get
			{
				return (SideboardToCollect.Count + MainDeckToCollect.Count) == 0 ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		public List<CardView> SideboardView
		{
			get
			{
				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				return SideboardToCollect.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = false, Stats = CardStats[cardsById[x.Key]] }).
					Concat(SuggestedSideboard.Select(x => new CardView() { Card = cardsById[x.Key], Quantity = x.Value, Collected = true, Stats = CardStats[cardsById[x.Key]] }))
					.ToList();
			}
		}

		public string ExportList
		{
			get
			{
				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				var mainDeck = SuggestedMainDeck.Concat(MainDeckToCollect).GroupBy(x => x.Key).
					Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
					Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");
				var sideboard = SuggestedSideboard.Concat(SideboardToCollect).GroupBy(x => x.Key).
					Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
					Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");

				return string.Format("{0}{1}{1}{2}", string.Join(Environment.NewLine, mainDeck), Environment.NewLine, string.Join(Environment.NewLine, sideboard));
			}
		}

		public string ExportListSuggested
		{
			get
			{
				Dictionary<int, int> mainDeckReplacements = new Dictionary<int, int>();
				Dictionary<int, int> sideboardReplacements = new Dictionary<int, int>();
				foreach(Tuple<int, int, int> replacement in SuggestedReplacements)
				{
					int replacementCount = replacement.Item3;
					if(MainDeckToCollect.ContainsKey(replacement.Item1))
					{
						if(MainDeckToCollect[replacement.Item1] >= replacementCount)
						{
							if (mainDeckReplacements.ContainsKey(replacement.Item2))
							{
								mainDeckReplacements[replacement.Item2] += replacementCount;
							}
							else
							{
								mainDeckReplacements.Add(replacement.Item2, replacementCount);
							}
							replacementCount = 0;
						}
						else
						{
							if (mainDeckReplacements.ContainsKey(replacement.Item2))
							{
								mainDeckReplacements[replacement.Item2] += MainDeckToCollect[replacement.Item1];
							}
							else
							{
								mainDeckReplacements.Add(replacement.Item2, MainDeckToCollect[replacement.Item1]);
							}
							replacementCount -= MainDeckToCollect[replacement.Item1];
						}
					}
					if(replacementCount > 0)
					{
						if (sideboardReplacements.ContainsKey(replacement.Item2))
						{
							sideboardReplacements[replacement.Item2] += replacementCount;
						}
						else
						{
							sideboardReplacements.Add(replacement.Item2, replacementCount);
						}
					}
				}

				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				var mainDeck = SuggestedMainDeck.Concat(mainDeckReplacements).GroupBy(x => x.Key).
					Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
					Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");
				var sideboard = SuggestedSideboard.Concat(sideboardReplacements).GroupBy(x => x.Key).
					Select(x => new { Card = cardsById[x.Key], Quantity = x.Sum(y => y.Value) }).
					Select(x => $"{x.Quantity} {x.Card.Name} ({x.Card.Set.ArenaCode}) {x.Card.CollectorNumber}");

				return string.Format("{0}{1}{1}{2}", string.Join(Environment.NewLine, mainDeck), Environment.NewLine, string.Join(Environment.NewLine, sideboard));
			}
		}

		public string NextBoosterSetToPurchase
		{
			get
			{
				ReadOnlyDictionary<int, Card> cardsById = Card.CardsById;
				var orderedSets = MainDeckToCollect.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value }).
					Concat(SideboardToCollect.Select(x => new { Card = cardsById[x.Key], Quantity = x.Value })).
					Select(x => new { SetName = x.Card.Set.Name, BoosterCost = x.Card.BoosterCost * x.Quantity }).
					GroupBy(x => x.SetName ).
					Select(x => new { SetName = x.Key, BoosterCost = x.Sum(y => y.BoosterCost) }).
					OrderByDescending(x => x.BoosterCost);
				
				if(orderedSets.Count() > 0)
				{
					return orderedSets.First().SetName;
				}

				return string.Empty;
			}
		}

		public static Dictionary<CardRarity, int> WildcardsOwned { get; set; }

		public static Dictionary<Card, CardStats> CardStats { get; set; }

		public static List<string> AnyNumber { get; set; }

		private static int NextIndex { get; set; } = 0;
	}
}
