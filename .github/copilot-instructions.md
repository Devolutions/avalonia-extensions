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
  `Screenshots/LocalBaselines/{OS}/{Theme}/` holds gitignored personal images
  for optional all-theme local development.
- Until the planned GitHub workflows generate canonical images,
  `--update-baselines` updates personal images for every selected theme and
  copies only native target-platform images into the tracked canonical tree.
- See the [Testing section](../README.md#testing) for commands and behavior.
