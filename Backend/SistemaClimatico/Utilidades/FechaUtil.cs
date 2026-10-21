namespace SistemaClimatico.Utilidades;

// TODAS las fechas del sistema se guardan en UTC. Así no importa en qué zona horaria
// esté el servidor (tu PC, una VPS en otro país...): el navegador convierte cada fecha
// UTC a la hora local de quien la mira.
public static class FechaUtil
{
    // Convierte cualquier DateTime a UTC:
    //  - Utc         -> se deja igual.
    //  - Local       -> se convierte (por ejemplo, un filtro que llegó como "2026-10-15T06:00:00Z").
    //  - Unspecified -> se asume UTC (así salen las fechas de SQL Server y los filtros sin zona).
    public static DateTime AUtc(DateTime fecha) => fecha.Kind switch
    {
        DateTimeKind.Utc => fecha,
        DateTimeKind.Local => fecha.ToUniversalTime(),
        _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc),
    };

    // Para el filtro "hasta": si solo se indicó el día (sin hora, p. ej. 2026-10-15),
    // el día completo queda incluido.
    public static DateTime HastaInclusivo(DateTime hasta)
    {
        var utc = AUtc(hasta);
        return hasta.Kind == DateTimeKind.Unspecified && hasta.TimeOfDay == TimeSpan.Zero
            ? utc.AddDays(1).AddTicks(-1)
            : utc;
    }
}
