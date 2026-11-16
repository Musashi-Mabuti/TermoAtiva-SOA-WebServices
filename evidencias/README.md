# Evidências reais — TermoAtiva SOA

Capturas fornecidas pela equipe na conversa em 16/11/2026, sem alterações no conteúdo. Os 12 cenários do Insomnia retornaram os códigos HTTP esperados. As duas capturas SQLite mostram a estrutura do banco e o histórico de migrações.

| Arquivo | Cenário | HTTP esperado e observado |
|---|---|---|
| 01-health-200.png | Saúde da API | 200 |
| 02-pontos-200.png | Consulta de pontos | 200 |
| 03-pontos-filtro-200.png | Busca com filtro | 200 |
| 04-telemetria-201.png | Cadastro de leitura autenticada | 201 |
| 05-telemetria-invalida-400.png | Rejeição de leitura inválida | 400 |
| 06-telemetria-sem-chave-401.png | Rejeição sem credencial | 401 |
| 07-alerta-atual-200.png | Alerta após telemetria | 200 |
| 08-criar-ocorrencia-201.png | Cadastro de ocorrência | 201 |
| 09-buscar-ocorrencia-200.png | Consulta de ocorrência | 200 |
| 10-atualizar-ocorrencia-200.png | Atualização de descrição | 200 |
| 11-ocorrencia-inexistente-404.png | Recurso inexistente | 404 |
| 12-busca-invalida-400.png | Parâmetro de busca inválido | 400 |
| 13-sqlite-tabelas.png | Estrutura SQLite, tabelas e índices | — |
| 14-sqlite-migracoes.png | Migrações 001 e 002 aplicadas | — |

**Observações:** testes realizados localmente em `localhost:5000`. Não comprovam implantação em produção nem integração com hardware real. As credenciais exibidas em capturas auxiliares de configuração não foram incluídas nas evidências. Dados de pontos são fictícios para demonstração.
