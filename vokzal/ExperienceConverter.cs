using System;
using System.Globalization;
using System.Windows.Data;

namespace vokzal
{
    public class ExperienceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int experience)
            {
                int lastDigit = experience % 10;
                int lastTwoDigits = experience % 100;

                if (lastTwoDigits >= 11 && lastTwoDigits <= 14)
                {
                    return $"{experience} лет";
                }
                else if (lastDigit == 1)
                {
                    return $"{experience} год";
                }
                else if (lastDigit >= 2 && lastDigit <= 4)
                {
                    return $"{experience} года";
                }
                else
                {
                    return $"{experience} лет";
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}