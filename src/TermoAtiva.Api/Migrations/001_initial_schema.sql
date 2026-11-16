-- 001: tabelas iniciais da TermoAtiva (SQLite)
PRAGMA foreign_keys = ON;
CREATE TABLE IF NOT EXISTS LeiturasClimaticas (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DispositivoId TEXT NOT NULL,
                Bairro TEXT NOT NULL,
                TemperaturaC REAL NOT NULL,
                UmidadePct REAL NOT NULL,
                IndiceRadiacao INTEGER NOT NULL,
                IndiceCalorC REAL NOT NULL,
                Nivel INTEGER NOT NULL,
                RegistradoEmUtc TEXT NOT NULL);

CREATE TABLE IF NOT EXISTS PontosAcolhimento (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL,
                Tipo TEXT NOT NULL,
                Endereco TEXT NOT NULL,
                Bairro TEXT NOT NULL,
                Latitude REAL NOT NULL,
                Longitude REAL NOT NULL,
                Ativo INTEGER NOT NULL DEFAULT 1);

CREATE TABLE IF NOT EXISTS RegistrosOcorrencia (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PontoId INTEGER NOT NULL REFERENCES PontosAcolhimento(Id),
                Categoria TEXT NOT NULL,
                Descricao TEXT NOT NULL,
                Origem TEXT NOT NULL,
                FotoArquivo TEXT NULL,
                CriadoEmUtc TEXT NOT NULL);

CREATE TABLE IF NOT EXISTS ParceirosContato (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PontoId INTEGER NOT NULL REFERENCES PontosAcolhimento(Id),
                Responsavel TEXT NOT NULL,
                Telefone TEXT NOT NULL,
                Email TEXT NOT NULL);
