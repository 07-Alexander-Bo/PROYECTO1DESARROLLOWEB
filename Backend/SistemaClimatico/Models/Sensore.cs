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

    // Comunidad a la que pertenece el sensor y su posición en el mapa (opcionales).
    // Si no tiene coordenadas propias, el mapa lo ubica en el centro de su comunidad.
    public int? IdComunidad { get; set; }

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public virtual Comunidad? IdComunidadNavigation { get; set; }

    public virtual ICollection<Alerta> Alerta { get; set; } = new List<Alerta>();

    public virtual ICollection<LecturasSensore> LecturasSensores { get; set; } = new List<LecturasSensore>();
}
