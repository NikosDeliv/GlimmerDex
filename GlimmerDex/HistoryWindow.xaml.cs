using System;
using System.Collections.Generic;
using System.Windows;

namespace GlimmerDex
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow(List<HistoryEntry> historyEntries)
        {
            InitializeComponent();

            historyListView.ItemsSource = historyEntries;
        }
    }
}
