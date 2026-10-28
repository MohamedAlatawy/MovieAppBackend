# Android integration contract — backend v1

This document describes the implemented and live-tested Android backend integration. The backend API, SQLite persistence, protected PATCH and Admin Portal are implemented. Firebase topic `movies` is configured; subscription and sender acceptance were verified. The owner observed a successful automatic Android Details update. Independent `onMessageReceived` and Room-write tracing were not conclusively recorded. The existing TMDb verification policy remains authoritative for its verified cache.

## Base URL and connectivity

Retrofit needs a trailing slash. Development desktop: `http://127.0.0.1:5187/`. Release builds must use an owner-controlled HTTPS base URL.

For an Android emulator, the conventional Windows-host alias is `http://10.0.2.2:5187/`. If using that path, add `10.0.2.2` to the server's **process-local** `AllowedHosts` configuration; it is not in the default allowlist. The emulator route and host loopback behavior depend on the emulator setup. An alternative for both emulator and physical USB devices is owner-run `adb reverse tcp:5187 tcp:5187`, then use `http://127.0.0.1:5187/` on the device. This leaves the backend listening on loopback; no firewall opening is necessary. These device commands were not run in this task. A physical phone's own `localhost` is not the PC without port reversal. Avoid public `0.0.0.0` bindings for the demo. Debug-only Android cleartext permission may be required later; never relax release network security.

## Endpoints and models

Reads are unauthenticated for the local demo. Do not add the administrator key to the Android binary. Android must not expose editing.

| Operation | Request |
| --- | --- |
| Browse | `GET api/movies?page=1&pageSize=20` |
| Filter list | `GET api/movies?page=1&pageSize=20&query=toy` |
| Search | `GET api/movies/search?query=toy&page=1&pageSize=20` |
| Details/refetch | `GET api/movies/1001` |
| Register token (optional compatibility only) | `POST api/devices/register`, JSON `{"token":"<real-registration-token>"}`; not needed for topic delivery |
| Remove token (compatibility only) | `POST api/devices/unregister`, same body; 204 even when absent; does not unsubscribe topics |

Exact response shape for `GET /api/movies?page=1&pageSize=1` on the initial database:

```json
{
  "items": [
    {
      "id": 1001,
      "title": "Toy Harbor",
      "overview": "Demo fiction: In Toy Harbor, a resourceful community follows an unexpected clue and learns to solve a shared problem. Original synthetic synopsis for API demonstrations.",
      "releaseDate": "2020-01-01",
      "posterUrl": null,
      "voteAverage": 6,
      "genres": ["Adventure", "Family"],
      "adult": false,
      "usCertification": null,
      "lastModified": "2026-01-01T00:00:00Z",
      "dataSource": "synthetic-demo",
      "eligibilityEvidence": "unverified",
      "isDemo": true
    }
  ],
  "page": 1,
  "pageSize": 1,
  "totalItems": 31,
  "totalPages": 31,
  "hasNextPage": true
}
```

Details and PATCH return the movie object directly, without the pagination wrapper. Timestamp fractional precision may vary; parse ISO-8601 UTC as an Instant, not a fixed character count. Dates are ISO local dates. Movie IDs are stable backend-scoped integers. Genres are string names, not TMDb genre IDs. Poster URLs are full HTTPS URLs, not TMDb relative image paths; null means show a placeholder. Overview/title map to display strings; `voteAverage` maps to Double; `adult` and `usCertification` are metadata **without verification authority**. All property names are camelCase. Unknown future response fields should be tolerated by the Android JSON adapter.

Paging 3 should use page 1 as initial key and the selected `pageSize` on every request; nextKey is `page + 1` only when `hasNextPage` is true. Use a consistent size despite Paging's larger initial load request. The backend counts pages after filtering, sorts by ascending ID, and permits sizes 1–100. Empty result sets have `totalItems: 0`, `totalPages: 0`, `hasNextPage: false`. Debounce search and cancel obsolete calls. Do not call search with blank input; show the browse stream instead. Percent-encode query values through Retrofit.

## Errors and resilience

HTTP 400 validation uses this shape (error text and traceId may vary):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "pageSize": ["The field pageSize must be between 1 and 100."] },
  "traceId": "<request-trace-id>"
}
```

404 means a movie does not exist in this backend. 401/503 on PATCH refer to admin authorization/configuration. 409 indicates an overlapping edit; refresh before retrying. Device operations may return 429; retry with bounded exponential backoff. 500/503 and network timeouts are temporary failures, not evidence that the catalog is empty or that a movie has become eligible/ineligible. Do not clear Room or replace verified data with an empty list after a failed request. Keep the last successful view with an offline/error indicator; perform refreshes transactionally after successful response validation.

## Trust boundary: synthetic data is not TMDb evidence

Every backend movie currently declares `isDemo: true`, `dataSource: "synthetic-demo"`, and `eligibilityEvidence: "unverified"`. Certifications are null. An immutable `adult: false` does not certify suitability. Synthetic IDs can numerically collide with TMDb IDs; there is no identity mapping between these catalogs.

Safe adaptation:

1. Introduce an explicitly labeled debug/demo source, separate from the verified TMDb source. Key storage by `(source, movieId)` or use a separate demo Room database and paging keys.
2. Preserve all existing verified TMDb records and their provenance. Do not copy or overwrite eligibility evidence from this demo backend, even if an ID/title happens to match.
3. If the current production eligibility gate requires authoritative TMDb evidence, these movies remain unverified and fail that gate. A separately labeled demo screen may demonstrate backend browsing/cache synchronization without describing its records as verified. Do not globally weaken the existing gate.
4. To later replace TMDb in the real verified path, implement a backend-owned authoritative TMDb retrieval and provenance/revalidation contract first, with explicit TMDb identity mappings and policy tests. That capability is not claimed by this synthetic backend.

PATCH cannot change adult, certification, source, evidence, or ID. Edits to title/overview/date/genres are still administrator-controlled demo data, not new eligibility proof. Treat fetched text as untrusted display content.

## Topic subscription and push refresh

Configure the Android Firebase client for project `movieapp-19491`. Subscribe explicitly and handle asynchronous completion/failure:

```kotlin
FirebaseMessaging.getInstance().subscribeToTopic("movies")
```

Use `unsubscribeFromTopic("movies")` for the corresponding opt-out. Do not infer subscription success merely from invoking the method. Backend `/api/devices/register` and `/unregister` remain compatible, optional token storage endpoints; they do not manage topic membership. Movie-update publishing is independent of stored tokens and subscribers. Topics are broadcast channels, not access controls, so the server publishes only non-sensitive ID/timestamp hints.

The owner confirmed revocation of the previously exposed service-account key and generation of a fresh server credential. Keep credentials outside source control and never reuse an exposed credential. Any additional live send requires separate owner authorization.

One data-only message is published to topic **movies** after each successful committed edit. There is no Notification payload. Exact data contract (all values are strings):

```json
{"eventType":"movie_updated","movieId":"9001","updatedAt":"2026-10-28T00:00:00.0000000Z"}
```

There is no notification-body object or full movie. FCM string values must be parsed defensively. `FirebaseMessagingService.onMessageReceived` must inspect `remoteMessage.data["eventType"]`, `["movieId"]`, and `["updatedAt"]`, and should:

1. Accept only `movie_updated` with a valid backend movie ID; never use payload fields as movie content or certification evidence.
2. Schedule unique WorkManager refresh work for that backend/source ID; avoid long networking directly in the callback. Coalesce duplicate messages.
3. Fetch `GET /api/movies/{id}` using the backend Retrofit client. Compare fetched `lastModified` to the cached backend version; ignore stale/reordered updates. Treat payload `updatedAt` only as a refresh hint.
4. Validate the DTO and source marker, then update only the corresponding **demo-source** Room row within a transaction. Flow/StateFlow should update the visible details/list. Preserve a verified TMDb row independently.
5. On timeout/5xx/offline, retain cache and retry with backoff. On 404, mark the backend-source entry unavailable and reconcile; do not delete an unrelated verified TMDb entry.
6. Refresh on foreground/manual refresh as well. FCM can be delayed, duplicated, suppressed after force-stop, or lost. Android background execution may require deferred work. Topic acceptance does not guarantee immediate delivery or execution; the backend makes one application-level publish, not an exactly-once device-delivery guarantee.

Register and refetch APIs never need the administrator key. The Android application must not add editing. A service-account JSON is **server-only**; Android's `google-services.json` is a different client configuration.

## Verification status and limits

The backend API, SQLite persistence, protected PATCH and Admin Portal are implemented. Android backend integration is implemented and live-tested. Firebase topic `movies` is configured, and subscription and sender acceptance were verified. The owner confirmed successful manual Admin Portal testing and observed an automatic Android Details update.

Independent `onMessageReceived` and Room-write tracing were not conclusively recorded. Sender acceptance alone does not prove device receipt, and the observed UI update does not independently prove every internal receipt, refetch and database-write stage. The fake/local backend tests remain separate from this owner-observed live result.
