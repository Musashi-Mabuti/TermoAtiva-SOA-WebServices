# Migrações SQL versionadas

O projeto usa **SQLite**, banco relacional com chaves estrangeiras, consultas parametrizadas e índices. A escolha reaproveita o banco já usado pela API de Operating Systems, garantindo compatibilidade com a implantação IIS.

- `001_initial_schema.sql`: quatro tabelas do domínio.
- `002_indexes.sql`: índices para consultas por bairro, tipo e ponto.
- `SchemaMigrations`: histórico de versões executadas.

Na primeira inicialização, `Migrador.Aplicar` lê os arquivos em ordem, aplica cada migração em transação e registra a versão. Reiniciar a aplicação não duplica migrações. Para demonstrar: execute a API, abra o banco `App_Data/termoativa.db` em um visualizador SQLite e rode `SELECT * FROM SchemaMigrations;`. Faça uma captura de tela.

**Observação:** a versão anterior de Operating Systems criava tabelas com `CREATE TABLE IF NOT EXISTS`. As migrações aqui mantêm o mesmo esquema, mas introduzem histórico de versões. Se o banco já existir, faça backup antes de implantar esta versão; os scripts são idempotentes.
