namespace SampleApp.FluentThemeAddOns;

// ReSharper disable once RedundantUsingDirective
using Avalonia.Markup.Xaml;
using System;
using Avalonia.Styling;

public class TreeDataGridFluentTheme : Styles
{
  public TreeDataGridFluentTheme(IServiceProvider? sp = null)
  {
#if ENABLE_ACCELERATE
        AvaloniaXamlLoader.Load(sp, this);
#endif
  }
}