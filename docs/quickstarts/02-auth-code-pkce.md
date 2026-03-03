## Quickstart 2 — Authorization Code + PKCE (Postman)

Este quickstart assume que você rodou o servidor com `--seed true`.

### Dados

- Authorization endpoint: `http://localhost:5148/connect/authorize`
- Token endpoint: `http://localhost:5148/connect/token`
- Client ID: `interactive-demo`
- Redirect URI (Postman): `https://oauth.pstmn.io/v1/callback`

### Passo a passo (Postman)

1. Crie uma requisição qualquer e abra a aba **Authorization**
2. Tipo: **OAuth 2.0**
3. Configure:
   - Grant Type: **Authorization Code (with PKCE)**
   - Auth URL: `http://localhost:5148/connect/authorize`
   - Access Token URL: `http://localhost:5148/connect/token`
   - Client ID: `interactive-demo`
   - Scope: `openid profile email`
   - Callback URL: `https://oauth.pstmn.io/v1/callback`
4. Clique **Get New Access Token**
5. Faça login com:
   - `demo@opengate.test` / `Demo@1234!abcd`
6. Na tela de consent, aceite e finalize

Se tudo der certo, o Postman vai receber um `access_token`.
