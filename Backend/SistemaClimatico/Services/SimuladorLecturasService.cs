using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Models;

namespace SistemaClimatico.Services;

// Servicio en segundo plano que simula los sensores: cada N segundos genera una
// lectura para cada sensor ACTIVO y la manda por el mismo camino que una lectura
// manual (ProcesadorLecturas): se guarda, se evalúa, genera alertas y se envía
// por SignalR a los navegadores.
//
// Se puede pausar/reanudar desde la API (SimuladorControl) sin reiniciar nada.
public class SimuladorLecturasService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SimuladorControl _control;
    private readonly ILogger<SimuladorLecturasService> _logger;
    private readonly GeneradorLecturas _generador;

    // Estado de simulación por sensor (id -> valor actual y evento en curso).
    private readonly Dictionary<int, EstadoSimulacion> _estados = new();
    private int _generacionVista;

    public SimuladorLecturasService(
        IServiceScopeFactory scopeFactory,
        SimuladorControl control,
        IConfiguration config,
        ILogger<SimuladorLecturasService> logger)
    {
        _scopeFactory = scopeFactory;
        _control = control;
        _logger = logger;
        _generador = new GeneradorLecturas(config.GetValue("Simulador:ProbabilidadEvento", 0.004));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Pequeña espera para que la API termine de arrancar (y los seeders de datos).
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            _logger.LogInformation(
                "Simulador de sensores listo: una lectura cada {Intervalo} s por sensor activo. Estado inicial: {Estado}.",
                _control.IntervaloSegundos, _control.Activo ? "activo" : "en pausa");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_control.IntervaloSegundos));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (!_control.Activo) continue;

                try
                {
                    // El candado evita escribir lecturas mientras se reinicia el monitoreo.
                    await _control.Candado.WaitAsync(stoppingToken);
                    try { await GenerarCicloAsync(stoppingToken); }
                    finally { _control.Candado.Release(); }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Por ejemplo, SQL Server caído un momento: se avisa y se sigue intentando.
                    _logger.LogWarning("Simulador: no se pudo generar el ciclo de lecturas: {Mensaje}", ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // La API se está apagando: salida normal.
        }
    }

    private async Task GenerarCicloAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SistemaClimaticoContext>();
        var procesador = scope.ServiceProvider.GetRequiredService<ProcesadorLecturas>();

        // Si se reinició el monitoreo, se olvida el estado anterior de cada sensor.
        if (_generacionVista != _control.Generacion)
        {
            _estados.Clear();
            _generacionVista = _control.Generacion;
        }

        var sensores = await db.Sensores
            .Where(s => s.Activo)
            .OrderBy(s => s.IdSensor)
            .ToListAsync(ct);

        foreach (var sensor in sensores)
        {
            if (!_control.Activo) break; // pausado a mitad del ciclo

            var estado = await ObtenerEstadoAsync(db, sensor, ct);

            // Valor fijado a mano: la simulación sigue desde ese punto.
            if (_control.TomarValorForzado(sensor.IdSensor, out var forzado))
            {
                estado.Actual = forzado;
                estado.ObjetivoEvento = null;
                estado.TicksRestantes = 0;
            }

            var valor = _generador.Siguiente(sensor.Tipo, estado);

            await procesador.RegistrarLecturaAsync(sensor, valor, ct);
        }
    }

    // Si el sensor ya tiene lecturas (por ejemplo, después de reiniciar la API),
    // la simulación continúa desde su último valor en vez de volver a empezar.
    private async Task<EstadoSimulacion> ObtenerEstadoAsync(SistemaClimaticoContext db, Sensore sensor, CancellationToken ct)
    {
        if (_estados.TryGetValue(sensor.IdSensor, out var existente))
            return existente;

        var ultimo = await db.LecturasSensores
            .Where(l => l.IdSensor == sensor.IdSensor)
            .OrderByDescending(l => l.IdLectura)
            .Select(l => (decimal?)l.Valor)
            .FirstOrDefaultAsync(ct);

        var nuevo = new EstadoSimulacion { Actual = ultimo ?? _generador.ValorInicial(sensor.Tipo) };
        _estados[sensor.IdSensor] = nuevo;
        return nuevo;
    }
}
