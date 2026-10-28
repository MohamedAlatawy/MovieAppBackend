# MovieApp Bonus Backend

Working C# / ASP.NET Core .NET 10 backend for a deterministic MovieApp demonstration. **Backend API, SQLite persistence, protected PATCH and Admin Portal are implemented. Android backend integration is implemented and live-tested.** Firebase topic `movies` is configured; subscription and sender acceptance were verified. The owner observed a successful automatic Android Details update. Independent `onMessageReceived` and Room-write tracing were not conclusively recorded. Android is maintained separately from this solution.

## Quick start (PowerShell)

From the root of your cloned MovieAppBackend repository.

```powershell
dotnet restore
dotnet build
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Admin__ApiKey = '<replace-with-a-random-secret-of-at-least-32-characters>'
$env:Firebase__Mode = 'Local'
dotnet run --project src/MovieApp.Api --no-launch-profile --urls http://127.0.0.1:5187
```

Use your own random administrator key; the placeholder is not a supplied credential. Keep it out of source control and Android. An alternative is `dotnet user-secrets set "Admin:ApiKey" "<your-secret>" --project src/MovieApp.Api` in Development. Environment variables override user secrets. A missing/short key disables edits with HTTP 503; reads still work. A wrong/missing request key returns 401 when editing is configured.

- API health: `http://127.0.0.1:5187/health`
- Swagger UI: `http://127.0.0.1:5187/swagger`
- Minimal editor: `http://127.0.0.1:5187/admin`
- Stop the foreground API with Ctrl+C.

Swagger and the admin page are available only in Development. The editor is a public development UI shell over public reads, but **every save requires the protected PATCH endpoint**. Enter the key into its password field; it is kept only in page memory, never cookies or browser storage. No separate frontend build or accounts are needed. Swagger exposes an `X-Admin-Key` input on PATCH; Postman uses the same header. Neither tool is an Android editing feature.

## Technology and layout

- .NET SDK 10.0.403 / net10.0; EF Core SQLite 10.0.14.
- Firebase Admin SDK 3.7.0; Swashbuckle.AspNetCore 10.1.0; patched Microsoft.OpenApi 2.7.5.
- xUnit with Microsoft.AspNetCore.Mvc.Testing and actual temporary SQLite databases.
- Controllers use DbContext directly, with DI notification services; no generic repositories, queues, or extra infrastructure.

```text
src/MovieApp.Api/
  Controllers/       Movies and device endpoints
  Contracts/         Response DTOs, patch validation, device input
  Entities/          Movie and DeviceToken persistence models
  Data/Migrations/   Initial schema, deterministic seed, EF snapshot
  Services/          Post-commit dispatcher and Firebase transport
  Configuration/     Administrator protection and Swagger examples
  Admin/             Small HTML/JavaScript editing page
  Program.cs         DI, middleware, local migration startup
tests/MovieApp.Api.Tests/  Integration and fake-transport tests
scripts/Verify-Local.ps1   Repeatable isolated HTTP/restart test
docs/ANDROID_INTEGRATION.md
artifacts/           Ignored local test evidence and isolated database
MovieAppBackend.slnx
```

Versions were checked against the [Swashbuckle package's net10.0 dependency group](https://www.nuget.org/packages/Swashbuckle.AspNetCore/10.1.0), [EF Core SQLite package](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/10.0.14), and [Firebase Admin package](https://www.nuget.org/packages/FirebaseAdmin/3.7.0). Microsoft.OpenApi is explicitly pinned above the affected version identified in the [maintainer's security advisory](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc). Successful compilation and HTTP schema/UI checks also verify this package combination.

## Configuration

ASP.NET Core accepts nested environment keys with double underscores. There are **no supplied credentials** in appsettings.

| Variable | Default / purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` unless launch profile selects Development. Set `Development` locally for Swagger/editor and loopback HTTP. |
| `ASPNETCORE_URLS` | Optional binding, e.g. `http://127.0.0.1:5187`; `--urls` can override it. |
| `Admin__ApiKey` | Required only for editing; random secret, 32+ characters. User-secrets equivalent: `Admin:ApiKey`. |
| `Database__Path` | Optional absolute SQLite filename. Default: `src/MovieApp.Api/App_Data/movies.db` when run via project. |
| `Firebase__Mode` | `Auto` by default. `Local` explicitly disables real sending even if credentials exist. |
| `GOOGLE_APPLICATION_CREDENTIALS` | Optional absolute path to a trusted Firebase service-account JSON file **outside this project**. |
| `Firebase__CredentialsPath` | Optional explicit service-account path; takes precedence over `GOOGLE_APPLICATION_CREDENTIALS`. |
| `Firebase__ProjectId` | Firebase target project ID; defaults to `movieapp-19491`. This is not a secret. |
| `AllowedHosts` | Defaults to localhost, 127.0.0.1, ::1. For a future deployment/device alias, explicitly configure the approved hostnames; do not use a public wildcard by default. |

No system environment or global PATH changes are required. Environment settings in the quick start apply only to that shell and child process. Production requires HTTPS and proper host/reverse-proxy configuration; the project enables HTTPS redirection/HSTS outside Development. This task did not deploy or alter firewall rules.

## SQLite and seeds

Startup applies `Database.MigrateAsync()`; it never uses `EnsureDeleted` or destructive recreation. The initial migration creates:

- `Movies`: stable integer ID, title/search-normalized title, overview, date, nullable poster URL, numeric vote, JSON genre list, adult flag, nullable US certification, UTC last-modified timestamp.
- `DeviceTokens`: internal integer ID, opaque FCM token, timestamp; unique token index.
- EF migration bookkeeping. Movie title-search and release-date indexes are present. Substring search uses normalized text and may scan; a B-tree does not make arbitrary substring queries indexed. This is appropriate for 31 demo records.

The migration seeds 30 original fictional titles (`1001`–`1030`) plus `9001`, **Demo Edit Sandbox**. Titles, dates, scores, and synopses are synthetic. Posters start null. Certifications are unknown/null, not invented ratings. `adult: false` is demo metadata, not eligibility evidence. Every DTO says `dataSource: "synthetic-demo"`, `eligibilityEvidence: "unverified"`, `isDemo: true`.

Seeds run once as migration inserts; restarting does not reset edits. Use separate database paths for demonstrations. Generated SQLite files, build outputs, and artifacts are ignored by `.gitignore`. Protect the database with normal local filesystem permissions because it contains private device tokens. No Firebase secrets are stored in SQLite.

The EF CLI is a **project-local tool**, declared in `dotnet-tools.json`:

```powershell
dotnet tool restore
dotnet ef migrations list --project src/MovieApp.Api
# For a future schema change, after changing the model:
dotnet ef migrations add <MigrationName> --project src/MovieApp.Api --output-dir Data/Migrations
```

## API contract

| Method / path | Behavior |
| --- | --- |
| `GET /health` | Database connectivity; healthy JSON or 503. |
| `GET /api/movies?page=1&pageSize=20&query=toy` | Stable ID ordering, optional title query. |
| `GET /api/movies/search?query=toy&page=1&pageSize=20` | Required nonblank case-insensitive substring title search. |
| `GET /api/movies/{id}` | Full DTO; 404 if absent. |
| `PATCH /api/movies/{id}` | Protected partial object edit; full updated DTO. |
| `POST /api/devices/register` | Register/refresh token; 200 `{"status":"registered"}`. |
| `POST /api/devices/unregister` | Delete by token possession; idempotent 204. |

Pages start at 1; `page` must be 1–1,000,000 and `pageSize` 1–100 (default 20). Out-of-range pages return empty items with correct total counts. Search query is limited to 200 characters. Missing/whitespace search gives 400. Errors are ProblemDetails with status/title and validation `errors` where applicable. See [Android integration contract](docs/ANDROID_INTEGRATION.md) for exact JSON and cache rules.

PATCH accepts an ordinary partial JSON object, **not JSON Patch operation arrays**. Omitted fields are unchanged; `posterUrl: null` clears a poster. Other explicit nulls are invalid. Unknown properties, duplicates, empty objects, ID changes, adult/certification/trust changes, invalid values and lengths return 400 without saving or dispatching. Allowed fields:

- `title`: trimmed, nonblank, at most 200 characters.
- `overview`: nonblank, at most 5,000 characters.
- `releaseDate`: `yyyy-MM-dd`, years 1888–2100.
- `voteAverage`: finite number from 0 to 10.
- `genres`: 1–10 nonblank strings, max 40 characters each; deduplicated case-insensitively.
- `posterUrl`: null or absolute HTTPS URL without embedded credentials, max 2,048 characters. The backend never fetches this URL.

Example in Postman/Swagger:

```http
PATCH /api/movies/9001
Content-Type: application/json
X-Admin-Key: <your-development-administrator-key>

{"title":"Demo Edit Sandbox - Updated","voteAverage":8.2,"posterUrl":null}
```

The database commit precedes notification dispatch. Concurrent tracked updates use `LastModified` as an EF concurrency token; conflicts return 409. This detects overlapping database writes, but does not enforce an ETag against a previously loaded admin form; review current details before editing.

## Firebase

Without credentials (or with `Firebase__Mode=Local`), PATCH still succeeds, logs **no push sent**, and returns `X-Notification-Status: not_configured`. This is a non-delivery outcome, not a simulated success.

**Never reuse an exposed service-account credential.** The owner confirmed revocation of the previously exposed key and generation of a fresh credential. Subscription and sender acceptance were verified, and the owner observed an automatic Android Details update. Independent receipt-handler and Room-write tracing remain inconclusive.

After the owner has revoked the exposed key, configure a freshly generated service-account file for **movieapp-19491** through the owner-controlled secure setup. Keep it outside the solution with restricted filesystem access. Confirm the FCM API and account permissions separately. Only after fresh configuration and separate authorization for a live send, use:

```powershell
$env:Firebase__Mode = 'Auto'
$env:GOOGLE_APPLICATION_CREDENTIALS = '<absolute-secure-path-to-fresh-service-account.json>'
$env:Firebase__ProjectId = 'movieapp-19491'
```

Only explicitly configured service-account paths are loaded through the typed credential factory; `Firebase__CredentialsPath` overrides `GOOGLE_APPLICATION_CREDENTIALS`. The application cannot detect whether a file contains a previously exposed key, so the owner must verify freshness. No credential file was read or configured for this change. Keep `Firebase__Mode=Local` during local demonstrations; once Auto mode and credentials are configured, a successful PATCH will attempt a live topic send.

The device registration endpoints remain for compatibility, but **movie-update topic delivery does not use DeviceTokens, require registered tokens, or subscribe/unsubscribe devices**. An optional compatibility request remains:

```http
POST /api/devices/register
Content-Type: application/json

{"token":"<actual-device-FCM-registration-token>"}
```

Use the same body with `/unregister` for removal from SQLite; this does not unsubscribe the device from `movies`. Syntax checks accept 20–4,096 ASCII token characters (letters, numbers, `_`, `-`, `:`); they cannot establish that Firebase issued the token. The local-demo model treats possession as the registration/removal credential. There is no public token-list endpoint. Device operations are rate-limited per remote IP to 30/minute; the demo capacity is roughly 1,000 tokens (the admission count is not a strict concurrent quota). Public deployment should add application attestation/authenticated device ownership and trusted proxy handling.

After each successful PATCH commits, the notifier constructs one Firebase Admin `Message` with `Topic = "movies"` and calls the transport once. The real transport calls `FirebaseMessaging.GetMessaging(app).SendAsync(message, cancellationToken)` once. There is no multicast, device iteration, subscriber-count lookup, or Notification payload. Firebase performs fanout to topic subscribers. The data contains exactly these three string fields:

```json
{"eventType":"movie_updated","movieId":"9001","updatedAt":"2026-10-28T00:00:00.0000000Z"}
```

No full movie, token, key, or certificate is sent. `movieId` is invariant-culture decimal text; `updatedAt` is the committed movie's UTC ISO-8601 round-trip timestamp. Actual accepted Firebase message IDs are logged; private keys, credentials and SDK exception bodies are not. See [Firebase's official topic-send guidance](https://firebase.google.com/docs/cloud-messaging/send-topic-messages). One application-level send is not an exactly-once delivery guarantee: SDK/network retries and device behavior can produce duplicate, delayed, or missed events.

Response headers distinguish `not_configured`, `accepted`, or `failed`. `X-Notification-Accepted` is 1 only after the single topic publish is accepted, otherwise 0; it is never a subscriber count. The existing `X-Notification-Failed` header remains 0 when an exception leaves delivery outcome unknown. `failed` reports the failure separately, while HTTP 200 still means the edit committed. A timeout may occur after Firebase accepted the request. `accepted` does **not** prove that any subscriber exists or received/processed it, and topic acceptance does not guarantee immediate device delivery. Never repeat an edit merely to infer delivery success.

Movie-update dispatch no longer reads, removes or validates stored device tokens. Dispatch retains its 20-second cancellation budget after commit and runs independently of request disconnect. There is no durable outbox or guaranteed retry; a crash after commit can lose a notification. Android must reconcile by fetching on foreground/refresh, not depend solely on push.

The Android client must call `FirebaseMessaging.getInstance().subscribeToTopic("movies")`, handle subscription completion/failure, and inspect `eventType`, `movieId`, and `updatedAt` in `FirebaseMessagingService.onMessageReceived`. For `movie_updated`, fetch current details from the backend and update only the appropriate demo cache. Schedule deferred WorkManager work when Android background execution requires it. Do not mix synthetic records with the authoritative TMDb verified cache. Topics are broadcast distribution, not an authorization boundary; only non-sensitive update hints belong here. Android backend integration is implemented and live-tested; this does not establish independent tracing of every internal stage.

## Verification

```powershell
dotnet restore
dotnet build
dotnet test --logger 'trx;LogFileName=backend-tests.trx' --results-directory artifacts/TestResults
./scripts/Verify-Local.ps1
# If the default port is occupied:
./scripts/Verify-Local.ps1 -Port 5188
```

The local verifier requires a Debug build, creates a unique isolated database beneath `artifacts/`, generates a temporary administrator key only in process memory, forces local/no-delivery Firebase mode, and starts Kestrel on loopback. It checks required HTTP endpoints, protected PATCH, JSON persistence, Swagger UI/script/schema, the admin page, device operations, and persistence after restarting its own process. It restores process environment settings and stops only its own API process, even on failure. Evidence/logs/database are retained. Ordinary seed records and the normal application database are not edited.

Tests use actual SQLite migrations with one unique TEMP database per factory and fake notification transport only. They cover pagination, search, details, validation, authentication, committed-state visibility during dispatch, notification failure, exactly one data-only `movies` topic message, dispatch without a DeviceTokens table, compatibility device endpoints, local-mode non-delivery, and persistence with a new DbContext. Test databases are retained under `%TEMP%\MovieAppBackendTests`; no existing files are removed. The latest topic delivery report records actual counts and HTTP outcomes.

## Current status and limitations

- Backend API, SQLite persistence, protected PATCH and Admin Portal are implemented. Android backend integration is implemented and live-tested in its separate project.
- Firebase topic `movies` is configured; subscription and sender acceptance were verified. The owner observed a successful automatic Android Details update. Independent `onMessageReceived` and Room-write tracing were not conclusively recorded.
- Synthetic IDs are not TMDb IDs. Demo metadata must never silently become TMDb eligibility evidence. Use a separate demo data source/cache and retain the existing verified cache; see the contract.
- The optional admin page is implemented without a frontend framework. HTTP loading and its protected backend flows were tested; the owner also confirmed successful manual Admin Portal testing.
- This is a practical local assignment backend, not a hardened public multi-tenant service. Deployment needs HTTPS, credential management, device ownership checks, operational database backups, and controlled migration rollout.
- No TMDb token, SQL Server, EF/Firebase Android packages, or additional global tools are required for the local backend demo.

Remaining verification limitation: the observed automatic Details update is successful UI evidence, but it does not independently trace each receipt, refetch and Room-write stage.
