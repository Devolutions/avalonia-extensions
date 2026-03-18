namespace Devolutions.AvaloniaControls.Extensions;

public static class EnumerableExtensions
{
  public static IEnumerable<T> Add<T>(this IEnumerable<T> enumerable, T addedValue)
  {
    foreach (var value in enumerable) yield return value;

    yield return addedValue;
  }
  
  public static IEnumerable<T> SkipNulls<T>(this IEnumerable<T?> enumerable) where T: struct
  {
      foreach (var value in enumerable)
      {
          if (value != null)
          {
              yield return value.Value;
          }
      }
  }

  public static IEnumerable<T> SkipNulls<T>(this IEnumerable<T?> enumerable)
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