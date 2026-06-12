-- ==============================================================
-- CPTM Ambiental - DDL Incremental
-- Semana 2: Tabelas de autenticação (complementam PT_EFLUENTE / RT_EFLUENTE já existentes)
-- ==============================================================
-- TABELAS EXISTENTES (NÃO ALTERAR):
--   PT_EFLUENTE  (73 colunas) – formulário de efluentes + SDO_GEOMETRY
--   RT_EFLUENTE  (6 colunas)  – fotos como BLOB
-- ==============================================================

-- 1. Tabela de Usuários (login / JWT)
CREATE TABLE USUARIOS (
    ID              NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    NOME            VARCHAR2(200)   NOT NULL,
    EMAIL           VARCHAR2(200)   NOT NULL,
    SENHA_HASH      VARCHAR2(128)   NOT NULL, -- SHA256 hex (em prod usar BCrypt)
    PERFIL          VARCHAR2(20)    DEFAULT 'operador' CHECK (PERFIL IN ('operador', 'supervisor')),
    ATIVO           NUMBER(1)       DEFAULT 1,
    CRIADO_EM       TIMESTAMP       DEFAULT LOCALTIMESTAMP
);

-- 2. Tabela de Sessões (rastreia tokens emitidos)
CREATE TABLE SESSOES (
    ID              NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    USUARIO_ID      NUMBER          NOT NULL REFERENCES USUARIOS(ID),
    TOKEN           VARCHAR2(2000),
    CRIADO_EM       TIMESTAMP       DEFAULT SYSTIMESTAMP,
    EXPIRA_EM       TIMESTAMP
);

-- ========= Índices =========
CREATE INDEX IDX_SESSOES_USUARIO ON SESSOES(USUARIO_ID);
CREATE UNIQUE INDEX UQ_USUARIOS_EMAIL_ATIVO
  ON USUARIOS (CASE WHEN ATIVO = 1 THEN LOWER(TRIM(EMAIL)) END);

-- 2.1 Tabela de Auditoria de Formulários
CREATE TABLE FORMULARIOS_AUDITORIA (
    ID              NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    FORMULARIO_PK   VARCHAR2(100)   NOT NULL,
    ACAO            VARCHAR2(20)    NOT NULL CHECK (ACAO IN ('CRIACAO', 'EDICAO', 'EXCLUSAO')),
    USUARIO_ID      NUMBER,
    USUARIO_NOME    VARCHAR2(200),
    USUARIO_PERFIL  VARCHAR2(20),
    DATA_HORA       TIMESTAMP       DEFAULT LOCALTIMESTAMP NOT NULL,
    IP              VARCHAR2(45),
    USER_AGENT      VARCHAR2(500),
    PAYLOAD_ANTES   CLOB,
    PAYLOAD_DEPOIS  CLOB
);

CREATE INDEX IDX_FORM_AUD_FORMULARIO ON FORMULARIOS_AUDITORIA(FORMULARIO_PK);
CREATE INDEX IDX_FORM_AUD_ACAO ON FORMULARIOS_AUDITORIA(ACAO);
CREATE INDEX IDX_FORM_AUD_DATA_HORA ON FORMULARIOS_AUDITORIA(DATA_HORA);

-- ========= Coluna de vínculo operador → PT_EFLUENTE (se não existir) =========
-- A PT_EFLUENTE já possui TX_AUTOR_PF_DO_CADASTRO / TX_NM_RESPONSAVEL_CADASTRO
-- que identificam o operador. Não é necessário FK formal pois a tabela já existe.

COMMIT;
