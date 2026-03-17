namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public class EnumPickerTextOverride<T> where T : struct, Enum
{
    public required T Key { get; init; }

    public required string Value { get; init; }
}

public class EnumPickerTextOverrides<T> : IReadOnlyDictionary<T, string>, ICollection<EnumPickerTextOverride<T>>
    where T : struct, Enum
{
    private readonly Dictionary<T, EnumPickerTextOverride<T>> textOverrides = new();

    IEnumerator<EnumPickerTextOverride<T>> IEnumerable<EnumPickerTextOverride<T>>.GetEnumerator() => this.textOverrides.Values.GetEnumerator();

    public void Add(EnumPickerTextOverride<T> item)
    {
        this.textOverrides.TryAdd(item.Key, item);
    }

    public void Clear()
    {
        this.textOverrides.Clear();
    }

    public bool Contains(EnumPickerTextOverride<T> item) => this.textOverrides.ContainsKey(item.Key);

    public void CopyTo(EnumPickerTextOverride<T>[] array, int arrayIndex)
    {
        // Not required for the limited scope of this companion tool, we only really need Add()
    }

    public bool Remove(EnumPickerTextOverride<T> item) => this.textOverrides.Remove(item.Key);

    int ICollection<EnumPickerTextOverride<T>>.Count => this.textOverrides.Count;

    public bool IsReadOnly => false;

    public string this[T key] => this.textOverrides[key].Value;

    public IEnumerable<T> Keys => this.textOverrides.Keys;

    public IEnumerable<string> Values => this.textOverrides.Values.Select(textOverride => textOverride.Value);

    IEnumerator<KeyValuePair<T, string>> IEnumerable<KeyValuePair<T, string>>.GetEnumerator() =>
        this.textOverrides.Select(pair => new KeyValuePair<T, string>(pair.Key, pair.Value.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<T, string>>)this).GetEnumerator();

    int IReadOnlyCollection<KeyValuePair<T, string>>.Count => this.textOverrides.Count;

    public bool ContainsKey(T key) => this.textOverrides.ContainsKey(key);

    public bool TryGetValue(T key, [MaybeNullWhen(false)] out string value)
    {
        if (this.textOverrides.TryGetValue(key, out EnumPickerTextOverride<T>? textOverride))
        {
            value = textOverride.Value;
            return true;
        }

        value = null;
        return false;
    }
}