using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;
using System;

namespace MPDCtrl.Helpers
{
    public sealed partial class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isBold && isBold)
            {
                return FontWeights.SemiBold;
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed partial class TimeIntToStringValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double sec)
            {
                int min, hour, s;

                min = (int)(sec / 60);
                s = (int)(sec % 60);
                hour = min / 60;
                min %= 60;

                return $"{hour}:{min:00}:{s:00}";
            }
            return value?.ToString() ?? "0:00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // Not needed for slider tooltips
        }
    }
}
