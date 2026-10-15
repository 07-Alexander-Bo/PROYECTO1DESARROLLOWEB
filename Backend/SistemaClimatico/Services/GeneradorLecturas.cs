namespace SistemaClimatico.Services;

// Estado interno de la simulación de UN sensor (vive en memoria).
public class EstadoSimulacion
{
    public decimal Actual { get; set; }

    // Si no es null hay un "evento climático" en curso: el valor se dirige hacia
    // este objetivo (por ejemplo, 85 km/h de viento) durante varias lecturas.
    public double? ObjetivoEvento { get; set; }
    public int TicksRestantes { get; set; }
}

// Genera valores creíbles para cada tipo de sensor.
//
// Funcionamiento: el valor da pasos pequeños y aleatorios alrededor de un valor
// "normal" (clima tranquilo). De vez en cuando (probabilidadEvento) arranca un
// evento: durante 10 a 20 lecturas el valor sube o baja hacia una zona de riesgo
// y después regresa solo a la normalidad. Así el dashboard recorre
// Verde -> Amarillo -> Naranja -> Rojo de forma gradual, como en la vida real.
//
// No usa base de datos: solo recibe y modifica un EstadoSimulacion.
public class GeneradorLecturas
{
    private readonly Random _rng;
    private readonly double _probabilidadEvento;

    public GeneradorLecturas(double probabilidadEvento, Random? rng = null)
    {
        _probabilidadEvento = Math.Clamp(probabilidadEvento, 0, 1);
        _rng = rng ?? new Random();
    }

    private sealed record Perfil(
        double Normal, double Min, double Max, double Ruido,
        Func<Random, double>? ObjetivoRiesgo);

    private static Perfil ObtenerPerfil(string tipo) => tipo switch
    {
        // Temperatura (°C): el evento puede ser calor extremo o helada.
        "Temperatura" => new Perfil(22, -5, 50, 0.4,
            r => r.NextDouble() < 0.5 ? Entre(r, 37, 48) : Entre(r, -2, 4)),

        // Humedad relativa (%): el riesgo es que baje mucho (sequía).
        "Humedad" => new Perfil(60, 0, 100, 1.2, r => Entre(r, 4, 28)),

        // Viento (km/h)
        "Viento" => new Perfil(12, 0, 130, 2.0, r => Entre(r, 42, 100)),

        // Lluvia (mm)
        "Lluvia" => new Perfil(1.5, 0, 200, 0.8, r => Entre(r, 22, 120)),

        // Nivel de río o reservorio (m)
        "NivelRio" => new Perfil(1.0, 0, 8, 0.03, r => Entre(r, 2.1, 4.6)),

        // Cualquier otro tipo: varía suave y nunca dispara alertas.
        _ => new Perfil(50, 0, 100, 1.0, null)
    };

    private static double Entre(Random r, double min, double max) => min + r.NextDouble() * (max - min);

    // Valor con el que empieza un sensor que todavía no tiene lecturas.
    public decimal ValorInicial(string tipo) => (decimal)ObtenerPerfil(tipo).Normal;

    // Calcula la siguiente lectura y actualiza el estado.
    public decimal Siguiente(string tipo, EstadoSimulacion estado)
    {
        var perfil = ObtenerPerfil(tipo);

        // ¿Arranca un evento nuevo?
        if (estado.ObjetivoEvento is null
            && perfil.ObjetivoRiesgo is not null
            && _rng.NextDouble() < _probabilidadEvento)
        {
            estado.ObjetivoEvento = perfil.ObjetivoRiesgo(_rng);
            estado.TicksRestantes = _rng.Next(10, 21);
        }

        double objetivo;
        double fuerza; // qué tan rápido se acerca al objetivo en cada paso

        if (estado.ObjetivoEvento is double objetivoEvento)
        {
            objetivo = objetivoEvento;
            fuerza = 0.28;
            estado.TicksRestantes--;
            if (estado.TicksRestantes <= 0)
                estado.ObjetivoEvento = null; // termina el evento: vuelve a la normalidad
        }
        else
        {
            objetivo = perfil.Normal;
            fuerza = 0.08;
        }

        var actual = (double)estado.Actual;
        var ruido = (_rng.NextDouble() * 2 - 1) * perfil.Ruido;
        var siguiente = actual + (objetivo - actual) * fuerza + ruido;
        siguiente = Math.Clamp(siguiente, perfil.Min, perfil.Max);

        var resultado = Math.Round((decimal)siguiente, 2);
        estado.Actual = resultado;
        return resultado;
    }
}
