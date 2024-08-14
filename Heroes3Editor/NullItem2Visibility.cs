using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Heroes3Editor
{
    public class NullItem2Visibility : IValueConverter{
        public object Convert(object value, Type type, object parameter, CultureInfo culture){
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
