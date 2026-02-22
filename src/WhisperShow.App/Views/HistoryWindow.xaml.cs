using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WhisperShow.App.ViewModels;

namespace WhisperShow.App.Views;

public partial class HistoryWindow : Window
{
    private readonly HistoryViewModel _viewModel;

    public HistoryWindow(HistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public void ShowAndRefresh()
    {
        _viewModel.Refresh();
        Show();
        Activate();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
