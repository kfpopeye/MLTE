using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WindowClasses
{
    public class ValueDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is MLTE.ParameterItems)
            {
                MLTE.ParameterItems pitem = item as MLTE.ParameterItems;

                if (pitem.ItsValueType == "text")
                    return element.FindResource("TextValueTemplate") as DataTemplate;
                else
                {
                    DataTemplate dt = element.FindResource("KKValueTemplate") as DataTemplate;
                    return dt;
                }
            }

            return null;
        }
    }
}
