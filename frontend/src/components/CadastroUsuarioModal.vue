<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-[100] flex items-end sm:items-center justify-center bg-black/60 px-0 sm:px-4"
         @click.self="$emit('fechar')">
      <div class="w-full sm:max-w-md sm:rounded-2xl rounded-t-2xl p-6 shadow-2xl max-h-[90vh] overflow-y-auto"
           :style="{ backgroundColor: 'var(--cptm-surface)' }">

        <div class="flex items-center justify-between mb-6">
          <h2 class="text-lg font-bold" :style="{ color: 'var(--cptm-text)' }">Cadastrar Usuário</h2>
          <button @click="$emit('fechar')" class="p-1 rounded-lg cursor-pointer hover:opacity-70"
                  :style="{ color: 'var(--cptm-text-muted)' }">
            <XIcon class="w-5 h-5" />
          </button>
        </div>

        <form @submit.prevent="salvar" class="grid gap-4">
          <div>
            <label class="block text-xs font-semibold mb-1.5" :style="{ color: 'var(--cptm-text-muted)' }">Nome completo</label>
            <input v-model="form.nome" type="text" required maxlength="200"
                   class="field-input w-full" placeholder="Ex: João Silva" />
          </div>

          <div>
            <label class="block text-xs font-semibold mb-1.5" :style="{ color: 'var(--cptm-text-muted)' }">E-mail</label>
            <input v-model="form.email" type="email" required maxlength="200"
                   class="field-input w-full" placeholder="joao@cptm.sp.gov.br" />
          </div>

          <div>
            <label class="block text-xs font-semibold mb-1.5" :style="{ color: 'var(--cptm-text-muted)' }">Senha</label>
            <input v-model="form.senha" type="password" required minlength="6" maxlength="100"
                   class="field-input w-full" placeholder="Mínimo 6 caracteres" />
          </div>




          <div>
            <label class="block text-xs font-semibold mb-2" :style="{ color: 'var(--cptm-text-muted)' }">Perfil</label>
            <div class="flex gap-6">
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="radio" v-model="form.perfil" value="operador" 
                       class="w-4 h-4 cursor-pointer" style="accent-color: var(--cptm-primary);">
                <span class="text-sm font-medium" :style="{ color: 'var(--cptm-text)' }">Operador</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="radio" v-model="form.perfil" value="supervisor" 
                       class="w-4 h-4 cursor-pointer" style="accent-color: var(--cptm-primary);">
                <span class="text-sm font-medium" :style="{ color: 'var(--cptm-text)' }">Supervisor</span>
              </label>
            </div>
          </div>




          <div v-if="mensagem" class="rounded-xl px-4 py-3 text-sm"
               :class="erro ? 'bg-red-50 text-red-700 dark:bg-red-950/40 dark:text-red-300' : 'bg-green-50 text-green-700 dark:bg-green-950/40 dark:text-green-300'">
            {{ mensagem }}
          </div>

          <div class="flex gap-3 pt-2">
            <button type="button" @click="$emit('fechar')"
                    class="flex-1 py-2.5 rounded-xl border text-sm font-semibold cursor-pointer"
                    :style="{ borderColor: 'var(--cptm-border)', color: 'var(--cptm-text)' }">
              Cancelar
            </button>
            <button type="submit" :disabled="salvando"
                    class="flex-1 py-2.5 rounded-xl text-white text-sm font-semibold cursor-pointer"
                    style="background-color: var(--cptm-primary);">
              {{ salvando ? 'Salvando...' : 'Cadastrar' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref } from 'vue'
import { X as XIcon } from 'lucide-vue-next'
import axios from 'axios'

const emit = defineEmits(['fechar', 'usuarioCadastrado'])

const form = ref({ nome: '', email: '', senha: '', perfil: 'operador' })
const salvando = ref(false)
const mensagem = ref('')
const erro = ref(false)

  async function salvar() {
  salvando.value = true
  mensagem.value = ''
  erro.value = false

  try {
    await axios.post('/api/usuarios', form.value)
    
    mensagem.value = 'Usuário cadastrado com sucesso!'
    emit('usuarioCadastrado', { ...form.value })
    
    setTimeout(() => {
      form.value = { nome: '', email: '', senha: '', perfil: 'operador' }
      emit('fechar')
    }, 1200)
  } catch (e) {
    erro.value = true
    mensagem.value = e.response?.data?.mensagem || 'Erro ao cadastrar usuário.'
  } finally {
    salvando.value = false
  }
}
</script>
