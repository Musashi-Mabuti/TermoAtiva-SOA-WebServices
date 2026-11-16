using Microsoft.Data.Sqlite;

namespace TermoAtiva.Api.Data;

/// <summary>
/// Fábrica de conexões. O arquivo fica em App_Data (fora do wwwroot) e,
/// no IIS, só a identidade do Application Pool tem permissão de escrita nele.
/// </summary>
public sealed class Database(string caminhoArquivo)
{
    public string CaminhoArquivo { get; } = caminhoArquivo;

    public SqliteConnection Abrir()
    {
        var conn = new SqliteConnection($"Data Source={CaminhoArquivo}");
        conn.Open();
        return conn;
    }

    public void Inicializar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CaminhoArquivo)!);
        using var conn = Abrir();

        // Migrações versionadas, aplicadas uma única vez e registradas em SchemaMigrations.
        Migrador.Aplicar(conn);

        using var check = conn.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM PontosAcolhimento";
        if (Convert.ToInt64(check.ExecuteScalar()) > 0) return;

        // Pontos de exemplo na região Sé / Brás (São Paulo - SP)
        (string nome, string tipo, string end, string bairro, double lat, double lon)[] seed =
        [
            ("Bebedouro Praça da Sé", "bebedouro", "Praça da Sé, s/n", "Sé", -23.5503, -46.6339),
            ("Sombra Pátio do Colégio", "sombra", "Pátio do Colégio, 2", "Sé", -23.5477, -46.6326),
            ("UBS Sé", "ubs", "R. Frederico Alvarenga, 259", "Sé", -23.5512, -46.6290),
            ("Padaria Parceira Brás", "comercio", "R. do Gasômetro, 450", "Brás", -23.5455, -46.6198),
            ("Bebedouro Largo da Concórdia", "bebedouro", "Largo da Concórdia, s/n", "Brás", -23.5413, -46.6155),
            ("Praça Sombreada Brás", "sombra", "Praça Agente Cícero, s/n", "Brás", -23.5432, -46.6170)
        ];
        foreach (var p in seed)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO PontosAcolhimento (Nome, Tipo, Endereco, Bairro, Latitude, Longitude, Ativo)
                VALUES ($nome, $tipo, $end, $bairro, $lat, $lon, 1)
                """;
            cmd.Parameters.AddWithValue("$nome", p.nome);
            cmd.Parameters.AddWithValue("$tipo", p.tipo);
            cmd.Parameters.AddWithValue("$end", p.end);
            cmd.Parameters.AddWithValue("$bairro", p.bairro);
            cmd.Parameters.AddWithValue("$lat", p.lat);
            cmd.Parameters.AddWithValue("$lon", p.lon);
            cmd.ExecuteNonQuery();
        }

        // Contatos fictícios — usados para provar que SQLi não consegue vazá-los
        Executar(conn, """
            INSERT INTO ParceirosContato (PontoId, Responsavel, Telefone, Email)
            VALUES (4, 'Responsável Fictício', '(11) 90000-0000', 'parceiro@exemplo.org')
            """);
    }

    private static void Executar(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
