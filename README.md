# TermoAtiva — SOA & Web Services | FIAP Global Solution 2026.2

Backend REST da CitizenTech **TermoAtiva**, sistema de monitoramento microclimático e apoio urbano para trabalhadores ao ar livre, idosos e pedestres em São Paulo. O backend recebe leituras de estações IoT, calcula índice de calor, consulta pontos de hidratação/sombra e registra ocorrências cidadãs.

## Equipe

| Integrante | RM |
|---|---|
| Felipe Gimenes | 99799 |
| Musashi Mabuti | 559444 |
| Jonathan Henrique da Silva | 561139 |
| Marcos Vinicius Gonçalves Santos | 560062 |

## Critérios da disciplina

| Critério | Implementação |
|---|---|
| Domínio (3,0) | `Domain/`, `Services/`, `Endpoints/`, `Data/Repositories.cs` |
| Banco de dados (3,0) | SQLite relacional, `Database.cs`, consultas SQL parametrizadas |
| Migrações (2,5) | `Migrations/001_initial_schema.sql`, `002_indexes.sql`, `Data/Migrador.cs` |
| Testes manuais (1,5) | `insomnia/TermoAtiva-Insomnia.json`, `docs/ROTEIRO_TESTES.md` |

## Arquitetura

`ESP32 / Mobile / Totem → API REST ASP.NET Core 8 → Repositories → SQLite`

- **Domain:** entidades `LeituraClimatica`, `PontoAcolhimento`, `RegistroOcorrencia`, enums e catálogos.
- **Services:** cálculo de índice de calor e classificação de risco ambiental (regra determinística; não é modelo treinado de ML).
- **Endpoints:** rotas HTTP e validação de entradas.
- **Repositories:** acesso ao banco relacional com parâmetros SQL.
- **Migrations:** evolução versionada do esquema, com histórico e transações.
- **Security:** autenticação dos dispositivos, limites de requisição e proteção de upload.

## Pré-requisitos

- .NET SDK 8 instalado (Windows, Linux ou macOS).
- Para publicar no IIS: Windows Server + ASP.NET Core Hosting Bundle 8 (conforme projeto de Operating Systems).
- Não é necessário instalar um servidor de banco separado: SQLite cria `App_Data/termoativa.db` automaticamente.

## Executar localmente

```bash
dotnet restore src/TermoAtiva.Api/TermoAtiva.Api.csproj
dotnet run --project src/TermoAtiva.Api/TermoAtiva.Api.csproj --urls http://localhost:5000
```

Abra `http://localhost:5000/health`. Na primeira execução, o banco é criado, as migrações são aplicadas e os pontos **fictícios de demonstração** são cadastrados. Não trate esses pontos como locais verificados ou ativos na vida real.

## Endpoints REST

| Método | Rota | Função |
|---|---|---|
| GET | `/health` | Estado da aplicação |
| GET | `/api/pontos-acolhimento?tipo=bebedouro&busca=Sé` | Busca de pontos |
| POST | `/api/telemetria` | Recebe leitura IoT autenticada |
| GET | `/api/alertas/atual?bairro=Sé` | Último alerta térmico |
| POST | `/api/ocorrencias` | Cadastra ocorrência |
| GET | `/api/ocorrencias/{id}` | Consulta ocorrência |
| PUT | `/api/ocorrencias/{id}` | Atualiza descrição |
| POST | `/api/ocorrencias/{id}/foto` | Anexa foto validada |
| GET | `/api/fotos/{nome}` | Obtém foto |

Exemplo de POST ocorrência:

```json
{"pontoId":1,"categoria":"sem_agua","descricao":"Bebedouro sem água","origem":"app"}
```

Exemplo de PUT ocorrência:

```json
{"descricao":"Equipe de manutenção acionada"}
```

### Autenticação de telemetria

A rota de telemetria exige `X-Device-Key`. O valor armazenado em `Dispositivos:ESP32-SE-01` é o **hash SHA-256 em hexadecimal** da chave enviada, e não a chave em texto puro. Defina esse hash em configuração local segura/variáveis de ambiente e configure a chave correspondente no Insomnia. A chave de demonstração incluída no projeto original é pública e deve ser substituída em qualquer implantação real. Não publique segredos reais no GitHub.

Exemplo para gerar um hash de uma chave escolhida pela equipe (PowerShell):

```powershell
$chave = Read-Host 'Chave do dispositivo'
$bytes = [Text.Encoding]::UTF8.GetBytes($chave)
$hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
${env:Dispositivos__ESP32-SE-01} = $hash
```

## Migrações e testes

Veja [docs/MIGRACOES.md](docs/MIGRACOES.md) e [docs/ROTEIRO_TESTES.md](docs/ROTEIRO_TESTES.md). A coleção Insomnia possui cenários de sucesso e erro. **As capturas reais dos 12 testes e do SQLite estão em [`evidencias/`](evidencias/README.md)**, fornecidas pela equipe em 16/11/2026. Os 12 cenários retornaram os códigos esperados.

## Publicação compatível com Operating Systems

A aplicação foi derivada do código da API presente no ZIP `TermoAtiva-OperatingSystems`, mantendo os contratos dos quatro endpoints centrais. A publicação pode usar `dotnet publish src/TermoAtiva.Api/TermoAtiva.Api.csproj -c Release -o publish` e o mesmo site/pool IIS do projeto de SO. Faça backup do banco antes de substituir a publicação.

## Limitações / decisões

- SQLite é um banco **relacional**, mas o documento de visão geral sugeriu PostgreSQL, MySQL ou SQL Server. Confirmar com o professor se SQLite é aceito; para compatibilidade com SO, ele foi mantido.
- A classificação de risco desta API é por regras físicas, **não** substitui o modelo de Machine Learning da disciplina de IA.
- Não há integração ao vivo verificada com sensores, mobile ou totem; os endpoints são preparados para essa integração.
- A equipe informou compilação bem-sucedida e apresentou capturas dos testes HTTP executados localmente em .NET 8 em 16/11/2026. A implantação no IIS ainda não foi verificada nesta entrega.
- Registre os prompts usados com IA em `prompts.md` conforme orientação da GS.

## Evidências de validação local (16/11/2026)

A equipe executou os 12 cenários no Insomnia e forneceu capturas de todos os resultados esperados (`200`, `201`, `400`, `401` e `404`). A estrutura SQLite e as migrações `001_initial_schema.sql` e `002_indexes.sql` também foram demonstradas. Veja [índice de evidências](evidencias/README.md). Essas capturas documentam testes manuais, não uma suíte automatizada.
