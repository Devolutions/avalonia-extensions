# Third-party notices

This repository includes source code from third-party projects. Their licences are reproduced below.

---

## Ursa.Avalonia

Four controls are vendored into `Devolutions.AvaloniaControls` from
[Ursa.Avalonia](https://github.com/irihitech/Ursa.Avalonia), taken at commit
`e72d50d5e41efa75424c3d2b39f4131a2293b155` (shipped as `Irihi.Ursa` 2.0.1):

| vendored file | upstream path |
| --- | --- |
| `Controls/TagInput/TagInput.cs` | `src/Ursa/Controls/TagInput/TagInput.cs` |
| `Controls/TagInput/ClosableTag.cs` | `src/Ursa/Controls/TagInput/ClosableTag.cs` |
| `Controls/TagInput/LostFocusBehavior.cs` | `src/Ursa/Controls/TagInput/LostFocusBehavior.cs` |
| `Controls/WrapPanelWithTrailingItem.cs` | `src/Ursa/Controls/Panels/WrapPanelWithTrailingItem.cs` |
| `Controls/MultiComboBox/MultiComboBoxSelectedItemList.cs` | `src/Ursa/Controls/ComboBox/MultiComboBoxSelectedItemList.cs` |

**Why vendored.** We used four of Ursa's 491 types and wrapped two of them in empty subclasses purely to
re-expose them under our own namespace, so every theme keyed its `ControlTheme` on the Ursa type and then
aliased it to the wrapper. Vendoring collapses that into one type per control, and drops both the
`Irihi.Ursa` and `Irihi.Avalonia.Shared` package references — the latter was reached for three members in
total. It also stops Ursa being loaded while a theme is constructed, which happens during application
startup whether or not the host ever shows a tag input.

**Changes from upstream**, each noted in the file that carries it:

- Namespace moved to `Devolutions.AvaloniaControls.Controls`.
- `PseudoClassName.PC_Empty` (`Irihi.Avalonia.Shared`) replaced by a local `EmptyPseudoClass` constant with
  the same `":empty"` value.
- `MathHelpers.GreaterThan` (`Irihi.Avalonia.Shared`) replaced by a private tolerance-aware comparison;
  Avalonia's equivalent `MathUtilities` is internal in 12.x. Verified to agree with the original across the
  layout-relevant range.

Upstream's headless tests for these controls were not vendored: each passes in isolation but they
interfere with one another in this repository's test host, the same way its pre-existing
"Developer tools have already been attached" failures do. Worth revisiting once that harness
isolation is fixed.

Our own `MultiComboBox` and `MultiComboBoxItem`, which are not vendored, also used
`Irihi.Avalonia.Shared`. Those call sites now use `AvaloniaExtensions.AffectsPseudoClass`, a local
`PC_Selected` constant, and `System.Reactive`'s `IObservable<T>.Subscribe(Action<T>)`.

```
MIT License

Copyright (c) 2025 .NET Foundation and Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
