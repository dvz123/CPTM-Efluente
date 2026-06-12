
import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import LoginView from '@/views/LoginView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { publica: true }
    },
    {
      path: '/operador',
      name: 'operador',
      component: () => import('@/views/OperadorHomeView.vue'),
      meta: { perfis: ['operador', 'supervisor'] }
    },
    {
      path: '/operador/formulario',
      name: 'operador-formulario',
      component: () => import('@/views/FormularioOperadorView.vue'),
      meta: { perfis: ['operador', 'supervisor'] }
    },
    {
      path: '/supervisor',
      name: 'supervisor',
      component: () => import('@/views/PainelSupervisorView.vue'),
      meta: { perfis: ['supervisor'] }
    },
    {
      path: '/supervisor/relatorios',
      name: 'supervisor-relatorios',
      component: () => import('@/views/RelatoriosSupervisorView.vue'),
      meta: { perfis: ['supervisor'] }
    },
    {
      path: '/supervisor/usuarios',
      name: 'supervisor-usuarios',
      component: () => import('@/views/UsuariosSupervisorView.vue'),
      meta: { perfis: ['supervisor'] }
    },
    {
      path: '/',
      redirect: '/login'
    }
  ]
})

// Guard de autenticação
router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // Tenta restaurar sessão do IndexedDB se não logado
  if (!auth.logado) {
    await auth.restaurarSessao()
  }

  // Rotas públicas
  if (to.meta.publica) {
    // Se já logado, redireciona
    if (auth.logado) {
      return auth.isSupervisor ? '/supervisor' : '/operador'
    }
    return true
  }

  // Rotas protegidas
  if (!auth.logado) {
    return '/login'
  }

  // Verifica perfil
  if (to.meta.perfis && !to.meta.perfis.includes(auth.perfil)) {
    return auth.isSupervisor ? '/supervisor' : '/operador'
  }

  return true
})

export default router

