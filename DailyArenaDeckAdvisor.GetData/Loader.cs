using UnityEngine;

namespace DailyArena.DeckAdvisor.GetData
{
	public class Loader
	{
		static GameObject gameObject;
		public static void Load()
		{
			if(GameObject.Find("DADADataGetter") == null)
			{
				gameObject = new GameObject("DADADataGetter");
				gameObject.AddComponent<DADAGetData>();
				Object.DontDestroyOnLoad(gameObject);
			}
			else
			{
				Debug.Log($"[DailyArena.DeckAdvisor.GetData] Logger is already in place, no need to embed it again!");
			}
		}

		public static void Unload()
		{
			Object.Destroy(gameObject);
		}
	}
}
