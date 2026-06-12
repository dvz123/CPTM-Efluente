<template>
  <div v-if="showLogin" class="lp">
    <div class="lp-card">
      <!-- Accent bar -->
      <div class="lp-accent" />

      <!-- Brand -->
      <header class="lp-brand">
        <img :src="cptmLogo" alt="CPTM" class="lp-logo" />
        <div class="lp-brand-text">
          <h1 class="lp-title">CPTM <span class="lp-title-light">Efluentes</span></h1>
          <p class="lp-sub">Sistema de Monitoramento Ambiental</p>
        </div>
      </header>

      <!-- Form section -->
      <div class="lp-body">
        <p class="lp-heading">Acesse sua conta</p>

        <form @submit.prevent="handleLogin" class="lp-form">
          <!-- Email -->
          <div class="lp-field">
            <label class="lp-label" for="lp-email">Usuário</label>
            <div class="lp-input-box" :class="{ 'lp-input-box--focus': focusEmail }">
              <MailIcon class="lp-icon" />
              <input
                id="lp-email"
                v-model="email"
                type="email"
                required
                maxlength="200"
                autocomplete="email"
                class="lp-input"
                placeholder="usuario@cptm.sp.gov.br"
                @focus="focusEmail = true"
                @blur="focusEmail = false"
              />
            </div>
          </div>

          <!-- Senha -->
          <div class="lp-field">
            <label class="lp-label" for="lp-senha">Senha</label>
            <div class="lp-input-box" :class="{ 'lp-input-box--focus': focusSenha }">
              <LockIcon class="lp-icon" />
              <input
                id="lp-senha"
                v-model="senha"
                :type="showPw ? 'text' : 'password'"
                required
                minlength="6"
                maxlength="100"
                autocomplete="current-password"
                class="lp-input lp-input--pw"
                placeholder="••••••••"
                @focus="focusSenha = true"
                @blur="focusSenha = false"
              />
              <button type="button" @click="showPw = !showPw" class="lp-eye" tabindex="-1">
                <EyeIcon v-if="!showPw" />
                <EyeOffIcon v-else />
              </button>
            </div>
          </div>

          <!-- Error -->
          <div v-if="authStore.erro" class="lp-error">
            <AlertCircleIcon class="lp-error-icon" />
            <span>{{ authStore.erro }}</span>
          </div>

          <!-- Submit -->
          <button type="submit" :disabled="authStore.carregando" class="lp-btn">
            <svg v-if="authStore.carregando" class="lp-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
            </svg>
            <LogInIcon v-else class="lp-btn-icon" />
            {{ authStore.carregando ? 'Entrando...' : 'Entrar' }}
          </button>
        </form>
      </div>
    </div>

    <!-- Footer -->
    <footer class="lp-footer">
      <span>CPTM — Fatec</span>
      <span class="lp-footer-dot">·</span>
      <span>{{ new Date().getFullYear() }}</span>
      <button @click="themeStore.toggle()" class="lp-theme" :title="themeStore.isDark ? 'Modo Claro' : 'Modo Escuro'">
        <SunIcon v-if="themeStore.isDark" />
        <MoonIcon v-else />
      </button>
    </footer>
  </div>

  <!-- WELCOME SPLASH -->
  <WelcomeSplash
    v-if="showWelcome"
    @done="irParaPerfil"
  />

</template>

<script setup>
import { ref, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import {
  Mail as MailIcon,
  Lock as LockIcon,
  Eye as EyeIcon,
  EyeOff as EyeOffIcon,
  Sun as SunIcon,
  Moon as MoonIcon,
  LogIn as LogInIcon,
  AlertCircle as AlertCircleIcon,
} from 'lucide-vue-next'
import cptmLogo from '@/assets/Logo_CPTM.png'
import WelcomeSplash from '@/components/WelcomeSplash.vue'

const router = useRouter()
const authStore = useAuthStore()
const themeStore = useThemeStore()

const email = ref('')
const senha = ref('')
const showPw = ref(false)
const focusEmail = ref(false)
const focusSenha = ref(false)
const showLogin = ref(true)
const showWelcome = ref(false)

async function handleLogin() {
  const sucesso = await authStore.login(email.value, senha.value)

  console.log(" LOGIN RESULTADO:", sucesso)
  console.log(" PERFIL:", authStore.perfil)
  console.log(" NOME:", authStore.nome)

  if (!sucesso) return

  localStorage.setItem('perfil', authStore.perfil || '')
  await nextTick()

  // Esconde o formulário de login e mostra a welcome splash
  console.log("Exibindo WelcomeSplash...")
  showLogin.value = false
  showWelcome.value = true
}

function irParaPerfil() {
  const destino = authStore.isSupervisor ? '/supervisor' : '/operador'
  console.log("Redirecionando para:", destino)
  router.push(destino)
}

</script>

<style scoped>
/* ═══════════════════════════════════
   Login Page — CPTM Efluentes
   Palette: white / grey / black / #4C6246
   ═══════════════════════════════════ */

/* ── Page ── */
.lp {
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
  background: #f0f0f0;
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
}
:root.dark .lp {
  background: #0d0d0d;
}

/* ── Card ── */
.lp-card {
  width: 100%;
  max-width: 420px;
  background: #ffffff;
  border: 1px solid #d4d4d4;
  border-radius: 16px;
  overflow: hidden;
  box-shadow:
    0 1px 2px rgba(0,0,0,0.04),
    0 4px 16px rgba(0,0,0,0.06);
}
:root.dark .lp-card {
  background: #171717;
  border-color: #2a2a2a;
  box-shadow:
    0 1px 2px rgba(0,0,0,0.2),
    0 4px 16px rgba(0,0,0,0.3);
}

/* ── Accent bar ── */
.lp-accent {
  height: 4px;
  background: linear-gradient(90deg, #E11F26, #4C6246);
}

/* ── Brand header ── */
.lp-brand {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 1.75rem 2rem 0;
}
.lp-logo {
  width: 52px;
  height: auto;
  max-height: 52px;
  object-fit: contain;
  flex-shrink: 0;
}
.lp-brand-text {
  min-width: 0;
}
.lp-title {
  font-size: 1.25rem;
  font-weight: 800;
  color: #1a1a1a;
  letter-spacing: -0.02em;
  line-height: 1.2;
}
:root.dark .lp-title {
  color: #f5f5f5;
}
.lp-title-light {
  font-weight: 400;
  color: #4C6246;
}
:root.dark .lp-title-light {
  color: #8faa88;
}
.lp-sub {
  font-size: 0.6875rem;
  color: #999;
  margin-top: 2px;
  letter-spacing: 0.01em;
}
:root.dark .lp-sub {
  color: #5a5a5a;
}

/* ── Body ── */
.lp-body {
  padding: 1.75rem 2rem 2rem;
}
.lp-heading {
  font-size: 0.8125rem;
  font-weight: 600;
  color: #666;
  margin-bottom: 1.25rem;
  padding-bottom: 0.875rem;
  border-bottom: 1px solid #eaeaea;
}
:root.dark .lp-heading {
  color: #777;
  border-color: #262626;
}

/* ── Form ── */
.lp-form {
  display: flex;
  flex-direction: column;
  gap: 1.125rem;
}
.lp-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.lp-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: #555;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
:root.dark .lp-label {
  color: #888;
}

/* ── Input box ── */
.lp-input-box {
  position: relative;
  display: flex;
  align-items: center;
  border: 1.5px solid #d4d4d4;
  border-radius: 10px;
  background: #fafafa;
  transition: border-color 0.2s, box-shadow 0.2s, background 0.2s;
}
.lp-input-box:hover {
  border-color: #bbb;
}
.lp-input-box--focus {
  border-color: #4C6246 !important;
  box-shadow: 0 0 0 3px rgba(76,98,70,0.1);
  background: #fff;
}
:root.dark .lp-input-box {
  background: #111;
  border-color: #333;
}
:root.dark .lp-input-box:hover {
  border-color: #444;
}
:root.dark .lp-input-box--focus {
  border-color: #6b8964 !important;
  box-shadow: 0 0 0 3px rgba(107,137,100,0.12);
  background: #1a1a1a;
}

.lp-icon {
  position: absolute;
  left: 12px;
  width: 16px;
  height: 16px;
  color: #aaa;
  pointer-events: none;
}
:root.dark .lp-icon {
  color: #555;
}

.lp-input {
  width: 100%;
  height: 44px;
  padding: 0 14px 0 40px;
  font-size: 0.875rem;
  color: #1a1a1a;
  background: transparent;
  border: none;
  border-radius: 10px;
  outline: none;
}
.lp-input::placeholder {
  color: #bbb;
}
:root.dark .lp-input {
  color: #eee;
}
:root.dark .lp-input::placeholder {
  color: #444;
}
.lp-input--pw {
  padding-right: 42px;
}

/* ── Eye toggle ── */
.lp-eye {
  position: absolute;
  right: 8px;
  width: 30px;
  height: 30px;
  display: grid;
  place-items: center;
  background: none;
  border: none;
  cursor: pointer;
  color: #bbb;
  border-radius: 8px;
  transition: background 0.15s, color 0.15s;
}
.lp-eye:hover {
  background: #f0f0f0;
  color: #666;
}
.lp-eye svg {
  width: 16px;
  height: 16px;
}
:root.dark .lp-eye {
  color: #555;
}
:root.dark .lp-eye:hover {
  background: #222;
  color: #999;
}

/* ── Error ── */
.lp-error {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.8125rem;
  color: #d32f2f;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 10px;
  padding: 10px 14px;
}
.lp-error-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}
:root.dark .lp-error {
  background: rgba(211,47,47,0.08);
  border-color: rgba(211,47,47,0.2);
  color: #ef5350;
}

/* ── Button ── */
.lp-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  height: 48px;
  border: none;
  border-radius: 10px;
  font-size: 0.9375rem;
  font-weight: 700;
  color: #fff;
  background: #4C6246;
  cursor: pointer;
  transition: background 0.15s, transform 0.1s, box-shadow 0.15s;
  margin-top: 4px;
}
.lp-btn:hover:not(:disabled) {
  background: #3e5139;
  box-shadow: 0 2px 8px rgba(76,98,70,0.25);
}
.lp-btn:active:not(:disabled) {
  transform: scale(0.98);
}
.lp-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.lp-btn-icon {
  width: 18px;
  height: 18px;
}
:root.dark .lp-btn {
  background: #5a7553;
}
:root.dark .lp-btn:hover:not(:disabled) {
  background: #6b8964;
  box-shadow: 0 2px 8px rgba(90,117,83,0.3);
}
.lp-spin {
  width: 18px;
  height: 18px;
  animation: lp-rotate 0.7s linear infinite;
}
@keyframes lp-rotate {
  to { transform: rotate(360deg); }
}

/* ── Footer ── */
.lp-footer {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 1.75rem;
  font-size: 0.6875rem;
  font-weight: 500;
  color: #aaa;
  letter-spacing: 0.02em;
}
:root.dark .lp-footer {
  color: #444;
}
.lp-footer-dot {
  color: #ccc;
}
:root.dark .lp-footer-dot {
  color: #333;
}
.lp-theme {
  width: 30px;
  height: 30px;
  display: grid;
  place-items: center;
  margin-left: 4px;
  background: #fff;
  border: 1px solid #d4d4d4;
  border-radius: 8px;
  color: #999;
  cursor: pointer;
  transition: border-color 0.15s, color 0.15s, background 0.15s;
}
.lp-theme svg {
  width: 14px;
  height: 14px;
}
.lp-theme:hover {
  border-color: #4C6246;
  color: #4C6246;
  background: #f6f9f6;
}
:root.dark .lp-theme {
  background: #1a1a1a;
  border-color: #333;
  color: #555;
}
:root.dark .lp-theme:hover {
  border-color: #6b8964;
  color: #8faa88;
  background: #1f241f;
}

/* ── Responsive ── */
@media (max-width: 440px) {
  .lp { padding: 1rem; }
  .lp-card { border-radius: 14px; }
  .lp-brand { padding: 1.5rem 1.5rem 0; gap: 12px; }
  .lp-logo { width: 46px; max-height: 46px; }
  .lp-title { font-size: 1.125rem; }
  .lp-body { padding: 1.5rem; }
}
@media (min-width: 768px) {
  .lp-card { max-width: 440px; }
  .lp-brand { padding: 2rem 2.25rem 0; }
  .lp-body { padding: 1.75rem 2.25rem 2.25rem; }
}
</style>
