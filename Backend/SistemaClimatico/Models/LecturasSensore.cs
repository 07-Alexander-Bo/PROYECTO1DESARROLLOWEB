using System;
using System.Collections.Generic;

namespace SistemaClimatico.Models;

public partial class LecturasSensore
{
    public long IdLectura { get; set; }

    public int IdSensor { get; set; }

    public decimal Valor { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Sensore IdSensorNavigation { get; set; } = null!;
}
