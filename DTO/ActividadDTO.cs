using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json;

namespace GreenCoinMovil.DTO
{
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (DateTime.TryParse(dateString, out var date))
            {
                return date;
            }
            return DateTime.Now; // fallback
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}

namespace GreenCoinMovil.DTO
{
    public class ActividadDTO
    {
        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }

        [JsonPropertyName("puntos")]
        public int Puntos { get; set; }

        [JsonPropertyName("fecha")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime Fecha { get; set; }

        // Formateo para la vista
        public string FechaFormateada => Fecha.ToString("dd/MM");
        public string PuntosTexto => $"+{Puntos} pts";
    }
}
