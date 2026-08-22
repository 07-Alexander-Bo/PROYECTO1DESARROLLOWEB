namespace SistemaClimatico.Dtos;

public class HistorialEventoDto
{
    public long IdEvento { get; set; }
    public long? IdAlerta { get; set; }
    public string TipoFenomeno { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public DateTime FechaHora { get; set; }
}
