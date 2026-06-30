using SistemaTicoBus.MAUI.Views;

namespace SistemaTicoBus.MAUI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("DetalleReservaPage", typeof(DetalleReservaPage));
        }
    }
}