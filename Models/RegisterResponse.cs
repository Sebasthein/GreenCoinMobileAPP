using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GreenCoinMovil.UsuarioDTO;

namespace GreenCoinMovil.Models
{
    public class RegisterResponse
    {
        public bool Success { get; set; }
        public UsuarioDto Usuario { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }
}
