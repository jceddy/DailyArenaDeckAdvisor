using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace DailyArenaDeckAdvisor
{
	public class BindableString : INotifyPropertyChanged
	{
		private string _value;

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

		public event PropertyChangedEventHandler PropertyChanged;

		public void RaisePropertyChanged(string property)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}

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
