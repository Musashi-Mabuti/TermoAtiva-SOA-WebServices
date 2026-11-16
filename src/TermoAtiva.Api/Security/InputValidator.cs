using System.Text.RegularExpressions;

namespace TermoAtiva.Api.Security;

/// <summary>
/// CONTROLE C-02 (validação e sanitização de entrada): lista de permissões (allowlist).
/// O que não casa com o formato esperado é rejeitado com HTTP 400 antes de chegar ao banco.
/// </summary>
public static partial class InputValidator
{
    // Letras (inclui acentos), dígitos, espaço, hífen e ponto. Máx. 60 caracteres.
    [GeneratedRegex(@"^[\p{L}\p{N} .\-]{1,60}$")]
    private static partial Regex TermoBuscaRegex();

    // Ex.: ESP32-SE-01
    [GeneratedRegex(@"^ESP32-[A-Z]{2,4}-\d{2}$")]
    private static partial Regex DispositivoRegex();

    [GeneratedRegex(@"[\u0000-\u001F\u007F<>]")]
    private static partial Regex CaracteresPerigososRegex();

    public static bool TermoBuscaValido(string? termo) => termo is null || TermoBuscaRegex().IsMatch(termo);

    public static bool DispositivoValido(string? id) => id is not null && DispositivoRegex().IsMatch(id);

    public static bool CoordenadaValida(double lat, double lon) =>
        lat is >= -90 and <= 90 && lon is >= -180 and <= 180;

    public static bool NoCatalogo(string? valor, string[] catalogo) =>
        valor is not null && catalogo.Contains(valor, StringComparer.Ordinal);

    /// <summary>Texto livre: remove controle/HTML, corta espaços e limita o tamanho.</summary>
    public static string? TextoLivre(string? texto, int max)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var limpo = CaracteresPerigososRegex().Replace(texto, "").Trim();
        return limpo.Length is 0 or > 500 ? null : limpo.Length > max ? limpo[..max] : limpo;
    }
}
