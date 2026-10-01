namespace Toashot.Modules.Settings;

public partial class SettingsWindow : ShadUI.Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(SettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = Close;
        viewModel.TopLevel = this;
    }
}
