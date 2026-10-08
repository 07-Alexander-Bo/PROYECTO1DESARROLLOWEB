namespace SistemaClimatico.Dtos;

// Lo que se devuelve al consultar la bitácora de acciones
public class BitacoraDto
{
    public long IdBitacora { get; set; }
    public int? IdUsuario { get; set; }
    public string? NombreUsuario { get; set; }
    public string Accion { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime FechaHora { get; set; }
}
