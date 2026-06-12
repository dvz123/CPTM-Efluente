<template>
  <div class="operator-home">
    <header class="operator-home__header">
      <div class="operator-home__brand">
        <img :src="cptmLogo" alt="CPTM" class="operator-home__logo" />
        <div>
          <p class="operator-home__title">CPTM Efluentes</p>
          <p class="operator-home__subtitle">Operador de campo</p>
        </div>
      </div>

      <div class="operator-home__header-actions">
        <span class="operator-home__status" :class="isOnline ? 'operator-home__status--online' : 'operator-home__status--offline'">
          {{ isOnline ? 'Com conexão' : 'Sem conexão' }}
        </span>
        <button type="button" class="operator-home__icon-button" @click="themeStore.toggle()">
          <SunIcon v-if="themeStore.isDark" class="w-4 h-4" />
          <MoonIcon v-else class="w-4 h-4" />
        </button>
        <button type="button" class="operator-home__exit-button" @click="abrirModalLogout">
          Sair
        </button>
      </div>
    </header>

    <main class="operator-home__main">
      <section class="operator-home__welcome-card">
        <p class="operator-home__welcome-label">Operação de efluentes</p>
        <h1 class="operator-home__welcome-title">Olá, {{ authStore.usuario?.nome || 'Operador' }}</h1>
        <p class="operator-home__welcome-text">
          Aqui você cria o formulário de efluente, continua um rascunho salvo e acompanha o envio automático quando a internet voltar.
        </p>

        <div class="operator-home__primary-actions">
          <button type="button" class="operator-home__primary-button" @click="iniciarFormulario">
            <ClipboardPlusIcon class="w-5 h-5" />
            Criar formulário de efluente
          </button>

          <button v-if="temRascunho" type="button" class="operator-home__secondary-button" @click="continuarRascunho">
            <FilePenLineIcon class="w-5 h-5" />
            Continuar preenchimento
          </button>
        </div>
      </section>

      <section class="operator-home__grid">
        <article class="operator-home__panel">
          <h2 class="operator-home__panel-title">Resumo rápido</h2>
          <div class="operator-home__summary-list">
            <div class="operator-home__summary-item">
              <span>Pendentes de envio</span>
              <strong>{{ resumo.pendentes }}</strong>
            </div>
            <div class="operator-home__summary-item">
              <span>Sincronizados</span>
              <strong>{{ resumo.sincronizados }}</strong>
            </div>
            <div class="operator-home__summary-item">
              <span>Com erro</span>
              <strong>{{ resumo.erro }}</strong>
            </div>
          </div>
        </article>

        <article class="operator-home__panel">
          <h2 class="operator-home__panel-title">Situação do dispositivo</h2>
          <p class="operator-home__panel-text">
            <template v-if="temRascunho">
              Existe um rascunho salvo neste aparelho. Se o aplicativo fechar sem querer, o preenchimento continuará de onde parou.
            </template>
            <template v-else>
              Nenhum rascunho em andamento neste dispositivo.
            </template>
          </p>

          <button type="button" class="operator-home__secondary-button operator-home__secondary-button--compact" @click="sincronizarAgora" :disabled="sincronizando">
            <RefreshCwIcon class="w-4 h-4" :class="sincronizando ? 'animate-spin' : ''" />
            {{ sincronizando ? 'Sincronizando...' : 'Sincronizar agora' }}
          </button>

          <p v-if="mensagem" class="operator-home__feedback">{{ mensagem }}</p>
          <div v-if="ultimoErro" class="operator-home__error-box">
            <p class="operator-home__error-label">Último erro de sincronização</p>
            <p class="operator-home__error-text">{{ ultimoErro }}</p>
          </div>
        </article>
      </section>

      <section class="operator-home__agenda-grid">
        <article class="operator-home__panel">
          <h2 class="operator-home__panel-title">Próximos locais</h2>
          <p class="operator-home__panel-text operator-home__panel-text--tight">Esta agenda será definida pela futura tela do supervisor e exibida aqui para consulta do operador.</p>

          <div v-if="agenda.length" class="operator-home__agenda-list">
            <div v-for="item in agenda" :key="item.id" class="operator-home__agenda-item">
              <div>
                <p class="operator-home__agenda-local">{{ item.local }}</p>
                <p class="operator-home__agenda-meta">{{ formatarDataAgenda(item.data) }} · {{ item.status === 'concluido' ? 'Concluído' : 'Previsto' }}</p>
              </div>
            </div>
          </div>
          <p v-else class="operator-home__panel-text operator-home__panel-text--tight">Nenhum local programado pela supervisão até o momento.</p>
        </article>

        <article class="operator-home__panel">
          <h2 class="operator-home__panel-title">Locais visitados recentemente</h2>
          <div v-if="ultimosLocais.length" class="operator-home__visit-list">
            <div v-for="item in ultimosLocais" :key="item.id" class="operator-home__visit-item">
              <MapPinnedIcon class="w-4 h-4" />
              <div>
                <p class="operator-home__agenda-local">{{ item.local }}</p>
                <p class="operator-home__agenda-meta">{{ item.data }}</p>
              </div>
            </div>
          </div>
          <p v-else class="operator-home__panel-text operator-home__panel-text--tight">Os locais preenchidos no formulário aparecerão aqui conforme os registros forem sendo salvos.</p>
        </article>
      </section>
    </main>

    <Teleport to="body">
      <div
        v-if="modalLogoutAberto"
        class="fixed inset-0 z-[110] flex items-center justify-center bg-black/50 px-4"
        @click.self="fecharModalLogout"
      >
        <div class="rounded-2xl p-6 max-w-sm w-full shadow-2xl" :style="{ backgroundColor: 'var(--cptm-surface)' }">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-full bg-amber-100 dark:bg-amber-950/50 flex items-center justify-center">
              <TriangleAlertIcon class="w-5 h-5 text-amber-600" />
            </div>
            <h3 class="text-base font-bold" :style="{ color: 'var(--cptm-text)' }">Confirmar saída</h3>
          </div>
          <p class="text-sm mb-6" :style="{ color: 'var(--cptm-text-muted)' }">
            Deseja realmente sair da sessão?
          </p>
          <div class="flex gap-3">
            <button
              @click="fecharModalLogout"
              class="flex-1 py-2.5 rounded-xl border text-sm font-medium cursor-pointer"
              :style="{ borderColor: 'var(--cptm-border)', color: 'var(--cptm-text)' }"
            >
              Cancelar
            </button>
            <button
              @click="encerrarSessao"
              class="flex-1 py-2.5 rounded-xl text-white text-sm font-medium cursor-pointer"
              style="background-color: var(--cptm-danger);"
            >
              Sair
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { db } from '@/database'
import { sincronizarPendentes } from '@/services/syncService'
import { useConnectivityStatus } from '@/services/connectivityService'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import cptmLogo from '@/assets/Logo_CPTM.png'
import {
  ClipboardPlus as ClipboardPlusIcon,
  FilePenLine as FilePenLineIcon,
  MapPinned as MapPinnedIcon,
  Moon as MoonIcon,
  RefreshCw as RefreshCwIcon,
  Sun as SunIcon,
  TriangleAlert as TriangleAlertIcon
} from 'lucide-vue-next'

const router = useRouter()
const authStore = useAuthStore()
const themeStore = useThemeStore()
const { isOnline, checkConnectivity } = useConnectivityStatus()

const sincronizando = ref(false)
const mensagem = ref('')
const ultimoErro = ref('')
const temRascunho = ref(false)
const resumo = ref({ pendentes: 0, sincronizados: 0, erro: 0 })
const agenda = ref([])
const ultimosLocais = ref([])
const modalLogoutAberto = ref(false)

function normalizarChaveLocal(local) {
  return (local || '').toString().trim().toLowerCase()
}

function normalizarPk(valor) {
  return String(valor || '').trim().toUpperCase()
}

function obterPkFormulario(formulario) {
  return normalizarPk(formulario?.campos?.chavePrimaria)
}

function consolidarResumoPorPk(formularios = []) {
  const semPk = []
  const porPk = new Map()

  for (const form of formularios) {
    const pk = obterPkFormulario(form)
    if (!pk) {
      semPk.push(form)
      continue
    }

    const atual = porPk.get(pk)
    if (!atual) {
      porPk.set(pk, form)
      continue
    }

    const prioridade = (status) => {
      if (status === 'completo') return 3
      if (status === 'erro') return 2
      if (status === 'sincronizado') return 1
      return 0
    }

    const atualP = prioridade(atual.status)
    const novoP = prioridade(form.status)

    if (novoP > atualP) {
      porPk.set(pk, form)
      continue
    }

    if (novoP === atualP) {
      const dataAtual = new Date(atual?.ultimaTentativaEm || atual?.sincronizadoEm || atual?.criadoEm || 0).getTime()
      const dataNova = new Date(form?.ultimaTentativaEm || form?.sincronizadoEm || form?.criadoEm || 0).getTime()
      if (dataNova > dataAtual) {
        porPk.set(pk, form)
      }
    }
  }

  const consolidados = [...porPk.values(), ...semPk]
  const pendentes = consolidados.filter(f => f.status === 'completo').length
  const sincronizados = consolidados.filter(f => f.status === 'sincronizado').length
  const erro = consolidados.filter(f => f.status === 'erro').length

  return { pendentes, sincronizados, erro }
}

function mensagemErroAmigavel(erroOriginal) {
  if (!erroOriginal) {
    return ''
  }

  const texto = String(erroOriginal)
  if (texto.toLowerCase().includes('espaço disponível')) {
    return 'Não foi possível enviar agora porque o banco está indisponível para gravação. O registro continua salvo neste dispositivo para nova tentativa.'
  }

  return 'Não foi possível enviar alguns registros agora. Eles continuam salvos neste dispositivo e serão reenviados na próxima sincronização.'
}

function getDraftKey() {
  return `rascunho_operador_${authStore.usuario?.id || 'anon'}_efluente`
}

function getAgendaKey() {
  return `agenda_supervisor_${authStore.usuario?.id || 'anon'}_efluente`
}

function formatarDataAgenda(dataIso) {
  if (!dataIso) return 'Data não informada'
  const data = new Date(dataIso)
  if (Number.isNaN(data.getTime())) return dataIso
  return data.toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

async function carregarEstado() {
  const draft = await db.cache.get(getDraftKey())
  temRascunho.value = Boolean(draft?.dados)

  const operadorId = authStore.usuario?.id

  if (!operadorId) {
    resumo.value = { pendentes: 0, sincronizados: 0, erro: 0 }
    return
  }

  // Filtra os formulários pelo operador logado
  const todosFormularios = await db.formularios.where('operadorId').equals(operadorId).toArray()

  resumo.value = consolidarResumoPorPk(todosFormularios)

  const formulariosComErro = todosFormularios
    .filter(f => f.status === 'erro')
    .sort((a, b) => new Date(b.ultimaTentativaEm || 0) - new Date(a.ultimaTentativaEm || 0))

  const erroMaisRecente = formulariosComErro[0]
  ultimoErro.value = mensagemErroAmigavel(erroMaisRecente?.erroMensagem)

  const agendaSalva = await db.cache.get(getAgendaKey())
  agenda.value = agendaSalva?.dados?.itens || []

  const formulariosRecentes = todosFormularios
    .sort((a, b) => new Date(b.criadoEm || 0) - new Date(a.criadoEm || 0))

  const locaisUnicos = new Map()
  for (const item of formulariosRecentes) {
    const campos = item.campos || {}
    const local = campos.estacao || campos.empresa || campos.elementoNome || campos.municipio || ''
    if (!local) {
      continue
    }

    const chaveLocal = normalizarChaveLocal(local)
    if (!chaveLocal || locaisUnicos.has(chaveLocal)) {
      continue
    }

    locaisUnicos.set(chaveLocal, {
      id: item.id,
      local: String(local).trim(),
      data: formatarDataAgenda(item.sincronizadoEm || item.criadoEm || '')
    })

    if (locaisUnicos.size >= 5) {
      break
    }
  }

  ultimosLocais.value = Array.from(locaisUnicos.values())
}

async function iniciarFormulario() {
  await db.cache.delete(getDraftKey())
  await router.push({ path: '/operador/formulario', query: { novo: Date.now().toString() } })
}

function continuarRascunho() {
  router.push('/operador/formulario')
}

async function sincronizarAgora() {
  if (!(await checkConnectivity())) {
    mensagem.value = 'Sem internet no momento. Os registros continuam salvos localmente.'
    return
  }

  const operadorId = authStore.usuario?.id

  const [pendentesAntes, errosAntes] = await Promise.all([
    db.formularios.where({ status: 'completo', operadorId }).count(),
    db.formularios.where({ status: 'erro', operadorId }).count(),
  ])

  if (pendentesAntes + errosAntes === 0) {
    mensagem.value = 'Nenhum registro pendente ou com erro para sincronizar.'
    await carregarEstado()
    return
  }

  sincronizando.value = true
  mensagem.value = ''

  try {
    const resultado = await sincronizarPendentes()
    await carregarEstado()

    if (resultado.enviados > 0 && resultado.erros === 0) {
      mensagem.value = `Sincronização concluída: ${resultado.enviados} registro(s) enviados com sucesso.`
      return
    }

    if (resultado.enviados > 0 && resultado.erros > 0) {
      const ultimoErroSync = resultado.resultados.find(r => r.status === 'erro')?.mensagem
      mensagem.value = `Sincronização parcial: ${resultado.enviados} enviado(s) com sucesso e ${resultado.erros} com erro.`
      if (ultimoErroSync) {
        mensagem.value += ` Último erro: ${ultimoErroSync}`
      }
      return
    }

    if (resultado.erros > 0) {
      const ultimoErroSync = resultado.resultados.find(r => r.status === 'erro')?.mensagem
      mensagem.value = `Sincronização falhou: ${resultado.erros} registro(s) continuam com erro.`
      if (ultimoErroSync) {
        mensagem.value += ` Último erro: ${ultimoErroSync}`
      }
      return
    }

    mensagem.value = 'Nenhum registro pendente precisou ser reenviado.'
  } finally {
    sincronizando.value = false
  }
}

function abrirModalLogout() {
  modalLogoutAberto.value = true
}

function fecharModalLogout() {
  modalLogoutAberto.value = false
}

async function encerrarSessao() {
  modalLogoutAberto.value = false
  await authStore.logout()
  await router.push('/login')
}

onMounted(async () => {
  await checkConnectivity()
  await carregarEstado()
})
</script>

<style scoped>
.operator-home {
  min-height: 100dvh;
  background: color-mix(in srgb, var(--cptm-bg) 92%, #ffffff 8%);
}

.operator-home__header,
.operator-home__main {
  max-width: 980px;
  margin: 0 auto;
}

.operator-home__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 1.1rem 1.25rem;
}

.operator-home__brand {
  display: flex;
  align-items: center;
  gap: 0.85rem;
}

.operator-home__logo {
  width: 48px;
  height: 48px;
  object-fit: contain;
}

.operator-home__title {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 800;
  color: var(--cptm-text);
}

.operator-home__subtitle {
  margin: 0.15rem 0 0;
  color: color-mix(in srgb, var(--cptm-text) 72%, #000 28%);
  font-size: 0.8rem;
}

.operator-home__header-actions {
  display: flex;
  align-items: center;
  gap: 0.65rem;
}

.operator-home__status,
.operator-home__icon-button,
.operator-home__exit-button,
.operator-home__secondary-button,
.operator-home__primary-button {
  border-radius: 0.9rem;
  font-weight: 700;
}

.operator-home__status {
  padding: 0.55rem 0.85rem;
  font-size: 0.75rem;
}

.operator-home__status--online {
  background: rgba(34, 197, 94, 0.14);
  color: #15803d;
}

.operator-home__status--offline {
  background: rgba(245, 158, 11, 0.18);
  color: #b45309;
}

.operator-home__icon-button,
.operator-home__exit-button {
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface);
  color: var(--cptm-text);
}

.operator-home__icon-button {
  width: 2.5rem;
  height: 2.5rem;
  display: grid;
  place-items: center;
  cursor: pointer;
}

.operator-home__exit-button {
  padding: 0.7rem 1rem;
  cursor: pointer;
}

.operator-home__main {
  padding: 0 1.25rem 2rem;
}

.operator-home__welcome-card,
.operator-home__panel {
  border: 1px solid var(--cptm-border);
  border-radius: 1.25rem;
  background: var(--cptm-surface);
}

.operator-home__welcome-card {
  padding: 1.5rem;
}

.operator-home__welcome-label {
  margin: 0 0 0.4rem;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: color-mix(in srgb, var(--cptm-primary) 78%, #1f2937 22%);
}

.operator-home__welcome-title {
  margin: 0;
  font-size: 1.85rem;
  color: var(--cptm-text);
}

.operator-home__welcome-text,
.operator-home__panel-text,
.operator-home__feedback {
  margin: 0.7rem 0 0;
  color: color-mix(in srgb, var(--cptm-text) 75%, #000 25%);
  line-height: 1.5;
}

.operator-home__primary-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.8rem;
  margin-top: 1.25rem;
}

.operator-home__primary-button,
.operator-home__secondary-button {
  display: inline-flex;
  align-items: center;
  gap: 0.6rem;
  padding: 1rem 1.1rem;
  cursor: pointer;
}

.operator-home__primary-button {
  border: none;
  background: var(--cptm-primary);
  color: #fff;
}

.operator-home__secondary-button {
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface-alt);
  color: var(--cptm-text);
}

.operator-home__secondary-button--compact {
  margin-top: 1rem;
}

.operator-home__grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-top: 1rem;
}

.operator-home__agenda-grid {
  display: grid;
  grid-template-columns: 1.1fr 0.9fr;
  gap: 1rem;
  margin-top: 1rem;
}

.operator-home__panel {
  padding: 1.25rem;
}

.operator-home__panel-title {
  margin: 0;
  color: var(--cptm-text);
  font-size: 1rem;
}

.operator-home__summary-list {
  display: grid;
  gap: 0.7rem;
  margin-top: 1rem;
}

.operator-home__summary-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-radius: 0.9rem;
  background: var(--cptm-surface-alt);
  border: 1px solid var(--cptm-border);
  padding: 0.85rem 1rem;
  color: var(--cptm-text);
}

.operator-home__panel-text--tight {
  margin-top: 0.45rem;
}

.operator-home__error-box {
  margin-top: 0.9rem;
  padding: 0.85rem 0.95rem;
  border-radius: 0.95rem;
  border: 1px solid rgba(185, 28, 28, 0.18);
  background: rgba(239, 68, 68, 0.08);
}

.operator-home__error-label {
  margin: 0;
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: #b91c1c;
}

.operator-home__error-text {
  margin: 0.35rem 0 0;
  font-size: 0.82rem;
  line-height: 1.5;
  color: var(--cptm-text);
}

.operator-home__agenda-list,
.operator-home__visit-list {
  display: grid;
  gap: 0.75rem;
  margin-top: 1rem;
}

.operator-home__agenda-item,
.operator-home__visit-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.85rem;
  padding: 0.9rem 1rem;
  border: 1px solid var(--cptm-border);
  border-radius: 1rem;
  background: var(--cptm-surface-alt);
}

.operator-home__visit-item {
  justify-content: flex-start;
}

.operator-home__agenda-local {
  margin: 0;
  color: var(--cptm-text);
  font-weight: 700;
}

.operator-home__agenda-meta {
  margin: 0.2rem 0 0;
  color: color-mix(in srgb, var(--cptm-text) 70%, #000 30%);
  font-size: 0.82rem;
}


@media (max-width: 720px) {
  .operator-home__header {
    flex-direction: column;
    align-items: center;
    text-align: center;
  }

  .operator-home__brand {
    width: 100%;
    justify-content: center;
    text-align: center;
  }

  .operator-home__header-actions {
    display: grid;
    grid-template-columns: 1fr auto auto;
    width: 100%;
  }

  .operator-home__grid {
    grid-template-columns: 1fr;
  }

  .operator-home__agenda-grid {
    grid-template-columns: 1fr;
  }

  .operator-home__primary-actions {
    display: grid;
  }
}

.operator-primary-action,
.operator-secondary-action {
  border-radius: 1rem;
  padding: 0.95rem 1.1rem;
  font-size: 0.92rem;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 0.55rem;
  cursor: pointer;
}

.operator-primary-action {
  border: none;
  background: #4C6246;
  color: #fff;
}

.operator-secondary-action {
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface);
  color: var(--cptm-text);
}

.operator-metrics-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 1rem;
  margin-top: 1.25rem;
}

.operator-metric-card,
.operator-panel {
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface);
  border-radius: 1.25rem;
}

.operator-metric-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
}

.operator-metric-icon {
  width: 2.9rem;
  height: 2.9rem;
  border-radius: 1rem;
  display: grid;
  place-items: center;
}

.operator-metric-icon--green { background: rgba(34, 197, 94, 0.12); color: #16a34a; }
.operator-metric-icon--blue { background: rgba(59, 130, 246, 0.12); color: #2563eb; }
.operator-metric-icon--amber { background: rgba(245, 158, 11, 0.14); color: #d97706; }
.operator-metric-icon--slate { background: rgba(100, 116, 139, 0.14); color: #475569; }

.operator-metric-label {
  margin: 0;
  font-size: 0.8rem;
  color: var(--cptm-text-muted);
}

.operator-metric-value {
  margin: 0.2rem 0 0;
  font-size: 1.6rem;
  font-weight: 800;
  color: var(--cptm-text);
}

.operator-panels-grid {
  display: grid;
  grid-template-columns: 1.2fr 0.9fr;
  gap: 1rem;
  margin-top: 1rem;
}

.operator-panel {
  padding: 1.25rem;
}

.operator-panel-head {
  margin-bottom: 1rem;
}

.operator-panel-title {
  margin: 0;
  font-size: 1rem;
  color: var(--cptm-text);
}

.operator-panel-subtitle {
  margin: 0.3rem 0 0;
  font-size: 0.8rem;
  color: var(--cptm-text-muted);
}

.operator-action-list {
  display: grid;
  gap: 0.75rem;
}

.operator-action-card {
  border: 1px solid var(--cptm-border);
  border-radius: 1rem;
  background: var(--cptm-surface-alt);
  color: var(--cptm-text);
  padding: 1rem;
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  align-items: center;
  text-align: left;
  cursor: pointer;
}

.operator-action-title {
  margin: 0;
  font-size: 0.92rem;
  font-weight: 700;
}

.operator-action-text,
.operator-activity-text,
.operator-feedback {
  margin: 0.3rem 0 0;
  color: var(--cptm-text-muted);
  font-size: 0.8rem;
}

.operator-activity-box {
  border-radius: 1rem;
  border: 1px dashed var(--cptm-border);
  background: var(--cptm-surface-alt);
  padding: 1rem;
}

.operator-activity-label {
  margin: 0;
  font-size: 0.74rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--cptm-text-muted);
}

.operator-activity-title {
  margin: 0.45rem 0 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--cptm-text);
}

@media (max-width: 960px) {
  .operator-metrics-grid,
  .operator-panels-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .operator-hero {
    flex-direction: column;
    align-items: stretch;
  }
}

@media (max-width: 640px) {
  .operator-shell-inner,
  .operator-shell-main {
    padding-left: 1rem;
    padding-right: 1rem;
  }

  .operator-shell-inner,
  .operator-brand,
  .operator-shell-actions,
  .operator-hero-actions,
  .operator-metrics-grid,
  .operator-panels-grid {
    grid-template-columns: 1fr;
  }

  .operator-shell-inner {
    flex-direction: column;
    align-items: stretch;
  }

  .operator-shell-actions {
    justify-content: flex-end;
  }

  .operator-metrics-grid,
  .operator-panels-grid {
    display: grid;
  }
}
</style>