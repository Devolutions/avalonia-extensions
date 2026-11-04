// Vendored from Ursa.Avalonia, MIT licensed.
//   source:  https://github.com/irihitech/Ursa.Avalonia/blob/e72d50d5e41efa75424c3d2b39f4131a2293b155/src/Ursa/Controls/TagInput/LostFocusBehavior.cs
//   commit:  e72d50d5e41efa75424c3d2b39f4131a2293b155  (shipped as Irihi.Ursa 2.0.1)
//
// We used four types out of Ursa's 491 and wrapped two of them in empty
// subclasses purely to re-expose them under our own namespace. Vendoring drops
// both the Irihi.Ursa and Irihi.Avalonia.Shared package references, removes those
// wrappers, and stops Ursa loading during theme construction at application start.
//
// Changes from upstream:
//   - namespace moved to Devolutions.AvaloniaControls.Controls
// See THIRD-PARTY-NOTICES.md for the full licence text.
namespace Devolutions.AvaloniaControls.Controls;

public enum LostFocusBehavior
{
    None,
    Add,
    Clear,
}