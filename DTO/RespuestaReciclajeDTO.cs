using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class RespuestaReciclajeDTO
    {
        public bool Success { get; set; }
        public string Mensaje { get; set; }
        public string Estado { get; set; }
        public int IdReciclaje { get; set; }
        public DateTime FechaRevision { get; set; }
    }
}
