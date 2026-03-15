## Quickstart 5 - UI Administrativa Custom

Este quickstart mostra como uma UI própria pode autenticar um usuário administrativo via `authorization_code + PKCE` e consumir a Admin API por bearer token.

### Objetivo

Usar o OpenGate como backend de identidade e administração, sem depender da `OpenGate.UI` nem da Admin UI oficial.

### Pré-requisitos

- rode `samples/OpenGate.Sample.Basic`
- use o seed padrão do sample
- configure `OpenGate:UiMode` como `External` se quiser explicitar que a UI é externa

O sample cria um client público para frontend administrativo:

- `client_id`: `admin-dashboard`
- `redirect_uri`: `http://localhost/admin/callback`
- `post_logout_redirect_uri`: `http://localhost/admin`

Scopes disponíveis para esse client:

- `openid`
- `email`
- `profile`
- `roles`
- `admin_api`
- `admin_api.write`

### Contrato recomendado

Para uma UI administrativa própria:

- autentique o usuário com `authorization_code + PKCE`
- solicite `roles` para receber claims de role no token
- solicite `admin_api` para leitura
- solicite `admin_api.write` quando a UI precisar executar escrita

### 1. Redirecionar o usuário para autorização

Exemplo de URL:

```text
http://localhost:5148/connect/authorize?response_type=code&client_id=admin-dashboard&redirect_uri=http%3A%2F%2Flocalhost%2Fadmin%2Fcallback&scope=openid%20email%20profile%20roles%20admin_api&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256
```

Se a UI também precisar de escrita, inclua `admin_api.write` no `scope`.

### 2. Trocar o `code` por token

```bash
curl -s -X POST http://localhost:5148/connect/token \
  -H "content-type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code&client_id=admin-dashboard&redirect_uri=http://localhost/admin/callback&code=<authorization_code>&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
```

O access token retornado pode ser enviado para a Admin API.

### 3. Consumir a Admin API com bearer token

```bash
curl -s http://localhost:5148/admin/api/me \
  -H "Authorization: Bearer <access_token>"
```

Para um usuário admin, a resposta deve indicar `kind = "user"` e incluir as roles administrativas.

### 4. Regras de autorização

- usuário com roles administrativas pode acessar `/admin/api` via bearer token
- usuário sem role administrativa recebe `403 Forbidden`
- client de automação continua podendo usar `client_credentials`

### Observações importantes

- a UI própria não precisa compartilhar o cookie da Admin UI oficial
- a Admin API reutiliza a mesma identidade, roles e políticas administrativas
- a diferença entre operador humano e automação é o esquema de autenticação, não a base de usuários
- no modo `External`, o host deve fornecer sua própria experiência de login e acesso negado
