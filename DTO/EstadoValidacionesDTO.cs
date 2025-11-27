using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class EstadoValidacionesDTO
    {
        public int Pendientes { get; set; }
        public int AprobadosRecientes { get; set; }
        public int PuntosGanados { get; set; }
        public bool TieneAprobadosRecientes { get; set; }
    }
}
