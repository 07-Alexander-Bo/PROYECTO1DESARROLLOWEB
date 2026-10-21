namespace SistemaClimatico.Dtos;

public class ComunidadDto
{
    public int IdComunidad { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public bool Activa { get; set; }
    public int CantidadSensores { get; set; }
}

// Se usa tanto para crear como para editar una comunidad.
public class GuardarComunidadDto
{
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
}
