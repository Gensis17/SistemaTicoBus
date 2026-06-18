using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.BL;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ViajeCanceladoController : Controller
    {
        private readonly IConfiguration configuration;

        public ViajeCanceladoController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IActionResult Index()
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

            ViajeCanceladoBL bl = new ViajeCanceladoBL(connectionString);

            var lista = bl.ListarViajesCancelados();

            return View(lista);
        }

        public IActionResult Detalle(int id)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

            ViajeCanceladoBL bl = new ViajeCanceladoBL(connectionString);

            var viaje = bl.ObtenerDetalleViajeCancelado(id);

            if (viaje == null)
            {
                return NotFound();
            }

            return View(viaje);
        }
    }
}
