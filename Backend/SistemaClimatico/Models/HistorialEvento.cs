using System;
using System.Collections.Generic;

namespace SistemaClimatico.Models;

public partial class HistorialEvento
{
    public long IdEvento { get; set; }

    public long? IdAlerta { get; set; }

    public string TipoFenomeno { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public DateTime FechaHora { get; set; }

    public virtual Alerta? IdAlertaNavigation { get; set; }
}
