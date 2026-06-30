using SistemaTicoBus.MAUI.Models;
using SistemaTicoBus.MAUI.Services;

namespace SistemaTicoBus.MAUI.Views
{
    public partial class MisReservasPage : ContentPage
    {
        private readonly TicoBusApiService _apiService;

        public MisReservasPage(TicoBusApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarReservasAsync();
        }

        private async Task CargarReservasAsync()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            ListaReservas.IsVisible = false;
            LabelSinReservas.IsVisible = false;

            var resultado = await _apiService.ObtenerMisReservasAsync(App.UsuarioNombre);

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            if (!resultado.Exito || resultado.Datos == null || resultado.Datos.Count == 0)
            {
                LabelSinReservas.IsVisible = true;
                return;
            }

            ListaReservas.ItemsSource = resultado.Datos;
            ListaReservas.IsVisible = true;
        }

        private async void ReservaCard_Tapped(object? sender, EventArgs e)
        {
            if (sender is Border border && border.BindingContext is ReservaModelo reserva)
            {
                await Shell.Current.GoToAsync($"DetalleReservaPage?idReserva={reserva.IdReserva}");
            }
        }

        private async void BtnVolver_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//InicioPage");
        }

        private async void TabInicio_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//InicioPage");
        }

        private async void TabPerfil_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//PerfilPage");
        }
    }
}