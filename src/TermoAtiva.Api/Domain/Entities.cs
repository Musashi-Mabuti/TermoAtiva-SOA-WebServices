namespace TermoAtiva.Api.Domain;

/// <summary>Leitura enviada por uma estação ESP32 (DHT22 + LDR).</summary>
public sealed record LeituraClimatica(
    long Id,
    string DispositivoId,
    string Bairro,
    double TemperaturaC,
    double UmidadePct,
    int IndiceRadiacao,
    double IndiceCalorC,
    NivelAlerta Nivel,
    DateTime RegistradoEmUtc);

/// <summary>Bebedouro, praça sombreada, UBS ou comércio parceiro.</summary>
public sealed record PontoAcolhimento(
    long Id,
    string Nome,
    string Tipo,
    string Endereco,
    string Bairro,
    double Latitude,
    double Longitude,
    bool Ativo,
    double? DistanciaKm = null);

/// <summary>Aviso de falta de insumo (ex.: bebedouro sem água).</summary>
public sealed record RegistroOcorrencia(
    long Id,
    long PontoId,
    string Categoria,
    string Descricao,
    string Origem,
    string? FotoArquivo,
    DateTime CriadoEmUtc);

public enum NivelAlerta { Normal = 0, Atencao = 1, Perigo = 2 }

public static class Catalogo
{
    public static readonly string[] TiposPonto = ["bebedouro", "sombra", "ubs", "comercio"];
    public static readonly string[] CategoriasOcorrencia = ["sem_agua", "bebedouro_quebrado", "local_fechado", "outro"];
    public static readonly string[] Origens = ["totem", "app"];
}
