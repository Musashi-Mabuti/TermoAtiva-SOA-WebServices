using Microsoft.Data.Sqlite;

namespace TermoAtiva.Api.Data;

/// <summary>Aplica scripts SQL em ordem, em transação, e mantém histórico versionado.</summary>
public static class Migrador
{
    public static void Aplicar(SqliteConnection conn)
    {
        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON";
            pragma.ExecuteNonQuery();
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS SchemaMigrations (Versao TEXT PRIMARY KEY, AplicadaEmUtc TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
        }
        var pasta = Path.Combine(AppContext.BaseDirectory, "Migrations");
        if (!Directory.Exists(pasta)) throw new DirectoryNotFoundException($"Migrações não encontradas: {pasta}");
        foreach (var arquivo in Directory.GetFiles(pasta, "*.sql").OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            var versao = Path.GetFileName(arquivo);
            using var check = conn.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Versao = $v";
            check.Parameters.AddWithValue("$v", versao);
            if (Convert.ToInt64(check.ExecuteScalar()) > 0) continue;
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = File.ReadAllText(arquivo);
                cmd.ExecuteNonQuery();
                using var registro = conn.CreateCommand();
                registro.Transaction = tx;
                registro.CommandText = "INSERT INTO SchemaMigrations(Versao, AplicadaEmUtc) VALUES ($v, $utc)";
                registro.Parameters.AddWithValue("$v", versao);
                registro.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("O"));
                registro.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }
}
