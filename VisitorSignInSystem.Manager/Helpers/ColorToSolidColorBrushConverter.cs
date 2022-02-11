using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace VisitorSignInSystem.Manager.Helpers
{
    public class ColorToSolidColorBrushConverter : IValueConverter
    {
        // GBologna
        // Convert string color names to SolidColorBrush
        // Color is set in class property.

        // Other methods to use

        // Be sure to include the using at the top of the file:
        //using Microsoft.Toolkit.Uwp.Helpers;

        // Given an HTML color, lets convert it to a Windows Color
        //Windows.UI.Color color = ColorHelper.ToColor("#3a4ab0");

        // Also works with an Alpha code
        //Windows.UI.Color myColor = ColorHelper.ToColor("#ff3a4ab0");

        // Given a color name, lets convert it to a Windows Color
        //Windows.UI.Color redColor = "Red".ToColor();

    public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (null == value)
            {
                return null;
            }
            if (!(value.ToString().Length > 0))
            {
                return null;
            }

            // For a more sophisticated converter, check also the targetType and react accordingly.
            if (value is Color)
            {
                Color color = (Color)value;
                return new SolidColorBrush(color);
            }
            // You can support here more source types if you wish

            Type type = value.GetType();

            if (type.FullName is System.String)
            {
                Windows.UI.Color c = value.ToString().ToColor();
                return new SolidColorBrush(c);
            }

            throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // If necessary, here you can convert back. Check which brush it is (if its one),
            throw new NotImplementedException();
        }

    }
}
