using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// Class for tracking user's select deck filters.
	/// </summary>
	public class DeckFilters : INotifyPropertyChanged, IEquatable<DeckFilters>
	{
		/// <summary>
		/// Whether to hide decks from the user's collection.
		/// </summary>
		private bool _hideFromCollection;

		/// <summary>
		/// Gets or sets a value determining whether to hide decks from the user's collection.
		/// </summary>
		public bool HideFromCollection
		{
			get
			{
				return _hideFromCollection;
			}
			set
			{
				if(_hideFromCollection != value)
				{
					_hideFromCollection = value;
					RaisePropertyChanged("HideFromCollection");
				}
			}
		}

		/// <summary>
		/// Whether to hide decks that the user doesn't have enough wildcards to build.
		/// </summary>
		private bool _hideMissingWildcards;

		/// <summary>
		/// Gets or sets a value determining whether to hide decks that the user doesn't have enough wildcards to build.
		/// </summary>
		public bool HideMissingWildcards
		{
			get
			{
				return _hideMissingWildcards;
			}
			set
			{
				if (_hideMissingWildcards != value)
				{
					_hideMissingWildcards = value;
					RaisePropertyChanged("HideMissingWildcards");
				}
			}
		}

		/// <summary>
		/// Whether to hide decks that the user doesn't have enough cards to build.
		/// </summary>
		private bool _hideMissingCards;

		/// <summary>
		/// Gets or sets a value determining whether to hide decks that the user doesn't have enough cards to build.
		/// </summary>
		public bool HideMissingCards
		{
			get
			{
				return _hideMissingCards;
			}
			set
			{
				if (_hideMissingCards != value)
				{
					_hideMissingCards = value;
					RaisePropertyChanged("HideMissingCards");
				}
			}
		}

		/// <summary>
		/// Whether to hide decks with cards missing that the user doesn't have good replacements for.
		/// </summary>
		private bool _hideIncompleteReplacements;

		/// <summary>
		/// Gets or sets a value determining whether to hide decks with cards missing that the user doesn't have good replacements for.
		/// </summary>
		public bool HideIncompleteReplacements
		{
			get
			{
				return _hideIncompleteReplacements;
			}
			set
			{
				if (_hideIncompleteReplacements != value)
				{
					_hideIncompleteReplacements = value;
					RaisePropertyChanged("HideIncompleteReplacements");
				}
			}
		}

		/// <summary>
		/// Whether to hide decks with a number of mythic cards over a certain threshold.
		/// </summary>
		private bool _hideMythic;

		/// <summary>
		/// Gets or sets a value determining whether to hide decks with a number of mythic cards over a certain threshold.
		/// </summary>
		public bool HideMythic
		{
			get
			{
				return _hideMythic;
			}
			set
			{
				if (_hideMythic != value)
				{
					_hideMythic = value;
					RaisePropertyChanged("HideMythic");
				}
			}
		}

		/// <summary>
		/// The threshold of mythic cards allowed before a deck is hidden.
		/// </summary>
		private int _mythicCount;

		/// <summary>
		/// Gets or sets the threshold of mythic cards allowed before a deck is hidden.
		/// </summary>
		public int MythicCount
		{
			get
			{
				return _mythicCount;
			}
			set
			{
				if (_mythicCount != value)
				{
					_mythicCount = value;
					RaisePropertyChanged("MythicCount");
				}
			}
		}

		/// <summary>
		/// Whether to hide decks with a number of rare cards over a certain threshold.
		/// </summary>
		private bool _hideRare;

		/// <summary>
		/// Gets or sets a value determining whether to hide decks with a number of rare cards over a certain threshold.
		/// </summary>
		public bool HideRare
		{
			get
			{
				return _hideRare;
			}
			set
			{
				if (_hideRare != value)
				{
					_hideRare = value;
					RaisePropertyChanged("HideRare");
				}
			}
		}

		/// <summary>
		/// The threshold of rare cards allowed before a deck is hidden.
		/// </summary>
		private int _rareCount;

		/// <summary>
		/// Gets or sets the threshold of rare cards allowed before a deck is hidden.
		/// </summary>
		public int RareCount
		{
			get
			{
				return _rareCount;
			}
			set
			{
				if (_rareCount != value)
				{
					_rareCount = value;
					RaisePropertyChanged("RareCount");
				}
			}
		}

		/// <summary>
		/// Whether to hide decks with a number of uncommon cards over a certain threshold.
		/// </summary>
		private bool _hideUncommon;

		/// <summary>
		/// Gets or sets a value determining whether to hide decks with a number of uncommon cards over a certain threshold.
		/// </summary>
		public bool HideUncommon
		{
			get
			{
				return _hideUncommon;
			}
			set
			{
				if (_hideUncommon != value)
				{
					_hideUncommon = value;
					RaisePropertyChanged("HideUncommon");
				}
			}
		}

		/// <summary>
		/// The threshold of uncommon cards allowed before a deck is hidden.
		/// </summary>
		private int _uncommonCount;

		/// <summary>
		/// Gets or sets the threshold of uncommon cards allowed before a deck is hidden.
		/// </summary>
		public int UncommonCount
		{
			get
			{
				return _uncommonCount;
			}
			set
			{
				if (_uncommonCount != value)
				{
					_uncommonCount = value;
					RaisePropertyChanged("UncommonCount");
				}
			}
		}

		/// <summary>
		/// Whether to hide decks with a number of common cards over a certain threshold.
		/// </summary>
		private bool _hideCommon;

		/// <summary>
		/// Gets or sets a value determining whether to hide decks with a number of common cards over a certain threshold.
		/// </summary>
		public bool HideCommon
		{
			get
			{
				return _hideCommon;
			}
			set
			{
				if (_hideCommon != value)
				{
					_hideCommon = value;
					RaisePropertyChanged("HideCommon");
				}
			}
		}

		/// <summary>
		/// The threshold of common cards allowed before a deck is hidden.
		/// </summary>
		private int _commonCount;

		/// <summary>
		/// Gets or sets the threshold of common cards allowed before a deck is hidden.
		/// </summary>
		public int CommonCount
		{
			get
			{
				return _commonCount;
			}
			set
			{
				if (_commonCount != value)
				{
					_commonCount = value;
					RaisePropertyChanged("CommonCount");
				}
			}
		}

		/// <summary>
		/// Create a copy of this DeckFilters object.
		/// </summary>
		/// <remarks>Allows the main window to pass a copy of the current filters to the Filters dialog without an object reference.</remarks>
		/// <returns>A new DeckFilters object that is a copy of this one.</returns>
		public DeckFilters Clone()
		{
			return new DeckFilters()
			{
				HideFromCollection = HideFromCollection,
				HideMissingWildcards = HideMissingWildcards,
				HideMissingCards = HideMissingCards,
				HideIncompleteReplacements = HideIncompleteReplacements,
				HideMythic = HideMythic,
				MythicCount = MythicCount,
				HideRare = HideRare,
				RareCount = RareCount,
				HideUncommon = HideUncommon,
				UncommonCount = UncommonCount,
				HideCommon = HideCommon,
				CommonCount = CommonCount
			};
		}

		/// <summary>
		/// Set all fields to match the values of another DeckFilters object.
		/// </summary>
		/// <param name="other">The other DeckFilters object to copy values from.</param>
		public void SetAllFields(DeckFilters other)
		{
			HideFromCollection = other.HideFromCollection;
			HideMissingWildcards = other.HideMissingWildcards;
			HideMissingCards = other.HideMissingCards;
			HideIncompleteReplacements = other.HideIncompleteReplacements;
			HideMythic = other.HideMythic;
			MythicCount = other.MythicCount;
			HideRare = other.HideRare;
			RareCount = other.RareCount;
			HideUncommon = other.HideUncommon;
			UncommonCount = other.UncommonCount;
			HideCommon = other.HideCommon;
			CommonCount = other.CommonCount;
		}

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Event handler to trigger when the internal string value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raise the propery changed event.
		/// </summary>
		/// <param name="property">The name of the property that changed.</param>
		public void RaisePropertyChanged(string property)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}

		/// <summary>
		/// Raise the property changed event for a propery expression.
		/// </summary>
		/// <typeparam name="T">The type of the expression.</typeparam>
		/// <param name="propertyExpression">The expression to raise the changed event for.</param>
		public void RaisePropertyChanged<PType>(Expression<Func<PType>> propertyExpression)
		{
			if (propertyExpression == null)
			{
				return;
			}

			var handler = PropertyChanged;

			if (handler != null)
			{
				MemberExpression body = propertyExpression.Body as MemberExpression;
				if (body != null)
					handler(this, new PropertyChangedEventArgs(body.Member.Name));
			}
		}

		#endregion

		/// <summary>
		/// Check if this is equal to another DeckFilters object.
		/// </summary>
		/// <param name="other">The other DeckFilters object to compare this one to.</param>
		/// <returns>True if the object's fields are all the same, false otherwise.</returns>
		public bool Equals(DeckFilters other)
		{
			if(other is null)
			{
				return false;
			}

			return
				HideFromCollection == other.HideFromCollection &&
				HideMissingWildcards == other.HideMissingWildcards &&
				HideMissingCards == other.HideMissingCards &&
				HideIncompleteReplacements == other.HideIncompleteReplacements &&
				HideMythic == other.HideMythic &&
				MythicCount == other.MythicCount &&
				HideRare == other.HideRare &&
				RareCount == other.RareCount &&
				HideUncommon == other.HideUncommon &&
				UncommonCount == other.UncommonCount &&
				HideCommon == other.HideCommon &&
				CommonCount == other.CommonCount;
		}

		/// <summary>
		/// Check whther two DeckFilters object are equal.
		/// </summary>
		/// <param name="a">The first DeckFilters object.</param>
		/// <param name="b">The second DeckFilters object.</param>
		/// <returns>True if all of the fields of a and b have the same values; false otherwise.</returns>
		public static bool operator ==(DeckFilters a, DeckFilters b) => a.Equals(b);

		/// <summary>
		/// Check whther two DeckFilters object are not equal.
		/// </summary>
		/// <param name="a">The first DeckFilters object.</param>
		/// <param name="b">The second DeckFilters object.</param>
		/// <returns>True if any of the fields of a and b have different values; false otherwise.</returns>
		public static bool operator !=(DeckFilters a, DeckFilters b) => !a.Equals(b);
	}
}
