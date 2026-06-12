-- Script de migração para adicionar coluna UUID público na PT_EFLUENTE
-- Executar em ordem, em ambiente de TESTE primeiro

-- 1) Adicionar coluna UUID_PUBLICO
ALTER TABLE PT_EFLUENTE ADD (
    UUID_PUBLICO RAW(16) DEFAULT SYS_GUID()
);

-- 2) Tornar única e indexada
ALTER TABLE PT_EFLUENTE ADD CONSTRAINT UK_PT_EFLUENTE_UUID_PUBLICO UNIQUE (UUID_PUBLICO);
CREATE INDEX IX_PT_EFLUENTE_UUID_PUBLICO ON PT_EFLUENTE (UUID_PUBLICO);

-- 3) Tornar NOT NULL (após garantias que não há nulls)
ALTER TABLE PT_EFLUENTE MODIFY UUID_PUBLICO NOT NULL;

-- 4) Preencher para registros existentes (caso necessário)
-- UPDATE PT_EFLUENTE SET UUID_PUBLICO = SYS_GUID() WHERE UUID_PUBLICO IS NULL;

-- 5) Verificação
SELECT 'Registros com UUID vazio:' AS STATUS, COUNT(*) AS QTD FROM PT_EFLUENTE WHERE UUID_PUBLICO IS NULL UNION ALL
SELECT 'Total registros:', COUNT(*) FROM PT_EFLUENTE;

-- Pronto para uso em API (backend será ajustado para expor UUID_PUBLICO nas listagens e usar como parâmetro em endpoints GET/PUT/DELETE)
