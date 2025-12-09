# Visual Regression Testing Project Plan

## Goal
Implement an automated or semi-automated visual regression testing system for the Avalonia Extensions project. The system should:
1.  Capture screenshots of all controls showcased in the `SampleApp`.
2.  Compare these screenshots against a baseline set.
3.  Alert the developer of any unintended visual changes.
4.  Ideally, support future expansion to interactive behaviors (pointer-over, etc.).

## Research Phase

### Potential Approaches

#### 1. Custom Automation (Slash Command / Script)
*   **Concept:** A script or agent that launches the `SampleApp`, navigates through tabs/states, takes screenshots of the window, and saves them.
*   **Comparison:** Custom logic to compare images (pixel-by-pixel or perceptual hash) and report differences.
*   **Pros:** Full control over the process; tests the actual running app.
*   **Cons:** UI automation (clicking tabs) can be flaky; window chrome/OS differences might affect screenshots; requires reliable way to know when rendering is finished.

#### 2. Playwright
*   **Concept:** Use the Playwright testing framework.
*   **Analysis:** Playwright is primarily designed for web applications. While it supports Electron, its support for native .NET desktop applications (like Avalonia) is likely non-existent unless the app is running in a web context (WASM).
*   **Verdict:** Likely not suitable for the native desktop `SampleApp`.

#### 3. Avalonia Design Preview CLI (Private/Beta)
*   **Concept:** A command-line tool from Avalonia team to capture screenshots from the design previewer.
*   **Pros:** Native support; likely optimized for rendering controls in isolation.
*   **Cons:** Not public; might be limited to single controls rather than full app scenarios; dependency on unreleased software.

#### 4. Avalonia.Headless (Proposed Alternative)
*   **Concept:** Use `Avalonia.Headless` platform with a unit testing framework (xUnit/NUnit).
*   **Mechanism:** Run tests that instantiate controls or windows in a headless environment, render them to a bitmap, and compare the bitmap bytes.
*   **Pros:** Fast; no physical window required; standard unit test integration; deterministic rendering (no OS window chrome variations).
*   **Cons:** Might not catch issues specific to the actual rendering backend (Skia/Direct2D) if the headless renderer behaves differently (though usually it uses Skia).

## Next Steps
1.  Evaluate `Avalonia.Headless` as a primary candidate.
2.  Investigate the "Custom Automation" approach using accessibility APIs or simple keyboard navigation if Headless is insufficient.
3.  (Optional) Explore the private Avalonia CLI tool if provided by the user.

### Analysis of `AvaShot` (Approach 3)
*   **Findings:** The tool in `/TMP_AvaShot` is designed to inject into the **Avalonia Designer** (the previewer used in IDEs). It uses Harmony patches to intercept the designer window loading.
*   **Suitability:** It is excellent for capturing screenshots of what the developer is currently looking at in the IDE. However, for **automated batch regression testing** (CI/CD style), it is less suitable because it relies on the IDE's designer process being active.
*   **Decision:** We will proceed with **Approach 4 (Avalonia.Headless)** as it is the standard, robust way to perform automated visual testing without requiring an interactive GUI session.

## Proof of Concept (PoC) Plan
1.  Create a new test project: `tests/Devolutions.AvaloniaControls.VisualTests`.
2.  Configure it with `Avalonia.Headless.XUnit`.
3.  Add a reference to the `SampleApp` project.
4.  Create a test that:
    *   Instantiates `MainWindow` (or a specific Demo Page).
    *   Uses `Avalonia.Headless` to capture a bitmap.
    *   Saves the bitmap to disk (to verify it works).

## Proof of Concept (PoC) Results
*   **Status:** Success.
*   **Implementation:**
    *   Created `tests/Devolutions.AvaloniaControls.VisualTests`.
    *   Configured `Avalonia.Headless.XUnit` with `.UseSkia()` and `UseHeadlessDrawing = false`.
    *   Successfully captured screenshots of a simple `Button` and the `ButtonDemo` page from `SampleApp`.
    *   Resolved `SkiaSharp` version mismatch by removing incompatible `SkiaSharp.NativeAssets.macOS` package (Avalonia manages dependencies).
*   **Key Findings:**
    *   `CaptureRenderedFrame()` works reliably.
    *   `SampleApp` resources are loaded correctly via `AppBuilder.Configure<App>()`.
    *   `DetectDesignTheme` throws a handled exception because `App.axaml` is not on disk in the test project, but it doesn't stop the test.

## Next Steps
1.  **Refactor Test Structure:**
    *   Create a data-driven test that iterates over all available Demo Pages in `SampleApp`.
    *   Use reflection to find all types in `SampleApp.DemoPages` namespace.
2.  **Implement Comparison Logic:**
    *   Add a helper to compare the captured bitmap with a baseline image.
    *   Use a simple pixel-by-pixel comparison or a library like `ImageSharp` or `SkiaSharp` for diffing.
3.  **Baseline Management:**
    *   Define a directory for baseline images (e.g., `tests/baselines`).
    *   Add a mode to "update baselines" (overwrite existing images).
