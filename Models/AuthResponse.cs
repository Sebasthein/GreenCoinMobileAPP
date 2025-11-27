using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GreenCoinMovil.UsuarioDTO;

namespace GreenCoinMovil.Models
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } // El JWT.
        public UsuarioDto Usuario { get; set; } // El DTO del perfil.
        public string Error { get; set; } // Si el login falla (401 Unauthorized)
    }
}
