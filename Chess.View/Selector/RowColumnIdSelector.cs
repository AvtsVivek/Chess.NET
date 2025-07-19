using Chess.ViewModel.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Chess.View.Selector
{
    public class RowColumnIdSelector : DataTemplateSelector
    {
        public RowColumnIdSelector()
        {
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is RowColumnLabelVM id)
            {
                return Application.Current.FindResource(id.ResourceKey) as DataTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
