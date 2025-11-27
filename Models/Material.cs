using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Models
{
    public class Material
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public Usuario UsuarioCreador { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
        public int PuntosPorUnidad { get; set; }
        public bool Reciclable { get; set; }
        public string CodigoBarra { get; set; }
        public List<Reciclaje> Reciclajes { get; set; }
    }
}
