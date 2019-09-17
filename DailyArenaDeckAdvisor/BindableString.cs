using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace DailyArena.DeckAdvisor
{
	/// <summary>
	/// A string value that Xaml fields can easily bind to.
	/// </summary>
	public class BindableString : INotifyPropertyChanged
	{
		/// <summary>
		/// The internal string value.
		/// </summary>
		private string _value;

		/// <summary>
		/// Gets or sets the internal string value.
		/// </summary>
		public string Value
		{
			get
			{
				return _value;
			}
			set
			{
				if (_value != value)
				{
					_value = value;
					RaisePropertyChanged(() => Value);
				}
			}
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
		public void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
		{
			if (propertyExpression == null)
			{
				return;
			}

			var handler = PropertyChanged;

			if (handler != null)
			{
				if (propertyExpression.Body is MemberExpression body)
					handler(this, new PropertyChangedEventArgs(body.Member.Name));
			}
		}

		#endregion
	}
}
