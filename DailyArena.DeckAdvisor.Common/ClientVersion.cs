using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DailyArena.DeckAdvisor.Common
{
	/// <summary>
	/// Class that holds client version information for MTGA.
	/// </summary>
	public class ClientVersion
	{
		/// <summary>
		/// The parts of the client version (separated by dots in the version string).
		/// </summary>
		private int[] _versionParts;

		/// <summary>
		/// The underlying version string.
		/// </summary>
		public string VersionString { get; private set; }

		/// <summary>
		/// ClientVersion constructor that takes a string argument.
		/// </summary>
		/// <param name="versionString">The underlying version string.</param>
		public ClientVersion(string versionString)
		{
			if(string.IsNullOrWhiteSpace(versionString))
			{
				versionString = "0";
			}

			_versionParts = versionString.Split('.').Select(x => Math.Abs(int.Parse(x))).ToArray();
			VersionString = string.Join(".", _versionParts);
		}

		/// <summary>
		/// ClientVersion constructor that takes an array argument.
		/// </summary>
		/// <param name="versionParts">The parts of the client version (separated by dots in the version string).</param>
		public ClientVersion(params int[] versionParts)
		{
			if(versionParts.Length == 0)
			{
				versionParts = new int[] { 0 };
			}

			_versionParts = versionParts.Select(x => Math.Abs(x)).ToArray();
			VersionString = string.Join(".", _versionParts);
		}

		/// <summary>
		/// ClientVersion default constructor.
		/// </summary>
		public ClientVersion()
		{
			VersionString = "0";
			_versionParts = new int[] { 0 };
		}

		/// <summary>
		/// ClientVersion greater than operator.
		/// </summary>
		/// <param name="a">The first ClientVersion to compare.</param>
		/// <param name="b">The second ClientVersion to compare.</param>
		/// <returns>True if a > b, false otherwise.</returns>
		public static bool operator >(ClientVersion a, ClientVersion b)
		{
			for(int i = 0; i < Math.Min(a._versionParts.Length, b._versionParts.Length); i++)
			{
				if (a._versionParts[i] > b._versionParts[i])
					return true;
				else if (b._versionParts[i] > a._versionParts[i])
					return false;
			}

			return a._versionParts.Length > b._versionParts.Length;
		}

		/// <summary>
		/// ClientVersion less than operator.
		/// </summary>
		/// <param name="a">The first ClientVersion to compare.</param>
		/// <param name="b">The second ClientVersion to compare.</param>
		/// <returns>True if a &lt; b, false otherwise.</returns>
		public static bool operator <(ClientVersion a, ClientVersion b)
		{
			for (int i = 0; i < Math.Min(a._versionParts.Length, b._versionParts.Length); i++)
			{
				if (a._versionParts[i] < b._versionParts[i])
					return true;
				else if (b._versionParts[i] < a._versionParts[i])
					return false;
			}

			return a._versionParts.Length < b._versionParts.Length;
		}
	}
}
