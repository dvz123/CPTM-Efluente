import './assets/app.css'
import '@arcgis/core/assets/esri/themes/light/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import axios from 'axios'

import App from './App.vue'
import router from './router'

// Configuração global do Axios
axios.defaults.baseURL = 'http://localhost:5217'

const app = createApp(App)

app.use(createPinia())
app.use(router)

let logoutInProgress = false

// Interceptador de REQUISIÇÃO para adicionar token
axios.interceptors.request.use(
  config => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  error => Promise.reject(error)
)

// Interceptador de RESPOSTA para capturar 401 Unauthorized
axios.interceptors.response.use(
  response => response,
  async error => {
    if (error.response?.status === 401 && !logoutInProgress) {
      logoutInProgress = true
      try {
        const { useAuthStore } = await import('@/stores/auth.js')
        const authStore = useAuthStore()

        // Limpar sessão e redirecionar para login (apenas se não estiver lá)
        await authStore.logout()

        if (router.currentRoute.value.path !== '/login') {
          router.push('/login')
        }
      } finally {
        logoutInProgress = false
      }
    }
    return Promise.reject(error)
  }
)

app.mount('#app')

// Registrar Service Worker da PWA
import { registerSW } from 'virtual:pwa-register'
registerSW({
  onNeedRefresh() {
    if (confirm('Nova versão disponível. Atualizar agora?')) {
      location.reload()
    }
  }
})
