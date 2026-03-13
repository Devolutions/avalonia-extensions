namespace Devolutions.AvaloniaControls.Controls;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

public class EnumPickerTextOverride<T> where T : struct, Enum
{
    public required T Key { get; init; }

    public required string Value { get; init; }
}

public class EnumPickerTextOverrides<T> : List<EnumPickerTextOverride<T>>, IReadOnlyDictionary<T, string>
    where T : struct, Enum
{
    private List<EnumPickerTextOverride<T>> Items => this;

    public string this[T key] => this.Items.First(x => x.Key.Equals(key)).Value;

    IEnumerable<T> IReadOnlyDictionary<T, string>.Keys => this.Items.Select(x => x.Key);

    IEnumerable<string> IReadOnlyDictionary<T, string>.Values => this.Items.Select(x => x.Value);

    public bool ContainsKey(T key) => this.Items.Any(x => x.Key.Equals(key));

    public bool TryGetValue(T key, [MaybeNullWhen(false)] out string value)
    {
        foreach (var item in this)
        {
            if (item.Key.Equals(key))
            {
                value = item.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    IEnumerator<KeyValuePair<T, string>> IEnumerable<KeyValuePair<T, string>>.GetEnumerator()
        => ((List<EnumPickerTextOverride<T>>)this).Select(x => new KeyValuePair<T, string>(x.Key, x.Value)).GetEnumerator();
}
