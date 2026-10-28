# Bonus demonstration walkthrough

Use [the setup guide](BONUS_SETUP_GUIDE.md) first. Work with the dedicated synthetic movie **9001**. Choose a unique title for each deliberate demonstration, such as **Demo Edit Sandbox - Reviewer <your unique suffix>**; replace the angle-bracket portion. Do not reuse an earlier test title or edit another record.

## Path A — Local API and Admin Portal, no Firebase required

1. Clone the Backend and restore/build it using the setup guide. Run it in Development with **Firebase__Mode=Local**. Browsing works before an admin key is configured.
2. GET **/health** and expect HTTP 200 with status healthy. Open local Swagger to inspect the API.
3. Import the collection and environment; select MovieApp Local. Send **Movies — first page**, then **Movies — second page**. A fresh migrated database contains 31 rows: 20 then 11 with pageSize 20. There is no separate seed import or dotnet ef startup step.
4. Send **Search movies** with query toy and **Movie details** with movieId 9001. Search initially returns two matching titles; GET Details returns the direct object, not a page envelope. Record the title and lastModified.
5. If not already configured, set your private **Admin:ApiKey** using the setup guide, then restart only your own API process with Local mode still selected. Put the same key into the private local Postman adminKey value.
6. Set demoTitle to your unique title, confirm movieId is 9001, set patchConfirmation to **SEND ONCE**, and click Send on **PATCH demo title — MANUAL ONCE** exactly once. The collection sends only the title field with X-Admin-Key. Do not use a whole-collection run.
7. Expect HTTP 200, your updated title, a newer lastModified, **X-Notification-Status: not_configured** and **X-Notification-Accepted: 0**. **No real FCM was sent in Local mode.** GET the same movie again and compare the persisted title/timestamp. Restarting the same API/database preserves that edit.
8. Open **http://127.0.0.1:5187/admin**, select the dedicated record, enter your key and inspect the edited title. To demonstrate Portal saving, choose a second deliberate title and click Save once. This is a separate PATCH performed by the Portal; in Local mode it still sends no FCM. The page reports the notification status. GET Details again to confirm persistence.

Optional compatibility checks: in Local mode only, the two device requests can use the supplied fake token. Arm deviceConfirmation=SEND ONCE separately for registration and unregistration. Expect 200/registered then 204. These routes do not subscribe to movies, do not send a push, and are not part of Path B.

## Path B — Real FCM and Android reactive update

Use a controlled Firebase project and device. One publish targets **all devices subscribed to movies in that project**, not just the device on your desk. The server makes one application-level send per successful edit, but delivery is not guaranteed, immediate or exactly once.

1. Configure a matching Firebase project and a fresh external Admin credential as described in the setup guide. Confirm Firebase sending permissions. Never use an exposed key.
2. Register Android package **com.example.movieapp**, place the matching client google-services.json in app/, and address the existing **movieapp-19491** Gradle project-ID check if using your own project. Rebuild/install the Bonus Debug app.
3. Start your Backend in Development with **Firebase__Mode=Auto**, the matching project/credential path, and a configured private admin key. Verify health and GET movie 9001 before editing.
4. Connect the device/emulator, apply **adb reverse tcp:5187 tcp:5187**, and keep the PC backend and ADB connection running. The device also needs internet for Firebase.
5. Launch the Bonus app and inspect MovieFirebase Logcat for **Topic subscription succeeded**. The subscription attempt alone is insufficient. The app subscribes at application startup.
6. Find **Demo Edit Sandbox** using Search, or search its current title from GET 9001 if previously renamed. Open Details and note the existing title, synopsis and Updated timestamp. Leave this screen foregrounded and untouched while sending.
7. Start observing existing logs before the edit. In an Android terminal, this optional read-only command filters subscription/worker messages:

   ~~~powershell
   adb logcat -v time MovieFirebase:I WM-WorkerWrapper:I '*:S'
   ~~~

   WorkManager logs are supplementary evidence: a generic success line does not identify the message or prove a Room write. No dedicated receipt/Room tracing is added by this package.
8. Choose **one** edit method for this live test. In Postman, set a unique demoTitle, movieId=9001, your private local adminKey and patchConfirmation=SEND ONCE; click Send once on the PATCH request. Alternatively, use the Portal once and inspect that PATCH's response headers in browser Developer Tools. Do not use both methods for the same test.
9. Check HTTP status and the actual notification headers:

   | Result | Meaning |
   | --- | --- |
   | 200 + accepted + X-Notification-Accepted: 1 | Movie committed; Firebase accepted one topic publish, not one-device receipt |
   | 200 + not_configured + accepted count 0 | Movie committed; no real publish, usually Local mode or missing path |
   | 200 + failed | Movie committed; dispatch failed or outcome is uncertain |
   | 401 / 503 | Authentication rejected / administrator editing not configured |
   | 400 | Invalid edit body; no successful save |
   | Timeout / lost response | Outcome ambiguous; do not retry the PATCH |

   The backend logs the Firebase-returned message ID on acceptance and an exception category on failure. Keep only non-secret evidence; never share request headers containing the key. X-Notification-Failed is not a reliable count of subscriber failures.
10. GET 9001 again in Postman to confirm the persisted title and timestamp. This read does not notify Android. Without touching the still-open Android Details screen, observe whether its title/Updated value changes automatically.
11. Record the result separately: database edit persisted; publish accepted; automatic UI change observed or not observed. Do not navigate away, reopen, manually refresh, restart or reinstall during observation: opening Details and returning to the foreground can independently refetch data.
12. Stop after this one live edit. If the response failed/was ambiguous or the UI did not change, preserve the result and inspect connectivity/configuration before planning another separately deliberate test. Do not automatically resend.

### What the app is designed to do

The data-only hint contains string fields eventType=movie_updated, movieId and updatedAt. MovieMessagingService forwards the data to MovieUpdates. Valid events enqueue network-constrained work unique to the movie. MovieRefreshWorker GETs authoritative backend Details, maps the DTO, and upserts the isolated Demo Room table. The DAO keeps newer cached timestamps over stale responses. Details observes Room through a Flow; Compose displays the new state. Worker failures receive at most three retries.

This is the inspected implementation path, not a claim that every internal stage has been independently traced. The owner observed an automatic Details update after an accepted publish; independent onMessageReceived and Room-write traces were not conclusively recorded.

### Background versus force-stop

Normal backgrounding does not necessarily stop data-message handling, but delivery and WorkManager execution can be deferred by Android power/network restrictions. There is no visible Details screen to observe while backgrounded. When the app returns, it reconciles cached IDs with the backend; an update on return therefore cannot prove that FCM was the trigger.

Android **Force stop** is different from sending the app to the background. Do not use force-stop during this demonstration; it can prevent delivery/background execution until the app is explicitly launched again. Relaunching also triggers reconciliation, which is another source of fresh data. Missed events are not evidence that a visible notification is required: this implementation sends data only and does not create a system notification.

## Reviewer evidence checklist

- Record which path, Firebase mode, and movie title/timestamp you started with.
- Record GET/PATCH status and non-secret notification headers.
- For Path B, record subscription success, accepted publish, and the separate foreground UI observation.
- Report unobserved stages honestly; sender acceptance alone never proves delivery.
- Keep private keys, admin headers, tokens and client configuration out of evidence shared with others.
