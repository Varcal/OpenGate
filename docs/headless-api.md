## Uso via API sem UI

Este guia descreve como usar o OpenGate sem interface gráfica, tanto para os endpoints OAuth/OIDC quanto para a Admin API.

### Resposta curta

Sim, o OpenGate pode ser usado via API sem a UI para os fluxos de protocolo OAuth 2.0/OpenID Connect.

Exemplos atuais no repositório:

- discovery document em `/.well-known/openid-configuration`
- token endpoint em `/connect/token`
- outros endpoints padrão em `/connect/authorize`, `/connect/logout` e `/connect/userinfo`

Referências:

- [API Reference](api-reference.md)
- [Quickstart 3 - Client Credentials (curl)](quickstarts/03-client-credentials.md)
- [Quickstart 4 - Admin API Headless](quickstarts/04-admin-api-headless.md)

### O que funciona sem UI

Os endpoints de protocolo podem ser consumidos diretamente por qualquer cliente HTTP.

Casos de uso típicos:

- `client_credentials` para comunicação serviço-serviço
- descoberta automática do servidor OIDC
- consumo por Postman, `curl`, SDKs OAuth/OIDC e aplicações backend

Exemplo com `curl`:

```bash
curl -s -X POST http://localhost:5148/connect/token \
  -H "content-type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=machine-demo&client_secret=machine-demo-secret-change-in-prod&scope=api"
```

O retorno esperado contém `access_token`, `token_type` e `expires_in`.

### Admin API sem UI

O projeto também expõe uma Admin API REST em `/admin/api`, com endpoints para:

- `clients`
- `scopes`
- `users`
- `sessions`
- `audit-logs`
- import/export de configuração

Isso permite administrar o OpenGate por HTTP em vez de usar a Admin UI.

### Autenticação da Admin API

O OpenGate agora suporta dois modos de autenticação para a superfície administrativa:

- sessão web/cookie para uso humano via `/admin`
- bearer token para automação headless via `/admin/api`

Importante: isso não significa que existam duas identidades ou dois "logins" distintos. A diferença é apenas o mecanismo de apresentação das credenciais.

- interface web: autenticação por cookie
- automação: autenticação por bearer token

Nos dois casos, o modelo de autorização continua baseado nas mesmas roles e políticas administrativas.

### Comportamento sem credenciais

Como a Admin API é uma superfície REST, chamadas anônimas para `/admin/api/*` retornam `401 Unauthorized`.

Já as páginas da Admin UI continuam usando o comportamento web normal, com redirect para login quando necessário.

### Scopes administrativos

Para acesso headless, o OpenGate define os seguintes scopes:

- `admin_api`: leitura administrativa
- `admin_api.write`: escrita administrativa

Na prática:

- tokens com `admin_api` podem consultar endpoints administrativos
- tokens com `admin_api.write` podem executar operações de escrita
- operações de escrita também aceitam `admin_api.write` como superset do acesso de leitura

### Client headless de exemplo

No sample principal (`samples/OpenGate.Sample.Basic`), o seeding cria um client confidencial para automação administrativa:

- `client_id`: `admin-cli`
- `client_secret`: `admin-cli-secret-change-in-prod`

Esse client pode solicitar tokens com `client_credentials` para consumir a Admin API sem UI.

As operações administrativas headless também são auditadas com contexto adicional de autenticação e requisição, facilitando distinguir chamadas via cookie e chamadas via bearer token.

### Exemplo: obter token de leitura administrativa

```bash
curl -s -X POST http://localhost:5148/connect/token \
  -H "content-type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=admin-cli&client_secret=admin-cli-secret-change-in-prod&scope=admin_api"
```

### Exemplo: listar clients via Admin API

```bash
curl -s http://localhost:5148/admin/api/clients \
  -H "Authorization: Bearer <access_token>"
```

### Exemplo: obter token de escrita administrativa

```bash
curl -s -X POST http://localhost:5148/connect/token \
  -H "content-type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=admin-cli&client_secret=admin-cli-secret-change-in-prod&scope=admin_api.write"
```

### Exemplo: criar scope via Admin API

```bash
curl -s -X POST http://localhost:5148/admin/api/scopes \
  -H "Authorization: Bearer <access_token>" \
  -H "content-type: application/json" \
  -d "{\"name\":\"orders-api\",\"displayName\":\"Orders API\",\"description\":\"Orders scope\",\"resources\":[\"resource_server\",\"orders_api\"]}"
```

### Quando usar UI e quando usar API

Use a UI quando quiser:

- login interativo
- consentimento
- operação manual por administradores humanos

Use a API quando quiser:

- automação operacional
- scripts de provisionamento
- integrações CI/CD
- administração remota sem browser

### Resumo prático

Hoje o OpenGate suporta:

1. uso sem UI para os endpoints OAuth/OIDC
2. uso sem UI para a Admin API com bearer token e scopes administrativos
3. uso web tradicional para administradores humanos via cookie e Admin UI
