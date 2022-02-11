using System;

using Windows.UI.Xaml.Data;

namespace VisitorSignInSystem.Manager.Helpers
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public Type EnumType { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string enumString)
            {
                if (!Enum.IsDefined(EnumType, value))
                {
                    throw new ArgumentException("value must be an Enum!");
                }

                var enumValue = Enum.Parse(EnumType, enumString);

                return enumValue.Equals(value);
            }

            throw new ArgumentException("parameter must be an Enum name!");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string enumString)
            {
                return Enum.Parse(EnumType, enumString);
            }

            throw new ArgumentException("parameter must be an Enum name!");
        }

    }

    public class StringFormatSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                double number;

                if (Double.TryParse(((double)value).ToString(), out number))
                {
                    TimeSpan ts = TimeSpan.FromSeconds(number);
                    return string.Format("{0:g}:{1:D2}:{2:D2}",
                         ts.Hours,
                         ts.Minutes,
                         ts.Seconds);
                }
            }
            catch (Exception) {}
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
