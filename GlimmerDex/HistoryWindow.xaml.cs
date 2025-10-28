using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GlimmerDex
{
    public partial class HistoryWindow : Window
    {
        private ObservableCollection<HistoryEntry> _historyEntries;

        public HistoryWindow(ObservableCollection<HistoryEntry> historyEntries)
        {
            InitializeComponent();
            _historyEntries = historyEntries;
            historyListView.ItemsSource = _historyEntries;

            _historyEntries.CollectionChanged += (s, e) => ScrollToTop();
            Loaded += (s, e) => ScrollToTop();
        }

        private void ScrollToTop()
        {
            // Find the ScrollViewer in the visual tree and scroll to top
            var scrollViewer = FindVisualChild<ScrollViewer>(this);
            scrollViewer?.ScrollToTop();
        }

        // Helper method to find child controls in the visual tree
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child != null && child is T typedChild)
                    return typedChild;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}