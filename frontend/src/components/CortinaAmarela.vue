<template>
  <div ref="wrapper" class="relative inline-flex items-center">
    <button
      ref="trigger"
      type="button"
      class="tooltip-trigger"
      @mouseenter="show = true"
      @mouseleave="show = false"
      @pointerdown.stop.prevent="toggleTooltip"
      @click.stop.prevent
      :aria-expanded="show ? 'true' : 'false'"
      aria-label="Abrir ajuda do campo"
    >?</button>
    <Transition name="fade">
      <div v-if="show" ref="tooltip" class="tooltip-content" :style="tooltipStyle">
        <slot />
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, computed, nextTick, onMounted, onBeforeUnmount, watch } from 'vue'

const props = defineProps({
  posicao: { type: String, default: 'top' }
})

const show = ref(false)
const wrapper = ref(null)
const trigger = ref(null)
const tooltip = ref(null)
const tooltipStyle = ref({})

function toggleTooltip() {
  show.value = !show.value
}

function atualizarPosicao() {
  if (!show.value || !trigger.value || !tooltip.value) {
    return
  }

  const triggerRect = trigger.value.getBoundingClientRect()
  const tooltipRect = tooltip.value.getBoundingClientRect()
  const viewportWidth = window.innerWidth
  const viewportHeight = window.innerHeight
  const spacing = 10

  let left = triggerRect.left + (triggerRect.width / 2) - (tooltipRect.width / 2)
  left = Math.max(spacing, Math.min(left, viewportWidth - tooltipRect.width - spacing))

  let top
  const roomAbove = triggerRect.top
  const roomBelow = viewportHeight - triggerRect.bottom

  if (props.posicao === 'bottom' || (roomAbove < tooltipRect.height + spacing && roomBelow > roomAbove)) {
    top = triggerRect.bottom + spacing
  } else {
    top = triggerRect.top - tooltipRect.height - spacing
  }

  top = Math.max(spacing, Math.min(top, viewportHeight - tooltipRect.height - spacing))

  tooltipStyle.value = {
    position: 'fixed',
    left: `${left}px`,
    top: `${top}px`,
    transform: 'none',
    margin: '0',
  }
}

function fecharAoClicarFora(event) {
  if (!wrapper.value?.contains(event.target)) {
    show.value = false
  }
}

function fecharAoEsc(event) {
  if (event.key === 'Escape') {
    show.value = false
  }
}

watch(show, async (value) => {
  if (!value) {
    return
  }

  await nextTick()
  atualizarPosicao()
})

onMounted(() => {
  document.addEventListener('pointerdown', fecharAoClicarFora)
  document.addEventListener('keydown', fecharAoEsc)
  window.addEventListener('resize', atualizarPosicao)
  window.addEventListener('scroll', atualizarPosicao, true)
})

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', fecharAoClicarFora)
  document.removeEventListener('keydown', fecharAoEsc)
  window.removeEventListener('resize', atualizarPosicao)
  window.removeEventListener('scroll', atualizarPosicao, true)
})
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.15s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
