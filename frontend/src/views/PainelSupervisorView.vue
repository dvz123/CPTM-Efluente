<template>
  <div class="supervisor-view">
    <!-- Header -->
    <header class="supervisor-header">
      <div class="supervisor-header__brand">
        <div class="supervisor-header__icon">
          <ShieldIcon class="w-5 h-5 text-white" />
        </div>
        <div>
          <h1 class="supervisor-header__title">Supervisor</h1>
          <p class="supervisor-header__subtitle">CCA - CPTM Ambiental</p>
        </div>
      </div>

      <div class="supervisor-header__actions">
        <span class="supervisor-header__name">{{ authStore.usuario?.nome }}</span>
        <button @click="themeStore.toggle()" class="supervisor-header__btn-icon" :title="themeStore.isDark ? 'Modo claro' : 'Modo escuro'">
          <SunIcon v-if="themeStore.isDark" class="w-4 h-4" />
          <MoonIcon v-else class="w-4 h-4" />
        </button>
        <button @click="abrirModalLogout" class="supervisor-header__btn-exit">Sair</button>
      </div>
    </header>

    <!-- Área principal: Mapa -->
    <main class="supervisor-main">
      <SupervisorMap
        :registros="registrosFiltrados"
        :centro="mapCenter"
        :zoom="mapZoom"
        @marcador-click="abrirDetalheRegistro"
      />

      <!-- Overlay de contagem -->
      <div class="supervisor-map-overlay">
        <div class="supervisor-chip">
          <MapPinIcon class="w-3.5 h-3.5" />
          <span>{{ registrosFiltrados.length }} registros no mapa</span>
        </div>
      </div>
    </main>

    <!-- Barra de ação -->
    <nav class="supervisor-actionbar">
      <button class="supervisor-action" @click="$router.push('/supervisor/usuarios')">
        <UserIcon class="w-4 h-4" />
        <span>Usuários</span>
      </button>


      <button class="supervisor-action" @click="$router.push('/supervisor/relatorios')">
        <FileBarChartIcon class="w-4 h-4" />
        <span>Relatórios</span>
      </button>


      <button class="supervisor-action" @click="mostrarLista = !mostrarLista">
        <ListIcon class="w-4 h-4" />
        <span>Lista</span>
      </button>

      <button class="supervisor-action" @click="recarregar">
        <RefreshCwIcon class="w-4 h-4" :class="carregando ? 'animate-spin' : ''" />
        <span>Atualizar</span>
      </button>
    </nav>

    <!-- Painel de filtros (expansível) -->
    <Transition name="slide-up">
      <div v-if="mostrarFiltros" class="supervisor-panel" :style="{ backgroundColor: 'var(--cptm-surface)' }">
        <div class="supervisor-panel__header">
          <h3 class="supervisor-panel__title">Filtros</h3>
          <button @click="mostrarFiltros = false" class="supervisor-panel__close">
            <XIcon class="w-4 h-4" />
          </button>
        </div>
        <div class="supervisor-filters">
          <select v-model="filtroTipo" class="field-input">
            <option value="">Todos os tipos</option>
            <option value="efluente">Efluente</option>
            <option value="arvore">Árvore</option>
            <option value="fauna">Fauna</option>
            <option value="erosao">Erosão</option>
            <option value="residuo">Resíduo</option>
          </select>
          <input v-model="filtroOperador" class="field-input" placeholder="Buscar operador..." maxlength="200" />
          <input v-model="filtroDataInicio" type="date" class="field-input" />
          <input v-model="filtroDataFim" type="date" class="field-input" />
          <button @click="limparFiltros" class="supervisor-btn-secondary text-xs">Limpar</button>
        </div>
      </div>
    </Transition>

    <!-- Painel de lista (expansível) -->
    <Transition name="slide-up">
      <div v-if="mostrarLista" class="supervisor-panel supervisor-panel--lista" :style="{ backgroundColor: 'var(--cptm-surface)' }">
        <div class="supervisor-panel__header">
          <h3 class="supervisor-panel__title">Registros</h3>
          <button @click="mostrarLista = false" class="supervisor-panel__close">
            <XIcon class="w-4 h-4" />
          </button>
        </div>

        <div class="supervisor-lista">
          <div v-for="(reg, idx) in registrosFiltradosPaginados" :key="reg.pk || `${reg.dataCadastro || 'sem-data'}-${idx}`"
               class="supervisor-lista__item"
               :style="{ borderColor: 'var(--cptm-border)' }"
               @click="centralizarNoRegistro(reg)">
            <div class="supervisor-lista__meta">
              <span class="supervisor-lista__badge" :class="tipoBadgeClass((reg.natureza||'').toLowerCase())">
                {{ reg.natureza }}
              </span>
              <span class="supervisor-lista__data">{{ reg.dataCadastro ? formatarData(reg.dataCadastro) : '—' }}</span>
            </div>
            <p class="supervisor-lista__local">{{ reg.estacao || reg.municipio || 'Local não informado' }}</p>
            <p class="supervisor-lista__autor">{{ reg.autor }} · {{ reg.pk }}</p>
            <div class="supervisor-lista__acoes">
              <a :href="`https://www.google.com/maps?q=${reg.latitude},${reg.longitude}`"
                 target="_blank" rel="noopener noreferrer"
                 class="supervisor-lista__link" @click.stop>
                <MapPinIcon class="w-3 h-3" /> Maps
              </a>
              <button @click.stop="editarRegistro(reg)" class="supervisor-lista__link">
                <PencilIcon class="w-3 h-3" /> Editar
              </button>
              <button @click.stop="confirmarExclusao(reg)" class="supervisor-lista__excluir">
                <Trash2Icon class="w-3 h-3" /> Excluir
              </button>
            </div>
          </div>

          <div v-if="registrosFiltrados.length === 0" class="supervisor-lista__vazio">
            Nenhum registro encontrado.
          </div>

          <div v-if="registrosFiltrados.length > itensPorPagina" class="supervisor-paginacao">
            <button @click="pagina--" :disabled="pagina <= 1" class="supervisor-paginacao__btn">←</button>
            <span class="supervisor-paginacao__info">{{ pagina }} / {{ totalPaginas }}</span>
            <button @click="pagina++" :disabled="pagina >= totalPaginas" class="supervisor-paginacao__btn">→</button>
          </div>
        </div>
      </div>
    </Transition>


    <!-- Modal confirmação exclusão -->
    <Teleport to="body">
      <div v-if="registroParaExcluir" class="fixed inset-0 z-[100] flex items-center justify-center bg-black/50 px-4">
        <div class="rounded-2xl p-6 max-w-sm w-full shadow-2xl" :style="{ backgroundColor: 'var(--cptm-surface)' }">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-full bg-red-100 dark:bg-red-950/50 flex items-center justify-center">
              <AlertTriangleIcon class="w-5 h-5 text-red-500" />
            </div>
            <h3 class="text-base font-bold" :style="{ color: 'var(--cptm-text)' }">Confirmar Exclusão</h3>
          </div>
          <p class="text-sm mb-6" :style="{ color: 'var(--cptm-text-muted)' }">
            Deseja excluir permanentemente o registro <strong>{{ registroParaExcluir.pk }}</strong>
            ({{ registroParaExcluir.natureza }})? Esta ação não pode ser desfeita.
          </p>
          <div class="flex gap-3">
            <button @click="registroParaExcluir = null"
                    class="flex-1 py-2.5 rounded-xl border text-sm font-medium cursor-pointer"
                    :style="{ borderColor: 'var(--cptm-border)', color: 'var(--cptm-text)' }">
              Cancelar
            </button>
            <button @click="excluirRegistro"
                    class="flex-1 py-2.5 rounded-xl text-white text-sm font-medium cursor-pointer"
                    style="background-color: var(--cptm-danger);">
              Excluir
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <Teleport to="body">
      <div v-if="modalLogoutAberto" class="fixed inset-0 z-[110] flex items-center justify-center bg-black/50 px-4" @click.self="fecharModalLogout">
        <div class="rounded-2xl p-6 max-w-sm w-full shadow-2xl" :style="{ backgroundColor: 'var(--cptm-surface)' }">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-full bg-amber-100 dark:bg-amber-950/50 flex items-center justify-center">
              <AlertTriangleIcon class="w-5 h-5 text-amber-600" />
            </div>
            <h3 class="text-base font-bold" :style="{ color: 'var(--cptm-text)' }">Confirmar saída</h3>
          </div>
          <p class="text-sm mb-6" :style="{ color: 'var(--cptm-text-muted)' }">
            Deseja realmente sair da sessão?
          </p>
          <div class="flex gap-3">
            <button @click="fecharModalLogout"
                    class="flex-1 py-2.5 rounded-xl border text-sm font-medium cursor-pointer"
                    :style="{ borderColor: 'var(--cptm-border)', color: 'var(--cptm-text)' }">
              Cancelar
            </button>
            <button @click="fazerLogout"
                    class="flex-1 py-2.5 rounded-xl text-white text-sm font-medium cursor-pointer"
                    style="background-color: var(--cptm-danger);">
              Sair
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import axios from 'axios'
import SupervisorMap from '@/components/SupervisorMap.vue'
import {
  Shield as ShieldIcon,
  Sun as SunIcon,
  Moon as MoonIcon,
  User as UserIcon,
  List as ListIcon,
  RefreshCw as RefreshCwIcon,
  X as XIcon,
  MapPin as MapPinIcon,
  Trash2 as Trash2Icon,
  AlertTriangle as AlertTriangleIcon,
  FileBarChart as FileBarChartIcon,
  Pencil as PencilIcon
} from 'lucide-vue-next'


const router = useRouter()
const authStore = useAuthStore()
const themeStore = useThemeStore()

const registros = ref([])
const carregando = ref(false)
const filtroTipo = ref('')
const filtroOperador = ref('')
const filtroDataInicio = ref('')
const filtroDataFim = ref('')
const registroParaExcluir = ref(null)
const modalLogoutAberto = ref(false)

const mostrarFiltros = ref(false)
const mostrarLista = ref(true)
const pagina = ref(1)

const itensPorPagina = 10
const mapCenter = ref([-46.6388, -23.5489])
const mapZoom = ref(11)


const registrosFiltrados = computed(() => {
  const filtrados = registros.value.filter(r => {
    if (filtroTipo.value && (r.natureza || '').toLowerCase() !== filtroTipo.value) return false
    if (filtroOperador.value && !(r.autor || '').toLowerCase().includes(filtroOperador.value.toLowerCase())) return false
    if (filtroDataInicio.value && r.dataCadastro) {
      const d = new Date(r.dataCadastro).toISOString().slice(0, 10)
      if (d < filtroDataInicio.value) return false
    }
    if (filtroDataFim.value && r.dataCadastro) {
      const d = new Date(r.dataCadastro).toISOString().slice(0, 10)
      if (d > filtroDataFim.value) return false
    }
    return true
  })

  return filtrados.sort((a, b) => new Date(b.dataCadastro) - new Date(a.dataCadastro))
})


const totalPaginas = computed(() => Math.ceil(registrosFiltrados.value.length / itensPorPagina) || 1)

const registrosFiltradosPaginados = computed(() => {
  const inicio = (pagina.value - 1) * itensPorPagina
  return registrosFiltrados.value.slice(inicio, inicio + itensPorPagina)
})

function tipoBadgeClass(tipo) {
  const classes = {
    efluente: 'bg-blue-200 text-blue-900 dark:bg-blue-900/30 dark:text-blue-300',
    arvore: 'bg-green-200 text-green-900 dark:bg-green-900/30 dark:text-green-300',
    fauna: 'bg-amber-200 text-amber-900 dark:bg-amber-900/30 dark:text-amber-300',
    erosao: 'bg-orange-200 text-orange-900 dark:bg-orange-900/30 dark:text-orange-300',
    residuo: 'bg-purple-200 text-purple-900 dark:bg-purple-900/30 dark:text-purple-300'
  }
  return classes[tipo] || 'bg-gray-200 text-gray-900'
}

function formatarData(data) {
  return new Date(data).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function limparFiltros() {
  filtroTipo.value = ''
  filtroOperador.value = ''
  filtroDataInicio.value = ''
  filtroDataFim.value = ''
  pagina.value = 1
}

function centralizarNoRegistro(reg) {
  if (reg.latitude != null && reg.longitude != null) {
    mapCenter.value = [reg.longitude, reg.latitude]
    mapZoom.value = 16
  }
  mostrarLista.value = false
}

function abrirDetalheRegistro(reg) {
  centralizarNoRegistro(reg)
}

function confirmarExclusao(registro) {
  registroParaExcluir.value = registro
}

function editarRegistro(registro) {
  if (!registro?.pk) {
    alert('Registro sem identificador (PK). Atualize a lista e tente novamente.')
    return
  }
  router.push(`/operador/formulario?editar=${encodeURIComponent(registro.pk)}`)
}

async function excluirRegistro() {
  if (!registroParaExcluir.value) return
  if (!registroParaExcluir.value?.pk) {
    alert('Registro sem identificador (PK). Não foi possível excluir.')
    registroParaExcluir.value = null
    return
  }

  try {
    await axios.delete(`/api/formularios/${encodeURIComponent(registroParaExcluir.value.pk)}`)
    registros.value = registros.value.filter(r => r.pk !== registroParaExcluir.value.pk)
    registroParaExcluir.value = null
  } catch (e) {
    alert('Erro ao excluir: ' + (e.response?.data?.mensagem || e.message))
  }
}

async function carregarRegistros() {
  carregando.value = true
  try {
    const res = await axios.get('/api/formularios')
    registros.value = res.data
  } catch (e) {
    console.error('Erro ao carregar registros:', e)
  } finally {
    carregando.value = false
  }
}

function recarregar() {
  pagina.value = 1
  carregarRegistros()
}

function onUsuarioCadastrado() {
  // Pode ser usado para atualizar lista de usuários no futuro
}

function abrirModalLogout() {
  modalLogoutAberto.value = true
}

function fecharModalLogout() {
  modalLogoutAberto.value = false
}

async function fazerLogout() {
  modalLogoutAberto.value = false
  await authStore.logout()
  router.push('/login')
}

onMounted(carregarRegistros)
</script>

<style scoped>
.supervisor-view {
  display: flex;
  flex-direction: column;
  height: 100dvh;
  overflow: hidden;
  background: var(--cptm-bg);
}

/* Header */
.supervisor-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.75rem 1rem;
  background: var(--cptm-header);
  color: var(--cptm-header-text);
  flex-shrink: 0;
  z-index: 20;
}

.supervisor-header__brand {
  display: flex;
  align-items: center;
  gap: 0.65rem;
}

.supervisor-header__icon {
  width: 2rem;
  height: 2rem;
  border-radius: 0.6rem;
  display: grid;
  place-items: center;
  background: var(--cptm-primary);
}

.supervisor-header__title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 800;
  line-height: 1.2;
}

.supervisor-header__subtitle {
  margin: 0;
  font-size: 0.68rem;
  opacity: 0.7;
  line-height: 1.2;
}

.supervisor-header__actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.supervisor-header__name {
  display: none;
  font-size: 0.8rem;
  opacity: 0.8;
}

@media (min-width: 640px) {
  .supervisor-header__name {
    display: block;
  }
}

.supervisor-header__btn-icon,
.supervisor-header__btn-exit {
  border-radius: 0.7rem;
  display: grid;
  place-items: center;
  cursor: pointer;
  color: var(--cptm-header-text);
}

.supervisor-header__btn-icon {
  width: 2.1rem;
  height: 2.1rem;
  border: 1px solid rgba(255, 255, 255, 0.15);
  background: transparent;
}

.supervisor-header__btn-exit {
  padding: 0.5rem 0.85rem;
  font-size: 0.78rem;
  font-weight: 700;
  border: 1px solid rgba(255, 255, 255, 0.15);
  background: transparent;
}


/* Main / Mapa */
.supervisor-main {
  position: relative;
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

@media (max-width: 1023px) {
  .supervisor-main {
    flex: 0 0 40vh;
  }
}


.supervisor-map-overlay {
  position: absolute;
  top: 0.75rem;
  left: 0.75rem;
  z-index: 10;
  pointer-events: none;
}

.supervisor-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.75rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;
  background: var(--cptm-surface);
  color: var(--cptm-text);
  border: 1px solid var(--cptm-border);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  pointer-events: auto;
}


/* Action Bar */
.supervisor-actionbar {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
  padding: 0.75rem;
  background: var(--cptm-surface);
  border-top: 1px solid var(--cptm-border);
  flex-shrink: 0;
}

.supervisor-action {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  padding: 0.65rem 0.5rem;
  border-radius: 0.85rem;
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface-alt);
  color: var(--cptm-text);
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
  white-space: nowrap;
}


.supervisor-action:hover {
  background: var(--cptm-bg);
}

.supervisor-action:active {
  transform: scale(0.97);
}



/* Painéis expansíveis */
.supervisor-panel {
  flex-shrink: 0;
  max-height: 45vh;
  display: flex;
  flex-direction: column;
  border-top: 1px solid var(--cptm-border);
}

.supervisor-panel--lista {
  height: calc(60vh - 120px);
  min-height: 260px;
  max-height: 480px;
  flex: 0 0 auto;
  display: flex;
  flex-direction: column;
}


.supervisor-panel__header {

  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.85rem 1rem;
  border-bottom: 1px solid var(--cptm-border);
  flex-shrink: 0;
}

.supervisor-panel__title {
  margin: 0;
  font-size: 0.9rem;
  font-weight: 800;
  color: var(--cptm-text);
}

.supervisor-panel__close {
  width: 1.75rem;
  height: 1.75rem;
  display: grid;
  place-items: center;
  border-radius: 0.5rem;
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface-alt);
  color: var(--cptm-text-muted);
  cursor: pointer;
}

/* Filtros */
.supervisor-filters {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.6rem;
  padding: 0.85rem 1rem;
  overflow-y: auto;
}

@media (min-width: 640px) {
  .supervisor-filters {
    grid-template-columns: repeat(4, 1fr) auto;
  }
}

.supervisor-btn-secondary {
  padding: 0.55rem 0.9rem;
  border-radius: 0.75rem;
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface-alt);
  color: var(--cptm-text);
  font-weight: 700;
  cursor: pointer;
}


/* Lista */

.supervisor-lista {
  overflow-y: auto;
  padding: 0.5rem 1rem 1rem;
  display: grid;
  gap: 0.6rem;
  flex: 1;
  min-height: 0;
}



.supervisor-lista__item {
  padding: 0.85rem 1rem;
  border-radius: 1rem;
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface-alt);
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.supervisor-lista__item:hover {
  background: var(--cptm-bg);
}

.supervisor-lista__meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-bottom: 0.35rem;
}

.supervisor-lista__badge {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  font-size: 0.7rem;
  font-weight: 700;
}

.supervisor-lista__data {
  font-size: 0.72rem;
  color: color-mix(in srgb, var(--cptm-text) 75%, #000 25%);
}

.supervisor-lista__local {
  margin: 0;
  font-size: 0.88rem;
  font-weight: 700;
  color: var(--cptm-text);
}

.supervisor-lista__autor {
  margin: 0.15rem 0 0;
  font-size: 0.75rem;
  color: color-mix(in srgb, var(--cptm-text) 75%, #000 25%);
}

.supervisor-lista__acoes {
  display: flex;
  gap: 0.75rem;
  margin-top: 0.6rem;
}

.supervisor-lista__link,
.supervisor-lista__excluir {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.72rem;
  font-weight: 600;
  cursor: pointer;
  background: none;
  border: none;
  padding: 0;
}

.supervisor-lista__link {
  color: var(--cptm-info);
}

.supervisor-lista__editar {
  color: var(--cptm-primary);
}

.supervisor-lista__excluir {
  color: var(--cptm-danger);
}

.supervisor-lista__vazio {
  text-align: center;
  padding: 2rem;
  font-size: 0.85rem;
  color: var(--cptm-text-muted);
}

/* Paginação */
.supervisor-paginacao {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding-top: 0.5rem;
}

.supervisor-paginacao__btn {
  width: 2rem;
  height: 2rem;
  display: grid;
  place-items: center;
  border-radius: 0.6rem;
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface-alt);
  color: var(--cptm-text);
  font-weight: 700;
  cursor: pointer;
}

.supervisor-paginacao__btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.supervisor-paginacao__info {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--cptm-text-muted);
}

/* Transições */
.slide-up-enter-active,
.slide-up-leave-active {
  transition: transform 0.25s ease, opacity 0.25s ease;
}

.slide-up-enter-from,
.slide-up-leave-to {
  transform: translateY(20px);
  opacity: 0;
}

/* Desktop melhorias */

@media (max-width: 1023px) {
  .supervisor-panel--lista {
    height: calc(60vh - 120px);
    min-height: 260px;
    max-height: none;
  }
}

@media (min-width: 1024px) {
  .supervisor-view {
    display: grid;
    grid-template-columns: 1fr 380px;
    grid-template-rows: auto 1fr;
    grid-template-areas:
      "header header"
      "map sidebar";
  }

  .supervisor-header {
    grid-area: header;
  }

  .supervisor-main {
    grid-area: map;
    flex: 1;
  }

  .supervisor-actionbar {
    grid-area: sidebar;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 1rem;
    border-top: none;
    border-left: 1px solid var(--cptm-border);
    overflow-x: visible;
    overflow-y: auto;
  }

  .supervisor-action {
    justify-content: flex-start;
    padding: 0.75rem 1rem;
    font-size: 0.85rem;
  }


  .supervisor-panel,
  .supervisor-panel--lista {
    grid-area: sidebar;
    position: absolute;
    bottom: 0;
    right: 0;
    width: 380px;
    max-height: 70vh;
    border-left: 1px solid var(--cptm-border);
    border-top-left-radius: 1.25rem;
    box-shadow: -4px 0 24px rgba(0, 0, 0, 0.08);
    z-index: 30;
    display: flex;
    flex-direction: column;
  }

  .supervisor-panel--lista {
    height: auto;
    max-height: 480px;
    min-height: 0;
  }
}
</style>
