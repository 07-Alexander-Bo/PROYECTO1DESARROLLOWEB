using System;
using System.Collections.Generic;

namespace SistemaClimatico.Models;

public partial class BitacoraAccione
{
    public long IdBitacora { get; set; }

    public int? IdUsuario { get; set; }

    public string Accion { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
