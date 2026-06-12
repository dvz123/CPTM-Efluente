-- Garante unicidade lógica da PK de negócio dos formulários ativos
-- Coluna alvo: PT_EFLUENTE.PK_CD_MEIO_AMBIENTE_CPTM
-- Regra: somente registros ativos (status nulo ou diferente de 'excluido')

CREATE UNIQUE INDEX UQ_PT_EFLUENTE_PK_ATIVO
ON PT_EFLUENTE (
  CASE
    WHEN TX_STATUS_DO_REGISTRO_NO_BD IS NULL OR LOWER(TX_STATUS_DO_REGISTRO_NO_BD) <> 'excluido'
    THEN PK_CD_MEIO_AMBIENTE_CPTM
  END
);

COMMIT;
