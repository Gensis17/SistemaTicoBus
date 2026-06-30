namespace SistemaTicoBus.MAUI.Views
{
    public partial class PerfilPage : ContentPage
    {
        public PerfilPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LabelNombre.Text = App.UsuarioNombre;
            LabelRol.Text = App.UsuarioRol;
        }

        private async void BtnCerrarSesion_Clicked(object sender, EventArgs e)
        {
            App.UsuarioId = 0;
            App.UsuarioNombre = string.Empty;
            App.UsuarioRol = string.Empty;

            await Shell.Current.GoToAsync("//LoginPage");
        }

        private async void TabInicio_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//InicioPage");
        }

        private async void TabMisReservas_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MisReservasPage");
        }
    }
}