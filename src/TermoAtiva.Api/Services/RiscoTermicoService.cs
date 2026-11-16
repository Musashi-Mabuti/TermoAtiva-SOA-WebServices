using TermoAtiva.Api.Domain;

namespace TermoAtiva.Api.Services;

/// <summary>
/// Índice de Calor (Heat Index) pela regressão de Rothfusz / NOAA.
/// Classificação ambiental (sem diagnóstico de saúde individual).
/// </summary>
public static class RiscoTermicoService
{
    public static double IndiceCalor(double tempC, double umidade)
    {
        double t = tempC * 9 / 5 + 32;
        double hi = 0.5 * (t + 61.0 + (t - 68.0) * 1.2 + umidade * 0.094);
        if ((hi + t) / 2 >= 80)
        {
            hi = -42.379 + 2.04901523 * t + 10.14333127 * umidade
                 - 0.22475541 * t * umidade - 0.00683783 * t * t
                 - 0.05481717 * umidade * umidade + 0.00122874 * t * t * umidade
                 + 0.00085282 * t * umidade * umidade - 0.00000199 * t * t * umidade * umidade;
        }
        return Math.Round((hi - 32) * 5 / 9, 1);
    }

    public static NivelAlerta Classificar(double indiceCalorC, int indiceRadiacao) =>
        indiceCalorC >= 41 || (indiceCalorC >= 35 && indiceRadiacao >= 8) ? NivelAlerta.Perigo
        : indiceCalorC >= 32 ? NivelAlerta.Atencao
        : NivelAlerta.Normal;

    public static string Orientacao(NivelAlerta n) => n switch
    {
        NivelAlerta.Perigo => "Perigo térmico: procure sombra e água agora e faça pausas a cada 30 minutos.",
        NivelAlerta.Atencao => "Atenção: hidrate-se a cada 20 minutos e evite sol direto entre 11h e 16h.",
        _ => "Condição normal: mantenha a hidratação."
    };
}
