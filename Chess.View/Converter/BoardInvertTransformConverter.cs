//-----------------------------------------------------------------------
// <copyright file="BoardInvertTransformConverter.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.View.Converter
{
    using Chess.ViewModel.Game;
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    public class BoardInvertTransformConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverted = (bool)value;
            // CenterX and CenterY should be half the board's size
            double center = BoardConstants.FullCanvasLength / 2;
            return isInverted
                ? new ScaleTransform(-1, -1, center, center)
                : new ScaleTransform(1, 1, center, center);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}