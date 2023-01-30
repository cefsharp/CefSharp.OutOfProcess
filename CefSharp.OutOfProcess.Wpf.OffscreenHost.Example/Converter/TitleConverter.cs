using System;
using System.Globalization;
using System.Windows.Data;

namespace CefSharp.OutOfProcess.Wpf.OffscreenHost.Example.Converter
{
    public class TitleConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "CefSharp.OutOfProcess.Wpf.OffscreenHost.Example - " + (value ?? "No Title Specified");
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
