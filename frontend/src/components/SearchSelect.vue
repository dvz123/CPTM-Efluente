<template>
  <div class="relative" ref="wrapper">
    <button
      ref="trigger"
      type="button"
      class="field-input search-select-trigger flex items-center cursor-pointer text-left"
      @click="alternar"
      @keydown.esc.prevent="fechar"
    >
      <span v-if="textoSelecionado" :style="{ color: 'var(--cptm-text)' }">{{ textoSelecionado }}</span>
      <span v-else :style="{ color: 'var(--cptm-text-muted)' }">{{ placeholder }}</span>
      <ChevronDownIcon class="w-4 h-4 ml-auto shrink-0" :style="{ color: 'var(--cptm-text-muted)' }" />
    </button>

    <Teleport to="body">
      <Transition name="dropdown">
        <div
          v-if="aberto"
          ref="panel"
          class="search-select-panel fixed rounded-lg shadow-lg border overflow-hidden"
          :class="{ 'search-select-panel--up': abreParaCima }"
          :style="{
            ...panelStyle,
            backgroundColor: 'var(--cptm-surface)',
            borderColor: 'var(--cptm-border)'
          }"
        >
          <div class="sticky top-0 p-2" :style="{ backgroundColor: 'var(--cptm-surface)' }">
            <input
              v-if="mostrarBusca"
              v-model="busca"
              ref="inputBusca"
              class="field-input text-xs"
              :placeholder="searchPlaceholder"
              @click.stop
              @keydown.esc.prevent="fechar"
            />
          </div>

          <div class="search-select-options px-1 pb-1" :style="optionsStyle">
            <div
              v-for="op in opcoesFiltradas"
              :key="op.valor"
              class="px-3 py-2 text-sm rounded-md cursor-pointer transition-colors"
              :style="{
                color: 'var(--cptm-text)',
                backgroundColor: modelValue === op.valor ? 'rgba(22,163,74,0.1)' : 'transparent'
              }"
              @mouseenter="$event.target.style.backgroundColor = 'var(--cptm-surface-alt)'"
              @mouseleave="$event.target.style.backgroundColor = modelValue === op.valor ? 'rgba(22,163,74,0.1)' : 'transparent'"
              @click="selecionar(op.valor)"
            >
              {{ op.texto }}
            </div>
            <div v-if="opcoesFiltradas.length === 0" class="px-3 py-2 text-xs" :style="{ color: 'var(--cptm-text-muted)' }">
              {{ emptyText }}
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue'
import { ChevronDown as ChevronDownIcon } from 'lucide-vue-next'

const props = defineProps({
  modelValue: { type: String, default: '' },
  opcoes: { type: Array, default: () => [] },
  placeholder: { type: String, default: 'Selecione...' },
  searchPlaceholder: { type: String, default: 'Buscar opção...' },
  emptyText: { type: String, default: 'Nenhuma opção disponível.' },
  showSearch: { type: Boolean, default: null },
  searchMinOptions: { type: Number, default: 7 }
})

const emit = defineEmits(['update:modelValue'])

const aberto = ref(false)
const abreParaCima = ref(false)
const busca = ref('')
const wrapper = ref(null)
const trigger = ref(null)
const panel = ref(null)
const inputBusca = ref(null)
const panelStyle = ref({})
const optionsStyle = ref({})

function normalizeOption(option, index) {
  if (option == null) {
    return { valor: `opcao-${index}`, texto: '' }
  }

  if (typeof option === 'string' || typeof option === 'number') {
    const text = String(option)
    return { valor: text, texto: text }
  }

  const valor = option.valor ?? option.value ?? option.id ?? option.Id ?? option.codigo ?? option.Codigo ?? option.Nome ?? option.nome ?? option.texto ?? option.text ?? option.label ?? `opcao-${index}`
  const texto = option.texto ?? option.text ?? option.label ?? option.Nome ?? option.nome ?? option.descricao ?? option.Descricao ?? String(valor)

  return {
    ...option,
    valor: String(valor ?? ''),
    texto: String(texto ?? '')
  }
}

const opcoesNormalizadas = computed(() =>
  (props.opcoes || [])
    .map((option, index) => normalizeOption(option, index))
    .filter((option) => option.texto.trim().length > 0)
)

const textoSelecionado = computed(() =>
  opcoesNormalizadas.value.find((option) => option.valor === props.modelValue)?.texto || ''
)

const mostrarBusca = computed(() => {
  if (typeof props.showSearch === 'boolean') {
    return props.showSearch
  }

  return opcoesNormalizadas.value.length >= props.searchMinOptions
})

const opcoesFiltradas = computed(() => {
  if (!mostrarBusca.value || !busca.value) {
    return opcoesNormalizadas.value
  }

  const termo = busca.value.toLowerCase()
  return opcoesNormalizadas.value.filter((option) => option.texto.toLowerCase().includes(termo))
})

function selecionar(valor) {
  emit('update:modelValue', valor)
  fechar()
}

function fechar() {
  aberto.value = false
  busca.value = ''
}

function alternar() {
  aberto.value = !aberto.value
}

function ajustarPosicaoPanel() {
  const triggerElement = trigger.value
  if (!triggerElement) {
    return
  }

  const rect = triggerElement.getBoundingClientRect()
  const margemViewport = 8
  const folga = 6
  const viewportHeight = window.innerHeight
  const viewportWidth = window.innerWidth
  const largura = Math.min(Math.max(rect.width, 220), viewportWidth - margemViewport * 2)
  const left = Math.min(
    Math.max(rect.left, margemViewport),
    viewportWidth - largura - margemViewport
  )
  const espacoAbaixo = viewportHeight - rect.bottom - margemViewport
  const espacoAcima = rect.top - margemViewport
  const minimoPainel = viewportWidth <= 720 ? 180 : 220
  const abrirParaCima = espacoAbaixo < minimoPainel && espacoAcima > espacoAbaixo
  const alturaDisponivel = Math.max((abrirParaCima ? espacoAcima : espacoAbaixo) - folga, 140)
  const alturaPainel = Math.min(viewportWidth <= 720 ? 300 : 340, alturaDisponivel)
  const top = abrirParaCima
    ? Math.max(margemViewport, rect.top - alturaPainel - folga)
    : Math.min(viewportHeight - margemViewport - alturaPainel, rect.bottom + folga)
  const descontoBusca = mostrarBusca.value ? 62 : 10
  const alturaOpcoes = Math.max(alturaPainel - descontoBusca, 78)

  abreParaCima.value = abrirParaCima
  panelStyle.value = {
    top: `${top}px`,
    left: `${left}px`,
    width: `${largura}px`,
    maxHeight: `${alturaPainel}px`,
    zIndex: '1200'
  }
  optionsStyle.value = {
    maxHeight: `${alturaOpcoes}px`
  }
}

function garantirEspacoNaTela() {
  const triggerElement = trigger.value
  if (!triggerElement) {
    return
  }

  const rect = triggerElement.getBoundingClientRect()
  const viewportHeight = window.innerHeight
  const minimoPainel = viewportWidthPequena() ? 180 : 220
  const espacoAbaixo = viewportHeight - rect.bottom - 12
  const espacoAcima = rect.top - 12

  if (espacoAbaixo >= minimoPainel || espacoAcima > espacoAbaixo) {
    return
  }

  const ajuste = Math.min(minimoPainel - espacoAbaixo, Math.max(rect.top - 24, 0))
  if (ajuste > 0) {
    window.scrollBy({ top: ajuste, behavior: 'smooth' })
  }
}

function viewportWidthPequena() {
  return window.innerWidth <= 720
}

watch(aberto, async (val) => {
  if (val) {
    garantirEspacoNaTela()
    await nextTick()
    ajustarPosicaoPanel()
    requestAnimationFrame(() => ajustarPosicaoPanel())
    setTimeout(() => ajustarPosicaoPanel(), 220)
    if (mostrarBusca.value) {
      inputBusca.value?.focus()
    }
  } else {
    busca.value = ''
  }
})

function fecharExterno(e) {
  if (!aberto.value) {
    return
  }

  const clicouNoWrapper = wrapper.value?.contains(e.target)
  const clicouNoPainel = panel.value?.contains(e.target)

  if (!clicouNoWrapper && !clicouNoPainel) {
    fechar()
  }
}

function atualizarPosicaoSeAberto() {
  if (aberto.value) {
    ajustarPosicaoPanel()
  }
}

onMounted(() => {
  document.addEventListener('click', fecharExterno)
  window.addEventListener('resize', atualizarPosicaoSeAberto)
  window.addEventListener('scroll', atualizarPosicaoSeAberto, true)
})

onUnmounted(() => {
  document.removeEventListener('click', fecharExterno)
  window.removeEventListener('resize', atualizarPosicaoSeAberto)
  window.removeEventListener('scroll', atualizarPosicaoSeAberto, true)
})
</script>

<style scoped>
.dropdown-enter-active, .dropdown-leave-active { transition: all 0.15s ease; }
.dropdown-enter-from, .dropdown-leave-to { opacity: 0; transform: translateY(-4px); }

.search-select-trigger {
  min-height: 2.75rem;
}

.search-select-panel {
  transform-origin: top center;
  overscroll-behavior: contain;
}

.search-select-panel--up {
  transform-origin: bottom center;
}

.search-select-options {
  overflow-y: auto;
  overscroll-behavior: contain;
}
</style>
