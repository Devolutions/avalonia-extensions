namespace SampleApp.ViewModels;

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class CalendarDatePickerViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isToday;

    [ObservableProperty]
    private DateTime selectedDate = DateTime.Now;

    partial void OnIsTodayChanged(bool value)
    {
        if (value)
        {
            this.SelectedDate = DateTime.Today;
        }
    }
}