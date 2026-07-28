namespace SampleApp.ViewModels;

using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
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

    [ObservableProperty]
    private IBrush selectedBrush = Brushes.Transparent;

    [RelayCommand]
    private void Green() => this.SelectedBrush = Brushes.Green;

    [RelayCommand]
    private void Orange() => this.SelectedBrush = Brushes.Orange;

    [RelayCommand]
    private void Red() => this.SelectedBrush = Brushes.Red;
}