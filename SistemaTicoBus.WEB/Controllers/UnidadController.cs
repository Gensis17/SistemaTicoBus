using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.WEB.Controllers
{
    public class UnidadController : Controller
    {
        private readonly UnidadBL _unidadBL;

        public UnidadController()
        {
            _unidadBL = new UnidadBL();
        }

        public IActionResult Index()
        {
            var unidades = _unidadBL.Listar();
            return View(unidades);
        }

        [HttpPost]
        public IActionResult Crear(Unidad model)
        {
            string resultado = _unidadBL.Agregar(model);

            if (!string.IsNullOrEmpty(resultado))
                TempData["MensajeError"] = resultado;
            else
                TempData["MensajeExito"] = "Unidad registrada correctamente.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Editar(Unidad model, string placaOriginal)
        {
            string resultado = _unidadBL.Editar(model, placaOriginal);

            if (!string.IsNullOrEmpty(resultado))
                TempData["MensajeError"] = resultado;
            else
                TempData["MensajeExito"] = "Unidad actualizada correctamente.";

            return RedirectToAction("Index");
        }
    }
}
