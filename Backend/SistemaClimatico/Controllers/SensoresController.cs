using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Dtos;
using SistemaClimatico.Models;
using SistemaClimatico.Services;

namespace SistemaClimatico.Controllers;

// Consultar: cualquier usuario autenticado.
// Crear/editar sensores: solo Administrador.
// Activar/desactivar sensores y registrar lecturas: Administrador u Operador.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SensoresController : ControllerBase
{
    private readonly SistemaClimaticoContext _db;
    private readonly BitacoraService _bitacora;

    public SensoresController(SistemaClimaticoContext db, BitacoraService bitacora)
    {
        _db = db;
        _bitacora = bitacora;
    }

    // GET api/sensores
    // Por defecto solo trae los activos. ?incluirInactivos=true para traer todos.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SensorDto>>> GetSensores([FromQuery] bool incluirInactivos = false)
    {
        var query = _db.Sensores.AsQueryable();

        if (!incluirInactivos)
            query = query.Where(s => s.Activo);

        var sensores = await query
            .OrderBy(s => s.Nombre)
            .Select(s => new SensorDto
            {
                IdSensor = s.IdSensor,
                Nombre = s.Nombre,
                Tipo = s.Tipo,
                UnidadMedida = s.UnidadMedida,
                Ubicacion = s.Ubicacion,
                Activo = s.Activo,
                FechaCreacion = s.FechaCreacion
            })
            .ToListAsync();

        return Ok(sensores);
    }

    // GET api/sensores/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SensorDto>> GetSensor(int id)
    {
        var sensor = await _db.Sensores.FindAsync(id);

        if (sensor is null)
            return NotFound(new { mensaje = $"No existe un sensor con id {id}." });

        return Ok(new SensorDto
        {
            IdSensor = sensor.IdSensor,
            Nombre = sensor.Nombre,
            Tipo = sensor.Tipo,
            UnidadMedida = sensor.UnidadMedida,
            Ubicacion = sensor.Ubicacion,
            Activo = sensor.Activo,
            FechaCreacion = sensor.FechaCreacion
        });
    }

    // POST api/sensores
    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<ActionResult<SensorDto>> CrearSensor([FromBody] CrearSensorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { mensaje = "El nombre del sensor es obligatorio." });

        if (!TiposSensor.Valores.Contains(dto.Tipo))
            return BadRequest(new
            {
                mensaje = $"Tipo de sensor inválido. Valores permitidos: {string.Join(", ", TiposSensor.Valores)}."
            });

        if (string.IsNullOrWhiteSpace(dto.UnidadMedida))
            return BadRequest(new { mensaje = "La unidad de medida es obligatoria." });

        var sensor = new Sensore
        {
            Nombre = dto.Nombre.Trim(),
            Tipo = dto.Tipo,
            UnidadMedida = dto.UnidadMedida.Trim(),
            Ubicacion = dto.Ubicacion?.Trim(),
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        _db.Sensores.Add(sensor);
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("SENSOR_CREADO",
            $"Se creó el sensor '{sensor.Nombre}' (id {sensor.IdSensor}, tipo {sensor.Tipo}).");

        var resultado = new SensorDto
        {
            IdSensor = sensor.IdSensor,
            Nombre = sensor.Nombre,
            Tipo = sensor.Tipo,
            UnidadMedida = sensor.UnidadMedida,
            Ubicacion = sensor.Ubicacion,
            Activo = sensor.Activo,
            FechaCreacion = sensor.FechaCreacion
        };

        return CreatedAtAction(nameof(GetSensor), new { id = sensor.IdSensor }, resultado);
    }

    // PUT api/sensores/5
    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarSensor(int id, [FromBody] ActualizarSensorDto dto)
    {
        var sensor = await _db.Sensores.FindAsync(id);
        if (sensor is null)
            return NotFound(new { mensaje = $"No existe un sensor con id {id}." });

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { mensaje = "El nombre del sensor es obligatorio." });

        if (!TiposSensor.Valores.Contains(dto.Tipo))
            return BadRequest(new
            {
                mensaje = $"Tipo de sensor inválido. Valores permitidos: {string.Join(", ", TiposSensor.Valores)}."
            });

        sensor.Nombre = dto.Nombre.Trim();
        sensor.Tipo = dto.Tipo;
        sensor.UnidadMedida = dto.UnidadMedida.Trim();
        sensor.Ubicacion = dto.Ubicacion?.Trim();

        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("SENSOR_ACTUALIZADO",
            $"Se editó el sensor '{sensor.Nombre}' (id {sensor.IdSensor}).");

        return NoContent();
    }

    // PATCH api/sensores/5/activar
    [Authorize(Roles = "Administrador,Operador")]
    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> ActivarSensor(int id)
    {
        var sensor = await _db.Sensores.FindAsync(id);
        if (sensor is null)
            return NotFound(new { mensaje = $"No existe un sensor con id {id}." });

        sensor.Activo = true;
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("SENSOR_ACTIVADO",
            $"Se activó el sensor '{sensor.Nombre}' (id {sensor.IdSensor}).");

        return Ok(new { mensaje = $"Sensor '{sensor.Nombre}' activado." });
    }

    // PATCH api/sensores/5/desactivar
    [Authorize(Roles = "Administrador,Operador")]
    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> DesactivarSensor(int id)
    {
        var sensor = await _db.Sensores.FindAsync(id);
        if (sensor is null)
            return NotFound(new { mensaje = $"No existe un sensor con id {id}." });

        sensor.Activo = false;
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("SENSOR_DESACTIVADO",
            $"Se desactivó el sensor '{sensor.Nombre}' (id {sensor.IdSensor}).");

        return Ok(new { mensaje = $"Sensor '{sensor.Nombre}' desactivado." });
    }

    // GET api/sensores/5/lecturas?ultimas=20
    [HttpGet("{id:int}/lecturas")]
    public async Task<ActionResult<IEnumerable<LecturaDto>>> GetLecturas(int id, [FromQuery] int ultimas = 20)
    {
        var existe = await _db.Sensores.AnyAsync(s => s.IdSensor == id);
        if (!existe)
            return NotFound(new { mensaje = $"No existe un sensor con id {id}." });

        var lecturas = await _db.LecturasSensores
            .Where(l => l.IdSensor == id)
            .OrderByDescending(l => l.FechaHora)
            .Take(ultimas)
            .Select(l => new LecturaDto
            {
                IdLectura = l.IdLectura,
                IdSensor = l.IdSensor,
                Valor = l.Valor,
                FechaHora = l.FechaHora
            })
            .ToListAsync();

        return Ok(lecturas);
    }

    // POST api/sensores/5/lecturas
    // Registra una lectura simulada para el sensor (base para la Fase 7: simulación de sensores)
    [Authorize(Roles = "Administrador,Operador")]
    [HttpPost("{id:int}/lecturas")]
    public async Task<ActionResult<LecturaDto>> CrearLectura(int id, [FromBody] CrearLecturaDto dto)
    {
        var sensor = await _db.Sensores.FindAsync(id);
        if (sensor is null)
            return NotFound(new { mensaje = $"No existe un sensor con id {id}." });

        if (!sensor.Activo)
            return BadRequest(new { mensaje = "No se pueden registrar lecturas para un sensor inactivo." });

        var lectura = new LecturasSensore
        {
            IdSensor = id,
            Valor = dto.Valor,
            FechaHora = DateTime.Now
        };

        _db.LecturasSensores.Add(lectura);
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("LECTURA_MANUAL",
            $"Lectura manual de {dto.Valor} {sensor.UnidadMedida} en el sensor '{sensor.Nombre}' (id {id}).");

        // Evaluar automáticamente el riesgo de esta lectura (Fase 8: sistema automático de alertas)
        var evaluacion = MotorAlertas.Evaluar(sensor.Tipo, dto.Valor);

        if (evaluacion.Nivel != "Verde" && evaluacion.TipoFenomeno is not null)
        {
            var alerta = new Alerta
            {
                IdSensor = id,
                Nivel = evaluacion.Nivel,
                TipoFenomeno = evaluacion.TipoFenomeno,
                Mensaje = evaluacion.Mensaje,
                ValorDetectado = dto.Valor,
                FechaHora = DateTime.Now,
                Atendida = false
            };

            _db.Alertas.Add(alerta);
            await _db.SaveChangesAsync();

            _db.HistorialEventos.Add(new HistorialEvento
            {
                IdAlerta = alerta.IdAlerta,
                TipoFenomeno = evaluacion.TipoFenomeno,
                Descripcion = evaluacion.Mensaje,
                FechaHora = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetLecturas), new { id }, new LecturaDto
        {
            IdLectura = lectura.IdLectura,
            IdSensor = lectura.IdSensor,
            Valor = lectura.Valor,
            FechaHora = lectura.FechaHora
        });
    }
}
