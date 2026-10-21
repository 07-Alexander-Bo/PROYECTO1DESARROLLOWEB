using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaClimatico.Utilidades;

// Escribe todas las fechas como UTC con la "Z" al final (2026-10-15T20:30:00Z).
// Sin la "Z", el navegador no sabe en qué zona está la fecha y la interpretaría como
// hora local, desplazando las horas cuando el servidor está en otro huso (una VPS en UTC).
public class FechaUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => FechaUtil.AUtc(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(FechaUtil.AUtc(value));
}
