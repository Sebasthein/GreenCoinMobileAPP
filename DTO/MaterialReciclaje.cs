using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class MaterialReciclaje
    {
        public long Id { get; set; }
        public string Tipo { get; set; }
        public int Puntos { get; set; }
        public string Icono { get; set; }
        public string Color { get; set; }
        public string Descripcion { get; set; }
    }
}
