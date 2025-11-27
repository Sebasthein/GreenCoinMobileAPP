using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Models
{
    public class Reciclaje
    {
        public long Id { get; set; }
        public Material Material { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaReciclaje { get; set; }
        public int PuntosGanados { get; set; }
        public bool Validado { get; set; }
        public DateTime? FechaValidacion { get; set; }
        public string ImagenUrl { get; set; }
        public string Estado { get; set; }
        public Usuario UsuarioValidador { get; set; }
        public string MotivoRechazo { get; set; }
    }
}