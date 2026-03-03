## Quickstart 1 — Criar e rodar com `dotnet new opengate-server`

### Pré-requisitos

- .NET SDK conforme `global.json` (atualmente: `10.0.103`)
- SQL Server (local) **ou** Docker (para subir um SQL Server)

### 1) Instalar o template localmente (sem afetar seu usuário)

No root do repo:

- `dotnet new install templates/opengate-server --debug:custom-hive artifacts/template-hive`

### 2) Gerar um servidor

Exemplo (gera em `samples/MinhaEmpresa.Identity`):

- `dotnet new opengate-server -n MinhaEmpresa.Identity -o samples/MinhaEmpresa.Identity --debug:custom-hive artifacts/template-hive`

Se você gerar fora de `samples/`, ajuste o caminho para `src/`:

- `--opengateSrcPath ../../src`

### 3) Subir um SQL Server via Docker (opcional)

Se você não tiver SQL Server local:

- `docker run --name opengate-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=OpenGate_Dev!23 -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`

### 4) Rodar

Na pasta do projeto gerado:

- `dotnet run`

Abra:

- Login: `http://localhost:5148/Account/Login`
- Discovery: `http://localhost:5148/.well-known/openid-configuration`
- Health: `http://localhost:5148/health`

### Credenciais de demo (se `--seed true`)

- Usuário: `demo@opengate.test`
- Senha: `Demo@1234!abcd`
