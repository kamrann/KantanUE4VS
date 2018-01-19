// Copyright 2018 Cameron Angus. All Rights Reserved.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KUE4VS_UI
{
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
