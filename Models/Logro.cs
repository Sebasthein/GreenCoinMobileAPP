using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Models
{
    public class Logro
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string ImagenTrofeo { get; set; }
        public int PuntosRequeridos { get; set; }
        public List<UsuarioLogro> UsuarioLogros { get; set; }
    }
}