using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;

namespace DailyArenaDeckAdvisor.Database
{
	/// <summary>
	/// Class that reprsents a Magic Card.
	/// </summary>
	public class Card : IComparable<Card>, IComparable<int>, IComparable<string>, INotifyPropertyChanged
	{
		/// <summary>
		/// The Unique Identifier of this card on Arena.
		/// </summary>
		public int ArenaId { get; private set; }

		/// <summary>
		/// This card's Rarity.
		/// </summary>
		public CardRarity Rarity { get; private set; }

		/// <summary>
		/// This card's Color definition.
		/// </summary>
		public CardColors Colors { get; private set; }

		/// <summary>
		/// This card's Name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The Set that contains this card.
		/// </summary>
		public Set Set { get; private set; }

		/// <summary>
		/// This card's Collector Number.
		/// </summary>
		public string CollectorNumber { get; private set; }

		/// <summary>
		/// The string used to identify this specfic card/printing when exporting from or importing to Arena.
		/// </summary>
		public string FullName { get; private set; }

		/// <summary>
		/// This card's power ranking used when generating replacement suggestions.
		/// </summary>
		public int Rank { get; private set; }

		/// <summary>
		/// A string containing this card's types.
		/// </summary>
		public string Type { get; private set; }

		/// <summary>
		/// This card's casting cost.
		/// </summary>
		public string Cost { get; private set; }

		/// <summary>
		/// This card's converted mana cost.
		/// </summary>
		public int Cmc { get; private set; }

		/// <summary>
		/// The average number of boosters required to open this card or a wildcard with this card's rarity.
		/// </summary>
		public double BoosterCost { get; private set; }

		/// <summary>
		/// The card's unique identifier on Scryfall.
		/// </summary>
		public string ScryfallId { get; private set; }

		/// <summary>
		/// The Uri for the card's (cached) image.
		/// </summary>
		private Uri _imageUri = null;

		/// <summary>
		/// The Uri for the card's (normal size) image.
		/// </summary>
		public Uri ImageUri {
			get
			{
				if(string.IsNullOrWhiteSpace(ScryfallId))
				{
					return null;
				}
				if(_imageUri == null)
				{
					string cachedImageLocation = $"{_cachedCardImageFolder}\\{ScryfallId}.jpg";
					Uri cachedImageUri = new Uri(cachedImageLocation);
					if (File.Exists(cachedImageLocation))
					{
						File.SetLastAccessTime(cachedImageLocation, DateTime.Now);
						_imageUri = cachedImageUri;
					}
					else
					{
						using (WebClient client = new WebClient())
						{
							client.DownloadDataCompleted += (sender, e) =>
							{
								if (e.Error == null)
								{
									File.WriteAllBytes(cachedImageLocation, e.Result);
									_imageUri = cachedImageUri;
									PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageUri"));
								}
							};
							client.DownloadDataAsync(new Uri($"https://www.jceddy.com/mtg/rmm/v2/card_images/normal/{ScryfallId}.jpg"));
						}
					}
				}
				return _imageUri;
			}
		}

		/// <summary>
		/// File path to the cached card images folder.
		/// </summary>
		private static string _cachedCardImageFolder;

		/// <summary>
		/// Static constructor, sets up folder for local image caching, clears any images that haven't been used for over a month.
		/// </summary>
		static Card()
		{
			_cachedCardImageFolder = $"{Directory.GetCurrentDirectory()}\\cached_card_images";
			if (!Directory.Exists(_cachedCardImageFolder))
			{
				Directory.CreateDirectory(_cachedCardImageFolder);
			}
			else
			{
				string[] files = Directory.GetFiles(_cachedCardImageFolder);
				DateTime aMonthAgo = DateTime.Now.AddMonths(-1);
				foreach (string file in files)
				{
					FileInfo fi = new FileInfo(file);
					if(fi.LastAccessTime < aMonthAgo)
					{
						fi.Delete();
					}
				}
			}
		}

		/// <summary>
		/// The constructor for creating a new Card object. This is called internally by the public static CreateCard method.
		/// </summary>
		/// <param name="arenaId">The Unique Identifier of the card on Arena.</param>
		/// <param name="name">The card's Name.</param>
		/// <param name="set">The Set that contains the card.</param>
		/// <param name="collectorNumber">The card's collector number.</param>
		/// <param name="rarity">The card's Rarity.</param>
		/// <param name="colors">The card's Color definition.</param>
		/// <param name="fullName">The string used to identify the specfic card/printing when exporting from or importing to Arena.</param>
		/// <param name="rank">The card's power ranking used when generating replacement suggestions.</param>
		/// <param name="type">A string containing the card's types.</param>
		/// <param name="cost">The card's casting cost.</param>
		/// <param name="cmc">The card's converted mana cost.</param>
		/// <param name="scryfallId">The card's unique identifier on Scryfall.</param>
		private Card(int arenaId, string name, Set set, string collectorNumber, string rarity, string colors, string fullName, int rank, string type,
			string cost, int cmc, string scryfallId)
		{
			ArenaId = arenaId;
			Name = name;
			Set = set;
			CollectorNumber = collectorNumber;
			Rarity = CardRarity.CardRarityFromString(rarity);
			Colors = CardColors.CardColorFromString(colors);
			FullName = fullName;
			Rank = rank;
			Type = type;
			Cost = cost;
			Cmc = cmc;
			ScryfallId = scryfallId;

			if (Rarity == CardRarity.BasicLand)
			{
				BoosterCost = 0;
			}
			else
			{
				double cardFreq = WildcardFreq[Rarity];
				if (!Set.NotInBooster.Contains(Name))
				{
					cardFreq += BoosterFreq();
				}
				BoosterCost = 1.0 / cardFreq;
			}
		}

		/// <summary>
		/// Conver the Card object into a string.
		/// </summary>
		/// <returns>The string used to identify this specfic card/printing when exporting from or importing to Arena.</returns>
		public override string ToString()
		{
			return FullName;
		}

		/// <summary>
		/// Check if the card is equal to some other object.
		/// </summary>
		/// <param name="obj">The other object to compart the card to.</param>
		/// <returns>True if the objects are equal, false otherwise.</returns>
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
			else if (obj is Card)
			{
				return CompareTo(obj as Card) == 0;
			}

			return false;
		}

		/// <summary>
		/// Get the card's hash code.
		/// </summary>
		/// <returns>The card's Arena Id.</returns>
		public override int GetHashCode()
		{
			return ArenaId;
		}

		/// <summary>
		/// Compare this card to another card.
		/// </summary>
		/// <param name="other">The other card to compare this card to.</param>
		/// <returns>0 if the cards have the same Arena Id, negative if this card has a lower Arena Id, positive if this card has a higher Arena Id</returns>
		public int CompareTo(Card other)
		{
			return ArenaId.CompareTo(other.ArenaId);
		}

		/// <summary>
		/// Compare this card to an integer.
		/// </summary>
		/// <param name="other">The integer to compare this card to.</param>
		/// <returns>0 if the integer is equal to the card's Arena Id, negative if this card's Arena Id is lower, positive if this card's Arena Id is higher</returns>
		public int CompareTo(int other)
		{
			return ArenaId.CompareTo(other);
		}

		/// <summary>
		/// Compare this card to a string.
		/// </summary>
		/// <param name="other">The string to compare this card to.</param>
		/// <returns>0 is the string is equal to this card's full name, otherwise positive or negative based on the current string comparison rules</returns>
		public int CompareTo(string other)
		{
			return FullName.CompareTo(other);
		}

		/// <summary>
		/// A dictionary that indexes all Cards in the database by their Arena Id.
		/// </summary>
		private static Dictionary<int, Card> _cardsById = new Dictionary<int, Card>();

		/// <summary>
		/// A dictionary that contains lists of Cards grouped by Name.
		/// </summary>
		private static Dictionary<string, List<Card>> _cardsByName = new Dictionary<string, List<Card>>();

		/// <summary>
		/// A dictionary that indexes all Cards by their Full Name.
		/// </summary>
		private static Dictionary<string, Card> _cardsByFullName = new Dictionary<string, Card>();

		/// <summary>
		/// Static Method to create new card object and add it to the database.
		/// </summary>
		/// <param name="arenaId">The Unique Identifier of the card on Arena.</param>
		/// <param name="name">The card's Name.</param>
		/// <param name="setName">The name of the Set that contains the card.</param>
		/// <param name="collectorNumber">The card's collector number.</param>
		/// <param name="rarity">A string representing the card's Rarity.</param>
		/// <param name="colors">A string representing the card's Colors.</param>
		/// <param name="rank">The card's power ranking used when generating replacement suggestions.</param>
		/// <param name="type">A string containing the card's types.</param>
		/// <param name="cost">The card's casting cost</param>
		/// <param name="cmc">The card's converted mana cost.</param>
		/// <param name="scryfallId">The card's unique identifier on Scryfall.</param>
		/// <returns>The new Card object that was added to the database.</returns>
		public static Card CreateCard(int arenaId, string name, string setName, string collectorNumber, string rarity, string colors, int rank,
			string type, string cost, int cmc, string scryfallId)
		{
			Set set = Set.GetSet(setName);

			string fullName = $"{name} ({set.ArenaCode}) {collectorNumber}";
			if (_cardsById.ContainsKey(arenaId))
			{
				Card card = _cardsById[arenaId];
				if (card.FullName != fullName)
				{
					throw new ArgumentException($"Card with ArenaId {arenaId} exists with full name {card.FullName}, but {fullName} was passed.");
				}
				else if (_cardsByFullName.ContainsKey(fullName))
				{
					int existingId = _cardsByFullName[fullName].ArenaId;
					throw new ArgumentException($"Card with full name {fullName} exists with ArenaId {existingId}, but {arenaId} was passed.");
				}
				return card;
			}

			Card newCard = new Card(arenaId, name, set, collectorNumber, rarity, colors, fullName, rank, type, cost, cmc, scryfallId);
			_cardsById.Add(arenaId, newCard);
			if (!_cardsByFullName.ContainsKey(fullName))
			{
				// there are a few cards with art variants that aren't distinguishable by full name,
				// in those cases, we just keep the first, so _cardsByFullName will be a slightly smaller
				// set of cards than _cardsById
				_cardsByFullName.Add(fullName, newCard);
			}
			if(!_cardsByName.ContainsKey(name))
			{
				_cardsByName.Add(name, new List<Card>());
			}
			_cardsByName[name].Add(newCard);
			return newCard;
		}

		/// <summary>
		/// Get a card by Arena Id.
		/// </summary>
		/// <param name="arenaId">The Arena Id of the card to get.</param>
		/// <returns>The card with the specified Arena Id.</returns>
		public static Card GetCard(int arenaId)
		{
			return _cardsById[arenaId];
		}

		/// <summary>
		/// Get a card by Full Name.
		/// </summary>
		/// <param name="fullName">The string used to identify the specfic card/printing when exporting from or importing to Arena.</param>
		/// <returns>The card with the specified Full Name.</returns>
		public static Card GetCard(string fullName)
		{
			return _cardsByFullName[fullName];
		}

		/// <summary>
		/// Gets a read-only list of all cards in the database.
		/// </summary>
		public static ReadOnlyCollection<Card> AllCards
		{
			get
			{
				return _cardsById.Values.ToList().AsReadOnly();
			}
		}

		/// <summary>
		/// Gets a read-only dictionary that indexes all Cards in the database by their Arena Id.
		/// </summary>
		public static ReadOnlyDictionary<int, Card> CardsById
		{
			get
			{
				return new ReadOnlyDictionary<int, Card>(_cardsById);
			}
		}

		/// <summary>
		/// Gets a read-only dictionary that contains lists of Cards grouped by Name.
		/// </summary>
		public static ReadOnlyDictionary<string, List<Card>> CardsByName
		{
			get
			{
				return new ReadOnlyDictionary<string, List<Card>>(_cardsByName);
			}
		}

		/// <summary>
		/// Clear the card database.
		/// </summary>
		public static void ClearCards()
		{
			_cardsById.Clear();
			_cardsByName.Clear();
			_cardsByFullName.Clear();
		}

		/// <summary>
		/// Average frequency of Wildcards collected when opening boosters, by rarity.
		/// </summary>
		private static Dictionary<CardRarity, double> WildcardFreq { get; set; } = new Dictionary<CardRarity, double>()
		{
			{ CardRarity.Common, 1.0 / 3 },
			{ CardRarity.Uncommon, (1.0 / 5) + (1.0 / 6) },
			{ CardRarity.Rare, (1.0 / 24) + (2.0 / 15) },
			{ CardRarity.MythicRare, (1.0 / 24) + (1.0 / 30) }
		};

		/// <summary>
		/// Average frequency of Cards collected when opening boosters, by rarity.
		/// </summary>
		private static Dictionary<CardRarity, double> RarityBoosterFreq { get; set; } = new Dictionary<CardRarity, double>()
		{
			{ CardRarity.Common, 5 - (1.0 / 3) },
			{ CardRarity.Uncommon, 2 - (1.0 / 5) },
			{ CardRarity.Rare, (7.0 / 8) - (1.0 / 12) },
			{ CardRarity.MythicRare, (1.0 / 8) - (1.0 / 12) }
		};

		/// <summary>
		/// PropertyChanged Handler (only used for ImageUri, currently).
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Private field to store this card's booster freqeuency the first time it is computed.
		/// </summary>
		private double _boosterFreq = -1;

		/// <summary>
		/// Gets the average frequency that this card will be opened in a booster.
		/// </summary>
		/// <returns>The average number of times this card will show up when opening a booster of the set that contains it.</returns>
		private double BoosterFreq()
		{
			if (_boosterFreq < 0)
			{
				if (Set.TotalCards == 0)
				{
					_boosterFreq = 0;
				}
				else
				{
					_boosterFreq = RarityBoosterFreq[Rarity] * Set.RarityCounts[Rarity] / Set.TotalCards;
				}
			}
			return _boosterFreq;
		}
	}
}
