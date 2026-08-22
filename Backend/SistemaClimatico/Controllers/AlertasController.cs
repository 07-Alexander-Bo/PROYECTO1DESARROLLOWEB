using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Dtos;
using SistemaClimatico.Models;
using SistemaClimatico.Services;

namespace SistemaClimatico.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertasController : ControllerBase
{
    private readonly SistemaClimaticoContext _db;

    public AlertasController(SistemaClimaticoContext db)
    {
        _db = db;
    }

    // GET api/alertas?nivel=Rojo&tipoFenomeno=Inundacion&atendida=false
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertaDto>>> GetAlertas(
        [FromQuery] string? nivel,
        [FromQuery] string? tipoFenomeno,
        [FromQuery] bool? atendida)
    {
        var query = _db.Alertas.Include(a => a.IdSensorNavigation).AsQueryable();

        if (!string.IsNullOrWhiteSpace(nivel))
            query = query.Where(a => a.Nivel == nivel);

        if (!string.IsNullOrWhiteSpace(tipoFenomeno))
            query = query.Where(a => a.TipoFenomeno == tipoFenomeno);

        if (atendida.HasValue)
            query = query.Where(a => a.Atendida == atendida.Value);

        var alertas = await query
            .OrderByDescending(a => a.FechaHora)
            .Select(a => new AlertaDto
            {
                IdAlerta = a.IdAlerta,
                IdSensor = a.IdSensor,
                NombreSensor = a.IdSensorNavigation != null ? a.IdSensorNavigation.Nombre : null,
                Nivel = a.Nivel,
                TipoFenomeno = a.TipoFenomeno,
                Mensaje = a.Mensaje,
                ValorDetectado = a.ValorDetectado,
                FechaHora = a.FechaHora,
                Atendida = a.Atendida
            })
            .ToListAsync();

        return Ok(alertas);
    }

    // GET api/alertas/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<AlertaDto>> GetAlerta(long id)
    {
        var alerta = await _db.Alertas.Include(a => a.IdSensorNavigation)
            .FirstOrDefaultAsync(a => a.IdAlerta == id);

        if (alerta is null)
            return NotFound(new { mensaje = $"No existe una alerta con id {id}." });

        return Ok(new AlertaDto
        {
            IdAlerta = alerta.IdAlerta,
            IdSensor = alerta.IdSensor,
            NombreSensor = alerta.IdSensorNavigation?.Nombre,
            Nivel = alerta.Nivel,
            TipoFenomeno = alerta.TipoFenomeno,
            Mensaje = alerta.Mensaje,
            ValorDetectado = alerta.ValorDetectado,
            FechaHora = alerta.FechaHora,
            Atendida = alerta.Atendida
        });
    }

    // PATCH api/alertas/5/atender
    [HttpPatch("{id:long}/atender")]
    public async Task<IActionResult> AtenderAlerta(long id)
    {
        var alerta = await _db.Alertas.FindAsync(id);
        if (alerta is null)
            return NotFound(new { mensaje = $"No existe una alerta con id {id}." });

        alerta.Atendida = true;
        await _db.SaveChangesAsync();

        return Ok(new { mensaje = "Alerta marcada como atendida." });
    }

    // GET api/alertas/estado-actual
    // Estado en vivo de cada sensor activo (incluye Verde), útil para el dashboard.
    [HttpGet("estado-actual")]
    public async Task<ActionResult<IEnumerable<EstadoSensorDto>>> GetEstadoActual()
    {
        var sensores = await _db.Sensores
            .Where(s => s.Activo)
            .ToListAsync();

        var resultado = new List<EstadoSensorDto>();

        foreach (var sensor in sensores)
        {
            var ultimaLectura = await _db.LecturasSensores
                .Where(l => l.IdSensor == sensor.IdSensor)
                .OrderByDescending(l => l.FechaHora)
                .FirstOrDefaultAsync();

            if (ultimaLectura is null)
            {
                resultado.Add(new EstadoSensorDto
                {
                    IdSensor = sensor.IdSensor,
                    NombreSensor = sensor.Nombre,
                    TipoSensor = sensor.Tipo,
                    UnidadMedida = sensor.UnidadMedida,
                    UltimoValor = null,
                    FechaUltimaLectura = null,
                    Nivel = "Verde",
                    TipoFenomeno = null,
                    Mensaje = "Sin lecturas registradas todavía."
                });
                continue;
            }

            var evaluacion = MotorAlertas.Evaluar(sensor.Tipo, ultimaLectura.Valor);

            resultado.Add(new EstadoSensorDto
            {
                IdSensor = sensor.IdSensor,
                NombreSensor = sensor.Nombre,
                TipoSensor = sensor.Tipo,
                UnidadMedida = sensor.UnidadMedida,
                UltimoValor = ultimaLectura.Valor,
                FechaUltimaLectura = ultimaLectura.FechaHora,
                Nivel = evaluacion.Nivel,
                TipoFenomeno = evaluacion.TipoFenomeno,
                Mensaje = evaluacion.Mensaje
            });
        }

        return Ok(resultado);
    }
}
