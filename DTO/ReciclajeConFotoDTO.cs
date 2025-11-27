using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class ReciclajeConFotoDTO
    {
        public long MaterialId { get; set; }
        public int Cantidad { get; set; } = 1;
        public string FotoBase64 { get; set; }
        public string NombreUsuario { get; set; }
        public string Observaciones { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
