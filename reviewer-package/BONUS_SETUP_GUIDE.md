# MovieApp Bonus setup guide

Use this guide for the **Custom Backend Bonus** branch. The original Android **main** branch remains the standalone TMDb application; the Bonus is not merged into main. The optional **Admin Portal Extra Bonus** is served by the same backend process.

## Prerequisites

- Windows with .NET SDK **10.0.403** (the backend's global.json selects this .NET 10 SDK; later compatible patches are allowed).
- Git, VS Code with .NET support or another compatible development environment.
- Android Studio compatible with the checked-in Android Gradle Plugin **9.4.1** and Gradle wrapper **9.6.0**. Use a compatible Studio Gradle JDK, not Java 11 merely because the source compatibility is 11.
- Android SDK Platform **37**, SDK Platform-Tools, and a device/emulator running **API 26 or later**. Use a Google Play-enabled emulator or a device with working Google Play services for FCM.
- USB debugging and an authorized ADB connection for a physical device.
- Current Postman desktop app (or web app with its Desktop Agent for localhost).
- A Firebase project only for the real-push path. Local API, Admin Portal and Android browsing can run without Firebase.

## Clone the two repositories

Run these commands in a parent directory of your choice:

~~~powershell
git clone https://github.com/MohamedAlatawy/MovieAppBackend.git
git clone --branch feature/bonus-custom-backend --single-branch https://github.com/MohamedAlatawy/MovieApp.git MovieApp-Bonus
~~~

The second command selects the Bonus directly; it does not modify main or develop. Open **MovieApp-Bonus** in Android Studio. To review the original assignment separately, use the [Core Android main branch](https://github.com/MohamedAlatawy/MovieApp/tree/main) and its own README/TMDb setup. No TMDb token is needed for Bonus backend browsing.

## Start the backend in Local mode

From the parent directory, enter the backend root:

~~~powershell
Set-Location .\MovieAppBackend
dotnet restore
dotnet build
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:Firebase__Mode = 'Local'
~~~

Before starting, configure a private administrator key. The API project already declares its UserSecretsId, so do not initialize another one. Replace the placeholder below with your own random secret of **32–512 characters**. The server requires at least 32; request headers longer than 512 are rejected.

~~~powershell
dotnet user-secrets set "Admin:ApiKey" "<YOUR_RANDOM_ADMIN_KEY_AT_LEAST_32_CHARACTERS>" --project .\src\MovieApp.Api
~~~

The example contains no usable key. Enter your own key privately; do not share shell history or screenshots containing it. Development User Secrets are local configuration, not an encrypted credential vault. Alternatively, set **Admin__ApiKey** in the launching PowerShell session to your private value. Environment variables override User Secrets; remove a stale override in your own session if using User Secrets.

Start the API from this same terminal:

~~~powershell
dotnet run --project .\src\MovieApp.Api --no-launch-profile --urls http://127.0.0.1:5187
~~~

Leave that terminal running. New environment variables in another terminal do not reconfigure an already running server.

Startup creates the SQLite directory/database, applies the existing EF Core migrations, and seeds **31 synthetic records** on a new database. The default location is **src/MovieApp.Api/App_Data/movies.db**. Subsequent launches preserve edits. **dotnet ef is not needed for normal startup**; no database file needs to be copied from the owner. The dedicated edit record is **9001**, initially titled **Demo Edit Sandbox**. These records are labeled Demo Data and are not TMDb eligibility evidence.

In a second terminal:

~~~powershell
Invoke-RestMethod http://127.0.0.1:5187/health
Invoke-RestMethod http://127.0.0.1:5187/api/movies/9001
~~~

Health should return status **healthy** and dataSource **synthetic-demo**.

## Postman and the actual request contract

Import both [the collection](MovieApp_Bonus.postman_collection.json) and [the environment](MovieApp_Local.postman_environment.json), then select **MovieApp Local**. Set adminKey only in your private local environment value; do not export or share a populated environment. Its exported value is intentionally empty.

The collection has a read-only folder and separate manual-write folders. Run only the read-only folder in Collection Runner. For a single PATCH, set **demoTitle** to a unique title you choose, leave movieId as **9001**, set **patchConfirmation** to **SEND ONCE**, then click Send on that request. Its request-level guard consumes the confirmation before sending, validates the title/key, and JSON-escapes the title. Do not arm it and run the whole collection. There are no scripted extra requests, retries or automatic FCM sends. The skip guard uses [Postman's documented pre-request API](https://learning.postman.com/docs/tests-and-scripts/write-scripts/postman-sandbox-reference/pm-execution/).

| Request | Successful response |
| --- | --- |
| GET /health | 200; status and dataSource |
| GET /api/movies?page=1&pageSize=20 | 200; items, page, pageSize, totalItems, totalPages, hasNextPage |
| GET /api/movies/search?query=toy&page=1&pageSize=20 | Same page envelope; case-insensitive title search |
| GET /api/movies/9001 | 200; movie object directly |
| PATCH /api/movies/9001 | 200; updated movie object and notification headers |
| POST /api/devices/register | 200; status = registered |
| POST /api/devices/unregister | 204; no body |

Page is 1–1,000,000; pageSize is 1–100. Search requires a nonblank query of at most 200 characters. List accepts an optional nonblank query. Results are ordered by ID. A fresh database has 20 items on page 1, 11 on page 2, and two matches for **toy**; previous edits can change search results.

Movie JSON uses **id, title, overview, releaseDate, posterUrl, voteAverage, genres, adult, usCertification, lastModified, dataSource, eligibilityEvidence, isDemo**. Date is yyyy-MM-dd; lastModified is a UTC ISO timestamp. The trust markers are synthetic-demo, unverified and true.

PATCH takes a nonempty partial object, not JSON Patch operations. Only protected edit requests carry **X-Admin-Key: {{adminKey}}**. Omitted fields remain unchanged; only posterUrl accepts null.

| Editable field | Constraint |
| --- | --- |
| title | Nonblank, trimmed, at most 200 characters |
| overview | Nonblank, at most 5,000 input characters |
| releaseDate | yyyy-MM-dd, year 1888–2100 |
| voteAverage | Finite number, 0–10 |
| genres | 1–10 nonblank strings, each at most 40 characters |
| posterUrl | null or absolute HTTPS URL without embedded credentials, at most 2,048 characters |

ID, certification and trust fields are immutable. Unknown/duplicate fields are rejected. Validation errors return 400; unknown movies return 404.

Device requests accept **{"token":"..."}**, with 20–4,096 characters from letters, digits, underscore, colon and hyphen. The environment supplies an explicitly fake token for optional Local-mode compatibility testing. Set deviceConfirmation to **SEND ONCE** separately for each manual register/unregister request. These writes neither subscribe nor unsubscribe Firebase topics and are unnecessary for the Android demonstration. Registration returns no token; unregister is idempotent. Device routes are rate limited and may return 429.

## Local Admin Portal and Swagger

With the Development API running, open:

- [Admin Portal](http://127.0.0.1:5187/admin)
- [Swagger UI](http://127.0.0.1:5187/swagger)

These are **local URLs, not public hosted websites**. No separate frontend server or deployment is needed. The [Portal source](https://github.com/MohamedAlatawy/MovieAppBackend/blob/main/src/MovieApp.Api/Admin/index.html) lives inside the ASP.NET Core project.

Select the demo record, enter your administrator key into the password field, edit allowed fields and click **Save changes** deliberately. The Portal submits the displayed editable fields together; changing a title in Postman can submit only that field. It displays notification status, not proof of receipt. The key remains in page memory; do not share browser/network captures containing it. Both Portal and Swagger are unavailable outside Development.

## Android debug connectivity

The Bonus uses the custom backend as its active movie source. It retains Home, Search, Details and navigation, but stores demo rows in **backend-demo.db**, separate from the verified TMDb cache. It does not provide a runtime provider switch.

Open the Bonus clone in Android Studio, let it establish your local SDK path and sync, and choose a Debug build. For physical devices and emulators, the simplest supported route is ADB reverse. In the Bonus root:

~~~powershell
adb devices
adb reverse tcp:5187 tcp:5187
adb reverse --list
.\gradlew.bat :app:assembleDebug
adb install -r .\app\build\outputs\apk\debug\app-debug.apk
~~~

Use Android Studio's SDK Platform-Tools adb if it is not on PATH; no global PATH change is required. If multiple devices are attached, add **-s <DEVICE_SERIAL>** to each adb command. Launch MovieApp from the device or Android Studio.

The default debug Backend URL is **http://127.0.0.1:5187/**, including the trailing slash. Keep the PC backend running and the USB/ADB connection active. Reapply reverse after reconnecting if necessary. A device's own localhost reaches the PC only with this mapping.

For an emulator, **http://10.0.2.2:5187/** is an alternative. Rebuild with:

~~~powershell
.\gradlew.bat :app:assembleDebug -PbackendBaseUrl=http://10.0.2.2:5187/
~~~

Install the rebuilt APK, and include **10.0.2.2** in the backend's process-local AllowedHosts before launching it, for example **$env:AllowedHosts = 'localhost;127.0.0.1;[::1];10.0.2.2'**. If emulator host routing fails, use ADB reverse instead. No public IP, paid hosting or firewall opening is needed.

Debug cleartext is allowed only for localhost, 127.0.0.1 and 10.0.2.2. Release builds disallow cleartext and default to an unavailable HTTPS placeholder; use Debug for this local demo.

## Optional real Firebase setup

Local mode never loads or sends through the real Firebase transport. Without Android Firebase configuration, the app can still browse; it logs **Client configuration missing; messaging disabled**.

For a real demonstration, use a controlled Firebase project: topic **movies** targets every subscribed device in that project. Obtain a fresh authorized Admin service-account credential and store it outside both repositories. Never reuse a previously exposed credential; revocation and replacement are required because anyone possessing it could act as that account. This package contains no owner credential.

Register a Firebase Android app with package **com.example.movieapp** and download its client **google-services.json** to **MovieApp-Bonus/app/google-services.json**. This client file is different from the server's private Admin credential.

**Important implementation constraint:** app/build.gradle.kts currently requires project_info.project_id to equal **movieapp-19491** when google-services.json exists. A reviewer using another project must change that one comparison and its error message in their own Bonus clone to their Firebase project ID before syncing/building. There is no existing Firebase project Gradle override. Do not alter main/develop or send credentials to the owner. If using the existing project, obtain authorized client access rather than trying to use someone else's Admin key.

Client project, server Firebase:ProjectId and the Admin credential must all match. In the backend root, replace these placeholders with your own project and external credential path:

~~~powershell
dotnet user-secrets set "Firebase:ProjectId" "<YOUR_FIREBASE_PROJECT_ID>" --project .\src\MovieApp.Api
dotnet user-secrets set "Firebase:CredentialsPath" "<ABSOLUTE_PATH_TO_YOUR_FRESH_ADMIN_JSON_OUTSIDE_REPOS>" --project .\src\MovieApp.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:Firebase__Mode = 'Auto'
dotnet run --project .\src\MovieApp.Api --no-launch-profile --urls http://127.0.0.1:5187
~~~

If your Local-mode process is still running, stop it yourself with Ctrl+C in its terminal before launching Auto; do not start a second server on port 5187. Check for stale Firebase environment overrides in that launching terminal: environment values take precedence over User Secrets. Firebase:CredentialsPath takes precedence over GOOGLE_APPLICATION_CREDENTIALS. Firebase Cloud Messaging API and service-account permissions must allow sending in your project.

Auto with no configured credential path returns **not_configured** after a committed edit. Auto with an invalid file/permissions attempts delivery and returns **failed**. Configuration presence alone is not proof that Firebase accepts a message.

Rebuild/install Android after adding or changing google-services.json, launch it online, and inspect the **MovieFirebase** Logcat tag for **Topic subscription succeeded** before the live test. See [Path B](BONUS_DEMO_WALKTHROUGH.md#path-b--real-fcm-and-android-reactive-update).

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Connection refused / health unreachable | Keep the API running at 127.0.0.1:5187; inspect its terminal. Environment changes affect a new launch, not a different running process. |
| Desktop GET works, Android fails | Check debug URL/trailing slash, adb devices and adb reverse --list. Reapply reverse; use the selected device serial. |
| Android build rejects Firebase project | Update the documented project-ID guard in your own Bonus clone for your matching Firebase project. Do not edit private JSON to fake its project ID. |
| Missing Firebase client configuration | Place the matching client file under app/, sync and reinstall. Browsing alone does not prove messaging is enabled. |
| PATCH 401 | Wrong/missing X-Admin-Key; use the same private key selected by the running backend. |
| PATCH 503 | Administrator key missing or shorter than 32 characters in effective backend configuration. |
| Postman PATCH is skipped | Set your local adminKey, movieId=9001, a unique demoTitle and patchConfirmation=SEND ONCE. The flag is consumed before the request. |
| not_configured after edit | Local mode, or no credential path. The database edit can still have succeeded. |
| failed after edit | Check the backend's safe exception-category log and your own Firebase configuration. Do not repeat an edit to diagnose delivery. |
| accepted but no observed update | Check successful topic subscription, matching project, Google Play services, device internet, ADB reverse, backend reachability and WorkManager scheduling. Delivery can be delayed or missed. |
| No system notification | Expected: the message is data-only; the app does not create a notification here. Look at Details data. |
| Returning to app shows new data | Foreground reconciliation also refetches cached IDs; this alone is not evidence of FCM receipt. |

Firebase carries the update hint, not the movie data or a network tunnel. A remote subscriber cannot fetch your PC's localhost API merely because Firebase accepted a message. Keep the controlled test device connected by ADB reverse.
