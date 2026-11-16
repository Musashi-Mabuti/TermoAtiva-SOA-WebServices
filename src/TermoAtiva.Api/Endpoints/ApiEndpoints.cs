using TermoAtiva.Api.Data;
using TermoAtiva.Api.Domain;
using TermoAtiva.Api.Security;
using TermoAtiva.Api.Services;

namespace TermoAtiva.Api.Endpoints;

public sealed record TelemetriaDto(string? DispositivoId, string? Bairro, double TemperaturaC, double UmidadePct, int IndiceRadiacao);
public sealed record OcorrenciaDto(long PontoId, string? Categoria, string? Descricao, string? Origem);

public sealed record AtualizarOcorrenciaDto(string? Descricao);

public static class ApiEndpoints
{
    public static void MapTermoAtiva(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireRateLimiting("geral");

        // POST /api/telemetria — leituras dos totens ESP32 (autenticadas por chave do dispositivo)
        api.MapPost("/telemetria", (TelemetriaDto dto, HttpRequest req, DeviceAuth auth,
                                     LeituraRepository repo, ILogger<Program> log) =>
        {
            if (!InputValidator.DispositivoValido(dto.DispositivoId))
                return Results.Problem("dispositivoId inválido.", statusCode: 400);

            if (!auth.Autorizado(dto.DispositivoId!, req.Headers["X-Device-Key"]))
            {
                log.LogWarning("SEGURANCA telemetria rejeitada: chave invalida para {Disp} de {Ip}",
                    dto.DispositivoId, req.HttpContext.Connection.RemoteIpAddress);
                return Results.Problem("Dispositivo não autorizado.", statusCode: 401);
            }

            // Plausibilidade física: bloqueia leituras adulteradas (ex.: 95 °C no centro de SP)
            if (dto.TemperaturaC is < -10 or > 60 || dto.UmidadePct is < 0 or > 100 || dto.IndiceRadiacao is < 0 or > 15)
            {
                log.LogWarning("SEGURANCA telemetria fora da faixa fisica de {Disp}: T={T} U={U}",
                    dto.DispositivoId, dto.TemperaturaC, dto.UmidadePct);
                return Results.Problem("Leitura fora da faixa física aceitável.", statusCode: 400);
            }

            var bairro = InputValidator.TextoLivre(dto.Bairro, 40);
            if (bairro is null || !InputValidator.TermoBuscaValido(bairro))
                return Results.Problem("bairro inválido.", statusCode: 400);

            var ic = RiscoTermicoService.IndiceCalor(dto.TemperaturaC, dto.UmidadePct);
            var nivel = RiscoTermicoService.Classificar(ic, dto.IndiceRadiacao);
            var leitura = new LeituraClimatica(0, dto.DispositivoId!, bairro, dto.TemperaturaC, dto.UmidadePct,
                                               dto.IndiceRadiacao, ic, nivel, DateTime.UtcNow);
            var id = repo.Inserir(leitura);
            return Results.Created($"/api/telemetria/{id}", leitura with { Id = id });
        });

        // GET /api/pontos-acolhimento?lat=&lon=&raioKm=&tipo=&busca=
        api.MapGet("/pontos-acolhimento", (double? lat, double? lon, double? raioKm, string? tipo, string? busca,
                                           PontoRepository repo, HttpContext ctx, ILogger<Program> log) =>
        {
            if (!InputValidator.TermoBuscaValido(busca))
            {
                log.LogWarning("SEGURANCA busca rejeitada pela allowlist de {Ip}: {Busca}",
                    ctx.Connection.RemoteIpAddress, busca);
                return Results.Problem("Parâmetro 'busca' contém caracteres não permitidos.", statusCode: 400);
            }
            if (tipo is not null && !InputValidator.NoCatalogo(tipo, Catalogo.TiposPonto))
                return Results.Problem("Parâmetro 'tipo' inválido.", statusCode: 400);

            var raio = raioKm ?? 2.0;
            if (raio is <= 0 or > 20)
                return Results.Problem("raioKm deve estar entre 0 e 20.", statusCode: 400);

            var pontos = repo.Buscar(tipo, busca);
            if (lat is not null && lon is not null)
            {
                if (!InputValidator.CoordenadaValida(lat.Value, lon.Value))
                    return Results.Problem("Coordenadas inválidas.", statusCode: 400);
                pontos = pontos
                    .Select(p => p with { DistanciaKm = Math.Round(Haversine(lat.Value, lon.Value, p.Latitude, p.Longitude), 2) })
                    .Where(p => p.DistanciaKm <= raio)
                    .OrderBy(p => p.DistanciaKm)
                    .ToList();
            }
            return Results.Ok(pontos);
        });

        // POST /api/ocorrencias — falta de insumos (limite mais rígido contra bots)
        api.MapPost("/ocorrencias", (OcorrenciaDto dto, PontoRepository pontos, OcorrenciaRepository repo) =>
        {
            if (!InputValidator.NoCatalogo(dto.Categoria, Catalogo.CategoriasOcorrencia))
                return Results.Problem("categoria inválida.", statusCode: 400);
            if (!InputValidator.NoCatalogo(dto.Origem, Catalogo.Origens))
                return Results.Problem("origem inválida.", statusCode: 400);
            var descricao = InputValidator.TextoLivre(dto.Descricao, 280);
            if (descricao is null)
                return Results.Problem("descricao obrigatória (até 280 caracteres).", statusCode: 400);
            if (!pontos.Existe(dto.PontoId))
                return Results.Problem("Ponto de acolhimento não encontrado.", statusCode: 404);

            var id = repo.Inserir(dto.PontoId, dto.Categoria!, descricao, dto.Origem!);
            return Results.Created($"/api/ocorrencias/{id}", new { id, dto.PontoId, dto.Categoria, descricao, dto.Origem });
        }).RequireRateLimiting("ocorrencias");

        // GET /api/ocorrencias/{id} — consulta de ocorrência para demonstração CRUD.
        api.MapGet("/ocorrencias/{id:long}", (long id, OcorrenciaRepository repo) =>
        {
            var ocorrencia = repo.Obter(id);
            return ocorrencia is null ? Results.NotFound() : Results.Ok(ocorrencia);
        });

        // PUT /api/ocorrencias/{id} — atualização validada e parametrizada.
        api.MapPut("/ocorrencias/{id:long}", (long id, AtualizarOcorrenciaDto dto, OcorrenciaRepository repo) =>
        {
            var descricao = InputValidator.TextoLivre(dto.Descricao, 280);
            if (descricao is null)
                return Results.Problem("descricao obrigatória (até 280 caracteres).", statusCode: 400);
            if (!repo.AtualizarDescricao(id, descricao)) return Results.NotFound();
            return Results.Ok(repo.Obter(id));
        }).RequireRateLimiting("ocorrencias");

        // POST /api/ocorrencias/{id}/foto — upload do app (multipart/form-data, campo "foto")
        api.MapPost("/ocorrencias/{id:long}/foto", async (long id, IFormFile foto, UploadSanitizer up,
                                                           OcorrenciaRepository repo, ILogger<Program> log, HttpContext ctx) =>
        {
            var r = await up.SalvarAsync(foto);
            if (!r.Ok)
            {
                log.LogWarning("SEGURANCA upload rejeitado de {Ip}: nome={Nome} motivo={Motivo}",
                    ctx.Connection.RemoteIpAddress, foto.FileName, r.Erro);
                return Results.Problem(r.Erro, statusCode: 400);
            }
            if (!repo.AnexarFoto(id, r.NomeArquivo!))
                return Results.Problem("Ocorrência não encontrada.", statusCode: 404);
            return Results.Created($"/api/fotos/{r.NomeArquivo}", new { arquivo = r.NomeArquivo });
        }).DisableAntiforgery().RequireRateLimiting("uploads");

        // GET /api/fotos/{nome} — leitura protegida contra Path Traversal
        api.MapGet("/fotos/{nome}", (string nome, UploadSanitizer up, ILogger<Program> log, HttpContext ctx) =>
        {
            var caminho = up.ResolverCaminhoSeguro(nome);
            if (caminho is null)
            {
                log.LogWarning("SEGURANCA tentativa de leitura de arquivo invalida de {Ip}: {Nome}",
                    ctx.Connection.RemoteIpAddress, nome);
                return Results.NotFound();
            }
            var tipo = caminho.EndsWith(".png") ? "image/png" : "image/jpeg";
            return Results.File(caminho, tipo);
        });

        // GET /api/alertas/atual?bairro=Sé
        api.MapGet("/alertas/atual", (string? bairro, LeituraRepository repo) =>
        {
            var b = bairro ?? "Sé";
            if (!InputValidator.TermoBuscaValido(b))
                return Results.Problem("bairro inválido.", statusCode: 400);
            var ultima = repo.UltimaDoBairro(b);
            if (ultima is null)
                return Results.Problem($"Sem leituras para o bairro {b}.", statusCode: 404);
            return Results.Ok(new
            {
                bairro = ultima.Bairro,
                temperaturaC = ultima.TemperaturaC,
                umidadePct = ultima.UmidadePct,
                sensacaoTermicaC = ultima.IndiceCalorC,
                nivel = ultima.Nivel.ToString(),
                orientacao = RiscoTermicoService.Orientacao(ultima.Nivel),
                atualizadoEmUtc = ultima.RegistradoEmUtc
            });
        });

        app.MapGet("/health", () => Results.Ok(new { status = "ok", servico = "TermoAtiva.Api", utc = DateTime.UtcNow }));
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double dLat = (lat2 - lat1) * Math.PI / 180, dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
