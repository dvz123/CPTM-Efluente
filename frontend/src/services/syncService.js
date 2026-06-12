import { db } from '@/database'
import axios from 'axios'
import { checkConnectivity } from '@/services/connectivityService'

const MIN_FOTOS_POR_FORMULARIO = 4

function normalizarPk(valor) {
  return String(valor || '').trim().toUpperCase()
}

function obterPkFormulario(formulario) {
  return normalizarPk(formulario?.campos?.chavePrimaria)
}

function traduzirErroBackend(message) {
  if (!message) {
    return 'Falha ao enviar o formulário para a API.'
  }

  if (message.includes('ORA-01536')) {
    return 'O banco está sem espaço disponível no momento. O formulário permanece salvo neste dispositivo para reenvio posterior.'
  }

  return message
}

function getErroMensagem(error) {
  return traduzirErroBackend(error?.response?.data?.mensagem || error?.message)
}

function isErroChavePrimariaDuplicada(error) {
  const mensagemBackend = error?.response?.data?.mensagem || ''
  const mensagemErro = error?.message || ''
  const texto = `${mensagemBackend} ${mensagemErro}`.toLowerCase()
  return texto.includes('ora-00001') || texto.includes('mesma chave primária') || texto.includes('chave primária')
}

function gerarNovaChavePrimaria(formularioId) {
  const ts = new Date()
  const yyyy = ts.getUTCFullYear()
  const MM = String(ts.getUTCMonth() + 1).padStart(2, '0')
  const dd = String(ts.getUTCDate()).padStart(2, '0')
  const hh = String(ts.getUTCHours()).padStart(2, '0')
  const mm = String(ts.getUTCMinutes()).padStart(2, '0')
  const ss = String(ts.getUTCSeconds()).padStart(2, '0')
  const sufixo = `${Date.now().toString().slice(-5)}${String(formularioId || '').slice(-3)}`
  return `EFL-${yyyy}${MM}${dd}${hh}${mm}${ss}-${sufixo}`
}

function parseNullableNumber(value) {
  if (value === null || value === undefined || value === '') {
    return null
  }

  if (typeof value === 'number') {
    return Number.isFinite(value) ? value : null
  }

  if (typeof value === 'string') {
    const normalized = value.trim().replace(',', '.')
    if (!normalized) {
      return null
    }

    const parsed = Number(normalized)
    return Number.isFinite(parsed) ? parsed : null
  }

  return null
}

function normalizarCamposFormulario(campos) {
  return {
    ...campos,
    latitude: parseNullableNumber(campos?.latitude),
    longitude: parseNullableNumber(campos?.longitude),
    quantidadeLitros: parseNullableNumber(campos?.quantidadeLitros),
    distanciaVia: parseNullableNumber(campos?.distanciaVia)
  }
}

function validarFormularioParaSincronizacao(formulario, fotos) {
  const campos = formulario?.campos || {}

  const obrigatorios = [
    { key: 'contratada', label: 'Nome da Contratada' },
    { key: 'siglaMeioAmbiente', label: 'Sigla de Meio Ambiente' },
    { key: 'statusDesvioAmbiental', label: 'Status do Desvio Ambiental' },
    { key: 'autor', label: 'Autor do Cadastramento' },
    { key: 'data', label: 'Data de Emissão do Formulário' },
    { key: 'numero', label: 'Número do Formulário' },
    { key: 'dataColeta', label: 'Data do Cadastramento' },
    { key: 'horaColeta', label: 'Hora do Cadastramento' },
    { key: 'chavePrimaria', label: 'Chave Primária' },
    { key: 'elementoNumero', label: 'Elemento de Monitoramento - Número' },
    { key: 'elementoNome', label: 'Elemento de Monitoramento - Nome' },
    { key: 'municipio', label: 'Município' },
    { key: 'tipoAtividadeCptm', label: 'Tipo de Atividade CPTM' },
    { key: 'nomeEdificacao', label: 'Nome da Edificação' },
    { key: 'origemEfluente', label: 'Origem do Efluente' },
    { key: 'destinacao', label: 'Destinação do Efluente' },
    { key: 'fonteGeradora', label: 'Fonte Geradora' },
    { key: 'quantidadeLitros', label: 'Quantidade em Litros' }
  ]

  const faltantes = obrigatorios
    .filter(({ key }) => {
      const valor = campos[key]
      if (typeof valor === 'string') return !valor.trim()
      return valor === null || valor === undefined || valor === ''
    })
    .map(({ label }) => label)

  if (faltantes.length) {
    return {
      valido: false,
      codigo: 'campos_obrigatorios_ausentes',
      mensagemUsuario: `Registro incompleto. Revise os campos obrigatórios: ${faltantes.slice(0, 3).join(', ')}${faltantes.length > 3 ? '...' : ''}.`,
      detalheTecnico: `Campos ausentes: ${faltantes.join(', ')}`
    }
  }

  if (!Array.isArray(fotos) || fotos.length < MIN_FOTOS_POR_FORMULARIO) {
    return {
      valido: false,
      codigo: 'fotos_insuficientes',
      mensagemUsuario: `Registro incompleto. São necessárias pelo menos ${MIN_FOTOS_POR_FORMULARIO} fotos.`,
      detalheTecnico: `Quantidade de fotos inválida: ${fotos?.length || 0}`
    }
  }

  const fotoInvalida = fotos.find((foto) => !(foto?.blob instanceof Blob) && !foto?.blob)
  if (fotoInvalida) {
    return {
      valido: false,
      codigo: 'foto_blob_invalido',
      mensagemUsuario: 'Registro com anexo inválido. Reabra o formulário, confira as fotos e tente novamente.',
      detalheTecnico: `Foto inválida detectada no formulário ${formulario?.id}.`
    }
  }

  return { valido: true }
}

async function montarFormData(campos, fotos) {
  const formData = new FormData()
  formData.append('dados', JSON.stringify(normalizarCamposFormulario(campos)))

  fotos.forEach((foto, index) => {
    const mimeType = foto.tipoArquivo || foto.type || 'image/jpeg'
    const nomeArquivo = foto.nomeArquivo || `foto_${index + 1}.${mimeType.split('/')[1] || 'jpg'}`
    const blob = foto.blob instanceof Blob
      ? foto.blob
      : new Blob([foto.blob], { type: mimeType })

    formData.append('fotos', blob, nomeArquivo)
  })

  return formData
}

export async function enviarFormulario(campos, fotos) {
  const formData = await montarFormData(campos, fotos)

  const response = await axios.post('/api/formularios', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })

  return response.data
}

/**
 * Serviço de sincronização offline → online.
 * Busca formulários com status 'completo' no IndexedDB e envia para a API.
 */
export async function sincronizarPendentes(options = {}) {
  const { ids = null } = options

  if (!(await checkConnectivity())) {
    return { enviados: 0, erros: 0, resultados: [] }
  }

  const token = localStorage.getItem('token')
  let operadorIdAtual = null
  try {
    if (token) {
      const payloadBase64 = token.split('.')[1]
      if (payloadBase64) {
        const payloadJson = JSON.parse(atob(payloadBase64))
        operadorIdAtual = payloadJson?.nameid ? Number(payloadJson.nameid) : null
      }
    }
  } catch {
    operadorIdAtual = null
  }

  const formularios = await db.formularios.toArray()
  const pendentesBase = formularios.filter((formulario) => {
    const statusElegivel = formulario.status === 'completo' || formulario.status === 'erro'
    const idElegivel = !ids || ids.includes(formulario.id)
    const operadorElegivel = operadorIdAtual ? Number(formulario.operadorId) === Number(operadorIdAtual) : true
    return statusElegivel && idElegivel && operadorElegivel
  })

  const gruposPorPk = new Map()
  const semPk = []

  for (const form of pendentesBase) {
    const pk = obterPkFormulario(form)
    if (!pk) {
      semPk.push(form)
      continue
    }
    const lista = gruposPorPk.get(pk) || []
    lista.push(form)
    gruposPorPk.set(pk, lista)
  }

  const pendentes = []

  for (const [pk, lista] of gruposPorPk.entries()) {
    lista.sort((a, b) => {
      const dataA = new Date(a?.criadoEm || 0).getTime()
      const dataB = new Date(b?.criadoEm || 0).getTime()
      if (dataA !== dataB) return dataB - dataA
      return Number(b?.id || 0) - Number(a?.id || 0)
    })

    const principal = lista[0]
    const duplicados = lista.slice(1)

    if (duplicados.length) {
      const agora = new Date().toISOString()
      for (const dup of duplicados) {
        await db.formularios.update(dup.id, {
          status: 'sincronizado',
          sincronizadoEm: agora,
          erroMensagem: null,
          ultimaTentativaEm: agora
        })
      }
    }

    pendentes.push(principal)
  }

  pendentes.push(...semPk)

  let enviados = 0
  let erros = 0
  let resolvidosPorMesmaPk = 0
  const resultados = []

  for (const form of pendentes) {
    const tentativaEm = new Date().toISOString()

    try {
      const fotos = await db.fotos.where('formularioId').equals(form.id).toArray()
      const validacao = validarFormularioParaSincronizacao(form, fotos)

      if (!validacao.valido) {
        const mensagem = validacao.mensagemUsuario || 'Registro inválido para sincronização.'

        await db.formularios.update(form.id, {
          status: 'erro',
          erroMensagem: mensagem,
          ultimaTentativaEm: tentativaEm
        })

        await db.cache.put({
          chave: `sync_diag_${form.id}`,
          dados: {
            formularioId: form.id,
            codigo: validacao.codigo,
            detalhe: validacao.detalheTecnico,
            atualizadoEm: tentativaEm
          },
          atualizadoEm: tentativaEm
        })

        erros++
        resultados.push({ id: form.id, status: 'erro', mensagem })
        continue
      }

      await enviarFormulario(form.campos, fotos)

      const pkSincronizada = obterPkFormulario(form)
      if (pkSincronizada) {
        const comMesmaPk = await db.formularios.toArray()
        const idsMesmaPk = comMesmaPk
          .filter((item) => obterPkFormulario(item) === pkSincronizada)
          .map((item) => item.id)

        await Promise.all(idsMesmaPk.map((id) => db.formularios.update(id, {
          status: 'sincronizado',
          sincronizadoEm: tentativaEm,
          erroMensagem: null,
          ultimaTentativaEm: tentativaEm
        })))

        enviados++
        resolvidosPorMesmaPk += Math.max(0, idsMesmaPk.length - 1)
      } else {
        await db.formularios.update(form.id, {
          status: 'sincronizado',
          sincronizadoEm: tentativaEm,
          erroMensagem: null,
          ultimaTentativaEm: tentativaEm
        })
        enviados++
      }

      resultados.push({ id: form.id, status: 'sincronizado', mensagem: null })
    } catch (e) {
      if (isErroChavePrimariaDuplicada(e)) {
        try {
          const novaPk = gerarNovaChavePrimaria(form.id)
          const camposAtualizados = {
            ...(form.campos || {}),
            chavePrimaria: novaPk
          }

          await db.formularios.update(form.id, {
            campos: camposAtualizados,
            ultimaTentativaEm: tentativaEm
          })

          await enviarFormulario(camposAtualizados, await db.fotos.where('formularioId').equals(form.id).toArray())

          const pkSincronizada = normalizarPk(camposAtualizados?.chavePrimaria)
          if (pkSincronizada) {
            const comMesmaPk = await db.formularios.toArray()
            const idsMesmaPk = comMesmaPk
              .filter((item) => obterPkFormulario(item) === pkSincronizada)
              .map((item) => item.id)

            await Promise.all(idsMesmaPk.map((id) => db.formularios.update(id, {
              status: 'sincronizado',
              sincronizadoEm: tentativaEm,
              erroMensagem: null,
              ultimaTentativaEm: tentativaEm
            })))

            enviados++
            resolvidosPorMesmaPk += Math.max(0, idsMesmaPk.length - 1)
          } else {
            await db.formularios.update(form.id, {
              status: 'sincronizado',
              sincronizadoEm: tentativaEm,
              erroMensagem: null,
              ultimaTentativaEm: tentativaEm
            })
            enviados++
          }

          await db.cache.put({
            chave: `sync_diag_${form.id}`,
            dados: {
              formularioId: form.id,
              codigo: 'reenvio_com_nova_pk',
              detalhe: `PK duplicada detectada. Nova PK gerada: ${novaPk}`,
              atualizadoEm: tentativaEm
            },
            atualizadoEm: tentativaEm
          })

          resultados.push({ id: form.id, status: 'sincronizado', mensagem: null })
          continue
        } catch (retryError) {
          const mensagemRetry = getErroMensagem(retryError)
          console.error(`Erro ao reenviar formulário ${form.id} após regenerar PK:`, retryError)

          await db.formularios.update(form.id, {
            status: 'erro',
            erroMensagem: mensagemRetry,
            ultimaTentativaEm: tentativaEm
          })

          await db.cache.put({
            chave: `sync_diag_${form.id}`,
            dados: {
              formularioId: form.id,
              codigo: 'erro_reenvio_apos_nova_pk',
              detalhe: retryError?.response?.data || retryError?.message || String(retryError),
              atualizadoEm: tentativaEm
            },
            atualizadoEm: tentativaEm
          })

          erros++
          resultados.push({ id: form.id, status: 'erro', mensagem: mensagemRetry })
          continue
        }
      }

      const mensagem = getErroMensagem(e)
      console.error(`Erro ao sincronizar formulário ${form.id}:`, e)

      await db.formularios.update(form.id, {
        status: 'erro',
        erroMensagem: mensagem,
        ultimaTentativaEm: tentativaEm
      })

      await db.cache.put({
        chave: `sync_diag_${form.id}`,
        dados: {
          formularioId: form.id,
          codigo: 'erro_envio_backend',
          detalhe: e?.response?.data || e?.message || String(e),
          atualizadoEm: tentativaEm
        },
        atualizadoEm: tentativaEm
      })

      erros++
      resultados.push({ id: form.id, status: 'erro', mensagem })
    }
  }

  return {
    enviados,
    enviadosNovos: enviados,
    erros,
    errosRestantes: erros,
    resolvidosPorMesmaPk,
    resultados
  }
}

/**
 * Carrega as listas de referência (linhas, estações, etc.) e salva no cache local.
 */
export async function carregarCacheReferencias() {
  if (!(await checkConnectivity())) return

  try {
    // Adiciona timeout de 8 segundos para cada requisição
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), 8000)

    const [linhas, estacoes, naturezas, formularioOperador] = await Promise.all([
      axios.get('/api/referencia/linhas', { signal: controller.signal }),
      axios.get('/api/referencia/estacoes', { signal: controller.signal }),
      axios.get('/api/referencia/naturezas', { signal: controller.signal }),
      axios.get('/api/referencia/formulario-operador', { signal: controller.signal })
    ])

    clearTimeout(timeoutId)

    await db.cache.bulkPut([
      { chave: 'linhas', dados: linhas.data, atualizadoEm: new Date().toISOString() },
      { chave: 'estacoes', dados: estacoes.data, atualizadoEm: new Date().toISOString() },
      { chave: 'naturezas', dados: naturezas.data, atualizadoEm: new Date().toISOString() },
      { chave: 'formularioOperador', dados: formularioOperador.data, atualizadoEm: new Date().toISOString() }
    ])
    console.log('[CPTM] Cache de referências carregado com sucesso')
  } catch (e) {
    console.warn('[CPTM] Não foi possível atualizar cache de referências:', e.message)
  }
}

/**
 * Lê uma lista de referência do cache local.
 */
export async function obterCacheReferencia(chave) {
  const item = await db.cache.get(chave)
  return item?.dados || []
}
