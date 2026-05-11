namespace SampleApp.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class TimePickerUpDownViewModel : ObservableObject
{
  [ObservableProperty]
  private TimeSpan? defaultSelectedTime = new(10, 15, 0);

  [ObservableProperty]
  private TimeSpan? twelveHourSelectedTime = new(11, 20, 0);

  [ObservableProperty]
  private TimeSpan? twentyFourHourSelectedTime = new(18, 45, 0);

  [ObservableProperty]
  private TimeSpan? minuteIncrementSelectedTime = new(10, 30, 0);

  [ObservableProperty]
  private TimeSpan? disabledSelectedTime = new(9, 0, 0);

  [ObservableProperty]
  private TimeSpan? errorSelectedTime = new(11, 15, 0);

  [ObservableProperty]
  private TimeSpan? secondsSelectedTime = new(10, 15, 6);

  [ObservableProperty]
  private TimeSpan? twentyFourHourSecondsSelectedTime = new(18, 45, 30);

  public string DefaultSelectedTimeDisplay => Format12Hour(this.DefaultSelectedTime, "Selected time");

  public string SecondsSelectedTimeDisplay => this.SecondsSelectedTime.HasValue
    ? $"Selected time: {DateTime.Today.Add(this.SecondsSelectedTime.Value):h:mm:ss tt}"
    : "Selected time: --:--:-- --";

  public string TwentyFourHourSecondsSelectedTimeDisplay => this.TwentyFourHourSecondsSelectedTime.HasValue
    ? $"Selected time: 24-hour: {DateTime.Today.Add(this.TwentyFourHourSecondsSelectedTime.Value):HH:mm:ss}"
    : "Selected time: 24-hour: --:--:--";

  partial void OnDefaultSelectedTimeChanged(TimeSpan? value)
  {
    this.OnPropertyChanged(nameof(this.DefaultSelectedTimeDisplay));
  }

  partial void OnSecondsSelectedTimeChanged(TimeSpan? value)
  {
    this.OnPropertyChanged(nameof(this.SecondsSelectedTimeDisplay));
  }

  partial void OnTwentyFourHourSecondsSelectedTimeChanged(TimeSpan? value)
  {
    this.OnPropertyChanged(nameof(this.TwentyFourHourSecondsSelectedTimeDisplay));
  }

  private static string Format12Hour(TimeSpan? value, string label) =>
    value.HasValue
      ? $"{label}: {DateTime.Today.Add(value.Value):h:mm tt}"
      : $"{label}: --:-- --";
}