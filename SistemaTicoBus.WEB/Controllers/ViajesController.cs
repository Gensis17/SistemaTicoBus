using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaTicoBus.BL;
using SistemaTicoBus.DA.Data;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ViajesController : Controller
    {

        private readonly ViajeBL _viajeBL;
        private readonly AppDbContext _context;

        public ViajesController(ViajeBL viajeBL, AppDbContext context)
        {
            _viajeBL = viajeBL;
            _context = context;
        }
        public IActionResult Index(string? filtro)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            var viajes = _viajeBL.ObtenerViajes();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                viajes = viajes.Where(v =>
                    v.Ruta!.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    v.FechaHoraSalida.ToString("dd/MM/yyyy").Contains(filtro)
                ).ToList();
            }

            ViewBag.Filtro = filtro;
            CargarListasParaVista();
            return View(viajes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Agregar(Viaje viaje)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            var resultado = _viajeBL.AgregarViaje(viaje);

            if (resultado.Exitoso)
                TempData["Exito"] = resultado.Mensaje;
            else
                TempData["Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ObtenerViaje(int id)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            var viaje = _viajeBL.ObtenerViajePorId(id);
            if (viaje == null || viaje.Estado != "Programado")
            {
                TempData["Error"] = "Solo se pueden editar viajes en estado Programado.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ViajeEditarId"] = viaje.IdViaje.ToString();
            TempData["ViajeEditarRutaId"] = viaje.IdRuta.ToString();
            TempData["ViajeEditarPlaca"] = viaje.PlacaUnidad;
            TempData["ViajeEditarChoferId"] = viaje.ChoferId;
            TempData["ViajeEditarSalida"] = viaje.FechaHoraSalida.ToString("yyyy-MM-ddTHH:mm");
            TempData["ViajeEditarLlegada"] = viaje.FechaHoraLlegadaEstimada.ToString("yyyy-MM-ddTHH:mm");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Viaje viaje)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            var resultado = _viajeBL.EditarViaje(viaje);

            if (resultado.Exitoso)
                TempData["Exito"] = resultado.Mensaje;
            else
                TempData["Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar(int idViaje, string motivo)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            var resultado = _viajeBL.CancelarViaje(idViaje, motivo);

            if (resultado.Exitoso)
                TempData["Exito"] = resultado.Mensaje;
            else
                TempData["Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Iniciar(int idViaje)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            var resultado = _viajeBL.IniciarViaje(idViaje);

            if (resultado.Exitoso)
                TempData["Exito"] = resultado.Mensaje;
            else
                TempData["Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        //Metodo para cargar las listas de rutas, unidades y choferes en la vista
        private void CargarListasParaVista()
        {
            ViewBag.Rutas = new SelectList(_context.Rutas.ToList(), "Id", "Nombre");
            ViewBag.Unidades = new SelectList(_context.Unidades.ToList(), "Placa", "Placa");
            ViewBag.Choferes = new SelectList(
                _context.Choferes.Select(c => new {
                    c.Identificacion,
                    NombreCompleto = c.Nombre + " " + c.Apellidos
                }).ToList(),
                "Identificacion", "NombreCompleto"
            );
        }
    }
}
