namespace SistemaClimatico.Dtos;

// Tipos válidos de sensor, según el CHECK de la tabla Sensores
public static class TiposSensor
{
    public static readonly string[] Valores =
    [
        "Temperatura", "Humedad", "Viento", "Lluvia", "NivelRio", "Otro"
    ];
}

// Lo que se devuelve al consultar sensores
public class SensorDto
{
    public int IdSensor { get; set; }
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string UnidadMedida { get; set; } = null!;
    public string? Ubicacion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

// Lo que se envía para crear un sensor nuevo
public class CrearSensorDto
{
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string UnidadMedida { get; set; } = null!;
    public string? Ubicacion { get; set; }
}

// Lo que se envía para editar un sensor existente
public class ActualizarSensorDto
{
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string UnidadMedida { get; set; } = null!;
    public string? Ubicacion { get; set; }
}

// Lo que se envía al registrar una lectura simulada de un sensor
public class CrearLecturaDto
{
    public decimal Valor { get; set; }
}

public class LecturaDto
{
    public long IdLectura { get; set; }
    public int IdSensor { get; set; }
    public decimal Valor { get; set; }
    public DateTime FechaHora { get; set; }
}
