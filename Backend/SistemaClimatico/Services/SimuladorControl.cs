namespace SistemaClimatico.Services;

// Estado compartido del simulador de sensores (singleton).
// Permite pausarlo y reanudarlo sin reiniciar la API.
//
// Configuración (appsettings / user-secrets / variables de entorno):
//   Simulador:Habilitado         true  -> arranca activo; false -> arranca en pausa
//   Simulador:IntervaloSegundos  5     -> cada cuántos segundos se genera una lectura por sensor
public class SimuladorControl
{
    private volatile bool _activo;

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
}
