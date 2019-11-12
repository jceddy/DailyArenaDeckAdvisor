using DailyArena.DeckAdvisor.Common;
using DailyArena.DeckAdvisor.Common.Extensions;
using Serilog;
using S = System;

namespace DailyArena.DeckAdvisor.Console
{
	/// <summary>
	/// The main program for the platform-independent console version of the application.
	/// </summary>
	public class Program : IDeckAdvisorApp
	{
		/// <summary>
		/// The application's Cached State.
		/// </summary>
		public CachedState State { get; set; } = new CachedState();

		/// <summary>
		/// The application's Logger.
		/// </summary>
		public ILogger Logger { get; set; }

		/// <summary>
		/// Logger for first chance exceptions.
		/// </summary>
		public ILogger FirstChanceLogger { get; set; }

		/// <summary>
		/// The Program constructor.
		/// </summary>
		public Program()
		{
			this.Initialize("-console", false);
		}

		/// <summary>
		/// The current executing program.
		/// </summary>
		public static Program CurrentProgram { get; private set; }

		/// <summary>
		/// The execution entry point.
		/// </summary>
		/// <param name="args">Arguments passed on the command line.</param>
		static void Main(string[] args)
		{
			CurrentProgram = new Program();
			S.Console.WriteLine("Press any key to exit.");
			S.Console.ReadKey();
		}
	}
}
