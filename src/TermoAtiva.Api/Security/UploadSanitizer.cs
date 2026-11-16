using System.Text.RegularExpressions;

namespace TermoAtiva.Api.Security;

/// <summary>
/// CONTROLE C-03 (anti-LFI / Path Traversal no upload de fotos do app).
/// 1. O nome enviado pelo cliente é DESCARTADO; o servidor gera um GUID.
/// 2. Extensão em allowlist (.jpg/.jpeg/.png) E assinatura binária (magic bytes) conferida.
/// 3. Limite de tamanho (2 MB).
/// 4. Arquivos ficam em App_Data/uploads, fora do wwwroot, sem permissão de execução.
/// 5. Na leitura, o nome precisa ser um GUID e o caminho final precisa estar DENTRO da pasta base.
/// </summary>
public sealed partial class UploadSanitizer(string pastaBase)
{
    public const long TamanhoMaximo = 2 * 1024 * 1024;
    private static readonly string[] ExtensoesPermitidas = [".jpg", ".jpeg", ".png"];

    [GeneratedRegex(@"^[0-9a-f]{32}\.(jpg|png)$")]
    private static partial Regex NomeSeguroRegex();

    public string PastaBase { get; } = Path.GetFullPath(pastaBase);

    public record Resultado(bool Ok, string? NomeArquivo, string? Erro);

    public async Task<Resultado> SalvarAsync(IFormFile arquivo)
    {
        if (arquivo.Length is 0 or > TamanhoMaximo)
            return new(false, null, "Arquivo vazio ou maior que 2 MB.");

        var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!ExtensoesPermitidas.Contains(ext))
            return new(false, null, "Extensão não permitida. Use JPG ou PNG.");

        var cabecalho = new byte[8];
        await using (var s = arquivo.OpenReadStream())
            _ = await s.ReadAsync(cabecalho);

        string? tipoReal = DetectarTipo(cabecalho);
        if (tipoReal is null)
            return new(false, null, "Conteúdo não é uma imagem JPG/PNG válida.");

        // Nome gerado pelo servidor: o "../../web.config" do atacante nunca chega ao disco.
        var nome = $"{Guid.NewGuid():N}.{tipoReal}";
        Directory.CreateDirectory(PastaBase);
        var destino = Path.Combine(PastaBase, nome);

        await using var fs = new FileStream(destino, FileMode.CreateNew, FileAccess.Write);
        await arquivo.CopyToAsync(fs);
        return new(true, nome, null);
    }

    /// <summary>Resolve um nome pedido pelo cliente para um caminho seguro, ou null.</summary>
    public string? ResolverCaminhoSeguro(string nomePedido)
    {
        if (!NomeSeguroRegex().IsMatch(nomePedido)) return null;

        var completo = Path.GetFullPath(Path.Combine(PastaBase, nomePedido));
        var baseComSeparador = PastaBase.EndsWith(Path.DirectorySeparatorChar)
            ? PastaBase : PastaBase + Path.DirectorySeparatorChar;

        if (!completo.StartsWith(baseComSeparador, StringComparison.Ordinal)) return null;
        return File.Exists(completo) ? completo : null;
    }

    private static string? DetectarTipo(byte[] h)
    {
        if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return "jpg";
        if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return "png";
        return null;
    }
}
