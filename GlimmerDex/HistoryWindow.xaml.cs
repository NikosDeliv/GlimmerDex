using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace GlimmerDex
{
    public partial class HistoryWindow : Window
    {
        public ObservableCollection<HistoryEntry> HistoryEntries { get; set; } // ObservableCollection to support data binding

        public HistoryWindow(List<HistoryEntry> historyEntries)
        {
            InitializeComponent();
            HistoryEntries = new ObservableCollection<HistoryEntry>(historyEntries);
            historyListBox.ItemsSource = HistoryEntries;
        }
    }
}