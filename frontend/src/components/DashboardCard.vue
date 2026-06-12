<template>
  <div class="rounded-xl p-4 flex items-center gap-4" :style="{ backgroundColor: 'var(--cptm-surface)' }">
    <div class="w-12 h-12 rounded-xl flex items-center justify-center shrink-0"
         :style="{ backgroundColor: iconBg }">
      <component :is="iconComponent" class="w-5 h-5" :style="{ color: iconColor }" />
    </div>
    <div>
      <p class="text-xs font-medium" :style="{ color: 'var(--cptm-text-muted)' }">{{ titulo }}</p>
      <p class="text-2xl font-bold" :style="{ color: 'var(--cptm-text)' }">{{ valor }}</p>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { ClipboardList, Droplets, TreeDeciduous, CalendarDays } from 'lucide-vue-next'

const props = defineProps({
  titulo: { type: String, required: true },
  valor: { type: Number, default: 0 },
  icone: { type: String, default: 'clipboard' }
})

const iconMap = {
  clipboard: { comp: ClipboardList, bg: 'rgba(59,130,246,0.1)', color: '#3b82f6' },
  droplets: { comp: Droplets, bg: 'rgba(6,182,212,0.1)', color: '#06b6d4' },
  tree: { comp: TreeDeciduous, bg: 'rgba(34,197,94,0.1)', color: '#22c55e' },
  calendar: { comp: CalendarDays, bg: 'rgba(168,85,247,0.1)', color: '#a855f7' }
}

const iconComponent = computed(() => iconMap[props.icone]?.comp || ClipboardList)
const iconBg = computed(() => iconMap[props.icone]?.bg || 'rgba(59,130,246,0.1)')
const iconColor = computed(() => iconMap[props.icone]?.color || '#3b82f6')
</script>
