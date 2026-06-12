<script setup>
import { RouterView } from 'vue-router'
import { ref, onMounted, watch } from 'vue'
import { sincronizarPendentes, carregarCacheReferencias } from '@/services/syncService'
import { useThemeStore } from '@/stores/theme'
import { useConnectivityStatus } from '@/services/connectivityService'
import SplashScreen from '@/components/SplashScreen.vue'

const themeStore = useThemeStore()
const { isOnline, checkConnectivity } = useConnectivityStatus()

const onOnline = async () => {
  console.log('[CPTM] Internet voltou - iniciando sincronizacao...')
  const resultado = await sincronizarPendentes()
  console.log(`[CPTM] Sync: ${resultado.enviados} enviados, ${resultado.erros} erros`)
  await carregarCacheReferencias()
}

onMounted(async () => {
  themeStore.aplicar()
  try {
    const online = await checkConnectivity()
    if (online) {
      try {
        await Promise.race([
          carregarCacheReferencias(),
          new Promise((_, reject) => 
            setTimeout(() => reject(new Error('Timeout ao carregar referências')), 5000)
          )
        ])
      } catch (error) {
        console.warn('[CPTM] Erro ao carregar cache de referências:', error.message)
      }
    }
  } catch (error) {
    console.warn('[CPTM] Erro ao verificar conectividade:', error.message)
  }
})

watch(isOnline, (online, previous) => {
  if (online && previous === false) {
    onOnline()
  }
})

const showSplash = ref(true)

function handleSplashDone() {
  showSplash.value = false
}

</script>

<template>
  <!-- Splash apenas visual (overlay) -->
  <SplashScreen
    v-if="showSplash"
    @done="handleSplashDone"
  />


  <!-- Router SEMPRE montado (mas oculto enquanto splash está visível) -->
  <RouterView v-if="!showSplash" />

</template>
