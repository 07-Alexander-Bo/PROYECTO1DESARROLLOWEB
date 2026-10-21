using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Dtos;
using SistemaClimatico.Models;

namespace SistemaClimatico.Services;

public record ResultadoLectura(LecturasSensore Lectura, ResultadoEvaluacion Evaluacion, Alerta? AlertaCreada);

// ÚNICO camino para registrar una lectura, venga del simulador o de una persona
// (SensoresController). Así las dos fuentes se comportan exactamente igual:
//
//   1. Guarda la lectura.
//   2. La evalúa con el MotorAlertas (Verde / Amarillo / Naranja / Rojo).
//   3. Si el riesgo EMPEORA y no es un duplicado (PoliticaAlertas), crea la
//      alerta y su evento de historial.
//   4. Envía la lectura (y la alerta, si hubo) a todos los navegadores por SignalR.
public class ProcesadorLecturas
{
    private readonly SistemaClimaticoContext _db;
    private readonly NotificadorTiempoReal _notificador;
    private readonly int _cooldownMinutos;

    public ProcesadorLecturas(SistemaClimaticoContext db, NotificadorTiempoReal notificador, IConfiguration config)
    {
        _db = db;
        _notificador = notificador;
        _cooldownMinutos = Math.Max(0, config.GetValue("Alertas:CooldownMinutos", 10));
    }

    public async Task<ResultadoLectura> RegistrarLecturaAsync(
        Sensore sensor, decimal valor, CancellationToken ct = default)
    {
        // La columna guarda 2 decimales: se redondea antes para evaluar
        // exactamente lo mismo que queda almacenado.
        valor = Math.Round(valor, 2);

        // Lectura anterior del mismo sensor, para saber si la situación empeoró.
        var valorAnterior = await _db.LecturasSensores
            .Where(l => l.IdSensor == sensor.IdSensor)
            .OrderByDescending(l => l.IdLectura)
            .Select(l => (decimal?)l.Valor)
            .FirstOrDefaultAsync(ct);

        var lectura = new LecturasSensore
        {
            IdSensor = sensor.IdSensor,
            Valor = valor,
            FechaHora = DateTime.Now
        };
        _db.LecturasSensores.Add(lectura);
        await _db.SaveChangesAsync(ct);

        var evaluacion = MotorAlertas.Evaluar(sensor.Tipo, valor);
        Alerta? alerta = null;

        if (evaluacion.Nivel != "Verde" && evaluacion.TipoFenomeno is not null)
        {
            var nivelAnterior = valorAnterior.HasValue
                ? MotorAlertas.Evaluar(sensor.Tipo, valorAnterior.Value).Nivel
                : "Verde";

            var empeora = PoliticaAlertas.Empeora(nivelAnterior, evaluacion.Nivel);
            var hayAlertaReciente = empeora
                && await HayAlertaRecienteAsync(sensor.IdSensor, evaluacion.TipoFenomeno, evaluacion.Nivel, ct);

            if (PoliticaAlertas.DebeGenerarAlerta(nivelAnterior, evaluacion.Nivel, hayAlertaReciente))
            {
                alerta = new Alerta
                {
                    IdSensor = sensor.IdSensor,
                    Nivel = evaluacion.Nivel,
                    TipoFenomeno = evaluacion.TipoFenomeno,
                    Mensaje = evaluacion.Mensaje,
                    ValorDetectado = valor,
                    FechaHora = DateTime.Now,
                    Atendida = false
                };
                _db.Alertas.Add(alerta);
                await _db.SaveChangesAsync(ct);

                _db.HistorialEventos.Add(new HistorialEvento
                {
                    IdAlerta = alerta.IdAlerta,
                    TipoFenomeno = evaluacion.TipoFenomeno,
                    Descripcion = evaluacion.Mensaje,
                    FechaHora = DateTime.Now
                });
                await _db.SaveChangesAsync(ct);
            }
        }

        // Tiempo real: primero la lectura, luego la alerta (si la hubo).
        await _notificador.LecturaAsync(new EstadoSensorDto
        {
            IdSensor = sensor.IdSensor,
            NombreSensor = sensor.Nombre,
            TipoSensor = sensor.Tipo,
            UnidadMedida = sensor.UnidadMedida,
            UltimoValor = valor,
            FechaUltimaLectura = lectura.FechaHora,
            Nivel = evaluacion.Nivel,
            TipoFenomeno = evaluacion.TipoFenomeno,
            Mensaje = evaluacion.Mensaje,
            IdComunidad = sensor.IdComunidad,
            Latitud = sensor.Latitud,
            Longitud = sensor.Longitud
        });

        if (alerta is not null)
        {
            await _notificador.AlertaNuevaAsync(new AlertaDto
            {
                IdAlerta = alerta.IdAlerta,
                IdSensor = alerta.IdSensor,
                NombreSensor = sensor.Nombre,
                Nivel = alerta.Nivel,
                TipoFenomeno = alerta.TipoFenomeno,
                Mensaje = alerta.Mensaje,
                ValorDetectado = alerta.ValorDetectado,
                FechaHora = alerta.FechaHora,
                Atendida = alerta.Atendida
            });
        }

        return new ResultadoLectura(lectura, evaluacion, alerta);
    }

    // ¿Ya hubo una alerta igual (sensor + fenómeno + nivel) dentro del tiempo de anti-rebote?
    private Task<bool> HayAlertaRecienteAsync(int idSensor, string tipoFenomeno, string nivel, CancellationToken ct)
    {
        var desde = DateTime.Now.AddMinutes(-_cooldownMinutos);

        return _db.Alertas.AnyAsync(a =>
            a.IdSensor == idSensor
            && a.TipoFenomeno == tipoFenomeno
            && a.Nivel == nivel
            && a.FechaHora >= desde, ct);
    }
}
