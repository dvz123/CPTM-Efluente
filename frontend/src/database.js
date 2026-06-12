import Dexie from 'dexie'

export const db = new Dexie('CptmAmbientalDB')

db.version(2).stores({
  // Sessão de login espelhada offline
  sessao: 'id, email, nome, perfil, token, hashSenha, expiraEm',

  // Formulários pendentes de sync
  formularios: '++id, tipo, status, operadorId, criadoEm, sincronizadoEm',

  // Fotos vinculadas a formulários
  fotos: '++id, formularioId, blob, descricao, criadoEm',

  // Cache de listas (linhas CPTM, estações, etc.)
  cache: 'chave'
})

// Status possíveis para formulários:
// 'rascunho'     -> em preenchimento
// 'completo'     -> finalizado, aguardando sync
// 'sincronizado' -> enviado ao backend com sucesso
// 'erro'         -> falhou no envio, tentar novamente
