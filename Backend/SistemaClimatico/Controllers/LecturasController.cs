using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Dtos;
using SistemaClimatico.Models;

namespace SistemaClimatico.Controllers;

// Datos para los gráficos de evolución del dashboard. Cualquier usuario autenticado.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LecturasController : ControllerBase
{
    private readonly SistemaClimaticoContext _db;

    public LecturasController(SistemaClimaticoContext db)
    {
        _db = db;
    }

    // GET api/lecturas/series?puntos=60&idComunidad=1
    // Las últimas N lecturas de cada sensor activo, en orden cronológico.
    [HttpGet("series")]
    public async Task<ActionResult<IEnumerable<SerieSensorDto>>> GetSeries(
        [FromQuery] int puntos = 60,
        [FromQuery] int? idComunidad = null)
    {
        puntos = Math.Clamp(puntos, 5, 500);

        var query = _db.Sensores.Where(s => s.Activo);
        if (idComunidad.HasValue)
            query = query.Where(s => s.IdComunidad == idComunidad.Value);

        var sensores = await query.OrderBy(s => s.IdSensor).ToListAsync();
        var series = new List<SerieSensorDto>();

        foreach (var sensor in sensores)
        {
            var ultimas = await _db.LecturasSensores
                .Where(l => l.IdSensor == sensor.IdSensor)
                .OrderByDescending(l => l.IdLectura)
                .Take(puntos)
                .Select(l => new PuntoLecturaDto { FechaHora = l.FechaHora, Valor = l.Valor })
                .ToListAsync();

            ultimas.Reverse(); // de la más antigua a la más reciente

            series.Add(new SerieSensorDto
            {
                IdSensor = sensor.IdSensor,
                Nombre = sensor.Nombre,
                Tipo = sensor.Tipo,
                UnidadMedida = sensor.UnidadMedida,
                IdComunidad = sensor.IdComunidad,
                Puntos = ultimas
            });
        }

        return Ok(series);
    }
}
