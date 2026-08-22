using System;
using System.Collections.Generic;

namespace SistemaClimatico.Models;

public partial class Sensore
{
    public int IdSensor { get; set; }

    public string Nombre { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string UnidadMedida { get; set; } = null!;

    public string? Ubicacion { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Alerta> Alerta { get; set; } = new List<Alerta>();

    public virtual ICollection<LecturasSensore> LecturasSensores { get; set; } = new List<LecturasSensore>();
}
