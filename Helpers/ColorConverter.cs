using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace PSTARV2MonitoringApp.Helpers
{
    public class ColorConverter
    {
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Running":
                    case "Connected":
                    case "Normal":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    case "Stopped":
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                    case "Standby":
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    case "Warning":
                    case "Abnormal":
                    case "LowPressure":
                    case "Disconnected":
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                }
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CommStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Connected":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    case "Disconnected":
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                }
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ModeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string mode)
            {
                switch (mode)
                {
                    case "Manual":
                        return new SolidColorBrush(Color.FromRgb(63, 81, 181)); // Indigo
                    case "StandBy":
                    case "Auto":
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                }
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StandbyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Standby":
                    case "StandbyStart":
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    case "Ready":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                }
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
