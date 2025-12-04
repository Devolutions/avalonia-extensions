namespace SampleApp;

// ReSharper disable once RedundantUsingDirective
using Avalonia.Markup.Xaml;
using System;
using Avalonia.Styling;

public class TreeDataGridFluentTheme : Styles
{
  public TreeDataGridFluentTheme(IServiceProvider? sp = null)
  {
#if ENABLE_TREEDATAGRID
        AvaloniaXamlLoader.Load(sp, this);
#endif
  }
}