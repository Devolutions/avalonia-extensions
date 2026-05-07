namespace SampleApp.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class TimePickerViewModel : ObservableObject
{
  [ObservableProperty]
  private TimeSpan? selectedTime;

  [ObservableProperty]
  private TimeSpan? segmentedSelectedTime = new(10, 15, 0);

  [ObservableProperty]
  private TimeSpan? segmentedSelectedTime24 = new(18, 45, 30);

  public string SelectedTimeDisplay => this.SelectedTime.HasValue
    ? $"Selected time: {DateTime.Today.Add(this.SelectedTime.Value):h:mm tt}"
    : "Selected time: --:-- --";

  partial void OnSelectedTimeChanged(TimeSpan? value)
  {
    this.OnPropertyChanged(nameof(this.SelectedTimeDisplay));
  }

  private static string Format12Hour(TimeSpan? value, string label) =>
    value.HasValue
      ? $"{label}: {DateTime.Today.Add(value.Value):h:mm tt}"
      : $"{label}: --:-- --";
}