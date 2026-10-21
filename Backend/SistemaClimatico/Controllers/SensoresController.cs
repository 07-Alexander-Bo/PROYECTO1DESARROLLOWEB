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
    private readonly ProcesadorLecturas _procesador;
    private readonly NotificadorTiempoReal _notificador;
    private readonly SimuladorControl _simulador;

    public SensoresController(
        SistemaClimaticoContext db,
        BitacoraService bitacora,
        ProcesadorLecturas procesador,
        NotificadorTiempoReal notificador,
        SimuladorControl simulador)
    {
        _db = db;
        _bitacora = bitacora;
        _procesador = procesador;
        _notificador = notificador;
        _simulador = simulador;
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
                FechaCreacion = s.FechaCreacion,
                IdComunidad = s.IdComunidad,
                NombreComunidad = s.IdComunidadNavigation != null ? s.IdComunidadNavigation.Nombre : null,
                Latitud = s.Latitud,
                Longitud = s.Longitud
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

        return Ok(await MapearAsync(sensor));
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

        var errorUbicacion = await ValidarUbicacionAsync(dto.IdComunidad, dto.Latitud, dto.Longitud);
        if (errorUbicacion is not null)
            return BadRequest(new { mensaje = errorUbicacion });

        var sensor = new Sensore
        {
            IdComunidad = dto.IdComunidad,
            Latitud = dto.Latitud,
            Longitud = dto.Longitud,
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
        await _notificador.SensoresActualizadosAsync();

        var resultado = await MapearAsync(sensor);

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

        if (string.IsNullOrWhiteSpace(dto.UnidadMedida))
            return BadRequest(new { mensaje = "La unidad de medida es obligatoria." });

        var errorUbicacion = await ValidarUbicacionAsync(dto.IdComunidad, dto.Latitud, dto.Longitud);
        if (errorUbicacion is not null)
            return BadRequest(new { mensaje = errorUbicacion });

        sensor.Nombre = dto.Nombre.Trim();
        sensor.Tipo = dto.Tipo;
        sensor.UnidadMedida = dto.UnidadMedida.Trim();
        sensor.Ubicacion = dto.Ubicacion?.Trim();
        sensor.IdComunidad = dto.IdComunidad;
        sensor.Latitud = dto.Latitud;
        sensor.Longitud = dto.Longitud;

        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("SENSOR_ACTUALIZADO",
            $"Se editó el sensor '{sensor.Nombre}' (id {sensor.IdSensor}).");
        await _notificador.SensoresActualizadosAsync();

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
        await _notificador.SensoresActualizadosAsync();

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
        await _notificador.SensoresActualizadosAsync();

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

        // Guardar, evaluar, generar alerta (sin duplicados) y avisar en tiempo real:
        // es el mismo camino que usa el simulador (Services/ProcesadorLecturas.cs).
        var resultado = await _procesador.RegistrarLecturaAsync(sensor, dto.Valor);
        var lectura = resultado.Lectura;

        // El simulador continúa desde el valor que fijó la persona.
        _simulador.ForzarValor(id, lectura.Valor);

        await _bitacora.RegistrarAsync("LECTURA_MANUAL",
            $"Lectura manual de {lectura.Valor} {sensor.UnidadMedida} en el sensor '{sensor.Nombre}' (id {id}).");

        return CreatedAtAction(nameof(GetLecturas), new { id }, new LecturaDto
        {
            IdLectura = lectura.IdLectura,
            IdSensor = lectura.IdSensor,
            Valor = lectura.Valor,
            FechaHora = lectura.FechaHora
        });
    }

    private async Task<SensorDto> MapearAsync(Sensore s)
    {
        string? nombreComunidad = null;
        if (s.IdComunidad.HasValue)
        {
            nombreComunidad = await _db.Comunidades
                .Where(c => c.IdComunidad == s.IdComunidad.Value)
                .Select(c => c.Nombre)
                .FirstOrDefaultAsync();
        }

        return new SensorDto
        {
            IdSensor = s.IdSensor,
            Nombre = s.Nombre,
            Tipo = s.Tipo,
            UnidadMedida = s.UnidadMedida,
            Ubicacion = s.Ubicacion,
            Activo = s.Activo,
            FechaCreacion = s.FechaCreacion,
            IdComunidad = s.IdComunidad,
            NombreComunidad = nombreComunidad,
            Latitud = s.Latitud,
            Longitud = s.Longitud
        };
    }

    // Devuelve un mensaje de error, o null si la comunidad y las coordenadas son válidas.
    private async Task<string?> ValidarUbicacionAsync(int? idComunidad, decimal? latitud, decimal? longitud)
    {
        if (idComunidad.HasValue && !await _db.Comunidades.AnyAsync(c => c.IdComunidad == idComunidad.Value))
            return "La comunidad indicada no existe.";

        if (latitud.HasValue != longitud.HasValue)
            return "Indica latitud y longitud juntas, o deja ambas vacías.";

        if (latitud is < -90 or > 90)
            return "La latitud debe estar entre -90 y 90.";

        if (longitud is < -180 or > 180)
            return "La longitud debe estar entre -180 y 180.";

        return null;
    }
}
