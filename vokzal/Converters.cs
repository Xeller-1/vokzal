using System;
using System.Windows;
using System.Windows.Data;

namespace vokzal
{
    public class ZeroToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is int intValue && intValue == 0) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ZeroToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is int intValue && intValue == 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class AgeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int age)
            {
                int lastDigit = age % 10;
                int lastTwoDigits = age % 100;

                if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                {
                    return $"{age} лет";
                }
                else if (lastDigit == 1)
                {
                    return $"{age} год";
                }
                else if (lastDigit >= 2 && lastDigit <= 4)
                {
                    return $"{age} года";
                }
                else
                {
                    return $"{age} лет";
                }
            }
            return "0 лет";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}