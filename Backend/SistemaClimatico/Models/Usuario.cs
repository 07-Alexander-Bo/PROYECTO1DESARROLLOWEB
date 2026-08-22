using System;
using System.Collections.Generic;

namespace SistemaClimatico.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<BitacoraAccione> BitacoraAcciones { get; set; } = new List<BitacoraAccione>();
}
