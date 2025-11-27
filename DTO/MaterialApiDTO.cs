using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class MaterialApiDTO
    {
        public long Id { get; set; }
        public string Tipo { get; set; }
        public string Nombre { get; set; }
        public int PuntosPorUnidad { get; set; }
        public string Descripcion { get; set; }
    }
}
