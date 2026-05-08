namespace SampleApp.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class TimePickerViewModel : ObservableObject
{
  [ObservableProperty]
  private TimeSpan? selectedTime;

  public string SelectedTimeDisplay => this.SelectedTime.HasValue
    ? $"Selected time: {DateTime.Today.Add(this.SelectedTime.Value):h:mm tt}"
    : "Selected time: --:-- --";

  partial void OnSelectedTimeChanged(TimeSpan? value)
  {
    this.OnPropertyChanged(nameof(this.SelectedTimeDisplay));
  }
}