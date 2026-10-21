using System.Collections.Concurrent;

namespace SistemaClimatico.Services;

// Estado compartido del simulador de sensores (singleton).
// Permite pausarlo y reanudarlo sin reiniciar la API, fijar el valor de un sensor
// a mano y reiniciar todo el monitoreo.
//
// Configuración (appsettings / user-secrets / variables de entorno):
//   Simulador:Habilitado         true  -> arranca activo; false -> arranca en pausa
//   Simulador:IntervaloSegundos  5     -> cada cuántos segundos se genera una lectura por sensor
public class SimuladorControl
{
    private volatile bool _activo;
    private int _generacion;
    private readonly ConcurrentDictionary<int, decimal> _valoresForzados = new();

    public SimuladorControl(IConfiguration config)
    {
        _activo = config.GetValue("Simulador:Habilitado", true);
        IntervaloSegundos = Math.Max(1, config.GetValue("Simulador:IntervaloSegundos", 5.0));
    }

    public bool Activo
    {
        get => _activo;
        set => _activo = value;
    }

    public double IntervaloSegundos { get; }

    // Evita que un ciclo del simulador escriba lecturas mientras se reinicia el monitoreo.
    public SemaphoreSlim Candado { get; } = new(1, 1);

    // Sube cada vez que se reinicia el monitoreo: el simulador descarta su estado interno.
    public int Generacion => Volatile.Read(ref _generacion);

    // Una persona fijó el valor de un sensor: la simulación continúa desde ahí
    // (y vuelve poco a poco a su rango normal) en vez de ignorar el cambio.
    public void ForzarValor(int idSensor, decimal valor) => _valoresForzados[idSensor] = valor;

    public bool TomarValorForzado(int idSensor, out decimal valor) => _valoresForzados.TryRemove(idSensor, out valor);

    public void Reiniciar()
    {
        _valoresForzados.Clear();
        Interlocked.Increment(ref _generacion);
    }
}
