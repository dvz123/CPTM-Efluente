<template>
  <div class="usuarios-view">
    <header class="usuarios-header">
      <div class="usuarios-header__brand">
        <div class="usuarios-header__icon">
          <UsersIcon class="w-5 h-5 text-white" />
        </div>
        <div>
          <h1 class="usuarios-header__title">Usuários</h1>
          <p class="usuarios-header__subtitle">Gestão de acessos CCA - CPTM Ambiental</p>
        </div>
      </div>

      <div class="usuarios-header__actions">
        <span class="usuarios-header__nome">{{ authStore.usuario?.nome }}</span>
        <button @click="themeStore.toggle()" class="usuarios-header__btn-icon" :title="themeStore.isDark ? 'Modo claro' : 'Modo escuro'">
          <SunIcon v-if="themeStore.isDark" class="w-4 h-4" />
          <MoonIcon v-else class="w-4 h-4" />
        </button>
        <button @click="router.back()" class="usuarios-header__btn-voltar">
          <ArrowLeftIcon class="w-4 h-4" />
          Voltar
        </button>
      </div>
    </header>

    <main class="usuarios-main">
      <section class="usuarios-toolbar">
        <button class="usuarios-toolbar__btn" :class="{ active: abaAtiva === 'usuarios' }" @click="abaAtiva = 'usuarios'">
          <UsersIcon class="w-4 h-4" />
          Usuários
        </button>
        <button class="usuarios-toolbar__btn" :class="{ active: abaAtiva === 'novo' }" @click="abaAtiva = 'novo'">
          <UserPlusIcon class="w-4 h-4" />
          Criar novo usuário
        </button>
        <button class="usuarios-toolbar__btn" :class="{ active: abaAtiva === 'editar' }" @click="abrirEdicao">
          <PencilIcon class="w-4 h-4" />
          Editar
        </button>
        <button class="usuarios-toolbar__btn" @click="carregarUsuarios">
          <RefreshCwIcon class="w-4 h-4" :class="carregandoUsuarios ? 'animate-spin' : ''" />
          Atualizar lista
        </button>
      </section>

      <section v-if="abaAtiva === 'novo'" class="usuarios-card">
        <h2 class="usuarios-card__title">Novo usuário</h2>
        <form class="usuarios-form" @submit.prevent="cadastrarUsuario">
          <input v-model="novo.nome" class="field-input" placeholder="Nome completo" required maxlength="200" />
          <input v-model="novo.email" class="field-input" type="email" placeholder="E-mail" required maxlength="200" />
          <input v-model="novo.senha" class="field-input" type="password" placeholder="Senha (mínimo 6 caracteres)" required minlength="6" maxlength="100" />
          <select v-model="novo.perfil" class="field-input">
            <option value="operador">Operador</option>
            <option value="supervisor">Supervisor</option>
          </select>
          <button type="submit" class="usuarios-btn-primary" :disabled="salvandoCadastro">
            {{ salvandoCadastro ? 'Cadastrando...' : 'Cadastrar usuário' }}
          </button>
        </form>
      </section>

      <section v-if="abaAtiva === 'editar'" class="usuarios-card">
        <h2 class="usuarios-card__title">Editar usuário selecionado</h2>
        <div v-if="usuarioSelecionado" class="usuarios-form">
          <p class="usuarios-selected">
            Selecionado: <strong>{{ usuarioSelecionado.nome }}</strong> ({{ usuarioSelecionado.email }})
          </p>

          <select v-model="edicao.perfil" class="field-input">
            <option value="operador">Operador</option>
            <option value="supervisor">Supervisor</option>
          </select>

          <input
            v-model="edicao.novaSenha"
            class="field-input"
            type="password"
            placeholder="Nova senha (opcional, mínimo 6 caracteres)"
            minlength="6"
            maxlength="100"
          />

          <div class="usuarios-edit-actions">
            <button class="usuarios-btn-primary" :disabled="salvandoEdicao || excluindoUsuario" @click="salvarEdicao">
              {{ salvandoEdicao ? 'Salvando...' : 'Salvar alterações' }}
            </button>
            <button class="usuarios-btn-secondary" :disabled="salvandoEdicao || excluindoUsuario" @click="cancelarEdicao">
              Cancelar
            </button>
            <button class="usuarios-btn-danger" :disabled="salvandoEdicao || excluindoUsuario" @click="excluirUsuarioSelecionado">
              {{ excluindoUsuario ? 'Bloqueando...' : 'Bloquear usuário' }}
            </button>
          </div>
        </div>
        <p v-else class="usuarios-empty">Selecione um usuário na tabela para editar.</p>
      </section>

      <section class="usuarios-card">
        <div v-if="modalConfirmacaoExclusao.aberto" class="usuarios-modal-backdrop" @click.self="fecharModalExclusao">
          <div class="usuarios-modal">
            <h3 class="usuarios-modal__title">Confirmar bloqueio</h3>
            <p class="usuarios-modal__text">
              Deseja realmente bloquear este usuário?
            </p>
            <p class="usuarios-modal__warning">
              O usuário ficará inativo e não poderá mais acessar o sistema.
            </p>
            <div class="usuarios-modal__actions">
              <button class="usuarios-btn-secondary" :disabled="excluindoUsuario" @click="fecharModalExclusao">
                Cancelar
              </button>
              <button class="usuarios-btn-danger" :disabled="excluindoUsuario" @click="confirmarExclusaoUsuario">
                {{ excluindoUsuario ? 'Bloqueando...' : 'Confirmar bloqueio' }}
              </button>
            </div>
          </div>
        </div>

        <div v-if="modalFeedback.aberto" class="usuarios-modal-backdrop" @click.self="fecharModalFeedback">
          <div class="usuarios-modal">
            <h3 class="usuarios-modal__title">{{ modalFeedback.titulo }}</h3>
            <p class="usuarios-modal__text">{{ modalFeedback.mensagem }}</p>
            <div class="usuarios-modal__actions">
              <button class="usuarios-btn-primary" @click="fecharModalFeedback">
                OK
              </button>
            </div>
          </div>
        </div>
        <div class="usuarios-list-header">
          <h2 class="usuarios-card__title">Usuários cadastrados</h2>
          <p class="usuarios-count">{{ usuarios.length }} registros</p>
        </div>

        <div class="usuarios-table-wrap">
          <table class="usuarios-table">
            <thead>
              <tr>
                <th>Nome</th>
                <th>E-mail</th>
                <th>Perfil</th>
                <th>Criado em</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="u in usuarios"
                :key="u.id"
                class="usuarios-row"
                :class="{ 'usuarios-row--selected': usuarioSelecionado?.id === u.id }"
                @click="selecionarUsuario(u)"
              >
                <td>{{ u.nome }}</td>
                <td>{{ u.email }}</td>
                <td class="usuarios-cell-capitalize">{{ u.perfil }}</td>
                <td>{{ formatarData(u.criadoEm) }}</td>
              </tr>
              <tr v-if="usuarios.length === 0">
                <td colspan="4" class="usuarios-empty">Nenhum usuário cadastrado.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </main>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import axios from 'axios'
import {
  Users as UsersIcon,
  Sun as SunIcon,
  Moon as MoonIcon,
  ArrowLeft as ArrowLeftIcon,
  UserPlus as UserPlusIcon,
  Pencil as PencilIcon,
  RefreshCw as RefreshCwIcon
} from 'lucide-vue-next'

const router = useRouter()
const authStore = useAuthStore()
const themeStore = useThemeStore()

const abaAtiva = ref('usuarios')
const usuarios = ref([])
const usuarioSelecionado = ref(null)

const carregandoUsuarios = ref(false)
const salvandoCadastro = ref(false)
const salvandoEdicao = ref(false)
const excluindoUsuario = ref(false)

const novo = ref({
  nome: '',
  email: '',
  senha: '',
  perfil: 'operador'
})

const edicao = ref({
  perfil: 'operador',
  novaSenha: ''
})

const modalConfirmacaoExclusao = ref({
  aberto: false
})

const modalFeedback = ref({
  aberto: false,
  titulo: '',
  mensagem: ''
})

function formatarData(data) {
  if (!data) return '—'
  return new Date(data).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function selecionarUsuario(usuario) {
  usuarioSelecionado.value = usuario
  edicao.value.perfil = (usuario.perfil || 'operador').toLowerCase()
  edicao.value.novaSenha = ''
}

function abrirEdicao() {
  abaAtiva.value = 'editar'
}

function cancelarEdicao() {
  abaAtiva.value = 'usuarios'
  edicao.value.novaSenha = ''
}

async function carregarUsuarios() {
  carregandoUsuarios.value = true
  try {
    const res = await axios.get('/api/usuarios')
    usuarios.value = (res.data || []).sort((a, b) => (a.nome || '').localeCompare((b.nome || ''), 'pt-BR'))

    if (usuarioSelecionado.value) {
      const atualizado = usuarios.value.find((u) => u.id === usuarioSelecionado.value.id)
      usuarioSelecionado.value = atualizado || null
      if (atualizado) {
        edicao.value.perfil = (atualizado.perfil || 'operador').toLowerCase()
      }
    }
  } catch (e) {
    alert(e.response?.data?.mensagem || 'Erro ao carregar usuários.')
  } finally {
    carregandoUsuarios.value = false
  }
}

async function cadastrarUsuario() {
  salvandoCadastro.value = true
  try {
    await axios.post('/api/usuarios', novo.value)
    novo.value = { nome: '', email: '', senha: '', perfil: 'operador' }
    abaAtiva.value = 'usuarios'
    await carregarUsuarios()
  } catch (e) {
    alert(e.response?.data?.mensagem || 'Erro ao cadastrar usuário.')
  } finally {
    salvandoCadastro.value = false
  }
}

function excluirUsuarioSelecionado() {
  if (!usuarioSelecionado.value) {
    alert('Selecione um usuário na tabela antes de bloquear.')
    return
  }

  modalConfirmacaoExclusao.value.aberto = true
}

function fecharModalExclusao() {
  if (excluindoUsuario.value) return
  modalConfirmacaoExclusao.value.aberto = false
}

function abrirModalFeedback(titulo, mensagem) {
  modalFeedback.value = {
    aberto: true,
    titulo,
    mensagem
  }
}

function fecharModalFeedback() {
  modalFeedback.value.aberto = false
}

async function confirmarExclusaoUsuario() {
  if (!usuarioSelecionado.value) {
    modalConfirmacaoExclusao.value.aberto = false
    return
  }

  excluindoUsuario.value = true
  try {
    const id = Number(usuarioSelecionado.value.id)
    if (!Number.isFinite(id) || id <= 0) {
      throw new Error('ID de usuário inválido para bloqueio.')
    }

    await axios.delete(`/api/usuarios/${id}`)

    usuarioSelecionado.value = null
    edicao.value = { perfil: 'operador', novaSenha: '' }
    abaAtiva.value = 'usuarios'
    modalConfirmacaoExclusao.value.aberto = false
    await carregarUsuarios()
    abrirModalFeedback('Bloqueio concluído', 'Usuário bloqueado com sucesso.')
  } catch (e) {
    const mensagemBackend = e?.response?.data?.mensagem
      || e?.response?.data
      || e?.message
      || 'Erro ao bloquear usuário.'
    abrirModalFeedback('Falha no bloqueio', String(mensagemBackend))
  } finally {
    excluindoUsuario.value = false
  }
}

async function salvarEdicao() {
  if (!usuarioSelecionado.value) {
    alert('Selecione um usuário na tabela antes de editar.')
    return
  }

  salvandoEdicao.value = true
  try {
    const id = usuarioSelecionado.value.id

    if (edicao.value.perfil && edicao.value.perfil !== (usuarioSelecionado.value.perfil || '').toLowerCase()) {
      await axios.put(`/api/usuarios/${id}/perfil`, { Perfil: edicao.value.perfil })
    }

    if (edicao.value.novaSenha) {
      if (edicao.value.novaSenha.length < 6) {
        alert('A nova senha deve ter no mínimo 6 caracteres.')
        salvandoEdicao.value = false
        return
      }
      await axios.put(`/api/usuarios/${id}/senha`, { NovaSenha: edicao.value.novaSenha })
    }

    edicao.value.novaSenha = ''
    await carregarUsuarios()
    abaAtiva.value = 'usuarios'
  } catch (e) {
    alert(e.response?.data?.mensagem || 'Erro ao salvar alterações do usuário.')
  } finally {
    salvandoEdicao.value = false
  }
}

onMounted(carregarUsuarios)
</script>

<style scoped>
.usuarios-view {
  display: flex;
  flex-direction: column;
  height: 100dvh;
  background: var(--cptm-bg);
  overflow-y: auto;
}

.usuarios-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.75rem 1rem;
  background: var(--cptm-header);
  color: var(--cptm-header-text);
  flex-shrink: 0;
  z-index: 20;
  position: sticky;
  top: 0;
}

.usuarios-header__brand {
  display: flex;
  align-items: center;
  gap: 0.65rem;
}

.usuarios-header__icon {
  width: 2rem;
  height: 2rem;
  border-radius: 0.6rem;
  display: grid;
  place-items: center;
  background: var(--cptm-primary);
}

.usuarios-header__title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 800;
  line-height: 1.2;
}

.usuarios-header__subtitle {
  margin: 0;
  font-size: 0.68rem;
  opacity: 0.7;
  line-height: 1.2;
}

.usuarios-header__actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.usuarios-header__nome {
  display: none;
  font-size: 0.8rem;
  opacity: 0.8;
}

@media (min-width: 640px) {
  .usuarios-header__nome {
    display: block;
  }
}

.usuarios-header__btn-icon,
.usuarios-header__btn-voltar {
  border-radius: 0.7rem;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  cursor: pointer;
  color: var(--cptm-header-text);
  border: 1px solid rgba(255, 255, 255, 0.15);
  background: transparent;
  transition: background-color 0.2s;
}

.usuarios-header__btn-icon {
  width: 2.1rem;
  height: 2.1rem;
}

.usuarios-header__btn-voltar {
  padding: 0.5rem 0.85rem;
  font-size: 0.78rem;
  font-weight: 700;
}

.usuarios-header__btn-icon:hover,
.usuarios-header__btn-voltar:hover {
  background: rgba(255, 255, 255, 0.1);
}

.usuarios-main {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
}

.usuarios-toolbar {
  display: grid;
  grid-template-columns: 1fr;
  gap: 0.6rem;
  background: var(--cptm-surface);
  border: 1px solid var(--cptm-border);
  border-radius: 1rem;
  padding: 0.85rem;
}

@media (min-width: 768px) {
  .usuarios-toolbar {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
}

.usuarios-toolbar__btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface-alt);
  color: var(--cptm-text);
  border-radius: 0.75rem;
  padding: 0.62rem 0.85rem;
  cursor: pointer;
  font-weight: 700;
  font-size: 0.82rem;
}

.usuarios-toolbar__btn.active {
  background: var(--cptm-primary);
  color: #fff;
  border-color: transparent;
}

.usuarios-card {
  background: var(--cptm-surface);
  border: 1px solid var(--cptm-border);
  border-radius: 1rem;
  padding: 1rem;
}

.usuarios-card__title {
  margin: 0 0 0.85rem;
  font-size: 0.95rem;
  font-weight: 800;
  color: var(--cptm-text);
}

.usuarios-form {
  display: grid;
  gap: 0.65rem;
}

.usuarios-selected {
  margin: 0;
  font-size: 0.82rem;
  color: var(--cptm-text-muted);
}

.usuarios-edit-actions {
  display: flex;
  gap: 0.6rem;
  flex-wrap: wrap;
}

.usuarios-btn-primary,
.usuarios-btn-secondary,
.usuarios-btn-danger {
  padding: 0.65rem 1rem;
  border-radius: 0.6rem;
  font-weight: 700;
  font-size: 0.85rem;
  cursor: pointer;
}

.usuarios-btn-primary {
  background: var(--cptm-primary);
  color: #fff;
  border: none;
}

.usuarios-btn-secondary {
  background: var(--cptm-surface-alt);
  color: var(--cptm-text);
  border: 1px solid var(--cptm-border);
}

.usuarios-btn-danger {
  background: #b91c1c;
  color: #fff;
  border: none;
}

.usuarios-btn-primary:disabled,
.usuarios-btn-secondary:disabled,
.usuarios-btn-danger:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.usuarios-list-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.usuarios-count {
  margin: 0;
  font-size: 0.8rem;
  color: var(--cptm-text-muted);
  font-weight: 700;
}

.usuarios-table-wrap {
  width: 100%;
  overflow-x: auto;
  border: 1px solid var(--cptm-border);
  border-radius: 0.8rem;
}

.usuarios-table {
  width: 100%;
  border-collapse: collapse;
  min-width: 760px;
  background: var(--cptm-surface);
}

.usuarios-table th,
.usuarios-table td {
  text-align: left;
  padding: 0.65rem 0.75rem;
  border-bottom: 1px solid var(--cptm-border);
  font-size: 0.82rem;
  color: var(--cptm-text);
}

.usuarios-table th {
  background: var(--cptm-surface-alt);
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  font-weight: 800;
  color: var(--cptm-text-muted);
}

.usuarios-row {
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.usuarios-row:hover {
  background: color-mix(in srgb, var(--cptm-primary) 8%, transparent);
}

.usuarios-row--selected {
  background: color-mix(in srgb, var(--cptm-primary) 18%, transparent);
}

.usuarios-cell-capitalize {
  text-transform: capitalize;
}

.usuarios-empty {
  text-align: center;
  color: var(--cptm-text-muted);
}

.usuarios-modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 70;
  background: rgba(0, 0, 0, 0.55);
  display: grid;
  place-items: center;
  padding: 1rem;
}

.usuarios-modal {
  width: min(100%, 28rem);
  background: var(--cptm-surface);
  border: 1px solid var(--cptm-border);
  border-radius: 1rem;
  padding: 1rem;
  box-shadow: 0 16px 40px rgba(0, 0, 0, 0.35);
}

.usuarios-modal__title {
  margin: 0 0 0.5rem;
  font-size: 1rem;
  font-weight: 800;
  color: var(--cptm-text);
}

.usuarios-modal__text {
  margin: 0;
  color: var(--cptm-text);
  font-size: 0.9rem;
}

.usuarios-modal__warning {
  margin: 0.45rem 0 0;
  color: #b91c1c;
  font-size: 0.86rem;
  font-weight: 700;
}

.usuarios-modal__actions {
  margin-top: 1rem;
  display: flex;
  gap: 0.6rem;
  justify-content: flex-end;
  flex-wrap: wrap;
}
</style>
