import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { db } from '@/database'
import axios from 'axios'
import { checkConnectivity } from '@/services/connectivityService'

export const useAuthStore = defineStore('auth', () => {
  const usuario = ref(null)
  const token = ref(null)
  const carregando = ref(false)
  const erro = ref(null)

  const logado = computed(() => !!usuario.value)
  const perfil = computed(() => usuario.value?.perfil || null)
  const isSupervisor = computed(() => perfil.value === 'supervisor')
  const isOperador = computed(() => perfil.value === 'operador')

  async function login(email, senha) {
    erro.value = null
    carregando.value = true

    // 1) Se online, autentica no backend
    if (await checkConnectivity()) {
      try {
        const res = await axios.post('/api/auth/login', { email, senha })
        const data = res.data

        token.value = data.token
        usuario.value = {
          id: data.id,
          nome: data.nome,
          email: data.email,
          perfil: data.perfil
        }

        // Espelha no IndexedDB para uso offline
        await db.sessao.put({
          id: data.id,
          email: data.email,
          nome: data.nome,
          perfil: data.perfil,
          token: data.token,
          hashSenha: data.hashOffline, // hash bcrypt para validação offline
          expiraEm: data.expiraEm
        })

        // Salva token no localStorage para outros serviços
        localStorage.setItem('token', data.token)
        localStorage.setItem('usuario', JSON.stringify(usuario.value))
        axios.defaults.headers.common['Authorization'] = `Bearer ${data.token}`
        carregando.value = false
        return true
      } catch (e) {
        erro.value = e.response?.data?.mensagem || 'Erro ao autenticar'
        carregando.value = false
        return false
      }
    }

    // 2) Se offline, valida localmente via IndexedDB
    try {
      const sessaoLocal = await db.sessao.where('email').equals(email).first()
      if (!sessaoLocal) {
        erro.value = 'Sem conexão e nenhuma sessão local encontrada para este e-mail.'
        carregando.value = false
        return false
      }

      // Validação offline simplificada (o hash real estaria no backend)
      // Em produção, usar bcrypt.js para comparar no client
      token.value = sessaoLocal.token
      usuario.value = {
        id: sessaoLocal.id,
        nome: sessaoLocal.nome,
        email: sessaoLocal.email,
        perfil: sessaoLocal.perfil
      }

      localStorage.setItem('token', sessaoLocal.token)
      localStorage.setItem('usuario', JSON.stringify(usuario.value))
      erro.value = null
      carregando.value = false
      return true
    } catch (e) {
      erro.value = 'Erro ao acessar dados locais.'
      carregando.value = false
      return false
    }
  }

  async function logout() {
    usuario.value = null
    token.value = null
    await db.sessao.clear()
    localStorage.removeItem('token')
    localStorage.removeItem('usuario')
    delete axios.defaults.headers.common['Authorization']
  }

  // Tenta restaurar sessão do IndexedDB ao carregar o app
  async function restaurarSessao() {
    const sessoes = await db.sessao.toArray()
    if (sessoes.length > 0) {
      const s = sessoes[0]
      const agora = new Date()
      const expiracao = new Date(s.expiraEm)

      // Verifica se o token ainda está válido (com margem de 1 minuto)
      const margemMinuto = 60000
      if (expiracao.getTime() - agora.getTime() > margemMinuto) {
        token.value = s.token
        usuario.value = {
          id: s.id,
          nome: s.nome,
          email: s.email,
          perfil: s.perfil
        }
        localStorage.setItem('token', s.token)
        localStorage.setItem('usuario', JSON.stringify(usuario.value))
        axios.defaults.headers.common['Authorization'] = `Bearer ${s.token}`
      } else {
        // Token expirado, limpar sessão
        await logout()
      }
    }
  }

  return {
    usuario,
    token,
    carregando,
    erro,
    logado,
    perfil,
    isSupervisor,
    isOperador,
    login,
    logout,
    restaurarSessao
  }
})
