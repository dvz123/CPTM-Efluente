<template>
  <div class="min-h-dvh operator-form-page" :style="{ backgroundColor: 'var(--cptm-bg)' }">
    <!-- ====== HEADER ====== -->
    <header class="sticky top-0 z-40 px-4 py-3 shadow-sm border-b"
            :style="{ backgroundColor: 'var(--cptm-header)', color: 'var(--cptm-header-text)', borderColor: 'var(--cptm-border)' }">
      <div class="max-w-5xl mx-auto flex items-center justify-between gap-3">
        <div class="flex items-center gap-3 min-w-0">
          <img :src="cptmLogo" alt="CPTM" class="w-10 h-10 object-contain rounded-lg bg-white p-1" />
        <div>
            <p class="text-sm font-bold leading-tight">Novo formulário de campo</p>
            <p class="text-[11px] opacity-70">Registro operacional para agentes em campo</p>
          </div>
        </div>

        <div class="flex items-center gap-2 shrink-0">
          <span class="hidden sm:flex items-center gap-1 text-[10px] font-medium px-2 py-1 rounded-full"
                :class="isOnline ? 'bg-green-500/20 text-green-400' : 'bg-amber-500/20 text-amber-400'">
            <span class="w-1.5 h-1.5 rounded-full" :class="isOnline ? 'bg-green-400' : 'bg-amber-400'"></span>
            {{ isOnline ? 'Online' : 'Offline' }}
          </span>
          <button @click="voltarParaInicio" class="px-3 py-2 rounded-lg cursor-pointer text-xs font-semibold border"
                  :style="{ color: 'var(--cptm-header-text)', borderColor: 'rgba(255,255,255,0.15)' }">
            Voltar
          </button>
          <button @click="themeStore.toggle()" class="p-2 rounded-lg cursor-pointer border"
                  :style="{ color: 'var(--cptm-header-text)', borderColor: 'rgba(255,255,255,0.15)' }">
            <SunIcon v-if="themeStore.isDark" class="w-4 h-4" />
            <MoonIcon v-else class="w-4 h-4" />
          </button>
        </div>
      </div>
    </header>

    <!-- ====== OFFLINE BANNER ====== -->
    <div v-if="!isOnline" class="offline-banner">
      Sem internet — dados serão salvos localmente
    </div>

    <!-- ====== STEPPER ====== -->
    <div class="px-4 py-4">
      <div class="max-w-3xl mx-auto mb-3">
        <div class="operator-form-draft-banner">
          <div>
            <p class="operator-form-draft-title">{{ isModoEdicao ? 'Editando Formulário' : 'Formulário de efluente' }}</p>
            <p class="operator-form-draft-text">{{ isModoEdicao ? 'Alterações serão salvas diretamente no banco de dados.' : 'Campos operacionais salvos automaticamente neste dispositivo.' }}</p>
          </div>
          <span class="operator-form-draft-status">{{ statusRascunho }}</span>
        </div>
      </div>
      <div class="flex items-center justify-between max-w-md mx-auto">
        <template v-for="(step, i) in steps" :key="i">
          <div class="flex flex-col items-center gap-1">
            <div class="stepper-dot" :class="stepClass(i)">
              <CheckIcon v-if="i < etapaAtual" class="w-4 h-4" />
              <span v-else>{{ i + 1 }}</span>
            </div>
            <span class="text-[10px] font-medium" :style="{ color: i <= etapaAtual ? 'var(--cptm-primary)' : 'var(--cptm-text-muted)' }">
              {{ step }}
            </span>
          </div>
          <div v-if="i < steps.length - 1" class="flex-1 h-0.5 mx-2 rounded-full"
               :style="{ backgroundColor: i < etapaAtual ? 'var(--cptm-primary)' : 'var(--cptm-border)' }"></div>
        </template>
      </div>
    </div>

    <!-- ====== FORM CONTENT ====== -->
    <div class="operator-form-content px-4 pb-44 md:pb-36 max-w-3xl mx-auto w-full">
      <div class="operator-form-required-note">
        <span class="operator-form-required-note__mark">*</span>
        Campos obrigatórios para avançar nas etapas e enviar o formulário.
      </div>

      <!-- ETAPA 1: Identificação -->
      <div v-show="etapaAtual === 0" class="space-y-4 operator-form-step">
        <div class="operator-form-cover-card">
          <img :src="cptmLogo" alt="CPTM" class="operator-form-cover-logo" />
          <div>
            <p class="operator-form-cover-title">Companhia Paulista de Trens Metropolitanos - CPTM</p>
            <p class="operator-form-cover-text">Gerência de Meio Ambiente - GEA</p>
            <p class="operator-form-cover-text">Formulário de Cadastramento/Caracterização - FDC</p>
            <p class="operator-form-cover-text">Programa Ambiental: Efluentes e Emissões Atmosféricas - EEA</p>
            <p class="operator-form-cover-text"><strong>Natureza:</strong> Efluentes - EF</p>
          </div>
        </div>

        <div class="operator-form-glossary-card">
          <p class="operator-form-glossary-title">Glossário</p>
          <p class="operator-form-glossary-text">E.M. = Elemento de Monitoramento · PF = Pessoa Física · PJ = Pessoa Jurídica · DRA = Documento de Regularidade Ambiental</p>
        </div>

        <h2 class="operator-form-section-title">1. Premissas Institucionais / Cabeçalho</h2>

        <FieldGroup label="Nome (PJ) da Contratada" :required="true" tooltip="Inserir o nome da empresa contratada responsável pela atividade." hint="Use a razão social conforme cadastro ou documento de referência.">
          <input v-model="form.contratada" @blur="form.contratada = sanitizeTrim(form.contratada)" class="field-input" placeholder="CPTM" maxlength="200" />
        </FieldGroup>

        <FieldGroup label="Nº do Contrato (da Contratada)" tooltip="Inserir o identificador do contrato da contratada, quando aplicável." hint="Preencha exatamente como no documento. Evite espaços extras no início ou no fim.">
          <input v-model="form.numContrato" @blur="form.numContrato = sanitizeTrim(form.numContrato)" class="field-input" placeholder="Ex: ARP 01823" maxlength="50" />
        </FieldGroup>

        <FieldGroup label="Local do Escopo Contratual (Pseudônimo)" tooltip="Indicar o nome genérico do local, área ou trecho relacionado ao serviço em campo." hint="Preencha com o nome adotado pela operação, sem espaços excedentes.">
          <input v-model="form.empresa" @blur="form.empresa = sanitizeTrim(form.empresa)" class="field-input" placeholder="Ex: Pátio Capuava" maxlength="200" />
        </FieldGroup>

        <FieldGroup label="Sigla da Área de Meio Ambiente" :required="true" tooltip="Sigla da gerência de Meio Ambiente responsável pelo cadastro." hint="Selecione uma sigla válida da lista institucional CPTM.">
          <SearchSelect v-model="form.siglaMeioAmbiente" :opcoes="opcoesSiglaMeioAmbienteSelect" placeholder="Selecione a sigla..." />
        </FieldGroup>

        <FieldGroup label="Status do Desvio Ambiental" :required="true" tooltip="Indica se o desvio ambiental está regularizado ou não regularizado." hint="Selecione a condição atual do desvio conforme a classificação CPTM.">
          <SearchSelect v-model="form.statusDesvioAmbiental" :opcoes="opcoesStatusDesvioAmbientalSelect" placeholder="Selecione o status..." />
        </FieldGroup>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Nome da Área Gestora CPTM" tooltip="Área da CPTM responsável pela gestão do contrato ou do local." hint="Preencha conforme a identificação institucional da CPTM.">
            <input v-model="form.nomeAreaGestoraCptm" @blur="form.nomeAreaGestoraCptm = sanitizeTrim(form.nomeAreaGestoraCptm)" class="field-input" maxlength="255" />
          </FieldGroup>
          <FieldGroup label="Sigla da Área Gestora CPTM" tooltip="Sigla institucional da área gestora CPTM." hint="Informe a sigla oficial da área gestora, quando disponível.">
            <input v-model="form.siglaAreaGestoraCptm" @input="form.siglaAreaGestoraCptm = sanitizeCodigo(form.siglaAreaGestoraCptm, true)" class="field-input" maxlength="255" inputmode="text" autocapitalize="characters" />
          </FieldGroup>
        </div>

        <FieldGroup label="Identificador da Área Gestora CPTM" tooltip="Código institucional da área gestora CPTM." hint="Use o identificador institucional completo, por exemplo ID.10-15-5-3-0000.">
          <input v-model="form.idAreaGestoraCptm" @input="form.idAreaGestoraCptm = sanitizeCodigo(form.idAreaGestoraCptm, true)" class="field-input" maxlength="255" inputmode="text" autocapitalize="characters" />
        </FieldGroup>

        <FieldGroup label="Nome (PJ) da Supervisora Ambiental" tooltip="Inserir o nome da empresa supervisora ambiental vinculada à atividade." hint="Use a razão social sem abreviações.">
          <input v-model="form.supervisor" @blur="form.supervisor = sanitizeTrim(form.supervisor)" class="field-input" placeholder="Empresa de Supervisão Ambiental" maxlength="200" />
        </FieldGroup>

        <h2 class="operator-form-section-title pt-4">2. Identificação do Cadastrador e Responsável Técnico</h2>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Autor(a) (PF) do Cadastramento" :required="true" tooltip="Nome completo da pessoa que realizou o cadastramento da informação." hint="Informar nome completo, sem apelidos.">
            <input v-model="form.autor" @blur="form.autor = sanitizeTrim(form.autor)" class="field-input" maxlength="200" />
          </FieldGroup>
          <FieldGroup label="Responsável Técnico - RT" tooltip="Nome completo do responsável técnico pelo cadastramento e caracterização." hint="Preencha com o nome completo, sem abreviações.">
            <input v-model="form.rt" @blur="form.rt = sanitizeTrim(form.rt)" class="field-input" maxlength="50" />
          </FieldGroup>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Registro Profissional (do RT)" tooltip="Registro profissional do responsável técnico." hint="Informar somente o código do registro, sem espaços.">
            <input v-model="form.registroProfissional" @input="form.registroProfissional = sanitizeCodigo(form.registroProfissional)" class="field-input" maxlength="50" inputmode="text" autocapitalize="characters" />
          </FieldGroup>
          <FieldGroup label="Documento de Responsabilidade Técnica" tooltip="Documento de responsabilidade técnica do responsável técnico." hint="Copiar o identificador do documento sem espaços extras.">
            <input v-model="form.documentoRt" @input="form.documentoRt = sanitizeCodigo(form.documentoRt)" class="field-input" maxlength="50" inputmode="text" autocapitalize="characters" />
          </FieldGroup>
        </div>

        <h2 class="operator-form-section-title pt-4">3. Identificação do Formulário</h2>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Natureza do PGA" tooltip="Para operadores desta frente, a natureza do formulário é sempre efluente." hint="Campo definido automaticamente pelo fluxo do operador.">
            <input v-model="form.natureza" class="field-input" readonly />
          </FieldGroup>
          <FieldGroup label="Tipo de Formulário" tooltip="Tipo do documento de campo utilizado pela equipe." hint="Campo institucional fixo.">
            <input v-model="form.tipoDocumento" class="field-input" maxlength="20" readonly />
          </FieldGroup>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Data de Emissão do Formulário" :required="true" tooltip="Data de emissão do formulário em campo." hint="Selecionar a data oficial do registro.">
            <input v-model="form.data" type="date" class="field-input" />
          </FieldGroup>
          <FieldGroup label="Número do Formulário" :required="true" tooltip="Número sequencial do formulário." hint="Use o padrão do formulário físico. Não inserir espaços.">
            <input v-model="form.numero" @input="form.numero = formatNumeroFormulario(form.numero)" class="field-input" placeholder="Ex: 1" maxlength="10" inputmode="numeric" />
          </FieldGroup>
        </div>
      </div>

      <!-- ETAPA 2: Local e Data -->
      <div v-show="etapaAtual === 1" class="space-y-4 operator-form-step">
        <h2 class="operator-form-section-title">4. Data e Hora do Cadastramento do E.M.</h2>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Data do Cadastramento" :required="true" tooltip="Data do cadastramento da informação em campo." hint="Registrar a data efetiva da vistoria/cadastro.">
            <input v-model="form.dataColeta" type="date" class="field-input" />
          </FieldGroup>
          <FieldGroup label="Hora do Cadastramento" :required="true" tooltip="Horário do cadastramento da informação." hint="Informar o horário efetivo do registro.">
            <input v-model="form.horaColeta" type="time" class="field-input" />
          </FieldGroup>
        </div>

        <h2 class="operator-form-section-title pt-4">5. Identificação do E.M.</h2>

        <FieldGroup label="Chave Primária - Meio Ambiente" :required="true" tooltip="Chave primária do elemento de monitoramento na base de dados." hint="Informar o identificador sem espaços.">
          <input v-model="form.chavePrimaria" @input="form.chavePrimaria = sanitizeCodigo(form.chavePrimaria)" class="field-input" maxlength="100" inputmode="text" autocapitalize="characters" />
        </FieldGroup>

        <FieldGroup label="Elemento de Monitoramento - Número" :required="true" tooltip="Número do elemento de monitoramento." hint="Preencha o código conforme cadastro interno, sem espaços.">
          <input v-model="form.elementoNumero" @input="form.elementoNumero = sanitizeCodigo(form.elementoNumero)" class="field-input" maxlength="50" inputmode="text" autocapitalize="characters" />
        </FieldGroup>

        <FieldGroup label="Elemento de Monitoramento - Nome" :required="true" tooltip="Nome descritivo do elemento monitorado." hint="Usar a denominação operacional adotada pela equipe.">
          <input v-model="form.elementoNome" @blur="form.elementoNome = sanitizeTrim(form.elementoNome)" class="field-input" placeholder="Ex: Caçamba A" maxlength="200" />
        </FieldGroup>

        <h2 class="operator-form-section-title pt-4">6. Localização do E.M.</h2>

        <FieldGroup label="Município" :required="true" tooltip="Município onde a atividade está sendo realizada." hint="Selecione um item da lista oficial.">
          <SearchSelect
            v-model="form.municipio"
            :opcoes="municipios"
            placeholder="Buscar município..."
          />
        </FieldGroup>

        <!-- Lógica Condicional: Na Estação ou Entre Estações -->
        <FieldGroup label="Local" :required="true" tooltip="Selecione se a atividade é na estação ou entre estações." hint="Escolha uma única opção para liberar os campos corretos abaixo.">
          <SearchSelect v-model="form.localTipo" :opcoes="opcoesLocalTipo" placeholder="Selecione o local..." />
        </FieldGroup>

        <!-- Campos de Estação (condicional) -->
        <div v-if="form.localTipo === 'estacao'" class="space-y-4">
          <FieldGroup label="Linha CPTM" :required="true" tooltip="Selecione a linha ferroviária." hint="Selecionar a linha correspondente ao local do registro.">
            <SearchSelect v-model="form.linhaCptm" :opcoes="linhasCptm" placeholder="Buscar linha..." />
          </FieldGroup>
          <FieldGroup label="Estação" :required="true" tooltip="Estação onde ocorre a atividade." hint="Selecionar a estação oficial da CPTM.">
            <SearchSelect v-model="form.estacao" :opcoes="estacoesFiltradas" placeholder="Buscar estação..." />
          </FieldGroup>
        </div>

        <!-- Campos de Entre Estações (condicional) -->
        <div v-if="form.localTipo === 'entre_estacoes'" class="space-y-4">
          <FieldGroup label="Linha CPTM" :required="true" tooltip="Selecione a linha ferroviária." hint="Selecionar a linha correspondente ao trecho informado.">
            <SearchSelect v-model="form.linhaCptm" :opcoes="linhasCptm" placeholder="Buscar linha..." />
          </FieldGroup>
          <FieldGroup label="Número da Via da Linha CPTM" :required="true" tooltip="Selecionar ou informar a via associada ao elemento monitorado." hint="Preencher conforme a identificação operacional da via.">
            <input v-model="form.via" @blur="form.via = sanitizeTrim(form.via)" class="field-input" placeholder="Via 03 - Trecho 2" maxlength="50" />
          </FieldGroup>
          <FieldGroup label="Trecho e Sentido da Linha CPTM" :required="true" tooltip="Trecho e sentido associados ao elemento monitorado." hint="Descrever o trecho com o sentido operacional adotado.">
            <input v-model="form.trechoSentido" @blur="form.trechoSentido = sanitizeTrim(form.trechoSentido)" class="field-input" maxlength="100" />
          </FieldGroup>
          <FieldGroup label="Número do Quilômetro e Poste" :required="true" tooltip="Quilometragem e poste mais próximos do elemento monitorado." hint="Informar conforme o padrão de campo, sem espaços.">
            <input v-model="form.kmPoste" @input="form.kmPoste = formatKmPoste(form.kmPoste)" class="field-input" placeholder="51/02" maxlength="20" inputmode="text" autocapitalize="characters" />
          </FieldGroup>
        </div>

        <!-- GPS -->
        <h2 class="operator-form-section-title pt-4">Latitude e Longitude (Datum: WGS84)</h2>
        <OperatorMapCard
          v-if="etapaAtual === 1"
          v-model:latitude="form.latitude"
          v-model:longitude="form.longitude"
        />

        <div v-if="etapaAtual === 1" class="grid grid-cols-1 md:grid-cols-2 gap-3">
          <FieldGroup label="Latitude" tooltip="Digite a latitude no formato decimal ou use o mapa para posicionar o ponto." hint="Ex: -23.550520">
            <input
              v-model="form.latitude"
              type="text"
              inputmode="decimal"
              class="field-input"
              placeholder="-23.550520"
            />
          </FieldGroup>

          <FieldGroup label="Longitude" tooltip="Digite a longitude no formato decimal ou use o mapa para posicionar o ponto." hint="Ex: -46.633308">
            <input
              v-model="form.longitude"
              type="text"
              inputmode="decimal"
              class="field-input"
              placeholder="-46.633308"
            />
          </FieldGroup>
        </div>
      </div>

      <!-- ETAPA 3: Caracterização (DRA) -->
      <div v-show="etapaAtual === 2" class="space-y-4 operator-form-step">
        <h2 class="operator-form-section-title">7. Caracterização do E.M.</h2>
        <h3 class="operator-form-subsection-title">7.1 Regulamentação Ambiental</h3>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Tipo de Atividade (Listada)" :required="true" tooltip="Selecionar o tipo de atividade relacionada ao elemento de monitoramento." hint="Escolha apenas uma opção listada. Se não existir, use o campo abaixo.">
            <SearchSelect v-model="form.tipoAtividade" :opcoes="opcoesTipoAtividadeSelect" placeholder="Selecione o tipo de atividade..." />
          </FieldGroup>
          <FieldGroup label="Tipo de DRA (Listado)" tooltip="Tipo do Documento de Regularidade Ambiental." hint="Informar a sigla ou tipo oficial do documento.">
            <SearchSelect v-model="form.tipoDra" :opcoes="opcoesTipoDraSelect" placeholder="Selecione o tipo de DRA..." />
          </FieldGroup>
        </div>

        <FieldGroup label="Tipo de Atividade (Não Listada)" :required="true" tooltip="Descrever a atividade quando ela não constar entre as opções listadas." hint="Preencher somente se a atividade não estiver na lista anterior.">
          <input v-model="form.atividadeNaoListada" @blur="form.atividadeNaoListada = sanitizeTrim(form.atividadeNaoListada)" class="field-input" maxlength="200" />
        </FieldGroup>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Código Identificador do DRA" :required="Boolean(form.tipoDra)" tooltip="Código identificador do documento de regularidade ambiental." hint="Copiar exatamente do documento, sem espaços adicionais.">
            <input v-model="form.codigoDra" @input="form.codigoDra = sanitizeCodigoDocumento(form.codigoDra)" class="field-input" placeholder="Nº 1.285.456" maxlength="50" inputmode="text" autocapitalize="characters" />
          </FieldGroup>
          <FieldGroup label="Data Validade DRA" :required="Boolean(form.tipoDra)" tooltip="Data de validade do documento." hint="Selecionar a data de validade informada no documento.">
            <input v-model="form.dataValidadeDra" type="date" class="field-input" />
          </FieldGroup>
        </div>

        <h3 class="operator-form-subsection-title pt-4">7.2 Detalhamento</h3>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Tipo de Atividade na CPTM" :required="true" tooltip="Tipo de atividade associada à CPTM." hint="Selecionar a categoria institucional correspondente.">
            <SearchSelect v-model="form.tipoAtividadeCptm" :opcoes="opcoesTipoAtividadeCptmSelect" placeholder="Selecione a atividade CPTM..." />
          </FieldGroup>
          <FieldGroup label="Nome Edificação/Local da CPTM" :required="true" tooltip="Nome da edificação ou local associado ao registro." hint="Selecionar a tipologia que mais se aproxima do local.">
            <SearchSelect v-model="form.nomeEdificacao" :opcoes="opcoesNomeEdificacaoSelect" placeholder="Selecione a edificação..." />
          </FieldGroup>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Origem Efluente" :required="true" tooltip="De onde o efluente é originado." hint="Selecionar a origem predominante do efluente.">
            <SearchSelect v-model="form.origemEfluente" :opcoes="opcoesOrigemEfluenteSelect" placeholder="Selecione a origem..." />
          </FieldGroup>
          <FieldGroup label="Nome Edificação/Local (Complemento)" tooltip="Complemento do nome da edificação ou local." hint="Usar apenas quando necessário para detalhar o local.">
            <SearchSelect v-model="form.edificacaoComplemento" :opcoes="opcoesComplementoLocalSelect" placeholder="Selecione o complemento..." />
          </FieldGroup>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Tipo de Destinação do Efluente" :required="true" tooltip="Selecionar o tipo de destinação do efluente." hint="Selecionar a forma real de encaminhamento do efluente.">
            <SearchSelect v-model="form.destinacao" :opcoes="opcoesDestinacaoSelect" placeholder="Selecione a destinação..." />
          </FieldGroup>
          <FieldGroup label="Fonte Geradora do Efluente" :required="true" tooltip="Selecionar a fonte geradora do efluente." hint="Descrever a origem objetiva do efluente, sem texto excessivo.">
            <input v-model="form.fonteGeradora" @blur="form.fonteGeradora = sanitizeTrim(form.fonteGeradora)" class="field-input" placeholder="Ex: Sanitário, caixa separadora, lavagem" maxlength="120" />
          </FieldGroup>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Código Identificador da Guia de Remessa" tooltip="Identificador da guia de remessa, quando aplicável." hint="Preencher exatamente como na guia. Não inserir espaços.">
            <input v-model="form.codigoGuiaRemessa" @input="form.codigoGuiaRemessa = formatGuiaRemessa(form.codigoGuiaRemessa)" class="field-input" placeholder="Ex: GR-2026-00125" maxlength="80" inputmode="text" autocapitalize="characters" />
          </FieldGroup>
          <FieldGroup label="Quantidade (Litros)" :required="true" tooltip="Quantidade de efluente em litros." hint="Usar apenas números. Casas decimais são permitidas.">
            <input v-model="form.quantidadeLitros" @input="form.quantidadeLitros = sanitizeDecimal(form.quantidadeLitros)" type="text" inputmode="decimal" class="field-input" placeholder="25.78" maxlength="12" />
          </FieldGroup>
        </div>

        <h3 class="operator-form-subsection-title pt-4">7.3 Transporte e Encaminhamento</h3>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Tipo de Veículo" tooltip="Tipo de veículo utilizado no transporte do efluente." hint="Selecionar o veículo efetivamente utilizado no transporte.">
            <SearchSelect v-model="form.veiculoTipo" :opcoes="opcoesTipoVeiculoSelect" placeholder="Selecione o tipo de veículo..." />
          </FieldGroup>
          <FieldGroup label="Identificador/Placa do Veículo" :required="Boolean(form.veiculoTipo)" tooltip="Identificador ou placa do veículo transportador." hint="Usar o padrão da placa/identificador, sem espaços excedentes.">
            <input v-model="form.veiculoPlaca" @input="form.veiculoPlaca = formatPlaca(form.veiculoPlaca)" class="field-input" placeholder="ABC-1234 ou ABC1D23" maxlength="10" inputmode="text" autocapitalize="characters" />
          </FieldGroup>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <FieldGroup label="Guia de Remessa / Remoção" tooltip="Número da guia de remessa ou remoção, quando aplicável." hint="Informar o número exatamente como emitido, sem espaços excedentes.">
            <input v-model="form.guiaRemocao" @input="form.guiaRemocao = formatGuiaRemessa(form.guiaRemocao)" class="field-input" maxlength="50" inputmode="text" autocapitalize="characters" placeholder="Ex: GR-2026-00125" />
          </FieldGroup>
          <FieldGroup label="Distância da Via CPTM (Metros)" tooltip="Distância da via mais próxima em relação ao efluente." hint="Informar apenas o valor numérico em metros.">
            <input v-model="form.distanciaVia" @input="form.distanciaVia = sanitizeInteiro(form.distanciaVia)" type="text" inputmode="numeric" class="field-input" maxlength="6" />
          </FieldGroup>
        </div>

        <FieldGroup label="Observações Gerais do Cadastramento" tooltip="Inserir observações relevantes do cadastramento ou da vistoria." hint="Registrar apenas informações complementares objetivas.">
          <textarea v-model="form.observacoesGerais" @blur="form.observacoesGerais = sanitizeTrim(form.observacoesGerais)" class="field-input min-h-[80px]" maxlength="2000" placeholder="Descreva..."></textarea>
        </FieldGroup>
      </div>

      <!-- ETAPA 4: Fotos / Evidências -->
      <div v-show="etapaAtual === 3" class="space-y-4 operator-form-step">
        <h2 class="operator-form-section-title">Registro Fotográfico</h2>
        <p class="text-sm" :style="{ color: 'var(--cptm-text-muted)' }">
          Inserir fotos (3x4 Paisagem) — Mínimo:
          <strong :style="{ color: 'var(--cptm-primary)' }">{{ minFotos }} fotos</strong>
        </p>

        <!-- Indicador de fotos -->
        <div class="flex items-center gap-2 px-3 py-2 rounded-lg text-sm"
             :class="fotos.length >= minFotos ? 'bg-green-500/10 text-green-600' : 'bg-amber-500/10 text-amber-600'">
          <CameraIcon class="w-4 h-4" />
          {{ fotos.length }} / {{ minFotos }} fotos
          <span v-if="fotos.length >= minFotos"> — OK</span>
        </div>

        <!-- Grid de fotos -->
        <div class="grid grid-cols-2 gap-3">
          <div v-for="(foto, i) in fotoSlots" :key="i" class="space-y-2">
            <p class="text-xs font-medium" :style="{ color: 'var(--cptm-text)' }">Fotografia {{ i + 1 }}</p>
            <div class="photo-slot" @click="abrirInputFoto(i)">
              <img v-if="foto.preview" :src="foto.preview" alt="Foto" />
              <template v-else>
                <CameraIcon class="w-8 h-8" :style="{ color: 'var(--cptm-text-muted)' }" />
                <span class="text-[10px] mt-1" :style="{ color: 'var(--cptm-text-muted)' }">Adicionar Foto</span>
              </template>
              <!-- Botão remover -->
              <button v-if="foto.preview" @click.stop="removerFoto(i)"
                      class="absolute top-1 right-1 w-6 h-6 rounded-full bg-red-500 text-white flex items-center justify-center text-xs cursor-pointer">
                <XIcon class="w-3 h-3" />
              </button>
            </div>
          </div>
        </div>

        <input ref="inputFoto" type="file" accept="image/*" capture="environment"
               class="hidden" @change="onFotoSelecionada" />

        <!-- Summary Review -->
        <div class="mt-6 p-4 rounded-xl" :style="{ backgroundColor: 'var(--cptm-surface)' }">
          <h3 class="text-sm font-bold mb-2" :style="{ color: 'var(--cptm-text)' }">Resumo</h3>
          <ul class="text-xs space-y-1" :style="{ color: 'var(--cptm-text-muted)' }">
            <li>Registro Fotográfico: {{ fotos.length >= minFotos ? 'Completo' : 'Incompleto' }}</li>
            <li>Mín fotos (3x4 Paisagem): {{ minFotos }}</li>
            <li>Fotografias inseridas: {{ fotos.length }}</li>
          </ul>
        </div>
      </div>
    </div>

    <!-- ====== FOOTER NAVIGATION ====== -->
    <div class="operator-form-footer fixed bottom-0 left-0 right-0 px-4 py-4 z-[950] shadow-[0_-4px_12px_rgba(0,0,0,0.1)]"
         :style="{ backgroundColor: 'var(--cptm-surface)' }">
      <div class="flex gap-3 max-w-lg mx-auto">
        <button v-if="etapaAtual > 0" @click="etapaAnterior"
                class="flex-1 py-3 rounded-xl font-semibold text-sm border cursor-pointer"
                :style="{ borderColor: 'var(--cptm-border)', color: 'var(--cptm-text)' }">
          Voltar
        </button>
        <button v-if="etapaAtual < steps.length - 1" @click="proximaEtapa"
                class="flex-1 py-3 rounded-xl text-white font-semibold text-sm cursor-pointer"
                :style="{ backgroundColor: 'var(--cptm-primary)' }">
          Próximo
        </button>
        <button v-if="etapaAtual === steps.length - 1" @click="finalizarFormulario"
                :disabled="!podeEnviar || enviandoFormulario"
                class="flex-1 py-3 rounded-xl text-white font-semibold text-sm cursor-pointer disabled:opacity-40"
                :style="{ backgroundColor: 'var(--cptm-primary)' }">
          {{ enviandoFormulario ? 'Enviando...' : (isModoEdicao ? 'Salvar Alterações' : (isOnline ? 'Enviar Formulário' : 'Salvar Localmente')) }}
        </button>
      </div>
    </div>

    <div v-if="feedbackModal.aberto" class="operator-feedback-backdrop" @click="fecharFeedbackModal">
      <div class="operator-feedback-modal" @click.stop>
        <div class="operator-feedback-icon" :class="`operator-feedback-icon--${feedbackModal.tipo}`">
          <CheckCircle2Icon v-if="feedbackModal.tipo === 'sucesso'" class="w-6 h-6" />
          <TriangleAlertIcon v-else-if="feedbackModal.tipo === 'erro'" class="w-6 h-6" />
          <CloudOffIcon v-else class="w-6 h-6" />
        </div>
        <h3 class="operator-feedback-title">{{ feedbackModal.titulo }}</h3>
        <p class="operator-feedback-message">{{ feedbackModal.mensagem }}</p>
        <button
          type="button"
          class="operator-feedback-button"
          @click="fecharFeedbackModal"
        >
          {{ feedbackModal.acaoLabel }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import { db } from '@/database'
import axios from 'axios'
import { useConnectivityStatus } from '@/services/connectivityService'
import { carregarCacheReferencias, obterCacheReferencia, sincronizarPendentes } from '@/services/syncService'
import { fallbackEstacoes, fallbackFormularioOperador, fallbackLinhas } from '@/constants/formReferences'
import cptmLogo from '@/assets/Logo_CPTM.png'
import FieldGroup from '@/components/FieldGroup.vue'
import OperatorMapCard from '@/components/OperatorMapCard.vue'
import SearchSelect from '@/components/SearchSelect.vue'
import {
  Sun as SunIcon, Moon as MoonIcon, Check as CheckIcon, Camera as CameraIcon,
  X as XIcon, CheckCircle2 as CheckCircle2Icon, TriangleAlert as TriangleAlertIcon,
  CloudOff as CloudOffIcon
} from 'lucide-vue-next'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const themeStore = useThemeStore()
const { isOnline, checkConnectivity } = useConnectivityStatus()

const statusRascunho = ref('Rascunho vazio')
const enviandoFormulario = ref(false)
const isModoEdicao = computed(() => {
  // Edit mode is only available for supervisors
  const isSupervisor = authStore.usuario?.perfil === 'supervisor'
  if (isSupervisor && route.query.editar) {
    return true
  }
  // For non-supervisors, redirect away from edit mode
  if (route.query.editar && !isSupervisor) {
    console.warn('Acesso à edição negado: apenas supervisores podem editar formulários.')
    return false
  }
  return false
})
const pkEdicao = computed(() => {
  // Only supervisors can use the edit parameter
  const isSupervisor = authStore.usuario?.perfil === 'supervisor'
  if (isSupervisor && route.query.editar) {
    return route.query.editar
  }
  return null
})
const feedbackModal = ref({
  aberto: false,
  tipo: 'sucesso',
  titulo: '',
  mensagem: '',
  acaoLabel: 'Fechar',
  redirecionar: false
})

// ====== Stepper ======
const steps = ['Identificação', 'Local', 'Caracterização', 'Fotos']
const etapaAtual = ref(0)

function stepClass(i) {
  if (i < etapaAtual.value) return 'completed'
  if (i === etapaAtual.value) return 'active'
  return 'inactive'
}

function scrollToTop() {
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function isFilled(value) {
  if (typeof value === 'string') {
    return value.trim().length > 0
  }

  return value !== null && value !== undefined && value !== ''
}

function buildMissingFieldsMessage(campos) {
  if (campos.length <= 4) {
    return campos.join(', ')
  }

  return `${campos.slice(0, 4).join(', ')} e mais ${campos.length - 4}`
}

function obterCamposObrigatoriosPendentes(etapaIndex) {
  const pendencias = []
  const adicionarPendencia = (condicao, label) => {
    if (condicao) {
      pendencias.push(label)
    }
  }

  if (etapaIndex === 0) {
    adicionarPendencia(!isFilled(form.value.contratada), 'Nome da Contratada')
    adicionarPendencia(!isFilled(form.value.siglaMeioAmbiente), 'Sigla da Área de Meio Ambiente')
    adicionarPendencia(!isFilled(form.value.statusDesvioAmbiental), 'Status do Desvio Ambiental')
    adicionarPendencia(!isFilled(form.value.autor), 'Autor do Cadastramento')
    adicionarPendencia(!isFilled(form.value.data), 'Data de Emissão do Formulário')
    adicionarPendencia(!isFilled(form.value.numero), 'Número do Formulário')
  }

  if (etapaIndex === 1) {
    adicionarPendencia(!isFilled(form.value.dataColeta), 'Data do Cadastramento')
    adicionarPendencia(!isFilled(form.value.horaColeta), 'Hora do Cadastramento')
    adicionarPendencia(!isFilled(form.value.chavePrimaria), 'Chave Primária - Meio Ambiente')
    adicionarPendencia(!isFilled(form.value.elementoNumero), 'Elemento de Monitoramento - Número')
    adicionarPendencia(!isFilled(form.value.elementoNome), 'Elemento de Monitoramento - Nome')
    adicionarPendencia(!isFilled(form.value.municipio), 'Município')
    adicionarPendencia(!isFilled(form.value.localTipo), 'Local')

    if (form.value.localTipo === 'estacao') {
      adicionarPendencia(!isFilled(form.value.linhaCptm), 'Linha CPTM')
      adicionarPendencia(!isFilled(form.value.estacao), 'Estação')
    }

    if (form.value.localTipo === 'entre_estacoes') {
      adicionarPendencia(!isFilled(form.value.linhaCptm), 'Linha CPTM')
      adicionarPendencia(!isFilled(form.value.via), 'Número da Via da Linha CPTM')
      adicionarPendencia(!isFilled(form.value.trechoSentido), 'Trecho e Sentido da Linha CPTM')
      adicionarPendencia(!isFilled(form.value.kmPoste), 'Número do Quilômetro e Poste')
    }
  }

  if (etapaIndex === 2) {
    adicionarPendencia(!isFilled(form.value.tipoAtividade) && !isFilled(form.value.atividadeNaoListada), 'Tipo de Atividade')
    adicionarPendencia(!isFilled(form.value.tipoAtividadeCptm), 'Tipo de Atividade na CPTM')
    adicionarPendencia(!isFilled(form.value.nomeEdificacao), 'Nome Edificação/Local da CPTM')
    adicionarPendencia(!isFilled(form.value.origemEfluente), 'Origem Efluente')
    adicionarPendencia(!isFilled(form.value.destinacao), 'Tipo de Destinação do Efluente')
    adicionarPendencia(!isFilled(form.value.fonteGeradora), 'Fonte Geradora do Efluente')
    adicionarPendencia(!isFilled(form.value.quantidadeLitros), 'Quantidade (Litros)')

    if (isFilled(form.value.tipoDra)) {
      adicionarPendencia(!isFilled(form.value.codigoDra), 'Código Identificador do DRA')
      adicionarPendencia(!isFilled(form.value.dataValidadeDra), 'Data Validade DRA')
    }

    if (isFilled(form.value.veiculoTipo) || isFilled(form.value.veiculoPlaca)) {
      adicionarPendencia(!isFilled(form.value.veiculoTipo), 'Tipo de Veículo')
      adicionarPendencia(!isFilled(form.value.veiculoPlaca), 'Identificador/Placa do Veículo')
    }
  }

  return pendencias
}

function validarEtapa(etapaIndex = etapaAtual.value) {
  const pendencias = obterCamposObrigatoriosPendentes(etapaIndex)
  if (!pendencias.length) {
    return true
  }

  abrirFeedbackModal({
    tipo: 'erro',
    titulo: 'Campos obrigatórios pendentes',
    mensagem: `Preencha antes de continuar: ${buildMissingFieldsMessage(pendencias)}.`
  })
  return false
}

function proximaEtapa() {
  if (!validarEtapa()) {
    scrollToTop()
    return
  }

  if (etapaAtual.value < steps.length - 1) {
    etapaAtual.value++
    scrollToTop()
  }
}

function etapaAnterior() {
  if (etapaAtual.value > 0) {
    etapaAtual.value--
    scrollToTop()
  }
}

function sanitizeTrim(value) {
  if (typeof value !== 'string') {
    return value
  }

  return value.replace(/\s+/g, ' ').trim()
}

function sanitizeSemEspacos(value, upperCase = false) {
  if (typeof value !== 'string') {
    return value
  }

  const normalized = value.replace(/\s+/g, '')
  return upperCase ? normalized.toUpperCase() : normalized
}

function sanitizeCodigo(value, upperCase = false) {
  if (typeof value !== 'string') {
    return value
  }

  const normalized = value.replace(/\s+/g, '').replace(/[^a-zA-Z0-9./_-]/g, '')
  return upperCase ? normalized.toUpperCase() : normalized
}

function formatNumeroFormulario(value) {
  if (typeof value !== 'string') {
    return value
  }

  return value.replace(/\D+/g, '')
}

function sanitizeCodigoDocumento(value) {
  if (typeof value !== 'string') {
    return value
  }

  return value
    .replace(/[^a-zA-Z0-9./_\-º°\s]/g, '')
    .replace(/\s+/g, ' ')
    .trimStart()
}

function sanitizePlaca(value) {
  if (typeof value !== 'string') {
    return value
  }

  return value.replace(/\s+/g, '').replace(/[^a-zA-Z0-9-]/g, '').toUpperCase()
}

function formatPlaca(value) {
  const cleaned = sanitizePlaca(value)
  const semHifen = cleaned.replace(/-/g, '')

  if (/^[A-Z]{3}\d[A-Z]\d{0,2}$/.test(semHifen)) {
    return semHifen
  }

  if (/^[A-Z]{3}\d{1,4}$/.test(semHifen)) {
    return `${semHifen.slice(0, 3)}-${semHifen.slice(3)}`
  }

  return cleaned
}

function formatGuiaRemessa(value) {
  if (typeof value !== 'string') {
    return value
  }

  const cleaned = value.replace(/\s+/g, '').replace(/[^a-zA-Z0-9/_-]/g, '').toUpperCase()
  const parts = cleaned.split('-').filter(Boolean)

  if (parts.length >= 3) {
    return `${parts[0]}-${parts[1]}-${parts.slice(2).join('')}`
  }

  if (parts.length === 2) {
    return `${parts[0]}-${parts[1]}`
  }

  if (/^[A-Z]{2}\d{5,}$/.test(cleaned)) {
    return `${cleaned.slice(0, 2)}-${cleaned.slice(2, 6)}-${cleaned.slice(6)}`
  }

  return cleaned
}

function formatKmPoste(value) {
  if (typeof value !== 'string') {
    return value
  }

  const cleaned = value.replace(/\s+/g, '').replace(/[^0-9/]/g, '')
  const [km, ...restante] = cleaned.split('/')

  if (restante.length) {
    return `${km}/${restante.join('').slice(0, 4)}`
  }

  if (km.length <= 2) {
    return km
  }

  return `${km.slice(0, -2)}/${km.slice(-2)}`
}

function sanitizeInteiro(value) {
  if (typeof value !== 'string') {
    return value
  }

  return value.replace(/\D+/g, '')
}

function sanitizeDecimal(value) {
  if (typeof value !== 'string') {
    return value
  }

  const normalized = value.replace(',', '.').replace(/[^0-9.]/g, '')
  const [inteiro, ...restante] = normalized.split('.')
  if (!restante.length) {
    return inteiro
  }

  return `${inteiro}.${restante.join('')}`
}

function mapReferenciaParaSearchSelect(items, labelKey = 'Nome') {
  return (items || []).map((item) => ({
    valor: item?.valor ?? item?.value ?? item?.nome ?? item?.Nome ?? item?.texto ?? item?.text ?? item?.label ?? '',
    texto: item?.texto ?? item?.text ?? item?.label ?? item?.nome ?? item?.Nome ?? item?.valor ?? item?.value ?? ''
  })).filter((item) => item.valor && item.texto)
}

function mapStringsToSearchSelect(items) {
  return (items || []).map((item) => ({
    valor: item,
    texto: item
  }))
}

const opcoesSiglaMeioAmbiente = ref([...fallbackFormularioOperador.SiglasMeioAmbiente])
const opcoesStatusDesvioAmbiental = ref([...fallbackFormularioOperador.StatusDesvioAmbiental])
const opcoesTipoAtividade = ref([...fallbackFormularioOperador.TiposAtividade])
const opcoesTipoDra = ref([...fallbackFormularioOperador.TiposDra])
const opcoesTipoAtividadeCptm = ref([...fallbackFormularioOperador.TiposAtividadeCptm])
const opcoesNomeEdificacao = ref([...fallbackFormularioOperador.NomesEdificacao])
const opcoesOrigemEfluente = ref([...fallbackFormularioOperador.OrigensEfluente])
const opcoesComplementoLocal = ref([...fallbackFormularioOperador.ComplementosLocal])
const opcoesDestinacao = ref([...fallbackFormularioOperador.Destinacoes])
const opcoesTipoVeiculo = ref([...fallbackFormularioOperador.TiposVeiculo])

// ====== Dados do formulário ======
function createDefaultForm() {
  return {
  // Etapa 1 - Identificação
  contratada: 'CPTM',
  numContrato: '',
  empresa: '',
  siglaMeioAmbiente: '',
  statusDesvioAmbiental: '',
  nomeAreaGestoraCptm: '',
  idAreaGestoraCptm: '',
  siglaAreaGestoraCptm: '',
  supervisor: '',
  autor: authStore.usuario?.nome || '',
  rt: '',
  registroProfissional: '',
  documentoRt: '',
  natureza: 'Efluente',
  tipoDocumento: 'Formulário de Cadastramento - FDC (FDC-EEA.EF)',
  data: new Date().toISOString().split('T')[0],
  numero: '',

  // Etapa 2 - Local
  dataColeta: new Date().toISOString().split('T')[0],
  horaColeta: new Date().toTimeString().slice(0, 5),
  chavePrimaria: '',
  elementoNumero: '',
  elementoNome: '',
  municipio: '',
  localTipo: '', // 'estacao' ou 'entre_estacoes'
  linhaCptm: '',
  estacao: '',
  via: '',
  trechoSentido: '',
  kmPoste: '',
  latitude: null,
  longitude: null,

  // Etapa 3 - Caracterização
  tipoAtividade: '',
  tipoDra: '',
  atividadeNaoListada: '',
  codigoDra: '',
  dataValidadeDra: '',
  tipoAtividadeCptm: '',
  nomeEdificacao: '',
  origemEfluente: '',
  edificacaoComplemento: '',
  destinacao: '',
  fonteGeradora: '',
  codigoGuiaRemessa: '',
  quantidadeLitros: '',
  veiculoTipo: '',
  veiculoPlaca: '',
  guiaRemocao: '',
  distanciaVia: '',
  observacoesGerais: ''
  }
}

const form = ref(createDefaultForm())

// ====== Listas de referência ======
const linhasCptm = ref([...fallbackLinhas])

const estacoes = ref([...fallbackEstacoes])

const estacoesFiltradas = computed(() =>
  form.value.linhaCptm ? estacoes.value.filter(e => e.linha === form.value.linhaCptm) : estacoes.value
)

function encontrarEstacaoSelecionada(valorEstacao, linhaPreferencial = '') {
  if (!valorEstacao) {
    return null
  }

  const correspondentes = estacoes.value.filter((item) => item.valor === valorEstacao)
  if (correspondentes.length === 0) {
    return null
  }

  if (linhaPreferencial) {
    return correspondentes.find((item) => item.linha === linhaPreferencial) || null
  }

  return correspondentes.length === 1 ? correspondentes[0] : null
}

const municipios = ref(fallbackFormularioOperador.Municipios.map((nome) => ({ valor: nome, texto: nome })))
const opcoesLocalTipo = [
  { valor: 'estacao', texto: 'Na Estação' },
  { valor: 'entre_estacoes', texto: 'Entre Estações' }
]
const opcoesSiglaMeioAmbienteSelect = computed(() => mapStringsToSearchSelect(opcoesSiglaMeioAmbiente.value))
const opcoesStatusDesvioAmbientalSelect = computed(() => mapStringsToSearchSelect(opcoesStatusDesvioAmbiental.value))
const opcoesTipoAtividadeSelect = computed(() => mapStringsToSearchSelect(opcoesTipoAtividade.value))
const opcoesTipoDraSelect = computed(() => mapStringsToSearchSelect(opcoesTipoDra.value))
const opcoesTipoAtividadeCptmSelect = computed(() => mapStringsToSearchSelect(opcoesTipoAtividadeCptm.value))
const opcoesNomeEdificacaoSelect = computed(() => mapStringsToSearchSelect(opcoesNomeEdificacao.value))
const opcoesOrigemEfluenteSelect = computed(() => mapStringsToSearchSelect(opcoesOrigemEfluente.value))
const opcoesComplementoLocalSelect = computed(() => mapStringsToSearchSelect(opcoesComplementoLocal.value))
const opcoesDestinacaoSelect = computed(() => mapStringsToSearchSelect(opcoesDestinacao.value))
const opcoesTipoVeiculoSelect = computed(() => mapStringsToSearchSelect(opcoesTipoVeiculo.value))

function onNaturezaChange() {}

function abrirFeedbackModal({ tipo, titulo, mensagem, acaoLabel = 'Fechar', redirecionar = false }) {
  feedbackModal.value = {
    aberto: true,
    tipo,
    titulo,
    mensagem,
    acaoLabel,
    redirecionar
  }
}

async function resetarFormularioAposEnvio() {
  await db.cache.delete(getDraftKey())
  etapaAtual.value = 0
  fotos.value = []
  form.value = createDefaultForm()
  statusRascunho.value = 'Novo formulário pronto'
  
  if (authStore.usuario?.perfil?.toLowerCase() === 'supervisor') {
    await router.push('/supervisor')
  } else {
    await router.push('/operador')
  }
}

async function fecharFeedbackModal() {
  const deveRedirecionar = feedbackModal.value.redirecionar
  feedbackModal.value.aberto = false

  if (deveRedirecionar) {
    await resetarFormularioAposEnvio()
  }
}

// ====== Fotos ======
const fotos = ref([])
const inputFoto = ref(null)
let fotoIndexAlvo = 0
let draftTimer = null
const FOTO_MAX_DIMENSAO = 1600
const FOTO_QUALIDADE_INICIAL = 0.82
const FOTO_QUALIDADE_MINIMA = 0.55
const FOTO_TAMANHO_ALVO_BYTES = 900 * 1024

const minFotos = computed(() => 4)
const maxFotos = computed(() => 4)

function lerBlobComoDataUrl(blob) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (event) => resolve(event.target?.result || '')
    reader.onerror = () => reject(new Error('Não foi possível gerar a pré-visualização da foto.'))
    reader.readAsDataURL(blob)
  })
}

function carregarImagem(blob) {
  return new Promise((resolve, reject) => {
    const imageUrl = URL.createObjectURL(blob)
    const image = new Image()
    image.onload = () => {
      URL.revokeObjectURL(imageUrl)
      resolve(image)
    }
    image.onerror = () => {
      URL.revokeObjectURL(imageUrl)
      reject(new Error('Não foi possível processar a imagem selecionada.'))
    }
    image.src = imageUrl
  })
}

function gerarBlobComQualidade(canvas, quality) {
  return new Promise((resolve, reject) => {
    canvas.toBlob((blob) => {
      if (!blob) {
        reject(new Error('Não foi possível otimizar a imagem selecionada.'))
        return
      }

      resolve(blob)
    }, 'image/jpeg', quality)
  })
}

async function otimizarFoto(file) {
  if (!file.type.startsWith('image/')) {
    return file
  }

  const image = await carregarImagem(file)
  const scale = Math.min(1, FOTO_MAX_DIMENSAO / Math.max(image.width, image.height))
  const width = Math.max(1, Math.round(image.width * scale))
  const height = Math.max(1, Math.round(image.height * scale))
  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height

  const context = canvas.getContext('2d')
  if (!context) {
    return file
  }

  context.drawImage(image, 0, 0, width, height)

  let quality = FOTO_QUALIDADE_INICIAL
  let optimizedBlob = await gerarBlobComQualidade(canvas, quality)

  while (optimizedBlob.size > FOTO_TAMANHO_ALVO_BYTES && quality > FOTO_QUALIDADE_MINIMA) {
    quality = Math.max(FOTO_QUALIDADE_MINIMA, Number((quality - 0.08).toFixed(2)))
    optimizedBlob = await gerarBlobComQualidade(canvas, quality)
  }

  return optimizedBlob.size < file.size
    ? new File([optimizedBlob], `${file.name.replace(/\.[^.]+$/, '') || 'foto'}.jpg`, { type: 'image/jpeg' })
    : file
}

function getDraftKey() {
  return `rascunho_operador_${authStore.usuario?.id || 'anon'}_efluente`
}

async function salvarRascunhoAgora() {
  if (isModoEdicao.value) return // Não salva rascunho no modo edição

  const fotosSerializadas = await Promise.all(
    fotos.value.map(async (foto) => ({
      preview: foto.preview,
      blob: foto.blob ? await foto.blob.arrayBuffer() : null,
      type: foto.type || foto.blob?.type || 'image/jpeg',
      descricao: foto.descricao || ''
    }))
  )

  await db.cache.put({
    chave: getDraftKey(),
    dados: {
      etapaAtual: etapaAtual.value,
      form: JSON.parse(JSON.stringify(form.value)),
      fotos: fotosSerializadas,
      atualizadoEm: new Date().toISOString()
    },
    atualizadoEm: new Date().toISOString()
  })

  statusRascunho.value = 'Rascunho salvo automaticamente'
}

function agendarSalvamentoRascunho() {
  if (isModoEdicao.value) return
  statusRascunho.value = 'Salvando rascunho...'
  clearTimeout(draftTimer)
  draftTimer = setTimeout(() => {
    salvarRascunhoAgora()
  }, 500)
}

async function restaurarRascunho() {
  if (isModoEdicao.value) return

  const draft = await db.cache.get(getDraftKey())
  if (!draft?.dados) {
    statusRascunho.value = 'Novo formulário pronto'
    return
  }

  form.value = { ...createDefaultForm(), ...draft.dados.form }
  etapaAtual.value = draft.dados.etapaAtual ?? 0
  fotos.value = (draft.dados.fotos || []).map((foto) => ({
    preview: foto.preview,
    blob: foto.blob ? new Blob([foto.blob], { type: foto.type || 'image/jpeg' }) : null,
    type: foto.type || 'image/jpeg',
    descricao: foto.descricao || ''
  }))
  statusRascunho.value = 'Rascunho restaurado'
}

async function carregarFormularioParaEdicao() {
  try {
    statusRascunho.value = 'Carregando dados...'
    const pk = pkEdicao.value
    console.log('[DEBUG] Carregando formulário para edição, pk:', pk)
    console.log('[DEBUG] Token:', axios.defaults.headers.common['Authorization'])
    const res = await axios.get(`/api/formularios/${encodeURIComponent(pk)}`)
    console.log('[DEBUG] Resposta GET formulário:', res.status, res.data)
    const dados = res.data
    
    // Preenche o form com os dados da API
    form.value = { ...createDefaultForm(), ...dados }
    
    // Ajusta campos duplicados/legados
    if (dados.codigoGuiaRemessa && !form.value.guiaRemocao) {
      form.value.guiaRemocao = dados.codigoGuiaRemessa
    }
    
    // Ajusta o localTipo baseado nos dados
    if (dados.estacao) {
      form.value.localTipo = 'estacao'
    } else if (dados.via || dados.trechoSentido || dados.kmPoste) {
      form.value.localTipo = 'entre_estacoes'
    }

    // Carregar fotos do backend
    try {
      const resFotos = await axios.get(`/api/formularios/${encodeURIComponent(pkEdicao.value)}/fotos`)
      if (resFotos.data && resFotos.data.length > 0) {
        fotos.value = await Promise.all(resFotos.data.map(async (f, index) => {
          // Converte base64 para Blob para poder reenviar se necessário
          const resBlob = await fetch(f.base64)
          const blob = await resBlob.blob()
          return {
            preview: f.base64,
            blob: blob,
            type: f.tipo,
            descricao: f.nome ? f.nome.replace(/\.[^/.]+$/, "") : `Foto ${index + 1}`
          }
        }))
      }
    } catch (eFotos) {
      console.warn('Não foi possível carregar as fotos:', eFotos)
    }
    
    statusRascunho.value = 'Pronto para edição'
  } catch (e) {
    abrirFeedbackModal({
      tipo: 'erro',
      titulo: 'Erro ao carregar',
      mensagem: 'Não foi possível carregar os dados do formulário para edição.',
      acaoLabel: 'Voltar',
      redirecionar: true
    })
  }
}

function voltarParaInicio() {
  if (authStore.usuario?.perfil === 'supervisor') {
    router.push('/supervisor')
  } else {
    router.push('/operador')
  }
}

async function carregarReferencias() {
  if (await checkConnectivity()) {
    await carregarCacheReferencias()
  }

  let [linhasCache, estacoesCache, formularioOperador] = await Promise.all([
    obterCacheReferencia('linhas'),
    obterCacheReferencia('estacoes'),
    obterCacheReferencia('formularioOperador')
  ])

  if (linhasCache.length) {
    linhasCptm.value = mapReferenciaParaSearchSelect(linhasCache)
  }

  if (estacoesCache.length) {
    const linhaPorId = new Map((linhasCache || []).map((item) => [item.Id ?? item.id, item.Nome ?? item.nome ?? item.texto ?? item.valor]))
    estacoes.value = estacoesCache.map((item) => ({
      valor: item.Nome ?? item.nome ?? item.texto ?? item.valor ?? '',
      texto: item.Nome ?? item.nome ?? item.texto ?? item.valor ?? '',
      linha: item.linha ?? (linhaPorId.get(item.LinhaId ?? item.linhaId) || '')
    })).filter((item) => item.valor && item.texto)
  }

  if (formularioOperador && Object.keys(formularioOperador).length) {
    opcoesSiglaMeioAmbiente.value = formularioOperador.SiglasMeioAmbiente || opcoesSiglaMeioAmbiente.value
    opcoesStatusDesvioAmbiental.value = formularioOperador.StatusDesvioAmbiental || opcoesStatusDesvioAmbiental.value
    opcoesTipoAtividade.value = formularioOperador.TiposAtividade || opcoesTipoAtividade.value
    opcoesTipoDra.value = formularioOperador.TiposDra || opcoesTipoDra.value
    opcoesTipoAtividadeCptm.value = formularioOperador.TiposAtividadeCptm || opcoesTipoAtividadeCptm.value
    opcoesNomeEdificacao.value = formularioOperador.NomesEdificacao || opcoesNomeEdificacao.value
    opcoesOrigemEfluente.value = formularioOperador.OrigensEfluente || opcoesOrigemEfluente.value
    opcoesComplementoLocal.value = formularioOperador.ComplementosLocal || opcoesComplementoLocal.value
    opcoesDestinacao.value = formularioOperador.Destinacoes || opcoesDestinacao.value
    opcoesTipoVeiculo.value = formularioOperador.TiposVeiculo || opcoesTipoVeiculo.value

    if (formularioOperador.Municipios?.length) {
      municipios.value = formularioOperador.Municipios.map((nome) => ({ valor: nome, texto: nome }))
    }

    if (!form.value.siglaMeioAmbiente && opcoesSiglaMeioAmbiente.value.length) {
      form.value.siglaMeioAmbiente = opcoesSiglaMeioAmbiente.value.includes('GEA.DEAE') ? 'GEA.DEAE' : opcoesSiglaMeioAmbiente.value[0]
    }

    if (!form.value.statusDesvioAmbiental && opcoesStatusDesvioAmbiental.value.length) {
      form.value.statusDesvioAmbiental = opcoesStatusDesvioAmbiental.value.includes('Não Regularizado') ? 'Não Regularizado' : opcoesStatusDesvioAmbiental.value[0]
    }
  }
}

const fotoSlots = computed(() => {
  const slots = []
  for (let i = 0; i < maxFotos.value; i++) {
    slots.push(fotos.value[i] || { preview: null, blob: null })
  }
  return slots
})

function abrirInputFoto(index) {
  fotoIndexAlvo = index
  inputFoto.value?.click()
}

async function onFotoSelecionada(event) {
  const file = event.target.files[0]
  if (!file) return

  const extPermitidas = ['image/jpeg', 'image/png', 'image/webp']
  if (!extPermitidas.includes(file.type)) {
    abrirFeedbackModal({
      tipo: 'erro',
      titulo: 'Formato de foto inválido',
      mensagem: 'Use apenas arquivos JPG, PNG ou WebP.'
    })
    event.target.value = ''
    return
  }

  if (file.size > 10 * 1024 * 1024) {
    abrirFeedbackModal({
      tipo: 'erro',
      titulo: 'Foto muito grande',
      mensagem: 'Cada foto deve ter no máximo 10 MB.'
    })
    event.target.value = ''
    return
  }

  try {
    const arquivoOtimizado = await otimizarFoto(file)
    const preview = await lerBlobComoDataUrl(arquivoOtimizado)
    const fotoObj = { preview, blob: arquivoOtimizado, type: arquivoOtimizado.type || file.type, descricao: `Foto ${fotoIndexAlvo + 1}` }
    if (fotoIndexAlvo < fotos.value.length) {
      fotos.value[fotoIndexAlvo] = fotoObj
    } else {
      fotos.value.push(fotoObj)
    }
    agendarSalvamentoRascunho()
  } catch (error) {
    abrirFeedbackModal({
      tipo: 'erro',
      titulo: 'Falha ao preparar a foto',
      mensagem: error?.message || 'Não foi possível preparar a foto para envio.'
    })
  } finally {
    event.target.value = '' // Reset input
  }
}

function removerFoto(index) {
  fotos.value.splice(index, 1)
  agendarSalvamentoRascunho()
}

// ====== Finalizar ======
const podeEnviar = computed(() => fotos.value.length >= minFotos.value)

async function salvarFormularioLocalmente(criadoEm) {
  // Pre-serialize photos to ArrayBuffer outside of the Dexie transaction
  let fotosSerializadas = []
  try {
    fotosSerializadas = await Promise.all(fotos.value.map(async (foto, index) => {
      const blobData = foto.blob instanceof Blob ? await foto.blob.arrayBuffer() : foto.blob
      return {
        formularioId: 0,
        blob: blobData,
        descricao: foto.descricao,
        criadoEm,
        tipoArquivo: foto.type || foto.blob?.type || 'image/jpeg',
        nomeArquivo: `foto_${index + 1}.${(foto.type || foto.blob?.type || 'image/jpeg').split('/')[1] || 'jpg'}`
      }
    }))
  } catch (err) {
    console.error('Erro ao serializar fotos antes do save local:', err)
    // Persist a debug entry so user possa compartilhar ao reproduzir
    try { await db.cache.put({ chave: `save_error_serialization_${Date.now()}`, dados: { message: err?.message || String(err), fotosCount: fotos.value.length, criadoEm: new Date().toISOString() } }) } catch (_) {}
    throw err
  }

  if (fotosSerializadas.length < minFotos.value) {
    throw new Error(`São necessárias pelo menos ${minFotos.value} fotos antes de salvar o formulário localmente.`)
  }

  const parseNumber = (valor) => {
    if (valor === null || valor === undefined || valor === '') return null
    const normalized = String(valor).trim().replace(',', '.')
    const parsed = Number(normalized)
    return Number.isFinite(parsed) ? parsed : null
  }

  try {
    const fotosValidas = fotosSerializadas.filter((foto) => foto && foto.blob)
    if (fotosValidas.length < minFotos.value) {
      throw new Error(`Não foi possível salvar: existem fotos inválidas. Confira as ${minFotos.value} fotos obrigatórias e tente novamente.`)
    }

    const formularioId = await db.transaction('rw', db.formularios, db.fotos, async () => {
      const id = await db.formularios.add({
        tipo: form.value.natureza,
        campos: {
          ...form.value,
          latitude: parseNumber(form.value.latitude),
          longitude: parseNumber(form.value.longitude)
        },
        status: 'completo',
        operadorId: authStore.usuario?.id || 0,
        criadoEm,
        ultimaTentativaEm: null,
        erroMensagem: null
      })

      if (fotosValidas.length > 0) {
        await db.fotos.bulkAdd(fotosValidas.map((foto) => ({
          ...foto,
          formularioId: id
        })))
      }

      return id
    })

    return formularioId
  } catch (err) {
    console.error('Erro ao salvar formulário localmente:', err)
    // Save a debug record to cache for later inspection
    try {
      await db.cache.put({ chave: `save_error_tx_${Date.now()}`, dados: { message: err?.message || String(err), stack: err?.stack || null, fotosCount: fotosSerializadas.length, criadoEm: new Date().toISOString() } })
    } catch (e) {
      console.error('Falha ao gravar log de erro no cache:', e)
    }
    throw err
  }
}

async function finalizarFormulario() {
  if (enviandoFormulario.value) {
    return
  }

  for (let etapa = 0; etapa < steps.length - 1; etapa += 1) {
    if (!validarEtapa(etapa)) {
      etapaAtual.value = etapa
      scrollToTop()
      return
    }
  }

  if (!podeEnviar.value) {
    abrirFeedbackModal({
      tipo: 'erro',
      titulo: 'Fotos insuficientes',
      mensagem: `Inclua pelo menos ${minFotos.value} fotos antes de enviar.`
    })
    return
  }

  enviandoFormulario.value = true

  try {
    if (isModoEdicao.value) {
      // Fluxo de Edição (Supervisor) - Envia direto para a API
      const formData = new FormData()
      form.value.chavePrimaria = pkEdicao.value // Garante que a PK está correta
      
      // Limpar e converter campos numéricos para evitar erro de conversão no backend
      const payload = { ...form.value }
      
      const parseNumber = (val) => {
        if (val === null || val === undefined || val === '') return null
        const num = Number(String(val).replace(',', '.'))
        return Number.isNaN(num) ? null : num
      }

      payload.latitude = parseNumber(payload.latitude)
      payload.longitude = parseNumber(payload.longitude)
      payload.quantidadeLitros = parseNumber(payload.quantidadeLitros)
      payload.distanciaVia = parseNumber(payload.distanciaVia)
      
      formData.append('dados', JSON.stringify(payload))
      
      for (const foto of fotos.value) {
        if (foto.blob) {
          formData.append('fotos', foto.blob, foto.descricao + '.jpg')
        }
      }

      await axios.put(`/api/formularios/${encodeURIComponent(pkEdicao.value)}`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })

      abrirFeedbackModal({
        tipo: 'sucesso',
        titulo: 'Alterações salvas',
        mensagem: 'O formulário foi atualizado com sucesso.',
        acaoLabel: 'Voltar ao painel',
        redirecionar: true
      })
    } else {
      // Fluxo de Criação (Operador) - Salva no IndexedDB e tenta sincronizar
      const criadoEm = new Date().toISOString()
      const formularioId = await salvarFormularioLocalmente(criadoEm)
      await db.cache.delete(getDraftKey())

      let online = false
      try {
        online = await checkConnectivity()
      } catch (checkError) {
        console.warn('Falha ao verificar conectividade, salvando localmente:', checkError)
        online = false
      }

      if (online) {
        const resultado = await sincronizarPendentes({ ids: [formularioId] })

        if (resultado.enviados > 0 && resultado.erros === 0) {
          abrirFeedbackModal({
            tipo: 'sucesso',
            titulo: 'Formulário enviado',
            mensagem: 'O registro foi salvo no banco de dados com sucesso.',
            acaoLabel: 'Voltar ao início',
            redirecionar: true
          })
          return
        }

        const mensagemErro = resultado.resultados?.find(r => r.status === 'erro')?.mensagem
        abrirFeedbackModal({
          tipo: 'erro',
          titulo: 'Falha no envio ao banco',
          mensagem: mensagemErro || 'O formulário ficou salvo localmente. Revise os dados/fotos e tente reenviar na tela inicial em "Sincronizar agora".',
          acaoLabel: 'Entendi'
        })
        return
      }

      abrirFeedbackModal({
        tipo: 'offline',
        titulo: 'Formulário salvo no dispositivo',
        mensagem: 'Sem conexão no momento. O registro será enviado assim que a internet voltar.',
        acaoLabel: 'Voltar ao início',
        redirecionar: true
      })
    }
  } catch (e) {
    abrirFeedbackModal({
      tipo: 'erro',
      titulo: 'Erro ao salvar',
      mensagem: e.response?.data?.mensagem || e.message || 'Ocorreu um erro inesperado.',
      acaoLabel: 'Entendi'
    })
  } finally {
    enviandoFormulario.value = false
  }
}

watch(form, () => {
  agendarSalvamentoRascunho()
}, { deep: true })

watch(() => form.value.localTipo, (localTipoAtual) => {
  if (localTipoAtual === 'estacao') {
    form.value.via = ''
    form.value.trechoSentido = ''
    form.value.kmPoste = ''
    return
  }

  if (localTipoAtual === 'entre_estacoes') {
    form.value.estacao = ''
    return
  }

  form.value.estacao = ''
  form.value.linhaCptm = ''
  form.value.via = ''
  form.value.trechoSentido = ''
  form.value.kmPoste = ''
})

watch(() => form.value.tipoAtividade, (tipoAtividadeAtual) => {
  if (tipoAtividadeAtual) {
    form.value.atividadeNaoListada = ''
  }
})

watch(() => form.value.atividadeNaoListada, (atividadeNaoListadaAtual) => {
  if (atividadeNaoListadaAtual?.trim()) {
    form.value.tipoAtividade = ''
  }
})

watch(() => form.value.tipoDra, (tipoDraAtual) => {
  if (!tipoDraAtual) {
    form.value.codigoDra = ''
    form.value.dataValidadeDra = ''
  }
})

watch(() => form.value.nomeEdificacao, (nomeEdificacaoAtual) => {
  if (!nomeEdificacaoAtual) {
    form.value.edificacaoComplemento = ''
  }
})

watch(() => form.value.veiculoTipo, (veiculoTipoAtual) => {
  if (!veiculoTipoAtual) {
    form.value.veiculoPlaca = ''
  }
})

watch(() => form.value.linhaCptm, (linhaAtual) => {
  if (!linhaAtual || !form.value.estacao) {
    return
  }

  const estacaoAtual = encontrarEstacaoSelecionada(form.value.estacao, linhaAtual)
  if (!estacaoAtual) {
    form.value.estacao = ''
  }
})

watch(() => form.value.estacao, (estacaoAtual) => {
  if (!estacaoAtual) {
    return
  }

  const estacaoSelecionada = encontrarEstacaoSelecionada(estacaoAtual, form.value.linhaCptm)
    || encontrarEstacaoSelecionada(estacaoAtual)

  if (estacaoSelecionada?.linha && form.value.linhaCptm !== estacaoSelecionada.linha) {
    form.value.linhaCptm = estacaoSelecionada.linha
  }
})

watch(etapaAtual, () => {
  agendarSalvamentoRascunho()
})

onMounted(async () => {
  await checkConnectivity()
  
  // Redirect non-supervisors away from edit mode (secondary protection)
  const isSupervisor = authStore.usuario?.perfil === 'supervisor'
  if (route.query.editar && !isSupervisor) {
    console.warn('Acesso à edição negado para operador. Redirecionando...')
    await router.replace({ path: '/operador/formulario' })
    return
  }
  
  if (route.query.novo) {
    await db.cache.delete(getDraftKey())
    etapaAtual.value = 0
    fotos.value = []
    form.value = createDefaultForm()
  }
  await carregarReferencias()
  
  if (isModoEdicao.value) {
    await carregarFormularioParaEdicao()
  } else {
    await restaurarRascunho()
  }
  
  scrollToTop()
})

onBeforeUnmount(() => {
  clearTimeout(draftTimer)
  if (!isModoEdicao.value) {
    salvarRascunhoAgora()
  }
})
</script>

<style scoped>
.operator-form-page {
  background:
    linear-gradient(180deg, color-mix(in srgb, var(--cptm-bg) 82%, #ffffff 18%) 0%, var(--cptm-bg) 100%);
}

.operator-form-content {
  position: relative;
  z-index: 1;
}

.operator-form-required-note {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  margin-bottom: 1rem;
  padding: 0.8rem 1rem;
  border-radius: 0.9rem;
  border: 1px solid var(--cptm-border);
  background: color-mix(in srgb, var(--cptm-surface) 82%, #fff 18%);
  color: var(--cptm-text-muted);
  font-size: 0.78rem;
  font-weight: 600;
}

.operator-form-required-note__mark {
  color: #dc2626;
  font-size: 0.92rem;
  font-weight: 800;
}

.operator-form-draft-banner {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 0.95rem 1rem;
  border-radius: 1rem;
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface);
}

.operator-form-draft-title {
  margin: 0;
  font-size: 0.86rem;
  font-weight: 800;
  color: var(--cptm-text);
}

.operator-form-draft-text {
  margin: 0.25rem 0 0;
  font-size: 0.76rem;
  color: var(--cptm-text-muted);
}

.operator-form-draft-status {
  white-space: nowrap;
  font-size: 0.76rem;
  font-weight: 700;
  color: var(--cptm-primary);
}

.operator-form-step {
  border: 1px solid var(--cptm-border);
  border-radius: 1.25rem;
  background: var(--cptm-surface);
  padding: 1.25rem;
  box-shadow: 0 18px 36px rgba(65, 54, 35, 0.08), 0 2px 6px rgba(0, 0, 0, 0.04);
}

.operator-form-step > .grid {
  grid-template-columns: minmax(0, 1fr);
}

.operator-form-cover-card {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 1rem;
  align-items: center;
  padding: 1rem;
  border: 1px solid color-mix(in srgb, var(--cptm-primary) 18%, var(--cptm-border) 82%);
  border-radius: 1rem;
  background:
    linear-gradient(135deg, color-mix(in srgb, var(--cptm-primary) 8%, var(--cptm-surface) 92%) 0%, var(--cptm-surface) 100%);
}

.operator-form-cover-logo {
  width: 4rem;
  height: 4rem;
  object-fit: contain;
  border-radius: 1rem;
  background: #fff;
  padding: 0.5rem;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.08);
}

.operator-form-cover-title {
  margin: 0;
  font-size: 0.94rem;
  font-weight: 800;
  color: var(--cptm-text);
}

.operator-form-cover-text {
  margin: 0.18rem 0 0;
  font-size: 0.78rem;
  color: var(--cptm-text-muted);
}

.operator-form-glossary-card {
  padding: 0.95rem 1rem;
  border-radius: 1rem;
  border: 1px dashed color-mix(in srgb, var(--cptm-primary) 30%, var(--cptm-border) 70%);
  background: color-mix(in srgb, var(--cptm-primary) 5%, var(--cptm-surface) 95%);
}

.operator-form-glossary-title {
  margin: 0;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: var(--cptm-primary);
}

.operator-form-glossary-text {
  margin: 0.35rem 0 0;
  font-size: 0.78rem;
  line-height: 1.5;
  color: var(--cptm-text-muted);
}

.operator-form-section-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin: 0 0 0.7rem;
  min-height: 1.9rem;
  font-size: 1rem;
  font-weight: 800;
  line-height: 1.3;
  color: var(--cptm-text);
}

.operator-form-section-title::before {
  content: '';
  width: 4px;
  height: 1.55rem;
  border-radius: 999px;
  background: var(--cptm-primary);
  flex: 0 0 auto;
}

.operator-form-subsection-title {
  margin: 0 0 0.45rem;
  font-size: 0.88rem;
  font-weight: 800;
  line-height: 1.35;
  color: var(--cptm-text);
}

.operator-form-footer {
  border-top: 1px solid color-mix(in srgb, var(--cptm-border) 82%, #ffffff 18%);
  backdrop-filter: blur(14px);
}

.operator-feedback-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1200;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: rgba(17, 17, 17, 0.48);
  backdrop-filter: blur(6px);
}

.operator-feedback-modal {
  width: min(100%, 28rem);
  padding: 1.4rem;
  border-radius: 1.25rem;
  border: 1px solid var(--cptm-border);
  background: var(--cptm-surface);
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.2);
}

.operator-feedback-icon {
  width: 3rem;
  height: 3rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  margin-bottom: 0.9rem;
}

.operator-feedback-icon--sucesso {
  color: #166534;
  background: rgba(34, 197, 94, 0.14);
}

.operator-feedback-icon--erro {
  color: #b91c1c;
  background: rgba(239, 68, 68, 0.14);
}

.operator-feedback-icon--offline {
  color: #92400e;
  background: rgba(245, 158, 11, 0.16);
}

.operator-feedback-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 800;
  color: var(--cptm-text);
}

.operator-feedback-message {
  margin: 0.65rem 0 1.2rem;
  font-size: 0.9rem;
  line-height: 1.55;
  color: var(--cptm-text-muted);
}

.operator-feedback-button {
  width: 100%;
  border: 0;
  border-radius: 0.9rem;
  padding: 0.9rem 1rem;
  font-size: 0.9rem;
  font-weight: 700;
  cursor: pointer;
  color: #fff;
  background: var(--cptm-primary);
}

.operator-feedback-button:hover {
  background: var(--cptm-primary-hover);
}

@media (max-width: 640px) {
  .operator-form-draft-banner {
    flex-direction: column;
    align-items: flex-start;
  }

  .operator-form-cover-card {
    grid-template-columns: 1fr;
  }

  .operator-form-cover-logo {
    width: 3.5rem;
    height: 3.5rem;
  }

  .operator-form-step {
    padding: 1rem;
    border-radius: 1rem;
  }

  .operator-form-step > .grid {
    gap: 0.85rem;
  }
}

@media (min-width: 640px) {
  .operator-form-step > .grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
