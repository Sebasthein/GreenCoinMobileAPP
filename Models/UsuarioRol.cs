using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Models
{
    public class UsuarioRol
    {
        public Usuario Usuario { get; set; }
        public Rol Rol { get; set; }
        public bool Activo { get; set; }
    }
}