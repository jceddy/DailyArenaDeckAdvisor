using DailyArena.DeckAdvisor.Common;
using DailyArena.DeckAdvisor.Common.Extensions;
using Serilog;
using System.Windows;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application, IDeckAdvisorApp
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
		/// The application constructor.
		/// </summary>
		public App()
		{
			this.Initialize();

			DispatcherUnhandledException += (sender, e) =>
			{
				Logger.Error(e.Exception, "DispatcherUnhandledException");
			};
		}
	}
}
