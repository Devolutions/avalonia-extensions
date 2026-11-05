namespace Devolutions.AvaloniaControls.Styling;

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

/// <summary>
/// A <see cref="ResourceInclude"/> that is not populated until something asks for a key it is responsible
/// for.
/// <para>
/// Merging a dictionary into a theme costs an assembly load at startup whenever that dictionary names types
/// from another assembly: the compiled XAML resolves them as it populates, so simply constructing the theme
/// drags the assembly in. A plain <see cref="ResourceInclude"/> does not help — it is still populated when
/// the owning dictionary is built. This provider answers <see cref="TryGetResource"/> for keys in
/// <see cref="TriggerNamespace"/> and loads <see cref="Source"/> only on the first such request.
/// </para>
/// <para>
/// By the time such a key is asked for, the host has already instantiated the control it belongs to and so
/// has loaded the assembly anyway — nothing loads earlier than it is needed, and nothing loads later than it
/// is used.
/// </para>
/// <example>
/// <code>
/// &lt;ResourceDictionary.MergedDictionaries&gt;
///   &lt;!-- non-merge entries must precede MergeResourceInclude ones (AVLN2000) --&gt;
///   &lt;styling:DeferredResourceInclude
///       Source="avares://Devolutions.AvaloniaTheme.DevExpress/Controls/ManagedFileChooser.axaml"
///       TriggerNamespace="Avalonia.Dialogs" /&gt;
///   &lt;MergeResourceInclude Source="Button.axaml" /&gt;
/// &lt;/ResourceDictionary.MergedDictionaries&gt;
/// </code>
/// </example>
/// <remarks>
/// The dictionary named by <see cref="Source"/> must not carry <c>x:ClassModifier</c>: that suppresses the
/// standalone loader the XAML compiler would otherwise emit for it, and loading it by URI then fails with
/// "No precompiled XAML found".
/// </remarks>
/// </summary>
public sealed class DeferredResourceInclude : ResourceProvider
{
    private ResourceInclude? resources;

    /// <summary>The dictionary to load, as an absolute <c>avares://</c> URI.</summary>
    public Uri? Source { get; set; }

    /// <summary>
    /// Requests for a <see cref="Type"/> key declared in this namespace trigger the load. It is matched as a
    /// string on purpose: naming the type here would reference the assembly and defeat the deferral.
    /// </summary>
    public string? TriggerNamespace { get; set; }

    public override bool HasResources => true;

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (!this.IsTriggerKey(key))
        {
            value = null;
            return false;
        }

        return this.EnsureLoaded().TryGetResource(key, theme, out value);
    }

    /// <summary>
    /// Only forwarded once the dictionary has been materialised. Forwarding eagerly would populate it, which
    /// is exactly what this class exists to avoid.
    /// </summary>
    protected override void OnAddOwner(IResourceHost owner) =>
        ((IResourceProvider?)this.resources)?.AddOwner(owner);

    protected override void OnRemoveOwner(IResourceHost owner) =>
        ((IResourceProvider?)this.resources)?.RemoveOwner(owner);

    private bool IsTriggerKey(object key) =>
        this.Source is not null
        && this.TriggerNamespace is { } ns
        && key is Type type
        && string.Equals(type.Namespace, ns, StringComparison.Ordinal);

    /// <summary>
    /// The IL2026 suppression mirrors the one on the theme entry points that already load dictionaries by
    /// URI: the dictionaries are our own precompiled XAML, preserved by each assembly's
    /// ILLink.Descriptors.xml (preserve="all"), so the dynamic load cannot lose anything to trimming.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Loads precompiled XAML from a Devolutions assembly, preserved via its " +
                        "assembly-level ILLink descriptor (preserve=\"all\").")]
    private ResourceInclude EnsureLoaded()
    {
        if (this.resources is null)
        {
            Uri source = this.Source!;
            this.resources = new ResourceInclude(source) { Source = source };

            if (this.Owner is { } owner)
            {
                ((IResourceProvider)this.resources).AddOwner(owner);
            }
        }

        return this.resources;
    }
}
