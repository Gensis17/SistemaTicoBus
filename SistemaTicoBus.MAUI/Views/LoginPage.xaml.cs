using SistemaTicoBus.MAUI.Services;

namespace SistemaTicoBus.MAUI.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly TicoBusApiService _apiService;

        public LoginPage(TicoBusApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
        }

        private async void BtnIngresar_Clicked(object sender, EventArgs e)
        {
            LabelError.IsVisible = false;

            string usuario = EntryUsuario.Text?.Trim() ?? string.Empty;
            string clave = EntryClave.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
            {
                MostrarError("Debe ingresar usuario y contraseña.");
                return;
            }

            BtnIngresar.IsEnabled = false;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var resultado = await _apiService.LoginAsync(usuario, clave);

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            BtnIngresar.IsEnabled = true;

            if (!resultado.Exito || resultado.Datos == null)
            {
                MostrarError(resultado.Mensaje);
                return;
            }

            if (resultado.Datos.Rol != "Pasajero")
            {
                MostrarError("Esta aplicación es exclusiva para pasajeros.");
                return;
            }

            App.UsuarioId = resultado.Datos.UsuarioId;
            App.UsuarioNombre = resultado.Datos.NombreUsuario;
            App.UsuarioRol = resultado.Datos.Rol;

            await Shell.Current.GoToAsync("//InicioPage");
        }

        private void MostrarError(string mensaje)
        {
            LabelError.Text = mensaje;
            LabelError.IsVisible = true;
        }
    }
}