namespace SistemaClimatico.Dtos;

public class AlertaDto
{
    public long IdAlerta { get; set; }
    public int? IdSensor { get; set; }
    public string? NombreSensor { get; set; }
    public string Nivel { get; set; } = null!;
    public string TipoFenomeno { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public decimal? ValorDetectado { get; set; }
    public DateTime FechaHora { get; set; }
    public bool Atendida { get; set; }
}

// Estado actual (en vivo) de un sensor: su última lectura evaluada contra los umbrales,
// incluyendo Verde. Pensado para pintar el dashboard/semáforo, sin necesidad de que
// exista una fila en Alertas cuando todo está en rango normal.
public class EstadoSensorDto
{
    public int IdSensor { get; set; }
    public string NombreSensor { get; set; } = null!;
    public string TipoSensor { get; set; } = null!;
    public string UnidadMedida { get; set; } = null!;
    public decimal? UltimoValor { get; set; }
    public DateTime? FechaUltimaLectura { get; set; }
    public string Nivel { get; set; } = null!;
    public string? TipoFenomeno { get; set; }
    public string Mensaje { get; set; } = null!;

    // Ubicación para el mapa
    public int? IdComunidad { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
}
