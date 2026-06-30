using SistemaTicoBus.MAUI.Views;

namespace SistemaTicoBus.MAUI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("InicioPage", typeof(InicioPage));
            Routing.RegisterRoute("MisReservasPage", typeof(MisReservasPage));
            Routing.RegisterRoute("DetalleReservaPage", typeof(DetalleReservaPage));
            Routing.RegisterRoute("PerfilPage", typeof(PerfilPage));
        }
    }
}