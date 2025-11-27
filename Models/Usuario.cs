using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Models
{
    public class Usuario
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int Puntos { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
        public string AvatarId { get; set; }
        public Nivel Nivel { get; set; }
        public List<Reciclaje> Reciclajes { get; set; }
        public List<UsuarioRol> UsuarioRoles { get; set; }
        public List<UsuarioLogro> UsuarioLogros { get; set; }
        public bool AccountNonExpired { get; set; }
        public bool CredentialsNonExpired { get; set; }
        public string Username { get; set; }
        public List<GrantedAuthority> Authorities { get; set; }
        public bool AccountNonLocked { get; set; }
        public bool Enabled { get; set; }
    }
}