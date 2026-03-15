## Quickstart 4 - Admin API Headless

Este quickstart mostra como administrar o OpenGate sem UI usando `client_credentials` e bearer token.

### Pré-requisitos

- rode `samples/OpenGate.Sample.Basic`
- use o seed padrão do sample
- opcionalmente, para simular backend-only, configure `OpenGate:UiMode` como `None`

O sample cria um client administrativo de automação:

- `client_id`: `admin-cli`
- `client_secret`: `admin-cli-secret-change-in-prod`

### 1. Solicitar token de leitura

```bash
curl -s -X POST http://localhost:5148/connect/token \
  -H "content-type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=admin-cli&client_secret=admin-cli-secret-change-in-prod&scope=admin_api"
```

O retorno deve conter `access_token`.

### 2. Consultar a Admin API

```bash
curl -s http://localhost:5148/admin/api/clients \
  -H "Authorization: Bearer <access_token>"
```

### 3. Solicitar token de escrita

```bash
curl -s -X POST http://localhost:5148/connect/token \
  -H "content-type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=admin-cli&client_secret=admin-cli-secret-change-in-prod&scope=admin_api.write"
```

### 4. Criar um scope via Admin API

```bash
curl -s -X POST http://localhost:5148/admin/api/scopes \
  -H "Authorization: Bearer <access_token>" \
  -H "content-type: application/json" \
  -d "{\"name\":\"billing-api\",\"displayName\":\"Billing API\",\"description\":\"Billing scope\",\"resources\":[\"resource_server\",\"billing_api\"]}"
```

### 5. Confirmar no audit log

As operações administrativas headless geram `AuditLog` com:

- `ClientId`
- `IpAddress`
- `UserAgent`
- `Details.actor.authenticationMode`
- `Details.actor.scopes`
- `Details.request.method`
- `Details.request.path`

Isso permite distinguir chamadas humanas por cookie de automações por bearer token.
