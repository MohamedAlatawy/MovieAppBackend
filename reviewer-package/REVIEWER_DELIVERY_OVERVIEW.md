# MovieApp reviewer delivery overview

The submission has three review areas. **Core Android main** is the original assignment. The **Custom Backend Bonus** is a separate Android branch plus an ASP.NET Core backend. The **Admin Portal Extra Bonus** is a lightweight editor served by that backend.

| Review area | Source | Main features |
| --- | --- | --- |
| Core Android | [main](https://github.com/MohamedAlatawy/MovieApp/tree/main) | TMDb discovery, eligibility verification, Paging 3, verified Room cache, dynamic Search and Details |
| Android Bonus | [feature/bonus-custom-backend](https://github.com/MohamedAlatawy/MovieApp/tree/feature/bonus-custom-backend) | Same screens/navigation using the active custom API, isolated demo Room cache, Firebase topic hints and WorkManager refetch |
| Backend Bonus | [MovieAppBackend](https://github.com/MohamedAlatawy/MovieAppBackend) | .NET 10, EF Core/SQLite, 31 synthetic records, paging/search/details, protected PATCH, topic movies, xUnit tests |
| Admin Portal Extra Bonus | [Admin/index.html](https://github.com/MohamedAlatawy/MovieAppBackend/blob/main/src/MovieApp.Api/Admin/index.html) | Movie selection and protected editing inside the same Development backend |

The Bonus is **not merged into main**. Synthetic demo records are not TMDb eligibility evidence and must remain isolated from the verified cache.

After starting the backend locally, open [Admin Portal](http://127.0.0.1:5187/admin) or [Swagger](http://127.0.0.1:5187/swagger). These are localhost pages on the reviewer's computer, not publicly hosted services. A GitHub source link does not run the API.

## Recommended review order

1. Read [BONUS_SETUP_GUIDE.md](BONUS_SETUP_GUIDE.md), including the Android Firebase project-ID guard if using your own project.
2. Import [MovieApp_Bonus.postman_collection.json](MovieApp_Bonus.postman_collection.json) and [MovieApp_Local.postman_environment.json](MovieApp_Local.postman_environment.json). Keep the exported administrator key empty.
3. Follow [Path A in BONUS_DEMO_WALKTHROUGH.md](BONUS_DEMO_WALKTHROUGH.md#path-a--local-api-and-admin-portal-no-firebase-required): no Firebase project is needed for the API/Portal demonstration.
4. Optionally follow [Path B](BONUS_DEMO_WALKTHROUGH.md#path-b--real-fcm-and-android-reactive-update) with a controlled Firebase project and connected Android device.
5. Review the source/tests and distinguish committed API data, Firebase acceptance and observed UI behavior.

## Completion and evidence

Backend API, SQLite, protected PATCH and Admin Portal are implemented. Android backend integration is implemented and live-tested. Topic movies is configured; subscription and sender acceptance were verified. The owner observed a successful automatic Android Details update after an accepted publish. Independent onMessageReceived and Room-write traces were not conclusively recorded.

This package documents how a reviewer can repeat the demonstration; package generation did not execute an edit or send FCM. There are no private credentials or database files in the package. The current Android build pins Firebase project movieapp-19491, so a separate project requires the documented local adjustment in the reviewer's Bonus clone.
