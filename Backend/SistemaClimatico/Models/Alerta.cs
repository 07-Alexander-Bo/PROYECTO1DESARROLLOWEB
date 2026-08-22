using System;
using System.Collections.Generic;

namespace SistemaClimatico.Models;

public partial class Alerta
{
    public long IdAlerta { get; set; }

    public int? IdSensor { get; set; }

    public string Nivel { get; set; } = null!;

    public string TipoFenomeno { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public decimal? ValorDetectado { get; set; }

    public DateTime FechaHora { get; set; }

    public bool Atendida { get; set; }

    public virtual ICollection<HistorialEvento> HistorialEventos { get; set; } = new List<HistorialEvento>();

    public virtual Sensore? IdSensorNavigation { get; set; }
}
