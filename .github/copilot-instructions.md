# Visual regression coverage

- Canonical screenshot coverage is required only on each theme's target platform:
  - Windows: DevExpress, WinUiClassic, and WinUiMica.
  - macOS: MacClassic and LiquidGlass.
  - Linux: Linux/Yaru.
- Do not flag missing cross-platform screenshot baselines as missing test
  coverage. For example, a WinUI control PR with Windows baseline coverage
  does not require macOS or Linux baseline PNGs.
- Target-platform coverage remains important for new controls and intentional
  appearance changes. This policy does not waive functional tests.
- Pages marked "same as reference" are tested by directly comparing both
  rendered themes. They do not require a separate stored PNG for the delegating
  variant.
- `Screenshots/Baseline/{TargetOS}/{Theme}/` holds tracked canonical images.
  `Screenshots/LocalBaselines/{Theme}/` holds gitignored machine-local images
  for non-native theme development. Native themes always use tracked images
  on their target OS; comparisons never use other OS images.
- Until the planned GitHub workflows generate canonical images,
  `--update-baselines` updates only tracked images for native themes and only
  personal images for non-native themes.
- At the start of a new agent session, always ask once whether to initialize
  a full development baseline set, even for non-visual tasks. If approved,
  run `./devtest visual --initialize-local-baselines` before editing. It
  captures non-native personal images without comparisons or WinUI identity
  checks and preserves tracked images. If LocalBaselines is non-empty, surface
  the overwrite confirmation to the user; never answer or pipe `y` without
  explicit overwrite approval. Do not initialize modified code without explicit approval
  or automatically copy personal images across worktrees.
- See the [Testing section](../README.md#testing) for commands and behavior.
