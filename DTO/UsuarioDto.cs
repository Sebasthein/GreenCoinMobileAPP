using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GreenCoinMovil.UsuarioDTO
{
    public class UsuarioDto
    {
        // Long en Java suele ser long o int? en C#
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("usuarioRolId")] // Campo actualizado según respuesta de registro
        public int? UsuarioRolId { get; set; }

        [JsonPropertyName("rolAsignado")] // Rol asignado al usuario
        public string RolAsignado { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("telefono")]
        public string Telefono { get; set; }

        [JsonPropertyName("avatarId")]
        public string AvatarId { get; set; } // Opcionalmente, la URL completa del avatar

        [JsonPropertyName("direccion")]
        public string Direccion { get; set; }

        // Campos específicos para el Dashboard

        [JsonPropertyName("puntosAcumulados")] // Asumo que este campo existe en tu modelo de BD
        public int PuntosAcumulados { get; set; } = 0;

        [JsonPropertyName("reciclajesRealizados")] // Asumo que tienes un contador en BD
        public int ReciclajesRealizados { get; set; } = 0;

        [JsonPropertyName("nivelActual")] // En tu imagen de API, dice "nombreNivel"
        public string NombreNivel { get; set; } // Ej: "Semilla Verde"

        [JsonPropertyName("puntos")] 
        public int Puntos { get; set; } = 0;

        

        // CORRECCIÓN: Déjalo como una propiedad simple de lectura y escritura
        public string AvatarUrl { get; set; }
    }
}
