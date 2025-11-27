using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class ReciclajeDTO
    {
        public long Id { get; set; }

        // Objetos anidados según la API /api/reciclajes/mis-reciclajes
        public UsuarioReciclajeDTO Usuario { get; set; }
        public MaterialReciclajeDTO Material { get; set; }

        public double Cantidad { get; set; }
        public int PuntosGanados { get; set; }
        public DateTime FechaReciclaje { get; set; }
        public string Estado { get; set; } // "VALIDADO", "PENDIENTE", etc.
        public bool Validado { get; set; }
        public string ImagenUrl { get; set; }

        // Propiedades adicionales que pueden venir de otros endpoints
        public string? Observaciones { get; set; }
        public string? ObservacionesAdmin { get; set; }

        // Propiedades calculadas para compatibilidad con el código existente
        public string MaterialNombre => Material?.Nombre ?? "Material desconocido";
        public string UsuarioNombre => Usuario?.Nombre ?? "Usuario desconocido";
        public DateTime Fecha => FechaReciclaje;

        public string CantidadFormateada { get; set; }
        public string FechaFormateada { get; set; }
    }

    // Clases auxiliares para los objetos anidados
    public class UsuarioReciclajeDTO
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
    }

    public class MaterialReciclajeDTO
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public int PuntosPorUnidad { get; set; }
    }
}
