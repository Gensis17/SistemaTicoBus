using SistemaTicoBus.MAUI.Services;

namespace SistemaTicoBus.MAUI.Views
{
    [QueryProperty(nameof(IdReserva), "idReserva")]
    public partial class DetalleReservaPage : ContentPage
    {
        private readonly TicoBusApiService _apiService;

        public int IdReserva { get; set; }

        public DetalleReservaPage(TicoBusApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarDetalleAsync();
        }

        private async Task CargarDetalleAsync()
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            LabelError.IsVisible = false;
            ContenidoDetalle.IsVisible = false;

            var resultado = await _apiService.ObtenerDetalleReservaAsync(App.UsuarioNombre, IdReserva);

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;

            if (!resultado.Exito || resultado.Datos == null || resultado.Datos.Viaje == null || resultado.Datos.Viaje.Ruta == null)
            {
                LabelError.IsVisible = true;
                return;
            }

            var reserva = resultado.Datos;
            var viaje = reserva.Viaje;
            var ruta = viaje.Ruta;

            LabelRuta.Text = ruta.NombreFormateado;
            LabelEstado.Text = viaje.Estado;
            LabelFechaSalida.Text = viaje.FechaHoraSalida.ToString("dd/MM/yyyy, HH:mm");
            LabelFechaLlegada.Text = viaje.FechaHoraLlegadaEstimada.ToString("dd/MM/yyyy, HH:mm");
            LabelAsiento.Text = $"Asiento {reserva.NumeroAsiento}";
            LabelMonto.Text = reserva.MontoPagado.ToString("C", new System.Globalization.CultureInfo("es-CR"));
            LabelPlaca.Text = $"Placa: {viaje.PlacaUnidad}";
            LabelPrecioBase.Text = ruta.PrecioBase.ToString("C", new System.Globalization.CultureInfo("es-CR"));
            LabelMontoResumen.Text = reserva.MontoPagado.ToString("C", new System.Globalization.CultureInfo("es-CR"));

            if (viaje.Chofer != null)
            {
                LabelChofer.Text = $"Chofer: {viaje.Chofer.Nombre} {viaje.Chofer.Apellidos}";
            }
            else
            {
                LabelChofer.Text = "Chofer: No asignado";
            }

            ContenidoDetalle.IsVisible = true;
        }

        private async void BtnVolver_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}