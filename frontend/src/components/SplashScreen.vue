<template>
  <Transition name="splash" @after-leave="emit('done')">
    <div v-if="visible" class="splash-screen" :class="{ dark: isDark }">
      <div class="splash-card">


        <div class="top-bar"></div>


        <div class="logo-wrapper">
          <div class="logo-ring"></div>
          <img src="@/assets/Logo_CPTM.png" alt="CPTM" class="splash-logo" />
        </div>


        <div class="splash-text">
          <h1 class="splash-title">CPTM <span>Efluentes</span></h1>
          <p class="splash-subtitle">Sistema de Monitoramento Ambiental</p>
        </div>


        <div class="divider">
          <span class="divider-dot"></span>
        </div>


        <div class="loader-wrapper">
          <div class="splash-loader">
            <div class="loader-bar"></div>
          </div>
          <p class="splash-loading-text">Inicializando sistema...</p>
        </div>


      </div>


      <p class="splash-footer">CPTM — Fatec · 2026</p>
    </div>
  </Transition>
</template>


<script setup>
import { ref, onMounted } from 'vue'
import { useThemeStore } from '@/stores/theme'
import { storeToRefs } from 'pinia'


const emit = defineEmits(['done'])
const visible = ref(true)


const themeStore = useThemeStore()
const { isDark } = storeToRefs(themeStore)


onMounted(() => {
  const minDisplay = new Promise(resolve => setTimeout(resolve, 3000))
  const appReady = new Promise(resolve => {
    if (document.readyState === 'complete') resolve(true)
    else window.addEventListener('load', resolve, { once: true })
  })


  Promise.all([minDisplay, appReady]).then(() => {
    visible.value = false
  })
})
</script>


<style scoped>
/* ── Base (Light) ───────────────────────────── */
.splash-screen {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background-color: #e8eae8;
  gap: 1.5rem;
  transition: background-color 0.4s ease;
}


.splash-card {
  position: relative;
  background: #ffffff;
  border-radius: 20px;
  box-shadow:
    0 8px 32px rgba(0, 0, 0, 0.12),
    0 2px 8px rgba(0, 0, 0, 0.06);
  padding: 2.8rem 2.8rem 2.2rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  width: 100%;
  max-width: 340px;
  overflow: hidden;
  animation: card-enter 0.6s cubic-bezier(0.22, 1, 0.36, 1) forwards;
  transition: background 0.4s ease, box-shadow 0.4s ease;
}


/* Barra vermelha no topo (visível só no dark) */
.top-bar {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, #E3000F, #ff4d4d);
  opacity: 0;
  transition: opacity 0.4s ease;
}


.splash-screen.dark .top-bar {
  opacity: 1;
}


/* Logo */
.logo-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100px;
  height: 100px;
  margin-bottom: 0.25rem;
}


.logo-ring {
  position: absolute;
  inset: 0;
  border-radius: 50%;
  border: 2.5px solid #E3000F;
  animation: ring-pulse 2s ease-out infinite;
  opacity: 0;
}


.splash-logo {
  width: 72px;
  height: 72px;
  object-fit: contain;
  animation: logo-pulse 2.4s ease-in-out infinite;
  position: relative;
  z-index: 1;
}


/* Textos */
.splash-text {
  text-align: center;
  animation: fade-up 0.6s ease 0.2s both;
}


.splash-title {
  font-family: 'Inter', sans-serif;
  font-size: 1.55rem;
  font-weight: 700;
  color: #1a1a1a;
  margin: 0 0 0.25rem;
  letter-spacing: -0.01em;
  transition: color 0.4s ease;
}


.splash-title span {
  color: #3a6b2a;
  transition: color 0.4s ease;
}


.splash-subtitle {
  font-family: 'Inter', sans-serif;
  font-size: 0.82rem;
  color: #999;
  margin: 0;
  transition: color 0.4s ease;
}


/* Divisor */
.divider {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  animation: fade-up 0.6s ease 0.35s both;
}


.divider::before,
.divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: #ebebeb;
  transition: background 0.4s ease;
}


.divider-dot {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background: #3a6b2a;
  flex-shrink: 0;
  transition: background 0.4s ease;
}


/* Loader */
.loader-wrapper {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.6rem;
  animation: fade-up 0.6s ease 0.5s both;
}


.splash-loader {
  width: 100%;
  height: 5px;
  background: #ebebeb;
  border-radius: 9999px;
  overflow: hidden;
  transition: background 0.4s ease;
}


.loader-bar {
  height: 100%;
  width: 0%;
  background: linear-gradient(90deg, #2d5a27, #5a9e4a);
  border-radius: 9999px;
  animation: progress 3s ease-in-out forwards;
}


.splash-loading-text {
  font-family: 'Inter', sans-serif;
  font-size: 0.72rem;
  color: #bbb;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  margin: 0;
  animation: text-blink 1.5s ease-in-out infinite;
  transition: color 0.4s ease;
}


/* Rodapé */
.splash-footer {
  font-family: 'Inter', sans-serif;
  font-size: 0.75rem;
  color: #aaa;
  margin: 0;
  animation: fade-up 0.6s ease 0.7s both;
  transition: color 0.4s ease;
}


/* ── Dark Mode ──────────────────────────────── */
.splash-screen.dark {
  background-color: #111111;
}


.splash-screen.dark .splash-card {
  background: #1e1e1e;
  border: 1px solid rgba(255, 255, 255, 0.07);
  box-shadow:
    0 8px 32px rgba(0, 0, 0, 0.5),
    0 2px 8px rgba(0, 0, 0, 0.3);
}


.splash-screen.dark .splash-title {
  color: #f0f0f0;
}


.splash-screen.dark .splash-title span {
  color: #5a9e4a;
}


.splash-screen.dark .splash-subtitle {
  color: #888;
}


.splash-screen.dark .divider::before,
.splash-screen.dark .divider::after {
  background: rgba(255, 255, 255, 0.08);
}


.splash-screen.dark .divider-dot {
  background: #5a9e4a;
}


.splash-screen.dark .splash-loader {
  background: rgba(255, 255, 255, 0.08);
}


.splash-screen.dark .splash-loading-text {
  color: #555;
}


.splash-screen.dark .splash-footer {
  color: #555;
}


/* ── Transição de saída ─────────────────────── */
.splash-leave-active {
  transition: opacity 0.5s ease, transform 0.5s ease;
}
.splash-leave-to {
  opacity: 0;
  transform: scale(1.03);
}


/* ── Keyframes ──────────────────────────────── */
@keyframes card-enter {
  from { opacity: 0; transform: translateY(24px) scale(0.97); }
  to   { opacity: 1; transform: translateY(0) scale(1); }
}


@keyframes fade-up {
  from { opacity: 0; transform: translateY(10px); }
  to   { opacity: 1; transform: translateY(0); }
}


@keyframes logo-pulse {
  0%, 100% { transform: scale(1); }
  50%       { transform: scale(1.09); }
}


@keyframes ring-pulse {
  0%   { transform: scale(0.85); opacity: 0.7; }
  100% { transform: scale(1.5);  opacity: 0; }
}


@keyframes progress {
  0%   { width: 0%; }
  20%  { width: 25%; }
  60%  { width: 70%; }
  90%  { width: 92%; }
  100% { width: 100%; }
}


@keyframes text-blink {
  0%, 100% { opacity: 1; }
  50%       { opacity: 0.4; }
}
</style>
