# CPTM Efluentes 🌊

Sistema de monitoramento ambiental para captação de dados de efluentes em campo, com operação **offline-first**, sincronização com backend e gestão por perfis (operador/supervisor).

---

## 1) Visão geral do projeto

O projeto foi desenvolvido para permitir:

- Coleta de formulários de efluentes em campo (inclusive sem internet);
- Armazenamento local temporário no dispositivo;
- Sincronização posterior com backend e banco Oracle;
- Visualização, edição e gestão por supervisão;
- Controle de usuários, perfis e bloqueio de acesso.

Arquitetura geral:

- **Frontend:** Vue 3 + Vite + Pinia + Dexie (IndexedDB) + Axios
- **Backend:** ASP.NET Core (.NET 10, Minimal API) + JWT + Oracle ManagedDataAccess
- **Banco:** Oracle (scripts SQL versionados em `backend/Cptm.Api/Scripts`)

---

## 2) Tecnologias e ferramentas utilizadas

### Frontend
- Vue 3
- Vite
- Pinia
- Vue Router
- Axios
- Dexie.js (IndexedDB)
- Vite PWA (service worker)
- Lucide Vue
- CSS customizado (tema claro/escuro)

### Backend
- ASP.NET Core (.NET 10)
- Minimal APIs
- JWT Bearer Authentication
- Swagger / OpenAPI
- Oracle.ManagedDataAccess

### Banco de dados
- Oracle Database
- Scripts SQL para criação, ajuste, seed e manutenção

---

## 3) Estrutura principal de pastas

```text
CPTM-Efluente-main/
├── backend/
│   └── Cptm.Api/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── DTOs/
│       ├── Models/
│       ├── Services/
│       │   └── JwtService.cs
│       └── Scripts/
├── frontend/
│   ├── src/
│   │   ├── views/
│   │   ├── services/
│   │   ├── stores/
│   │   └── database.js
│   ├── package.json
│   └── vite.config.js
├── pj_cptm.sln
└── README.md
```

---

## 4) Funcionalidades implementadas

### Operador
- Login e sessão local;
- Preenchimento de formulário de efluente;
- Operação offline com persistência local;
- Sincronização de pendências;
- Resumo de status dos envios.

### Supervisor
- Painel com listagem de formulários;
- Edição/exclusão (soft delete) de formulários;
- Gestão de usuários:
  - cadastro;
  - alteração de perfil;
  - alteração de senha;
  - bloqueio de usuário.

### API / Segurança
- Autenticação JWT;
- Autorização por perfil;
- CORS configurado para frontend local;
- Endpoints documentados via Swagger.

---

## 5) O que foi feito recentemente (melhorias e correções)

### 5.1 Sincronização por PK (frontend)
Arquivos impactados:
- `frontend/src/services/syncService.js`
- `frontend/src/views/OperadorHomeView.vue`

Melhorias aplicadas:
- Consolidação da lógica de sincronização por **PK de negócio**;
- Agrupamento por PK para evitar contagem inflada de “sincronizados”;
- Ajuste do resumo para refletir estado real por chave única.

### 5.2 Prevenção de duplicidade de formulário por PK (backend + banco)
Arquivos impactados:
- `backend/Cptm.Api/Program.cs`
- `backend/Cptm.Api/Scripts/create_unique_pk_pt_efluente.sql`

Melhorias aplicadas:
- Validação no backend antes do insert para bloquear registro ativo com mesma PK;
- Script SQL de índice único lógico para reforço no banco Oracle:
  - garante unicidade para registros ativos;
  - protege contra duplicidade mesmo em cenários concorrentes.

### 5.3 Ajustes na edição de usuários (frontend)
Arquivo impactado:
- `frontend/src/views/UsuariosSupervisorView.vue`

Melhorias aplicadas:
- Correção de payload para endpoints de edição:
  - perfil: envio com `Perfil`;
  - senha: envio com `NovaSenha`.
- Revisão do fluxo de bloqueio:
  - validação de ID antes da requisição;
  - tratamento de erro mais explícito exibindo retorno do backend.

---

## 6) Endpoints principais da API

> Base local padrão: `http://localhost:5217`

### Autenticação
- `POST /api/auth/login`

### Formulários
- `POST /api/formularios`
- `GET /api/formularios`
- `GET /api/formularios/{pk}`
- `GET /api/formularios/{pk}/fotos`
- `PUT /api/formularios/{pk}`
- `DELETE /api/formularios/{pk}`

### Referências
- `GET /api/referencia/linhas`
- `GET /api/referencia/estacoes`
- `GET /api/referencia/naturezas`
- `GET /api/referencia/formulario-operador`

### Usuários
- `POST /api/usuarios`
- `GET /api/usuarios`
- `PUT /api/usuarios/{id}/perfil`
- `PUT /api/usuarios/{id}/senha`
- `DELETE /api/usuarios/{id}`

---

## 7) Scripts SQL disponíveis

Local: `backend/Cptm.Api/Scripts/`

- `create_tables.sql` — criação de tabelas base;
- `create_formularios_auditoria.sql` — estrutura de auditoria;
- `seed_users.sql` / `seed_supervisor.sql` — dados iniciais;
- `fix_usuarios_email_unique_active_only.sql` — ajuste de unicidade de email ativo;
- `add_uuid_publico_pt_efluente.sql` — suporte a UUID público;
- `create_unique_pk_pt_efluente.sql` — **unicidade lógica por PK ativa** (novo);
- `reset_oracle_test_storage.sql` — reset de ambiente de teste.

---

## 8) Como rodar localmente

## Backend
```bash
cd backend/Cptm.Api
dotnet restore
dotnet run
```

API: `http://localhost:5217`  
Swagger: `http://localhost:5217/swagger`

## Frontend
```bash
cd frontend
npm install
npm run dev
```

App: `http://localhost:5173`

---

## 9) Configuração

### Backend (`appsettings.Development.json`)
- `ConnectionStrings:OracleConnection`
- `Jwt:SecretKey`
- `Jwt:Issuer`
- `Jwt:ExpiracaoMinutos`

### Frontend
- `axios.defaults.baseURL` em `frontend/src/main.js`
- Proxy do Vite em `frontend/vite.config.js` (quando aplicável)

---

## 10) Status atual e pontos de atenção

- Fluxo principal operador/supervisor funcional;
- Correções recentes de sincronização e unicidade aplicadas;
- Revisão de bloqueio de usuário ajustada no frontend para melhor tratamento de falha;
- Recomenda-se executar teste de caminho crítico pós-deploy:
  - login supervisor;
  - alteração de perfil;
  - bloqueio de usuário;
  - criação de formulário com PK duplicada (esperado bloquear).

---

## 11) Equipe e manutenção

Projeto mantido por: **Davi, Helen, Lucas e Pedro**  
Última atualização deste documento: **Junho/2026**
