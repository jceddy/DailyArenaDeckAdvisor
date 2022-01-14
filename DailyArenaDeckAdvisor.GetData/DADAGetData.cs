using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Wizards.Mtga.Decks;
using Wizards.Mtga.FrontDoorModels;

namespace DailyArena.DeckAdvisor.GetData
{
	public class LogElement
	{
		public string timestamp;
		public object Payload;
	}

	public class DeckForLog
	{
		public string Name;
		public List<Client_DeckCard> Cards;
	}

	public class DADAGetData : MonoBehaviour
	{
		private static bool gotInventoryData = false;
		private static List<string> dataWrittenHashes = new List<string>();
		private static readonly UnityCrossThreadLogger DADALogger = new UnityCrossThreadLogger("Daily Arena Deck Advisor Logger");

		public void Start()
		{
			try
			{
				System.Random RNG = new System.Random();
				int length = 32;
				StringBuilder randomString = new StringBuilder(length);
				for (var i = 0; i < length; i++)
				{
					randomString.Append(((char)(RNG.Next(1, 26) + 64)).ToString().ToLower());
				}
				DADALogger.Debug($"Unique Log Identifier: {randomString}");
				Task task = new Task(() => Get());
				task.Start();
			}
			catch(Exception e)
			{
				WriteToLog("Error", e);
			}
		}

		public static string CreateMD5(string input)
		{
			// Use input string to calculate MD5 hash
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				byte[] inputBytes = Encoding.ASCII.GetBytes(input);
				byte[] hashBytes = md5.ComputeHash(inputBytes);

				// Convert the byte array to hexadecimal string
				StringBuilder sb = new StringBuilder();
				for(int i = 0; i < hashBytes.Length; i++)
				{
					sb.Append(hashBytes[i].ToString("X2"));
				}
				return sb.ToString();
			}
		}

		public void Get()
		{
			try
			{
				while (!gotInventoryData)
				{
					Thread.Sleep(10000);

					WriteToLog("DADA Debug", "Checking for Inventory Data");
					if (!gotInventoryData && WrapperController.Instance != null && WrapperController.Instance.InventoryManager != null && WrapperController.Instance.InventoryManager.Cards != null && WrapperController.Instance.InventoryManager.Cards.Count > 0)
					{
						GetInventoryData();
					}
				}
			}
			catch (Exception e)
			{
				WriteToLog("Error - Get()", e);
			}
		}

		private void WriteToLog(string indicator, object report)
		{
			try
			{
				LogElement logElem = new LogElement
				{
					Payload = report,
					timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString()
				};
				string hashMD5 = CreateMD5(JsonConvert.SerializeObject(report));
				if(!dataWrittenHashes.Contains(hashMD5))
				{
					DADALogger.Debug($" **{indicator}** {JsonConvert.SerializeObject(logElem)}");
					dataWrittenHashes.Add(hashMD5);
				}
			}
			catch(Exception e)
			{
				DADALogger.Debug($" **WriteToLogError** {e}");
			}
		}

		private void GetInventoryData()
		{
			try
			{
				gotInventoryData = true;
				WriteToLog("DADA Debug", "Resubscribing Inventory Change Handler...");
				WrapperController.Instance.InventoryManager.UnsubscribeFromAll(InventoryChangeHandler);
				WrapperController.Instance.InventoryManager.SubscribeToAll(InventoryChangeHandler);
				WriteToLog("DADA Debug", "Finished Resubscribing Inventory Change Handler");

				Task task = new Task(() => PeriodicCollectionPrinter());
				task.Start();
			}
			catch(Exception e)
			{
				WriteToLog("Error - GetInventoryData()", e);
			}
		}

		private Client_Deck GetFullDeck(Guid id)
		{
			var deckPromise = WrapperController.Instance.DecksManager.GetFullDeck(id);
			deckPromise.AsTask.Wait();
			return deckPromise.Result;
		}

		private void PeriodicCollectionPrinter()
		{
			try
			{
				while (true)
				{
					WriteToLog("Collection", WrapperController.Instance.InventoryManager.Cards);
					WriteToLog("InventoryContent", WrapperController.Instance.InventoryManager.Inventory);
					var deckPromise = WrapperController.Instance.DecksManager.GetAllDecks();
					deckPromise.AsTask.Wait();
					WriteToLog("Decks", deckPromise.Result.Select((x) => GetFullDeck(x.Id)));
					Thread.Sleep(600000);
				}
			}
			catch(Exception e)
			{
				WriteToLog("Error - PeriodicCollectionPrinter()", e);
			}
		}

		private void InventoryChangeHandler(ClientInventoryUpdateReportItem obj)
		{
			try
			{
				WriteToLog("InventoryUpdate", obj);
			}
			catch(Exception e)
			{
				WriteToLog("Error - InventoryChangeHandler()", e);
			}
		}
	}
}
