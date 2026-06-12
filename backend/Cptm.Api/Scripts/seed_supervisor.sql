-- ==============================================================
-- SEED: Usuário Supervisor de teste
-- Login: supervisor@cptm.sp.gov.br
-- Senha: cptm2026
-- ==============================================================

INSERT INTO USUARIOS (NOME, EMAIL, SENHA_HASH, PERFIL, ATIVO)
VALUES (
    'Supervisor Teste',
    'supervisor@cptm.sp.gov.br',
    'beb9b226a7aa191956e2a654dcd57d25b13658ba2eeaca5e69375a2815c384bb',
    'supervisor',
    1
);

COMMIT;
