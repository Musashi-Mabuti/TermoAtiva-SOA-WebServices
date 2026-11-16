# Roteiro de evidências para a FIAP

1. Execute `dotnet restore` e `dotnet run --project src/TermoAtiva.Api --urls http://localhost:5000`.
2. Abra `http://localhost:5000/health` e registre print do status 200.
3. No Insomnia, importe `insomnia/TermoAtiva-Insomnia.json` (Import > File).
4. Configure `base_url` e `device_key` no ambiente. A chave deve corresponder ao hash SHA-256 cadastrado em `Dispositivos:ESP32-SE-01`.
5. Execute as requisições 01–12; a requisição de alerta depende de uma telemetria aceita; GET/PUT ocorrência dependem do ID retornado no POST.
6. Registre prints de GET 200, POST 201, PUT 200, validação 400 e recurso inexistente 404.
7. Mostre no SQLite as tabelas `LeiturasClimaticas`, `PontosAcolhimento`, `RegistrosOcorrencia`, `ParceirosContato` e `SchemaMigrations`.
8. Registre prints do GitHub, do README e da aplicação integrada com o IIS, se disponível.

**Não inclua chaves de dispositivos ou dados pessoais reais nos prints.** Os resultados acima são esperados, não testes executados neste pacote.
