using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Models
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public object Usuario { get; set; }
        public string Message { get; set; }
    }
}
