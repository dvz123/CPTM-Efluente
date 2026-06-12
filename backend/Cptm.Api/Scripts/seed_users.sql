-- ==============================================================
-- CPTM Ambiental - Seed: Usuários de teste
-- Senha para ambos: cptm2026
-- ==============================================================

INSERT INTO USUARIOS (NOME, EMAIL, SENHA_HASH, PERFIL) 
VALUES ('Operador CPTM', 'operador@cptm.sp.gov.br', 'beb9b226a7aa191956e2a654dcd57d25b13658ba2eeaca5e69375a2815c384bb', 'operador');

INSERT INTO USUARIOS (NOME, EMAIL, SENHA_HASH, PERFIL) 
VALUES ('Supervisor GEA', 'supervisor@cptm.sp.gov.br', 'beb9b226a7aa191956e2a654dcd57d25b13658ba2eeaca5e69375a2815c384bb', 'supervisor');

COMMIT;
