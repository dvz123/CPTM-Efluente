

<template>
  <div class="relatorios-view">
    <!-- Header -->
    <header class="relatorios-header">
      <div class="relatorios-header__brand">
        <div class="relatorios-header__icon">
          <FileBarChartIcon class="w-5 h-5 text-white" />
        </div>
        <div>
          <h1 class="relatorios-header__title">Relatórios</h1>
          <p class="relatorios-header__subtitle">Análises e métricas CCA - CPTM Ambiental</p>
        </div>
      </div>

      <div class="relatorios-header__actions">
        <span class="relatorios-header__nome">{{ authStore.usuario?.nome }}</span>
        <button @click="themeStore.toggle()" class="relatorios-header__btn-icon" :title="themeStore.isDark ? 'Modo claro' : 'Modo escuro'">
          <SunIcon v-if="themeStore.isDark" class="w-4 h-4" />
          <MoonIcon v-else class="w-4 h-4" />
        </button>
        <button @click="router.back()" class="relatorios-header__btn-voltar">
          <ArrowLeftIcon class="w-4 h-4" />
          Voltar
        </button>
      </div>
    </header>

    <!-- Main Content -->
    <main class="relatorios-main">
      <!-- Cards de Resumo -->
      <div class="relatorios-summary">
        <div class="summary-card">
          <div class="summary-card__icon bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400">
            <ClipboardListIcon class="w-5 h-5" />
          </div>
          <div class="summary-card__info">
            <p class="summary-card__label">Total de Registros</p>
            <h3 class="summary-card__value">{{ totalRegistros }}</h3>
          </div>
        </div>
        <div class="summary-card">
          <div class="summary-card__icon bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-400">
            <LeafIcon class="w-5 h-5" />
          </div>
          <div class="summary-card__info">
            <p class="summary-card__label">Árvores</p>
            <h3 class="summary-card__value">{{ totalArvores }}</h3>
          </div>
        </div>
        <div class="summary-card">
          <div class="summary-card__icon bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400">
            <BirdIcon class="w-5 h-5" />
          </div>
          <div class="summary-card__info">
            <p class="summary-card__label">Fauna</p>
            <h3 class="summary-card__value">{{ totalFauna }}</h3>
          </div>
        </div>
        <div class="summary-card">
          <div class="summary-card__icon bg-purple-100 text-purple-600 dark:bg-purple-900/30 dark:text-purple-400">
            <DropletsIcon class="w-5 h-5" />
          </div>
          <div class="summary-card__info">
            <p class="summary-card__label">Efluentes</p>
            <h3 class="summary-card__value">{{ totalEfluentes }}</h3>
          </div>
        </div>
      </div>

      <!-- Filtros -->
      <section class="relatorios-section">
        <h2 class="relatorios-section__title">Filtros</h2>
        <div class="relatorios-filters">
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
          <button @click="aplicarFiltros" class="relatorios-btn-primary">Aplicar</button>
        </div>
      </section>

      <!-- Gráficos (Placeholder) -->
      <section class="relatorios-section flex-1">
        <div class="relatorios-charts-placeholder">
          <BarChart2Icon class="w-12 h-12 text-gray-300 dark:text-gray-700 mb-3" />
          <p>Carregando gráficos...</p>
        </div>
      </section>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import axios from 'axios'
import {
  FileBarChart as FileBarChartIcon,
  Sun as SunIcon,
  Moon as MoonIcon,
  ArrowLeft as ArrowLeftIcon,
  ClipboardList as ClipboardListIcon,
  Leaf as LeafIcon,
  Bird as BirdIcon,
  Droplets as DropletsIcon,
  BarChart2 as BarChart2Icon
} from 'lucide-vue-next'

const router = useRouter()
const authStore = useAuthStore()
const themeStore = useThemeStore()

const registros = ref([])
const filtroTipo = ref('')
const filtroOperador = ref('')
const filtroDataInicio = ref('')
const filtroDataFim = ref('')

const registrosFiltrados = computed(() => {
  return registros.value.filter(r => {
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
})

const totalRegistros = computed(() => registrosFiltrados.value.length)
const totalArvores = computed(() => registrosFiltrados.value.filter(r => (r.natureza || '').toLowerCase() === 'arvore').length)
const totalFauna = computed(() => registrosFiltrados.value.filter(r => (r.natureza || '').toLowerCase() === 'fauna').length)
const totalEfluentes = computed(() => registrosFiltrados.value.filter(r => (r.natureza || '').toLowerCase() === 'efluente').length)

async function carregarRegistros() {
  try {
    const res = await axios.get('/api/formularios')
    registros.value = res.data
  } catch (e) {
    console.error('Erro ao carregar registros:', e)
  }
}

function aplicarFiltros() {
  // A reatividade do computed já cuida disso, mas podemos adicionar lógica extra aqui se necessário
}

onMounted(carregarRegistros)
</script>

<style scoped>
.relatorios-view {
  display: flex;
  flex-direction: column;
  height: 100dvh;
  background: var(--cptm-bg);
  overflow-y: auto;
}

/* Header */
.relatorios-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.75rem 1rem;
  background: var(--cptm-header);
  color: var(--cptm-header-text);
  flex-shrink: 0;
  z-index: 20;
  position: sticky;
  top: 0;
}

.relatorios-header__brand {
  display: flex;
  align-items: center;
  gap: 0.65rem;
}

.relatorios-header__icon {
  width: 2rem;
  height: 2rem;
  border-radius: 0.6rem;
  display: grid;
  place-items: center;
  background: var(--cptm-primary);
}

.relatorios-header__title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 800;
  line-height: 1.2;
}

.relatorios-header__subtitle {
  margin: 0;
  font-size: 0.68rem;
  opacity: 0.7;
  line-height: 1.2;
}

.relatorios-header__actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.relatorios-header__nome {
  display: none;
  font-size: 0.8rem;
  opacity: 0.8;
}

@media (min-width: 640px) {
  .relatorios-header__nome {
    display: block;
  }
}

.relatorios-header__btn-icon,
.relatorios-header__btn-voltar {
  border-radius: 0.7rem;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  cursor: pointer;
  color: var(--cptm-header-text);
  border: 1px solid rgba(255, 255, 255, 0.15);
  background: transparent;
  transition: background-color 0.2s;
}

.relatorios-header__btn-icon {
  width: 2.1rem;
  height: 2.1rem;
}

.relatorios-header__btn-voltar {
  padding: 0.5rem 0.85rem;
  font-size: 0.78rem;
  font-weight: 700;
}

.relatorios-header__btn-icon:hover,
.relatorios-header__btn-voltar:hover {
  background: rgba(255, 255, 255, 0.1);
}

/* Main Content */
.relatorios-main {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
}

/* Summary Cards */
.relatorios-summary {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.summary-card {
  background: var(--cptm-surface);
  border: 1px solid var(--cptm-border);
  border-radius: 1rem;
  padding: 1.25rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}

.summary-card__icon {
  width: 3rem;
  height: 3rem;
  border-radius: 0.75rem;
  display: grid;
  place-items: center;
}

.summary-card__info {
  display: flex;
  flex-direction: column;
}

.summary-card__label {
  font-size: 0.75rem;
  color: var(--cptm-text-muted);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.summary-card__value {
  font-size: 1.5rem;
  font-weight: 800;
  color: var(--cptm-text);
  margin: 0;
  line-height: 1.2;
}

/* Sections */
.relatorios-section {
  background: var(--cptm-surface);
  border: 1px solid var(--cptm-border);
  border-radius: 1rem;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}

.relatorios-section__title {
  font-size: 1.1rem;
  font-weight: 800;
  color: var(--cptm-text);
  margin-bottom: 1rem;
}

/* Filters */
.relatorios-filters {
  display: grid;
  grid-template-columns: 1fr;
  gap: 0.75rem;
}

@media (min-width: 640px) {
  .relatorios-filters {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (min-width: 1024px) {
  .relatorios-filters {
    grid-template-columns: repeat(4, 1fr) auto;
  }
}

.relatorios-btn-primary {
  padding: 0.625rem 1rem;
  border-radius: 0.5rem;
  background: var(--cptm-primary);
  color: white;
  font-weight: 700;
  font-size: 0.875rem;
  border: none;
  cursor: pointer;
  transition: background-color 0.2s;
}

.relatorios-btn-primary:hover {
  background: var(--cptm-primary-hover);
}

/* Charts Placeholder */
.relatorios-charts-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 300px;
  color: var(--cptm-text-muted);
  font-weight: 500;
  border: 2px dashed var(--cptm-border);
  border-radius: 0.75rem;
  background: var(--cptm-surface-alt);
}
</style>


