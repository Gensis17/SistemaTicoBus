namespace SistemaTicoBus.MAUI.Models
{
    public class ReservaModelo
    {
        public int IdReserva { get; set; }
        public int IdViaje { get; set; }
        public string IdPasajero { get; set; } = string.Empty;
        public int NumeroAsiento { get; set; }
        public decimal MontoPagado { get; set; }
        public DateTime FechaReserva { get; set; }

        public ViajeModelo? Viaje { get; set; }

        public string RutaTexto => Viaje?.Ruta != null
            ? $"{Viaje.Ruta.Origen} → {Viaje.Ruta.Destino}"
            : string.Empty;
    }

    public class ViajeModelo
    {
        public int IdViaje { get; set; }
        public int IdRuta { get; set; }
        public string PlacaUnidad { get; set; } = string.Empty;
        public string ChoferId { get; set; } = string.Empty;
        public DateTime FechaHoraSalida { get; set; }
        public DateTime FechaHoraLlegadaEstimada { get; set; }
        public string Estado { get; set; } = string.Empty;

        public RutaModelo? Ruta { get; set; }
        public ChoferModelo? Chofer { get; set; }
    }

    public class RutaModelo
    {
        public int IdRuta { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public string Duracion { get; set; } = string.Empty;
        public decimal PrecioBase { get; set; }
    }

    public class ChoferModelo
    {
        public string Identificacion { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
    }
}