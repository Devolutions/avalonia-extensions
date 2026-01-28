using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Devolutions.AvaloniaControls.Converters;

public class IsNullOrEmptyConverter : IValueConverter
{
    public static readonly IsNullOrEmptyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        static bool IsEnumerableEmpty(IEnumerable enumerable)
        {
            IEnumerator enumerator = enumerable.GetEnumerator();
            try
            {
                return !enumerator.MoveNext();
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        bool isNullOrEmpty = value switch
        {
            null => true,
            string s => s.Length == 0,
            ICollection c => c.Count == 0,
            IEnumerable e => IsEnumerableEmpty(e),
            _ => false
        };

        return parameter is false ? !isNullOrEmpty : isNullOrEmpty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

