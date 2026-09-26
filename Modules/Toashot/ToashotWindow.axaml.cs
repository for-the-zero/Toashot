using Avalonia.Controls;

namespace Toashot.Modules.Toashot;

public partial class ToashotWindow : Window
{
    public ToashotWindow()
    {
        InitializeComponent();
    }

    public ToashotWindow(ToashotViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = Close;
    }
}
