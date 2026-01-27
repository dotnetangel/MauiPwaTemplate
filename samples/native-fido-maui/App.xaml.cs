using Microsoft.Maui.Controls;

namespace NativeFidoMaui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }
}
