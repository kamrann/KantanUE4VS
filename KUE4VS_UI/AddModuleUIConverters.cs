
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KUE4VS_UI
{
    /*
    public class PublicInterfaceCheckStateConverter : IValueConverter
    {
        public PublicInterfaceCheckStateConverter()
        {
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool wants_to_be_checked = (bool)value; // Comes from own value the model
            bool can_be_checked = (bool)parameter;  // Comes from CustomImplementation value in the model
            return can_be_checked && wants_to_be_checked;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
    */

    public class PublicInterfaceVisibilityConverter : IValueConverter
    {
        public PublicInterfaceVisibilityConverter()
        {
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool enabled = (bool)value;
            return enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
