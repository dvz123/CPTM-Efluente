import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

export const useThemeStore = defineStore('theme', () => {
  const dark = ref(
    localStorage.getItem('cptm-theme') === 'dark' ||
    (!localStorage.getItem('cptm-theme') && window.matchMedia('(prefers-color-scheme: dark)').matches)
  )

  const isDark = computed(() => dark.value)

  function toggle() {
    dark.value = !dark.value
    aplicar()
  }

  function aplicar() {
    document.documentElement.classList.toggle('dark', dark.value)
    localStorage.setItem('cptm-theme', dark.value ? 'dark' : 'light')
  }

  // Aplicar ao iniciar
  aplicar()

  return { isDark, toggle, aplicar }
})
