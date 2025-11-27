using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class DashboardResponseDTO
    {
        [JsonPropertyName("usuarioNombre")]
        public string UsuarioNombre { get; set; }

        [JsonPropertyName("nivelActual")]
        public string NivelActual { get; set; }

        [JsonPropertyName("avatarUrl")]
        public string AvatarUrl { get; set; }

        [JsonPropertyName("direccion")]
        public string Direccion { get; set; }

        [JsonPropertyName("puntosTotales")]
        public int? PuntosTotales { get; set; }

        [JsonPropertyName("ranking")]
        public int? Ranking { get; set; }

        [JsonPropertyName("totalReciclajes")]
        public long? TotalReciclajes { get; set; }

        [JsonPropertyName("logrosDesbloqueados")]
        public long? LogrosDesbloqueados { get; set; }

        [JsonPropertyName("diasActivos")]
        public long? DiasActivos { get; set; }

        [JsonPropertyName("actividadesRecientes")]
        public List<ActividadDTO> ActividadesRecientes { get; set; }



    }
}
