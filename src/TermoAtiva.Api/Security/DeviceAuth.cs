using System.Security.Cryptography;
using System.Text;

namespace TermoAtiva.Api.Security;

/// <summary>
/// CONTROLE C-04 (integridade da telemetria): cada totem ESP32 tem uma chave própria.
/// A API guarda apenas o SHA-256 da chave e compara em tempo constante,
/// impedindo que bots injetem leituras falsas de calor.
/// </summary>
public sealed class DeviceAuth(IConfiguration config)
{
    public bool Autorizado(string dispositivoId, string? chaveRecebida)
    {
        if (string.IsNullOrEmpty(chaveRecebida)) return false;
        var hashEsperadoHex = config[$"Dispositivos:{dispositivoId}"];
        if (string.IsNullOrEmpty(hashEsperadoHex)) return false;

        var hashRecebido = SHA256.HashData(Encoding.UTF8.GetBytes(chaveRecebida));
        byte[] hashEsperado;
        try { hashEsperado = Convert.FromHexString(hashEsperadoHex); }
        catch (FormatException) { return false; }

        return CryptographicOperations.FixedTimeEquals(hashRecebido, hashEsperado);
    }
}
