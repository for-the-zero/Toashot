namespace Toashot.Modules.Toashot;

public partial class ToashotTempWindow : ShadUI.Window
{
    public ToashotTempWindow()
    {
        InitializeComponent();
    }

    public ToashotTempWindow(ToashotTempViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = Close;
    }
}
