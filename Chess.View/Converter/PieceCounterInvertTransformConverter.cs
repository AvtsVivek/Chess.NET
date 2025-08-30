//-----------------------------------------------------------------------
// <copyright file="PieceCounterInvertTransformConverter.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.View.Converter
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    public class PieceCounterInvertTransformConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverted = (bool)value;
            // If board is inverted, flip label text back to normal
            return isInverted
                ? new ScaleTransform(-1, -1)
                : new ScaleTransform(1, 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}