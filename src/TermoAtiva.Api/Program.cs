using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using TermoAtiva.Api.Data;
using TermoAtiva.Api.Endpoints;
using TermoAtiva.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// No Windows Server, avisos de segurança vão para o Visualizador de Eventos (origem "TermoAtiva")
if (OperatingSystem.IsWindows())
    builder.Logging.AddEventLog(o => { o.SourceName = "TermoAtiva"; o.LogName = "Application"; });

// Não revelar tecnologia/versão do servidor (reduz superfície para OSINT/fingerprinting)
builder.WebHost.ConfigureKestrel(k => k.AddServerHeader = false);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = UploadSanitizer.TamanhoMaximo + 64 * 1024);

var appData = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
builder.Services.AddSingleton(new Database(Path.Combine(appData, "termoativa.db")));
builder.Services.AddSingleton(new UploadSanitizer(Path.Combine(appData, "uploads")));
builder.Services.AddSingleton<PontoRepository>();
builder.Services.AddSingleton<LeituraRepository>();
builder.Services.AddSingleton<OcorrenciaRepository>();
builder.Services.AddSingleton<DeviceAuth>();
builder.Services.AddProblemDetails();

// CONTROLE C-05 (disponibilidade / anti-bot): limite de requisições por IP
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("geral", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("ocorrencias", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("uploads", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();
app.Services.GetRequiredService<Database>().Inicializar();

// Erros genéricos: nunca devolver stack trace ou SQL ao cliente
app.UseExceptionHandler();
app.UseStatusCodePages();

// CONTROLE C-06: cabeçalhos de segurança HTTP
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    h["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none'";
    h["Permissions-Policy"] = "geolocation=(self), camera=()";
    await next();
});
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapTermoAtiva();

app.Run();

public partial class Program;
