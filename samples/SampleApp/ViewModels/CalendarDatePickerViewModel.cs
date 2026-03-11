namespace SampleApp.ViewModels;

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class CalendarDatePickerViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime selectedDate = new DateTime(2026, 3, 6);

    [RelayCommand]
    private void SetToToday()
    {
        this.SelectedDate = DateTime.Today;
    }
}