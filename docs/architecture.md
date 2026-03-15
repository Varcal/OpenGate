## Architecture Overview

### Componentes

- **OpenGate.Server** (`src/OpenGate.Server`)
  - registra e configura o OpenIddict com defaults e presets de segurança
  - expõe `OpenGateOptions.UiMode` para operar com UI built-in, UI externa ou sem UI interativa

- **OpenGate.Data.EFCore** (`src/OpenGate.Data.EFCore`)
  - `OpenGateDbContext` herda de `IdentityDbContext<OpenGateUser>`
  - entidades extras: `UserProfile`, `AuditLog`, `UserSession`
  - migrations SQL Server em `Migrations/sqlserver`

- **OpenGate.Data.EFCore.Migrations.PostgreSql** (`src/OpenGate.Data.EFCore.Migrations.PostgreSql`)
  - migrations versionadas para PostgreSQL

- **OpenGate.Data.EFCore.Migrations.Sqlite** (`src/OpenGate.Data.EFCore.Migrations.Sqlite`)
  - migrations versionadas para SQLite

- **OpenGate.UI** (`src/OpenGate.UI`)
  - pacote opcional com a UI oficial built-in
  - Razor Pages para:
    - Login (`/Account/Login`)
    - Register (`/Account/Register`)
    - Consent/Authorize (`/connect/authorize`)
    - Logout (`/connect/logout`)
    - Admin UI (`/Admin`)
  - static web assets + Tailwind CSS

- **OpenGate.Admin.Api** (`src/OpenGate.Admin.Api`)
  - superfície REST para administração via `/admin/api`
  - suporta automação headless e frontends administrativos externos

### Modos de UI

- **BuiltIn**
  - o host registra Razor Pages e usa a UI oficial de `OpenGate.UI`

- **External**
  - o host fornece sua própria UI para login/acesso negado
  - uma Admin UI custom pode autenticar via OIDC e consumir `/admin/api`

- **None**
  - o servidor sobe sem UI interativa
  - adequado para cenários API-only, automação e integrações backend

### Fluxo (alto nível)

1. Cliente chama `/connect/authorize`
2. OpenIddict (com passthrough habilitado) delega para a UI configurada pelo host
3. Se o usuário não estiver autenticado, o app desafia o cookie do Identity e redireciona para `LoginPath`
4. A UI escolhida conclui o login e cria a sessão web
5. Consent aceito: a UI retorna um principal e o OpenIddict emite code/token conforme o flow

### Administração

- A UI oficial built-in pode administrar o sistema por páginas Razor em `/Admin`
- Uma UI própria também pode administrar o sistema consumindo `OpenGate.Admin.Api`
- Para automação, a mesma superfície `/admin/api` aceita bearer tokens com scopes administrativos

### Banco e migrations

- `UseSqlServer(...)` aplica migrations do assembly base (`OpenGate.Data.EFCore`).
- `UsePostgreSql(...)` usa o assembly `OpenGate.Data.EFCore.Migrations.PostgreSql`.
- `UseSqlite(...)` usa o assembly `OpenGate.Data.EFCore.Migrations.Sqlite`.
