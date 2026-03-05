namespace Devolutions.AvaloniaControls;

internal static class EnumerableExtensions
{
  internal static IEnumerable<T> Add<T>(this IEnumerable<T> enumerable, T addedValue)
  {
    foreach (var value in enumerable) yield return value;

    yield return addedValue;
  }

  internal static IEnumerable<T> SkipNulls<T>(this IEnumerable<T?> enumerable)
  {
      foreach (var value in enumerable)
      {
          if (value != null)
          {
              yield return value;
          }
      }
  }
}