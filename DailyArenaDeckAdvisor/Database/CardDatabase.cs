using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace DailyArenaDeckAdvisor.Database
{
	/// <summary>
	/// Class that imports and parses the card database, and keeps track of server timestamps for cached card/set/deck data.
	/// </summary>
	public static class CardDatabase
	{
		/// <summary>
		/// Dictionary to keep track of server-side cache timestamps.
		/// </summary>
		private static Dictionary<string, string> _serverTimestamps = new Dictionary<string, string>();

		/// <summary>
		/// Get a server-side cache timestamp by key.
		/// </summary>
		/// <param name="key">The key of the server-side cache timestamp to fetch.</param>
		/// <returns>The cache timestamp as an ISO 8601 string.</returns>
		public static string GetServerTimestamp(string key)
		{
			return _serverTimestamps.ContainsKey(key) ? _serverTimestamps[key] : null;
		}

		/// <summary>
		/// Timestamp of the last client-side card database update.
		/// </summary>
		public static string LastCardDatabaseUpdate { get; private set; }

		/// <summary>
		/// Timestamps of the last client-side standard sets update.
		/// </summary>
		public static string LastStandardSetsUpdate { get; private set; }

		/// <summary>
		/// Initialize the card database. Resets all timestamps, and reloads all card/set data from the server.
		/// </summary>
		public static void Initialize()
		{
			LastCardDatabaseUpdate = "1970-01-01T00:00:00Z";
			LastStandardSetsUpdate = "1970-01-01T00:00:00Z";
			Card.ClearCards();
			Set.ClearSets();
			LoadCardDatabase();
			UpdateCardDatabase();
		}

		/// <summary>
		/// Load the card database from the client-side cache.
		/// </summary>
		private static void LoadCardDatabase()
		{
			if (File.Exists("database.json"))
			{
				string json = File.ReadAllText("database.json");
				dynamic data = JsonConvert.DeserializeObject(json, new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None });

				LastCardDatabaseUpdate = data.LastCardDatabaseUpdate;
				LastStandardSetsUpdate = data.LastStandardSetsUpdate;
				foreach (dynamic set in data.Sets)
				{
					Set.CreateSet((string)set.Name, (string)set.Code, (string)set.ArenaCode, set.NotInBooster.ToObject<List<string>>(), (int)set.TotalCards,
						set.RarityCounts.ToObject<Dictionary<CardRarity, int>>());
				}
				foreach (dynamic card in data.Cards)
				{
					Card.CreateCard((int)card.ArenaId, (string)card.Name, (string)card.Set, (string)card.CollectorNumber, (string)card.Rarity,
						(string)card.Colors, (int)card.Rank, (string)card.Type, (string)card.Cost, (int)card.Cmc, (string)card.ScryfallId);
				}
			}
		}

		/// <summary>
		/// Save the card database to the client-side cache.
		/// </summary>
		private static void SaveCardDatabase()
		{
			var data = new
			{
				LastCardDatabaseUpdate,
				LastStandardSetsUpdate,
				Sets = Set.AllSets.Select(x => new
				{
					x.Name,
					x.Code,
					x.ArenaCode,
					x.NotInBooster,
					x.TotalCards,
					x.RarityCounts
				}),
				Cards = Card.AllCards.Select(x => new
				{
					x.ArenaId,
					x.CollectorNumber,
					Colors = x.Colors.ColorString,
					x.Name,
					Rarity = x.Rarity.Name,
					Set = x.Set.Name,
					x.Rank,
					x.Type,
					x.Cost,
					x.Cmc,
					x.ScryfallId
				})
			};

			string json = JsonConvert.SerializeObject(data);
			File.WriteAllText("database.json", json);
		}

		/// <summary>
		/// Check if the client-side card database cache is out of date. If so, re-load it from the server.
		/// </summary>
		private static void UpdateCardDatabase()
		{
			var ver = Guid.NewGuid();
			var timestampJsonUrl = $"https://clans.dailyarena.net/update_timestamps.json?_c={ver}";
			var cardDatabaseUrl = $"https://clans.dailyarena.net/card_database.json?_c={ver}";
			var standardSetsUrl = $"https://clans.dailyarena.net/standard_sets_info.json?_c={ver}";

			var downloadData = false;

			var timestampJsonRequest = (HttpWebRequest)WebRequest.Create(timestampJsonUrl);
			timestampJsonRequest.Method = "GET";

			using (var timestampJsonResponse = timestampJsonRequest.GetResponse())
			{
				using (Stream timestampJsonResponseStream = timestampJsonResponse.GetResponseStream())
				using (StreamReader timestampJsonResponseReader = new StreamReader(timestampJsonResponseStream))
				{
					string  result = timestampJsonResponseReader.ReadToEnd();
					using (StringReader resultStringReader = new StringReader(result))
					using (JsonTextReader resultJsonReader = new JsonTextReader(resultStringReader) { DateParseHandling = DateParseHandling.None })
					{
						dynamic json = JToken.ReadFrom(resultJsonReader);
						foreach (var timestamp in json)
						{
							_serverTimestamps[(string)timestamp.Name] = (string)timestamp.Value;
						}

						if ((string.Compare(_serverTimestamps["CardDatabase"], LastCardDatabaseUpdate) > 0) ||
							(string.Compare(_serverTimestamps["StandardSets"], LastStandardSetsUpdate) > 0))
						{
							downloadData = true;
						}
					}
				}
			}

			if (downloadData)
			{
				// Key => Name
				// Value =>
				//   Item1 => NotInBooster
				//   Item2 => RarityCounts
				//   Item3 => TotalCards
				Dictionary<string, Tuple<List<string>, Dictionary<CardRarity, int>, int>> standardSetsInfo =
					new Dictionary<string, Tuple<List<string>, Dictionary<CardRarity, int>, int>>();

				LastCardDatabaseUpdate = _serverTimestamps["CardDatabase"];
				LastStandardSetsUpdate = _serverTimestamps["StandardSets"];

				var standardSetsRequest = WebRequest.Create(standardSetsUrl);
				standardSetsRequest.Method = "GET";
				using (var standardSetsResponse = standardSetsRequest.GetResponse())
				{
					using (Stream standardSetsResponseStream = standardSetsResponse.GetResponseStream())
					using (StreamReader standardSetsResponseReader = new StreamReader(standardSetsResponseStream))
					{
						var result = standardSetsResponseReader.ReadToEnd();
						dynamic data = JToken.Parse(result);

						foreach(dynamic set in data)
						{
							standardSetsInfo[(string)set.Value["name"]] = new Tuple<List<string>, Dictionary<CardRarity, int>, int>(
								set.Value["not_in_booster"].ToObject<List<string>>(),
								set.Value["rarity_counts"].ToObject<Dictionary<CardRarity, int>>(),
								(int)set.Value["total_cards"]
							);
						}
					}
				}

				var cardDatabaseRequest = WebRequest.Create(cardDatabaseUrl);
				cardDatabaseRequest.Method = "GET";

				using (var cardDatabaseResponse = cardDatabaseRequest.GetResponse())
				{
					using (Stream cardDatabaseResponseStream = cardDatabaseResponse.GetResponseStream())
					using (StreamReader cardDatabaseResponseReader = new StreamReader(cardDatabaseResponseStream))
					{
						var result = cardDatabaseResponseReader.ReadToEnd();
						dynamic data = JToken.Parse(result);

						Set.ClearSets();
						foreach (dynamic set in data["sets"])
						{
							if (standardSetsInfo.ContainsKey(set.Name))
							{
								Tuple<List<string>, Dictionary<CardRarity, int>, int> setInfo = standardSetsInfo[set.Name];
								Set.CreateSet(set.Name, (string)set.Value["scryfall"], (string)set.Value["arenacode"], setInfo.Item1, setInfo.Item3, setInfo.Item2);
							}
							else
							{
								Set.CreateSet(set.Name, (string)set.Value["scryfall"], (string)set.Value["arenacode"], new List<string>(), 0,
									new Dictionary<CardRarity, int>() {
										{ CardRarity.Common, 0 },
										{ CardRarity.Uncommon, 0 },
										{ CardRarity.Rare, 0 },
										{ CardRarity.MythicRare, 0 }
									});
							}
						}
						Card.ClearCards();
						foreach (dynamic card in data.cards)
						{
							if ((bool)card.Value["collectible"] || (bool)card.Value["craftable"])
							{
								string scryfallId = string.Empty;
								if (card.Value["images"] != null)
								{
									scryfallId = (string)card.Value["images"]["normal"];
									scryfallId = scryfallId.Substring(scryfallId.LastIndexOf('/') + 1).Split('.')[0];
								}
								Card.CreateCard((int)card.Value["id"], (string)card.Value["name"], (string)card.Value["set"], (string)card.Value["cid"],
									(string)card.Value["rarity"], string.Join("", card.Value["cost"].ToObject<string[]>()).ToUpper(),
									(int)card.Value["rank"], (string)card.Value["type"], string.Join("", card.Value["cost"].ToObject<string[]>()).ToUpper(),
									(int)card.Value["cmc"], scryfallId);
							}
						}
					}
				}

				SaveCardDatabase();
			}
		}
	}
}
