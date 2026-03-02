# PLANEJAMENTO DE PROJETO

## OpenGate Identity Server

**Produto Turnkey sobre OpenIddict • Instalar e Usar**

*A solução de identity que o ecossistema .NET precisa*

Documento Confidencial — Março 2026 | Versão Final

---

## Sumário

1. [Resumo Executivo](#1-resumo-executivo)
2. [O Problema que Resolvemos](#2-o-problema-que-resolvemos)
3. [O Produto: OpenGate Identity](#3-o-produto-opengate-identity)
4. [Roadmap](#4-roadmap)
5. [Cronograma Visual](#5-cronograma-visual)
6. [Equipe](#6-equipe)
7. [Orçamento](#7-orçamento)
8. [Posicionamento Competitivo](#8-posicionamento-competitivo)
9. [Modelo de Monetização](#9-modelo-de-monetização)
10. [Riscos](#10-riscos)
11. [Próximos Passos](#11-próximos-passos)

---

## 1. Resumo Executivo

O OpenGate Identity Server é um produto turnkey de identity management para .NET que usa o OpenIddict como motor de protocolos via dependência NuGet. O conceito é simples: o OpenIddict já resolve a parte difícil (OAuth 2.1, OpenID Connect, PKCE, DPoP, mTLS). O que falta no ecossistema é tudo ao redor: Admin UI, templates prontos, migration tools, observabilidade, documentação passo-a-passo e uma experiência de "instalar e usar em 5 minutos".

> **Analogia:** OpenIddict é o React. OpenGate é o Next.js. O OpenIddict é o ASP.NET Core. OpenGate é o ABP Framework. Nós não reinventamos o motor — nós construímos o carro completo.

Não há fork, não há cópia de código, não há obrigação de contribuir ao upstream. O OpenIddict é uma dependência do projeto, assim como o Entity Framework Core é dependência de qualquer app .NET que usa banco de dados. Simples assim.

| Item | Detalhe |
|------|---------|
| **Projeto** | OpenGate Identity Server |
| **O que é** | Produto turnkey para identity management, construído SOBRE o OpenIddict |
| **Relação c/ OpenIddict** | Dependência via NuGet. Sem fork, sem PRs, sem obrigações. |
| **Licença** | Apache 2.0 (código nosso) + OpenIddict Apache 2.0 (dependência) |
| **Plataforma** | .NET 8+ / ASP.NET Core 8+ |
| **Timeline** | 10–14 meses até v1.0 |
| **Equipe** | 4–5 desenvolvedores + 1 security |
| **Orçamento** | R$ 1M–1.6M (primeiros 14 meses) |

---

## 2. O Problema que Resolvemos

### 2.1 O Desenvolvedor .NET Hoje

Quando um desenvolvedor .NET precisa implementar identity management em 2026, ele enfrenta estas opções:

| Opção | Vantagem | Problema |
|-------|----------|----------|
| **Duende IS** | Completo, documentado, certificado OpenID | Pago (US$1.5K–35K/ano); licença restritiva |
| **Keycloak** | Gratuito, Admin UI completa, pronto pra usar | Java (não é .NET); pesado; difícil de customizar |
| **OpenIddict** | Gratuito, .NET nativo, excelente motor de protocolos | Sem Admin UI, sem templates prontos, bare metal demais |
| **Auth0/Okta** | SaaS fácil de usar | Vendor lock-in; caro em escala; sem self-hosting |
| **ASP.NET Identity** | Built-in, simples para apps básicas | Não é um identity server; sem OAuth/OIDC; sem SSO |

### 2.2 A Lacuna

Ninguém oferece simultaneamente: gratuito + .NET nativo + pronto pra usar + Admin UI + documentação exemplar. O OpenGate preenche exatamente essa lacuna, combinando o motor do OpenIddict com tudo que falta para ser um produto completo.

> O OpenIddict é uma biblioteca fantástica, mas o próprio README diz: *"it is not a turnkey solution but a framework that requires writing custom code to be operational"*. O OpenGate é a solução turnkey que o ecossistema .NET precisa.

---

## 3. O Produto: OpenGate Identity

### 3.1 Experiência do Desenvolvedor

O setup completo do OpenGate será feito em 5 minutos:

**Passo 1: Criar projeto (30 segundos)**

```bash
dotnet new opengate-server -n MinhaEmpresa.Identity
```

**Passo 2: Configurar banco (30 segundos)**

Editar `appsettings.json` com a connection string. PostgreSQL, SQL Server ou SQLite.

**Passo 3: Rodar (10 segundos)**

```bash
dotnet run
```

**Resultado imediato:**

- Identity server rodando em `https://localhost:5001`
- Login page moderna e responsiva funcionando
- Admin dashboard em `https://localhost:5001/admin`
- Endpoints OAuth 2.1/OIDC configurados (`.well-known/openid-configuration`)
- PKCE obrigatório, refresh token rotation, key auto-rotation — tudo seguro por padrão
- Swagger da Admin API em `https://localhost:5001/swagger`

### 3.2 Componentes do Produto

| Componente | O que entrega |
|------------|---------------|
| **OpenGate.Server** | Pacote principal: configura o OpenIddict com defaults seguros, middleware de rate limiting, health checks, OpenTelemetry, CORS. Uma linha: `builder.AddOpenGate()` |
| **OpenGate.UI** | Templates de Login, Consent, Logout, Registro, Erro. Razor Pages com design moderno, dark mode, i18n (pt-BR, en, es). Customizável via CSS e Razor override. |
| **OpenGate.Admin** | Dashboard Blazor WASM: gerenciar clientes, escopos, usuários, tokens, sessões. Gráficos de métricas em tempo real. RBAC (Admin, Viewer). |
| **OpenGate.Admin.Api** | REST API completa para automação: CRUD de tudo, import/export JSON, webhooks, bulk operations. Documentada com Swagger. |
| **OpenGate.Data.EFCore** | Stores estendidos para EF Core: user profiles, audit log, sessions, login history. Migrations automáticas. PostgreSQL, SQL Server, SQLite. |
| **OpenGate.Data.MongoDB** | Mesmos stores estendidos para MongoDB. |
| **OpenGate.Migration** | dotnet tool CLI: importar clientes, escopos e configuração do Duende IS e IdentityServer4. `dotnet opengate migrate` |
| **OpenGate.Templates** | 4 templates dotnet new: server (completo), api-only, spa-bff (React/Angular/Blazor), docker-compose. |
| **OpenGate.Passkeys** | WebAuthn / Passkeys como método de autenticação nativo no fluxo de login. |
| **OpenGate.Saml** | SAML 2.0 bridge: atuar como IdP ou SP para integração com legados (AD FS, SAP, Salesforce). |

### 3.3 O Que Fica com o OpenIddict (Dependência)

Tudo que é protocolo vem pronto do OpenIddict via NuGet. Nós não tocamos nisso:

| Protocolo / Feature | Status no OpenIddict 7.x |
|----------------------|--------------------------|
| OAuth 2.0 / 2.1 completo | ✅ Authorization Code, Client Credentials, Device Auth, Refresh Tokens |
| OpenID Connect 1.0 | ✅ Discovery, UserInfo, End-Session, Registration |
| PKCE, DPoP, PAR, mTLS | ✅ Todos suportados nativamente |
| Token Exchange (RFC 8693) | ✅ Adicionado na v7.0 |
| Token Introspection / Revocation | ✅ Completo |
| Client Assertions (private_key_jwt) | ✅ Client, Server e Validation |
| EF Core + MongoDB stores | ✅ Nativo com stores customizáveis |
| Native AOT / Trimming | ✅ Compatível desde v7.0 |
| Event pipeline extensível | ✅ Handlers customizáveis para qualquer endpoint |

---

## 4. Roadmap

### 4.1 Fase 1 — MVP (Meses 1–3)

**Objetivo:** `dotnet new opengate-server` funcional, com login UI e documentação inicial.

- Criar OpenGate.Server: configuração opinativa do OpenIddict com 1 linha (`builder.AddOpenGate()`)
- Presets de segurança: Development, Production, HighSecurity — ativados por environment
- UI de Login / Consent / Logout / Registro: Razor Pages modernas, responsivas, com dark mode
- Integração com ASP.NET Core Identity para user management
- EF Core stores estendidos (users, audit log, sessions) com migrations para PostgreSQL, SQL Server, SQLite
- Template `dotnet new opengate-server` com configuração guiada
- Docker image oficial (Alpine, <100MB) + docker-compose (app + PostgreSQL + Redis)
- Documentação: 3 quickstarts, API reference, architecture overview
- 5 samples: SPA (React), API protegida, Blazor WASM BFF, Console M2M, Device Flow
- CI/CD (GitHub Actions), Codecov > 80%, CodeQL

**Entrega:** v0.1 Alpha — NuGet pré-release + Docker image

### 4.2 Fase 2 — Admin & Tools (Meses 4–7)

**Objetivo:** Admin UI, Migration CLI e ferramentas que transformam o projeto em produto real.

- Admin REST API: CRUD de clientes, escopos, usuários, tokens. Import/export JSON. Swagger completo.
- Admin Dashboard (Blazor WASM): listagens com search/filter, forms, dashboard de métricas, audit log viewer
- RBAC na Admin: Super Admin, Admin, Viewer
- Migration CLI: `dotnet opengate migrate --source duende|is4 --connection-string ...`
- Social login templates pré-configurados: Google, Microsoft, GitHub, Apple, Facebook
- MFA com TOTP (Google Authenticator) integrado na UI
- OpenTelemetry: traces, metrics, logs. Prometheus endpoint. Health checks para Kubernetes
- Rate limiting inteligente por client_id, IP, usuário
- 15 tutoriais adicionais + guia de migração do Duende IS completo
- Docker Compose com tudo pré-configurado (app + PostgreSQL + Redis + Prometheus + Grafana)

**Entrega:** v0.5 Beta — NuGet público + Admin UI + Docker Compose

### 4.3 Fase 3 — Enterprise (Meses 8–11)

**Objetivo:** Features enterprise, auditoria de segurança e preparação para certificação.

- Multi-tenancy: isolamento por banco, schema ou coluna discriminadora
- SAML 2.0 bridge: atuar como IdP e SP para integração com legados
- Passkeys / WebAuthn nativo no fluxo de login
- Account Management UI: perfil, sessões ativas, dispositivos, histórico de login
- Caching distribuído com Redis para tokens, configuração e sessões
- Audit log completo e pesquisável (quem, o quê, quando, de onde)
- Auditoria de segurança independente + pen testing
- Helm Charts oficiais para Kubernetes (HPA, PDB, resource limits, liveness/readiness)
- Performance benchmarks públicos: OpenGate vs Duende IS vs Keycloak

**Entrega:** v0.9 RC — production-ready para early adopters

### 4.4 Fase 4 — GA (Meses 12–14)

**Objetivo:** Release 1.0 com certificação e ecossistema completo.

- Executar conformance tests do OpenID Foundation contra o OpenGate
- SCIM 2.0 provisioning
- Programa de bug bounty
- 30+ tutoriais cobrindo todos os cenários
- 20+ samples no repositório
- Security hardening guide
- Lançamento do OpenGate Cloud (SaaS gerenciado) em beta privado

**Entrega:** OpenGate Identity v1.0 GA

---

## 5. Cronograma Visual

| Workstream | M1–M3 | M4–M7 | M8–M11 | M12–M14 | Entrega |
|------------|-------|-------|--------|---------|---------|
| Core Setup + Presets | ████ | █ | | | v0.1 Alpha |
| Login / Consent UI | ███ | ██ | █ | | v0.5 Beta |
| Admin UI + API | | ████ | ██ | █ | v0.5 Beta |
| Migration CLI | | ███ | █ | | v0.5 Beta |
| SAML + Passkeys | | | ████ | ██ | v0.9 RC |
| Multi-tenancy | | | ███ | █ | v0.9 RC |
| Segurança / Audit | █ | ██ | ████ | ██ | Auditado |
| Documentação + Samples | ██ | ███ | ███ | ████ | 30+ tutoriais |
| Comunidade | █ | ██ | ███ | ████ | 5K+ stars |

---

## 6. Equipe

| Papel | Qtd | Foco |
|-------|-----|------|
| Tech Lead / Arquiteto | 1 | Arquitetura; integração com OpenIddict; API design; DX; code review |
| Senior Full-Stack (.NET + Blazor) | 2 | Admin UI/API; Login/Consent UI; Account Management; templates dotnet new; MFA |
| Senior Backend (.NET) | 1 | Migration CLI; SAML bridge; Passkeys; multi-tenancy; stores estendidos; caching |
| Security Engineer | 1 | Segurança dos defaults; threat modeling; auditoria; FAPI profile; pen testing |
| DevOps / SRE (part-time) | 1 | CI/CD; Docker; Helm; benchmarks; OpenTelemetry; monitoring setup |
| Technical Writer (part-time) | 1 | Documentação; tutoriais; samples; blog; migration guides |

**Total:** 5 full-time + 2 part-time

---

## 7. Orçamento

### Investimento por Fase

| Fase | Período | Investimento | Principais Custos |
|------|---------|--------------|-------------------|
| MVP | M1–M3 | R$ 220K–320K | Equipe core; infra CI/CD |
| Admin & Tools | M4–M7 | R$ 300K–440K | Equipe + infra cloud |
| Enterprise | M8–M11 | R$ 320K–500K | Equipe + auditoria + pen test |
| GA | M12–M14 | R$ 200K–340K | Equipe + marketing + conferências |
| **TOTAL** | **14 meses** | **R$ 1.04M–1.6M** | |

### Custos Mensais Detalhados

| Categoria | Mensal | Anual |
|-----------|--------|-------|
| Salários (5 FT + 2 PT) | R$ 60K–85K | R$ 720K–1.02M |
| Infraestrutura Cloud (CI/CD, staging, registry) | R$ 3K–5K | R$ 36K–60K |
| Auditoria de Segurança (1x) | — | R$ 80K–150K |
| Marketing e Conferências | R$ 5K–8K | R$ 60K–96K |
| Ferramentas (GitHub, Figma, etc.) | R$ 2K–3K | R$ 24K–36K |

---

## 8. Posicionamento Competitivo

| Critério | Duende IS | Keycloak | OpenIddict | OpenGate | Vencedor |
|----------|-----------|----------|------------|----------|----------|
| **Custo** | US$1.5K–35K | Gratuito | Gratuito | **Gratuito** | — |
| **Nativo .NET** | ✅ | ❌ Java | ✅ | **✅** | — |
| **Turnkey / Pronto** | ⚠️ Médio | ✅ Sim | ❌ Não | **✅ Sim** | **OpenGate** |
| **Admin UI** | ❌ Pago | ✅ Sim | ❌ Não | **✅ Blazor** | **OpenGate** |
| **Setup < 5 min** | ⚠️ | ✅ Docker | ❌ Muito código | **✅ dotnet new** | **OpenGate** |
| **Migration Tool** | N/A | ❌ | ❌ | **✅ CLI** | **OpenGate** |
| **SAML 2.0** | ✅ Plugin£ | ✅ | ❌ | **✅ Incluso** | **OpenGate** |
| **Passkeys** | ❌ | ⚠️ | ❌ | **✅** | **OpenGate** |
| **Observabilidade** | ⚠️ | ✅ | ❌ | **✅ OTel** | **OpenGate** |
| **Multi-tenancy** | ❌ | ✅ | ❌ | **✅** | Empate |

---

## 9. Modelo de Monetização

O produto é 100% gratuito e open source (Apache 2.0). A sustentabilidade vem de serviços ao redor:

| Fonte de Receita | Descrição | Estimativa Anual |
|------------------|-----------|------------------|
| **Suporte Enterprise** | SLA garantido, hotfixes prioritários, consultoria dedicada, security advisories antecipados | R$ 500K–1.2M |
| **OpenGate Cloud (SaaS)** | Versão gerenciada: painel avançado, backups, updates automáticos, SLA 99.9% | R$ 300K–1M |
| **Treinamento** | Cursos oficiais, workshops, programa de certificação OpenGate Professional | R$ 200K–500K |
| **Consulting** | Migração assistida de Duende IS, IdentityServer4, Keycloak | R$ 200K–600K |

---

## 10. Riscos

| Risco | Prob. | Imp. | Mitigação |
|-------|-------|------|-----------|
| **OpenIddict descontinuado** | Baixa | Alto | Apache 2.0 é irrevogável; podemos internalizar a última versão e manter independentemente. Enquanto isso, é ativamente mantido com release anual. |
| **Breaking changes no OpenIddict** | Média | Médio | Pinning de versão major; testes de integração contra pré-releases; compatibility layer se necessário. Atualizar no nosso ritmo. |
| **Vulnerabilidade no OpenIddict** | Baixa | Crítico | Monitorar releases e CVEs; atualizar dependência em até 48h; security advisory próprio para usuários OpenGate. |
| **Percepção de "apenas wrapper"** | Alta | Médio | Comunicar o valor enorme do produto (Admin UI, CLI, docs, DX). Ninguém chama o Next.js de "apenas wrapper do React". O valor é real e tangível. |
| **Baixa adoção** | Média | Alto | DX excepcional; migration CLI para capturar base instalada de Duende IS e IS4; marketing ativo; presença em conferências. |
| **Duende fica gratuito** | Baixa | Alto | Diferenciar com Admin UI, DX superior, multi-tenancy, Passkeys, SAML bridge. Muitos features que o Duende não oferece. |
| **Scope creep** | Alta | Médio | Roadmap rígido; RFC process; dizer "não" é parte do processo. Foco no que importa: DX e produção-ready. |

---

## 11. Próximos Passos

1. Registrar domínio e criar GitHub org (opengate-identity)
2. Recrutar Tech Lead (perfil: experiência com OpenIddict, DX-minded, boa comunicação)
3. Criar repositório com estrutura de solução, CI/CD e pacotes iniciais
4. POC em 2 semanas: `builder.AddOpenGate()` + Login UI funcional + Docker
5. Validar o conceito com 5–10 devs .NET como beta testers internos
6. Iniciar Sprint 1: OpenGate.Server + OpenGate.UI + primeiro template
7. Publicar anúncio no blog, Reddit r/dotnet, Twitter/X, Hacker News

---

*Documento Final — Março 2026 — Produto Turnkey sobre OpenIddict*
