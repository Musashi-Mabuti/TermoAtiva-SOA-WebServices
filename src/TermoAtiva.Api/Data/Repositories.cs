using Microsoft.Data.Sqlite;
using TermoAtiva.Api.Domain;

namespace TermoAtiva.Api.Data;

/// <summary>
/// CONTROLE C-01 (anti-SQL Injection): todas as consultas usam Prepared Statements.
/// Nenhum valor vindo do usuário é concatenado ao texto SQL; ele trafega
/// separado, como parâmetro ($nome), e o SQLite o trata sempre como DADO.
/// </summary>
public sealed class PontoRepository(Database db)
{
    public IReadOnlyList<PontoAcolhimento> Buscar(string? tipo, string? termo)
    {
        using var conn = db.Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Nome, Tipo, Endereco, Bairro, Latitude, Longitude, Ativo
            FROM PontosAcolhimento
            WHERE Ativo = 1
              AND ($tipo IS NULL OR Tipo = $tipo)
              AND ($termo IS NULL OR Nome LIKE '%' || $termo || '%' OR Bairro LIKE '%' || $termo || '%')
            ORDER BY Nome
            """;
        cmd.Parameters.AddWithValue("$tipo", (object?)tipo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$termo", (object?)termo ?? DBNull.Value);

        var lista = new List<PontoAcolhimento>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new PontoAcolhimento(
                r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
                r.GetDouble(5), r.GetDouble(6), r.GetInt64(7) == 1));
        return lista;
    }

    public bool Existe(long id)
    {
        using var conn = db.Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM PontosAcolhimento WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }
}

public sealed class LeituraRepository(Database db)
{
    public long Inserir(LeituraClimatica l)
    {
        using var conn = db.Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO LeiturasClimaticas
              (DispositivoId, Bairro, TemperaturaC, UmidadePct, IndiceRadiacao, IndiceCalorC, Nivel, RegistradoEmUtc)
            VALUES ($disp, $bairro, $temp, $umid, $rad, $ic, $nivel, $quando)
            """;
        cmd.Parameters.AddWithValue("$disp", l.DispositivoId);
        cmd.Parameters.AddWithValue("$bairro", l.Bairro);
        cmd.Parameters.AddWithValue("$temp", l.TemperaturaC);
        cmd.Parameters.AddWithValue("$umid", l.UmidadePct);
        cmd.Parameters.AddWithValue("$rad", l.IndiceRadiacao);
        cmd.Parameters.AddWithValue("$ic", l.IndiceCalorC);
        cmd.Parameters.AddWithValue("$nivel", (int)l.Nivel);
        cmd.Parameters.AddWithValue("$quando", l.RegistradoEmUtc.ToString("O"));
        cmd.ExecuteNonQuery();

        using var id = conn.CreateCommand();
        id.CommandText = "SELECT last_insert_rowid()";
        return Convert.ToInt64(id.ExecuteScalar());
    }

    public LeituraClimatica? UltimaDoBairro(string bairro)
    {
        using var conn = db.Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, DispositivoId, Bairro, TemperaturaC, UmidadePct, IndiceRadiacao, IndiceCalorC, Nivel, RegistradoEmUtc
            FROM LeiturasClimaticas
            WHERE Bairro = $bairro
            ORDER BY Id DESC LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$bairro", bairro);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new LeituraClimatica(
            r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetDouble(3), r.GetDouble(4),
            (int)r.GetInt64(5), r.GetDouble(6), (NivelAlerta)r.GetInt64(7),
            DateTime.Parse(r.GetString(8), null, System.Globalization.DateTimeStyles.RoundtripKind));
    }
}

public sealed class OcorrenciaRepository(Database db)
{
    public long Inserir(long pontoId, string categoria, string descricao, string origem)
    {
        using var conn = db.Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO RegistrosOcorrencia (PontoId, Categoria, Descricao, Origem, FotoArquivo, CriadoEmUtc)
            VALUES ($ponto, $cat, $desc, $origem, NULL, $quando)
            """;
        cmd.Parameters.AddWithValue("$ponto", pontoId);
        cmd.Parameters.AddWithValue("$cat", categoria);
        cmd.Parameters.AddWithValue("$desc", descricao);
        cmd.Parameters.AddWithValue("$origem", origem);
        cmd.Parameters.AddWithValue("$quando", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        using var id = conn.CreateCommand();
        id.CommandText = "SELECT last_insert_rowid()";
        return Convert.ToInt64(id.ExecuteScalar());
    }

    public RegistroOcorrencia? Obter(long id)
    {
        using var conn = db.Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, PontoId, Categoria, Descricao, Origem, FotoArquivo, CriadoEmUtc FROM RegistrosOcorrencia WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new RegistroOcorrencia(r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetString(4),
            r.IsDBNull(5) ? null : r.GetString(5), DateTime.Parse(r.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    public bool AtualizarDescricao(long id, string descricao)
    {
        using var conn = db.Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE RegistrosOcorrencia SET Descricao = $descricao WHERE Id = $id";
        cmd.Parameters.AddWithValue("$descricao", descricao);
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() == 1;
    }

    public bool AnexarFoto(long id, string arquivo)
    {
        using var conn = db.Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE RegistrosOcorrencia SET FotoArquivo = $arq WHERE Id = $id";
        cmd.Parameters.AddWithValue("$arq", arquivo);
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() == 1;
    }
}
