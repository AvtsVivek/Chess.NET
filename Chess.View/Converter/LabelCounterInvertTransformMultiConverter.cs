//-----------------------------------------------------------------------
// <copyright file="LabelCounterInvertTransformConverter.cs">
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

    public class LabelCounterInvertTransformMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverted = (bool)values[0];
            int row = (int)values[1];
            int column = (int)values[2];
            string label = (string)values[3];

            // Use isInverted, row, and column as needed
            double center = 0;
            var scaleTransform = isInverted
                ? new ScaleTransform(-1, -1, center, center)
                : new ScaleTransform(1, 1, center, center);

            // The following adjustment is for H at top left corner after inversion
            // Before the inversion, the label H is at (0,7). So its Bottom Char 8
            if (row == 0 && column == 7 && isInverted && label == "Bottom Char 8")
            {
                scaleTransform.CenterX = -.125;
                scaleTransform.CenterY = 0;
            }

            // The following adjustment is for A at top right after inversion
            // Before the inversion, the label A is at (0 ,0). So its Top Char 1
            if (row == 0 && column == 0 && isInverted && label == "Bottom Char 1")
            {
                scaleTransform.CenterX = 0.125;
                scaleTransform.CenterY = 0;
            }

            // The following adjustment is for 8 at bottom left after inversion
            // Before the inversion, the label 8 is at (7,7). So its Right Digit 8
            if (row == 7 && column == 7 && isInverted && label == "Right Digit 8")
            {
                scaleTransform.CenterX = 0;
                scaleTransform.CenterY = 0.125;
            }

            // The following adjustment is for 8 at bottom right after inversion
            // Before the inversion, the label 8 is at (7,0). So its Left Digit 8
            if (row == 7 && column == 0 && isInverted && label == "Left Digit 8")
            {
                scaleTransform.CenterX = 0;
                scaleTransform.CenterY = 0.125;
            }

            return scaleTransform;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}