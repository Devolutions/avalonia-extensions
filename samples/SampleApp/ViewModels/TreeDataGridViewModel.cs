#if ENABLE_ACCELERATE
using Avalonia.Controls.Models.TreeDataGrid;
#endif

namespace SampleApp.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

public class Person
{
  public string? FirstName { get; set; }
  public string? LastName { get; set; }
  public int Age { get; set; }
  public ObservableCollection<Person> Children { get; } = new();
}

public class TreeDataGridViewModel : ObservableObject
{
#if ENABLE_ACCELERATE
  private readonly ObservableCollection<Person> _people = new()
  {
    new Person
    {
      FirstName = "Eleanor",
      LastName = "Pope",
      Age = 32,
      Children =
      {
        new Person { FirstName = "Marcel", LastName = "Gutierrez", Age = 4 }
      }
    },
    new Person
    {
      FirstName = "Jeremy",
      LastName = "Navarro",
      Age = 74,
      Children =
      {
        new Person
        {
          FirstName = "Jane",
          LastName = "Navarro",
          Age = 42,
          Children =
          {
            new Person { FirstName = "Lailah", LastName = "Velazquez", Age = 16 }
          }
        }
      }
    },
    new Person { FirstName = "Jazmine", LastName = "Schroeder", Age = 52 }
  };

  public TreeDataGridViewModel()
  {
    Source = new HierarchicalTreeDataGridSource<Person>(_people)
    {
      Columns =
      {
        new HierarchicalExpanderColumn<Person>(
          new TextColumn<Person, string>("First Name", x => x.FirstName),
          x => x.Children),
        new TextColumn<Person, string>("Last Name", x => x.LastName),
        new TextColumn<Person, int>("Age", x => x.Age)
      }
    };
  }

  public HierarchicalTreeDataGridSource<Person> Source { get; }
#else
  public object? Source { get; } = null;
#endif
}