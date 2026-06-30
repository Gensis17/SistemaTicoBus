using SistemaTicoBus.MAUI.Models;
using SistemaTicoBus.MAUI.Services;

namespace SistemaTicoBus.MAUI.Views
{
    public partial class InicioPage : ContentPage
    {
        private readonly TicoBusApiService _apiService;
        private int _idReservaProximoViaje;

        public InicioPage(TicoBusApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LabelSaludo.Text = $"¡Hola, {App.UsuarioNombre}!";
            await CargarProximoViajeAsync();
        }

        private async Task CargarProximoViajeAsync()
        {
            var resultado = await _apiService.ObtenerMisReservasAsync(App.UsuarioNombre);

            if (!resultado.Exito || resultado.Datos == null || resultado.Datos.Count == 0)
            {
                CardProximoViaje.IsVisible = false;
                LabelSinViajes.IsVisible = true;
                return;
            }

            ReservaModelo? proximo = resultado.Datos
                .Where(r => r.Viaje != null && (r.Viaje.Estado == "Programado" || r.Viaje.Estado == "En Curso"))
                .OrderBy(r => r.Viaje!.FechaHoraSalida)
                .FirstOrDefault();

            if (proximo == null || proximo.Viaje == null || proximo.Viaje.Ruta == null)
            {
                CardProximoViaje.IsVisible = false;
                LabelSinViajes.IsVisible = true;
                return;
            }

            _idReservaProximoViaje = proximo.IdReserva;

            LabelRutaProximoViaje.Text = $"{proximo.Viaje.Ruta.Origen} → {proximo.Viaje.Ruta.Destino}";
            LabelEstadoProximoViaje.Text = proximo.Viaje.Estado;
            LabelFechaHoraProximoViaje.Text = proximo.Viaje.FechaHoraSalida.ToString("dd/MM/yyyy, HH:mm");
            LabelAsientoProximoViaje.Text = $"Asiento {proximo.NumeroAsiento}";

            CardProximoViaje.IsVisible = true;
            LabelSinViajes.IsVisible = false;
        }

        private async void BtnBuscarRuta_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("TicoBus", "La búsqueda de rutas estará disponible próximamente.", "OK");
        }

        private async void BtnVerProximoViaje_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"DetalleReservaPage?idReserva={_idReservaProximoViaje}");
        }

        private async void TabMisReservas_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MisReservasPage");
        }

        private async void TabPerfil_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//PerfilPage");
        }
    }
}