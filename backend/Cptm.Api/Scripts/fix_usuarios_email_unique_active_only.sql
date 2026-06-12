-- ==============================================================
-- Ajuste de unicidade de e-mail para permitir histórico com soft delete
-- Regra desejada:
--   - usuários ATIVO=1 não podem repetir e-mail (case-insensitive)
--   - usuários ATIVO=0 podem manter histórico e permitir reuso do e-mail
-- ==============================================================

-- 1) Remover constraint UNIQUE antiga em EMAIL (se existir)
DECLARE
  v_constraint_name VARCHAR2(200);
BEGIN
  SELECT uc.CONSTRAINT_NAME
    INTO v_constraint_name
    FROM USER_CONSTRAINTS uc
   WHERE uc.TABLE_NAME = 'USUARIOS'
     AND uc.CONSTRAINT_TYPE = 'U'
     AND uc.CONSTRAINT_NAME = 'SYS_C002917172';

  EXECUTE IMMEDIATE 'ALTER TABLE USUARIOS DROP CONSTRAINT ' || v_constraint_name;
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    NULL;
END;
/

-- 2) Remover índice funcional antigo não único (se existir)
BEGIN
  EXECUTE IMMEDIATE 'DROP INDEX IDX_USUARIOS_EMAIL';
EXCEPTION
  WHEN OTHERS THEN
    IF SQLCODE != -1418 AND SQLCODE != -942 THEN
      RAISE;
    END IF;
END;
/

-- 3) Criar índice único funcional somente para ATIVO=1
--    (permite duplicidade quando ATIVO=0)
BEGIN
  EXECUTE IMMEDIATE '
    CREATE UNIQUE INDEX UQ_USUARIOS_EMAIL_ATIVO
    ON USUARIOS (
      CASE WHEN ATIVO = 1 THEN LOWER(TRIM(EMAIL)) END
    )
  ';
EXCEPTION
  WHEN OTHERS THEN
    IF SQLCODE = -955 THEN
      NULL; -- índice já existe
    ELSE
      RAISE;
    END IF;
END;
/

COMMIT;
