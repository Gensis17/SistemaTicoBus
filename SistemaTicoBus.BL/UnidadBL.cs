using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.BL
{
    public class UnidadBL
    {
        private readonly UnidadRepositorio _repositorio;

        public UnidadBL()
        {
            _repositorio = new UnidadRepositorio();
        }

        public List<Unidad> Listar()
        {
            return _repositorio.ObtenerUnidades();
        }

        public Unidad ObtenerPorPlaca(string placa)
        {
            return _repositorio.ObtenerUnidadPorPlaca(placa);
        }

        public string Agregar(Unidad unidad)
        {
            if (string.IsNullOrWhiteSpace(unidad.Placa))
                return "La placa es obligatoria.";

            if (string.IsNullOrWhiteSpace(unidad.Modelo))
                return "El modelo es obligatorio.";

            if (unidad.AnioFabricacion < 1900)
                return "Año de fabricación inválido.";

            if (unidad.CapacidadPasajeros <= 0)
                return "La capacidad debe ser mayor que cero.";

            if (_repositorio.ExistePlaca(unidad.Placa))
                return "Ya existe una unidad registrada con esa placa.";

            _repositorio.AgregarUnidad(unidad);

            return "";
        }

        public string Editar(Unidad unidad, string placaOriginal)
        {
            if (string.IsNullOrWhiteSpace(unidad.Placa))
                return "La placa es obligatoria.";

            if (string.IsNullOrWhiteSpace(unidad.Modelo))
                return "El modelo es obligatorio.";

            if (unidad.AnioFabricacion < 1900)
                return "Año de fabricación inválido.";

            if (unidad.CapacidadPasajeros <= 0)
                return "La capacidad debe ser mayor que cero.";

            if (placaOriginal != unidad.Placa &&
                _repositorio.ExistePlaca(unidad.Placa))
            {
                return "La nueva placa ya existe.";
            }

            _repositorio.EditarUnidad(unidad, placaOriginal);

            return "";
        }
    }
}
