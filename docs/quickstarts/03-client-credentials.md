## Quickstart 3 — Client Credentials (curl)

Este quickstart assume que você rodou o servidor com `--seed true`.

### Solicitar token

- Endpoint: `http://localhost:5148/connect/token`
- Client:
  - `client_id`: `machine-demo`
  - `client_secret`: `machine-demo-secret-change-in-prod`

Exemplo:

- `curl -s -X POST http://localhost:5148/connect/token -H "content-type: application/x-www-form-urlencoded" -d "grant_type=client_credentials&client_id=machine-demo&client_secret=machine-demo-secret-change-in-prod&scope=api"`

O retorno deve conter `access_token`, `token_type` e `expires_in`.
