-- 002: indices de pesquisa e consulta por bairro
CREATE INDEX IF NOT EXISTS IX_Leituras_Bairro_Id ON LeiturasClimaticas(Bairro, Id DESC);
CREATE INDEX IF NOT EXISTS IX_Pontos_Bairro_Tipo ON PontosAcolhimento(Bairro, Tipo);
CREATE INDEX IF NOT EXISTS IX_Ocorrencias_Ponto ON RegistrosOcorrencia(PontoId);
