namespace SistemaTicoBus.MAUI
{
    public partial class App : Application
    {
        public static string UsuarioNombre { get; set; } = string.Empty;
        public static string UsuarioRol { get; set; } = string.Empty;
        public static int UsuarioId { get; set; }

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}