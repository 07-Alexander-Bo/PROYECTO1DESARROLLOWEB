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
    public int? IdComunidad { get; set; }
    public string? NombreComunidad { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
}

// Lo que se envía para crear un sensor nuevo
public class CrearSensorDto
{
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string UnidadMedida { get; set; } = null!;
    public string? Ubicacion { get; set; }
    public int? IdComunidad { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
}

// Lo que se envía para editar un sensor existente
public class ActualizarSensorDto
{
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string UnidadMedida { get; set; } = null!;
    public string? Ubicacion { get; set; }
    public int? IdComunidad { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
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

// Serie de lecturas de un sensor, lista para dibujar un gráfico de evolución.
public class SerieSensorDto
{
    public int IdSensor { get; set; }
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string UnidadMedida { get; set; } = null!;
    public int? IdComunidad { get; set; }
    public List<PuntoLecturaDto> Puntos { get; set; } = new();
}

public class PuntoLecturaDto
{
    public DateTime FechaHora { get; set; }
    public decimal Valor { get; set; }
}
