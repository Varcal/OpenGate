## Fase 1 (MVP) - Status de Implementacao

Fonte: `docs/opengate-identity-projeto.md` secao **4.1 Fase 1 - MVP**.

| Item da Fase 1 | Status | Evidencias no repo |
|---|---|---|
| OpenGate.Server: setup em 1 linha (`AddOpenGate`) | FEITO | `src/OpenGate.Server/Extensions/OpenGateServiceCollectionExtensions.cs`, `src/OpenGate.Server/OpenGateBuilder.cs` |
| Presets de seguranca (Development/Production/HighSecurity) | FEITO | `src/OpenGate.Server/Extensions/OpenGateServiceCollectionExtensions.cs`, `src/OpenGate.Server/Options/OpenGateOptions.cs`, `samples/OpenGate.Sample.Basic/Program.cs` |
| UI Login / Consent / Logout / Registro (Razor Pages) | FEITO | `src/OpenGate.UI/Pages/Account/Login.*`, `src/OpenGate.UI/Pages/Account/Register.*`, `src/OpenGate.UI/Pages/Connect/Authorize.*`, `src/OpenGate.UI/Pages/Connect/Logout.*`, layout `src/OpenGate.UI/Pages/Shared/_Layout.cshtml` |
| Integracao com ASP.NET Core Identity | FEITO | `src/OpenGate.Data.EFCore/Extensions/OpenGateDataExtensions.cs`, `src/OpenGate.Data.EFCore/OpenGateDbContext.cs`, `src/OpenGate.Server/OpenGateBuilder.cs` |
| EF Core stores estendidos + migrations para PostgreSQL/SQL Server/SQLite | PARCIAL | Entidades existem (`src/OpenGate.Data.EFCore/Entities/*`) e migrations existem, mas provider/pacotes e migrations geradas hoje estao focados em SQL Server (`src/OpenGate.Data.EFCore/Migrations/*`, `src/OpenGate.Data.EFCore/OpenGate.Data.EFCore.csproj`) |
| Template `dotnet new opengate-server` (config guiada) | FEITO (in-repo) | `templates/opengate-server/.template.config/template.json` + conteudo do template |
| Docker image oficial (Alpine, <100MB) + compose (app + PostgreSQL + Redis) | PARCIAL | Existe `docker-compose.yml` + `samples/OpenGate.Sample.Basic/Dockerfile`, mas hoje e SQL Server + sem Redis/PostgreSQL + nao Alpine |
| Documentacao: 3 quickstarts, API reference, architecture overview | FEITO | `docs/README.md`, `docs/quickstarts/*`, `docs/api-reference.md`, `docs/architecture.md` |
| 5 samples (React SPA, API protegida, Blazor WASM BFF, Console M2M, Device Flow) | NAO FEITO | Apenas `samples/OpenGate.Sample.Basic` |
| CI/CD (GitHub Actions), Codecov, CodeQL | NAO FEITO | `.github/workflows/` existe mas esta vazio |

### Observacao sobre o template

O template atual e **in-repo**: ele gera um servidor que referencia `src/` via `ProjectReference`. Isso permite desenvolvimento/testes locais antes de publicar pacotes no NuGet.
