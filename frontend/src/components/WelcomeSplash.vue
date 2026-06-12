<template>
  <Transition name="welcome-fade" @after-leave="emit('done')">
    <div v-if="visible" class="welcome-screen" :class="{ dark: isDark }">
      <div class="welcome-card">
        <!-- Checkmark success icon -->
        <div class="welcome-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="20 6 9 17 4 12"></polyline>
          </svg>
        </div>

        <!-- Welcome message -->
        <div class="welcome-content">
          <h1 class="welcome-title">Bem-vindo(a)!</h1>
          <p class="welcome-subtitle">Autenticação realizada com sucesso</p>
          <p class="welcome-message">{{ usuarioNome }}</p>
        </div>

        <!-- Loading indicator -->
        <div class="welcome-loader">
          <div class="loader-dots">
            <span></span>
            <span></span>
            <span></span>
          </div>
          <p class="welcome-loading-text">Acessando {{ tipoAcesso }}...</p>
        </div>
      </div>
    </div>
  </Transition>
</template>


<script setup>
import { ref, onMounted, computed } from 'vue'
import { useThemeStore } from '@/stores/theme'
import { useAuthStore } from '@/stores/auth'
import { storeToRefs } from 'pinia'

const emit = defineEmits(['done'])
const visible = ref(true)

const themeStore = useThemeStore()
const authStore = useAuthStore()

const { isDark } = storeToRefs(themeStore)

const usuarioNome = computed(() => {
  const nome = authStore.usuario?.nome || 'Usuário'
  return `Olá, ${nome}`
})

const tipoAcesso = computed(() => {
  return authStore.isSupervisor ? 'Painel de Supervisor' : 'Painel de Operador'
})

onMounted(() => {
  const minDisplay = new Promise(resolve => setTimeout(resolve, 2500))
  
  Promise.all([minDisplay]).then(() => {
    visible.value = false
  })
})
</script>


<style scoped>
/* ── Welcome Screen (Pós-login) ───────────────────── */
.welcome-screen {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f0f0f0 0%, #e8eae8 100%);
  padding: 1.5rem;
  transition: background 0.4s ease;
}

.welcome-screen.dark {
  background: linear-gradient(135deg, #0a0a0a 0%, #1a1a1a 100%);
}

.welcome-card {
  position: relative;
  background: #ffffff;
  border-radius: 24px;
  box-shadow: 0 12px 48px rgba(0, 0, 0, 0.12),
              0 4px 12px rgba(0, 0, 0, 0.06);
  padding: 3rem 2.5rem;
  max-width: 380px;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
  animation: welcome-enter 0.6s cubic-bezier(0.22, 1, 0.36, 1) forwards;
  transition: background 0.4s ease, box-shadow 0.4s ease;
}

.welcome-screen.dark .welcome-card {
  background: #1e1e1e;
  border: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: 0 12px 48px rgba(0, 0, 0, 0.5),
              0 4px 12px rgba(0, 0, 0, 0.3);
}

/* ── Success Icon ─────────────────────────── */
.welcome-icon {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  background: linear-gradient(135deg, #4C6246 0%, #5a9e4a 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  animation: icon-pulse 0.6s cubic-bezier(0.22, 1, 0.36, 1) 0.2s both;
}

.welcome-icon svg {
  width: 40px;
  height: 40px;
  color: #ffffff;
  stroke-linecap: round;
  stroke-linejoin: round;
  animation: checkmark-draw 0.6s ease 0.4s both;
}

/* ── Content ──────────────────────────────── */
.welcome-content {
  text-align: center;
  animation: fade-up 0.6s ease 0.3s both;
}

.welcome-title {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 1.75rem;
  font-weight: 700;
  color: #1a1a1a;
  margin: 0 0 0.5rem;
  letter-spacing: -0.02em;
  transition: color 0.4s ease;
}

.welcome-screen.dark .welcome-title {
  color: #f5f5f5;
}

.welcome-subtitle {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.9rem;
  color: #999;
  margin: 0 0 0.75rem;
  transition: color 0.4s ease;
}

.welcome-screen.dark .welcome-subtitle {
  color: #777;
}

.welcome-message {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 1.1rem;
  font-weight: 600;
  color: #4C6246;
  margin: 0;
  transition: color 0.4s ease;
}

.welcome-screen.dark .welcome-message {
  color: #7fb570;
}

/* ── Loader ───────────────────────────────── */
.welcome-loader {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  margin-top: 0.5rem;
  animation: fade-up 0.6s ease 0.5s both;
}

.loader-dots {
  display: flex;
  gap: 8px;
  align-items: center;
  justify-content: center;
  height: 24px;
}

.loader-dots span {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #4C6246;
  animation: dot-bounce 1.4s infinite ease-in-out;
  transition: background 0.4s ease;
}

.loader-dots span:nth-child(2) {
  animation-delay: 0.2s;
}

.loader-dots span:nth-child(3) {
  animation-delay: 0.4s;
}

.welcome-screen.dark .loader-dots span {
  background: #7fb570;
}

.welcome-loading-text {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.8rem;
  color: #bbb;
  margin: 0;
  letter-spacing: 0.05em;
  animation: text-fade 1s infinite ease-in-out;
  transition: color 0.4s ease;
}

.welcome-screen.dark .welcome-loading-text {
  color: #555;
}

/* ── Transição de saída ─────────────────── */
.welcome-fade-leave-active {
  transition: opacity 0.5s ease, transform 0.5s ease;
}

.welcome-fade-leave-to {
  opacity: 0;
  transform: scale(1.05) translateY(-20px);
}

/* ── Keyframes ──────────────────────────── */
@keyframes welcome-enter {
  from {
    opacity: 0;
    transform: translateY(20px) scale(0.95);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

@keyframes icon-pulse {
  from {
    opacity: 0;
    transform: scale(0.5);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

@keyframes checkmark-draw {
  0% {
    stroke-dashoffset: 48;
    stroke-dasharray: 48;
    opacity: 0;
  }
  100% {
    stroke-dashoffset: 0;
    stroke-dasharray: 48;
    opacity: 1;
  }
}

@keyframes fade-up {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes dot-bounce {
  0%, 80%, 100% {
    transform: translateY(0);
    opacity: 0.5;
  }
  40% {
    transform: translateY(-12px);
    opacity: 1;
  }
}

@keyframes text-fade {
  0%, 100% {
    opacity: 0.6;
  }
  50% {
    opacity: 1;
  }
}
</style>
