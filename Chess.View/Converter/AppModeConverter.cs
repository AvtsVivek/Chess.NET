//-----------------------------------------------------------------------
// <copyright file="AppModeConverter.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.View.Converter
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class AppModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string enumValue = value.ToString();
            string parameterValue = parameter.ToString();

            return enumValue.Equals(parameterValue, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                if (parameter != null)
                {
                    return Enum.Parse(targetType, parameter.ToString());
                }
            }
            return Binding.DoNothing; // Or a default value if appropriate
        }
    }
}