using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class Logro
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Icono { get; set; }
        public string IconoEstado { get; set; }
        public string ColorFondo { get; set; }
        public string ColorEstado { get; set; }
        public bool Desbloqueado { get; set; }
        public int ProgresoActual { get; set; }
        public int ProgresoTotal { get; set; }
        public int PuntosRequeridos { get; set; }

        public string ProgresoTexto => $"{ProgresoActual}/{ProgresoTotal}";
        public string PuntosTexto => $"{PuntosRequeridos} pts";
    }
}
