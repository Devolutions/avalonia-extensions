namespace SampleApp.ViewModels;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Models;

public class DataGridViewModel : ObservableObject
{
    public DataGridViewModel()
    {
        List<DataGridItem> items = new()
        {
            new DataGridItem("computer", "PDQ-VM", "PDQ-VM$", "6/26/2023"),
            new DataGridItem("user", "AD TEST Guy2", "ADGuysam", "5/2/2023"),
            new DataGridItem("user", "krbtgt", "krbtgt", "11/10/2021"),
            new DataGridItem("computer", "WIN11-VM-BT", "WIN11-VM-BT$", "11/12/2021"),
            new DataGridItem("computer", "RDS6", "RDS6$", "30/03/2021"),
            new DataGridItem("computer", "MSSQL1", "MSSQL1$", "14/10/2024"),
            new DataGridItem("computer", "ITMANGER-DRIVE", "ITMANGER-DRIVE$", "13/10/2021"),
        };
        this.Items = new ObservableCollection<DataGridItem>(items);
    }

    public ObservableCollection<DataGridItem> Items { get; }
}