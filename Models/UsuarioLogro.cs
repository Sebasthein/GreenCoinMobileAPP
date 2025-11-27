using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Models
{
    public class UsuarioLogro
    {
        public long Id { get; set; }
        public DateTime FechaObtencion { get; set; }
        public Usuario Usuario { get; set; }
        public Logro Logro { get; set; }
    }
}