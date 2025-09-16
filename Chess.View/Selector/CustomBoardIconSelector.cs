//-----------------------------------------------------------------------
// <copyright file="CustomBoardIconSelector.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.View.Selector
{
    using Chess.ViewModel.Piece;
    using System.Windows;
    using System.Windows.Controls;

    public class CustomBoardIconSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is CustomBoardPlacedIconVM iconVM)
            {
                var iconKey = iconVM.Icon.CustomBoardIconKey();
                return Application.Current.FindResource(iconKey) as DataTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}