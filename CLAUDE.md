# CLAUDE.md — VSky E-Commerce Framework

Multi-tenant e-commerce platform. Two apps:
- **API** — .NET 9 Clean Architecture, solution `API/VSky.ECommerce.sln`, SQL Server + EF Core.
- **WEB** — Vue 3 + Quasar 2 **admin SPA** (`WEB/`), plain JS, Pinia, axios, Vuelidate. Includes a public **storefront** area under `/shop/*`.

Work is delivered against phased **Work Orders** tracked in the `software-factory` MCP server (see bottom).

## Repo layout
- `API/src/Domain` — `Entities/`, `Enums/`, `Common/BaseEntity.cs` (Guid `Id`; `AuditableEntity`; `ISoftDeletable`).
- `API/src/Application` — CQRS `Features/<Area>/`; `Common/{Interfaces,Models,Exceptions,Behaviors,Authorization}`. MediatR + FluentValidation (AutoMapper is registered but unused — hand-write `Dto.From`).
- `API/src/Infrastructure` — `Persistence/` (`AppDbContext`, `Configurations/`, `Migrations/`, `DatabaseInitializer`), service impls, `DependencyInjection.cs`.
- `API/src/API` — `Controllers/`, `Program.cs`, `Authorization/`, `Middleware/`, `appsettings.json`.
- `WEB/src` — `modules/<feature>/{routes.js,pages/,components/,api.js}`, `components/common/App*` (global), `router/`, `stores/` (Pinia), `composables/`, `layouts/`, `services/api.js`, `boot/`.

## Build / run / migrate
- **Build API:** `cd API && dotnet build VSky.ECommerce.sln`. **If the app is running (Visual Studio / a `VSky.API` process), `bin/Debug` DLLs are locked → build with `-c Release`** to use `bin/Release` and sidestep the lock. Do NOT kill the user's running app.
- **Migrations:** `dotnet ef migrations add <Name> --project API/src/Infrastructure/VSky.Infrastructure.csproj --startup-project API/src/API/VSky.API.csproj [--configuration Release]`. Applied automatically at startup (`DatabaseInitializer.MigrateAsync`). Runtime conn string in `appsettings.json` (`Server=VSky-MT\SQLEXPRESS;...`); design-time factory defaults to localhost. Check drift with `dotnet ef migrations has-pending-model-changes ...`.
- **WEB:** `cd WEB && npm run dev` (dev server on :9000, API base `http://localhost:5144`) / `npm run build` (→ `publish/spa/`). **Gotcha:** `npm run build | tail` returns *tail's* exit code (0) even on failure — capture the real code with `npm run build > log 2>&1; echo $?` and grep for `FAIL`.
- **Dev URLs / CORS:** Kestrel listens on `https://localhost:7238` + `http://localhost:5144` (both normal). SPA uses the http one. `Program.cs` must call `app.UseCors(SpaCorsPolicy)` and skip `UseHttpsRedirection` in Development, or the SPA login preflight fails. Allowed origins come from `Cors:AllowedOrigins` (default `http://localhost:9000`).

## Backend conventions (match exactly)
- **Vertical slice** `Features/<Area>/<UseCase>.cs`: positional `record XCommand/Query(...) : IRequest<TDto>` (or `IRequest` for void) + nested `class XValidator : AbstractValidator<X>` + `class XHandler : IRequestHandler<X,TDto>` injecting `IApplicationDbContext _db` (+ services). Reads use `.AsNoTracking()` (+ `.AsSplitQuery()` when eager-loading multiple collections). Throw `NotFoundException(nameof(E), id)` / `ConflictException` / `ForbiddenAccessException` / `UnauthorizedException`; `ValidationException(IEnumerable<ValidationFailure>)` for manual 400s. DTOs are plain classes in `<Entity>Models.cs` with `static From(e)`. Paging: `PaginatedList<T>.CreateAsync(orderedQuery, page, size, ct)`.
- **Add an entity:** entity in Domain + `IEntityTypeConfiguration<>` in `Infrastructure/Persistence/Configurations/` (auto-discovered) + `DbSet` in **both** `IApplicationDbContext` and `AppDbContext` + a migration. Soft-deletable entities need `b.HasQueryFilter(x => !x.Deleted)`. Avoid SQL Server multiple-cascade-path errors: cascade an owned child from its single parent; use `NoAction`/`Restrict` for lookup FKs and any second FK to the same principal table.
- **Cross-cutting service:** interface in `Application/Common/Interfaces` + impl in `Infrastructure/<Area>/` + register in `Infrastructure/DependencyInjection.cs`. MediatR handlers, FluentValidation validators, and controllers **auto-register** (no DI edit needed).
- **Controller:** inherit `ApiControllerBase` (lazy `Mediator`), explicit `[Route("api/...")]`, admin `[RequireModule(Modules.X)]` (add the const to `Common/Authorization/Modules.cs`), public `[AllowAnonymous]`. `Ok(await Mediator.Send(...))`; delete → `NoContent()`; update → `command with { Id = id }`. Multiple controllers may share a route prefix if action templates differ.

## Frontend conventions
- Vue 3 `<script setup>`, **plain JS** (no TS), 2-space, single quotes, no semicolons. `App*` primitives are globally registered (`boot/components.js`) — `AppFormDrawer`, `AppDataTable`, `AppListHeader`, `AppTextField`, `AppSelect`, etc.
- Feature = `modules/<f>/`. Add a **module-local `api.js`** importing `{ api, anonApi, unwrap, qsSerializer, getApiErrorMessage }` from `services/api` (use `anonApi` for public storefront endpoints). Toasts via `useNotify()`; validation via Vuelidate (import rules from `validators`); confirm dialogs from `dialogs/`.
- **Wire a new admin module:** import its routes into `router/index.js` `moduleChildren`; add a nav item in `components/app_menu.vue`; add permission consts in `composables/usePermissions.js`. Full-access roles (`SuperAdmin`/`TenantAdmin`) bypass all permission checks, so gating is effectively role-based today.
- **Storefront** is a public top-level `/shop/*` area (`layouts/storefront_layout.vue`, `modules/storefront/`), NOT under the authenticated app shell.

## Feature inventory by phase (all on `main` @ `c85486f`, 2026-07-02)
- **Phase 1** (baseline): tenant branding + Credential Vault, SMTP accounts, platform settings, file storage (Local + Azure adapters), background-task scheduler, currencies + rate refresher, admin alerts, Serilog observability; auth (RS256 JWT + refresh tokens + password hashing), users/roles (module-based access), API-key auth, stores + delivery zones. Migrations `InitialSchema`, `AddRoleAccessibleModules`, `AddApiKeys`.
- **Phase 2**: Catalog (Product — 5 types, variants, product/spec attribute libraries, tier prices, relationships, tags, images/video, category tree, manufacturers, minimal `TaxCategory`) + Inventory (per-store levels, low-stock alerts); product search + storefront catalog APIs; customer register/verify/login/reset + profile/address book; order routing engine (+ a minimal `Order` model); admin UIs (tenant, RBAC, catalog), product gallery, storefront catalog views. Migrations `AddCatalogAndInventory`, `AddCustomerAccountsAndTokens`, `AddOrders`.
- **Phase 3**: Pricing (discounts, coupons, multi-currency), cart, **checkout orchestrator**, payments (router + Stripe/PayPal/Razorpay/Square/Authorize.NET/COD/BankTransfer, authorize/capture/refund, expired-auth scanner), tax (service + caching + flat-rate fallback + TaxJar/StripeTax), shipping (custom methods/zones + DHL/UPS carriers), order lifecycle state machine + status history + PDF invoice/packing-slip (QuestPDF); admin UIs (SMTP/Twilio, email templates, file storage, currency) + storefront cart/checkout/currency selector. Migration `AddPhase3Commerce`.
- **reCAPTCHA** (WO-106 full; 107/108 buildable slices): config API (`RecaptchaConfig`, admin `GET/PUT /api/tenant/recaptcha`, public `GET /api/storefront/config/recaptcha`), server `RecaptchaVerifier` wired onto register/login/password-reset/guest-checkout, storefront `useRecaptcha` composable. Migration `AddRecaptchaConfig`.

## Known gaps / deferred
- All external adapters (payment gateways, TaxJar/StripeTax, DHL/UPS, reCAPTCHA) are **HTTP/REST, structurally correct, need live credentials** to transact — no SDK packages, not runtime-verified.
- reCAPTCHA server/frontend only cover the 4 forms that exist; **Contact Us / Newsletter / Product Reviews / Q&A features don't exist yet** (block the other 4 forms). v2 widget rendering is a follow-up (only v3 token flow implemented).
- Checkout is single ship-to address (no split shipments); no card-tokenization UI (`paymentToken` sent null). Multi-currency is display-only (orders priced/placed in base currency).
- `Order.Status` carries both the Phase-2 store-routing states and the WO-45 lifecycle (`Pending→Processing→Shipped→Delivered`); checkout creates orders as `Pending`.
- Nothing has been exercised against a live SQL Server DB from an agent session — verification is compile + migration-generation + model-in-sync only.

## Work-order workflow (software-factory MCP)
`list_work_orders` / `read_work_order` / `edit_work_order` (status `backlog→in_progress→in_review`, `blocked` for missing deps), `list_phases`, blueprints via `read_blueprint`, and `add_comment` / `resolve_comment`. Set status when starting/finishing a WO; when a WO is genuinely blocked by a missing prerequisite, build the pragmatic default and leave a flagged comment rather than pausing.
