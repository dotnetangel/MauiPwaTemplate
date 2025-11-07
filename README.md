
# MauiPwaWrapperSolution

A sample .NET 8 solution demonstrating:
- An ASP.NET Core **PWA** (`PwaWeb`) with **VAPID web push** support (service worker + subscription API).
- A **.NET MAUI** wrapper (`MauiPwaShell`) that hosts the PWA in a `WebView`, registers for native FCM push tokens, sends tokens to the backend, and injects the native token into the PWA when it loads.

---

## Projects

### PwaWeb
- Minimal ASP.NET Core static-site PWA with:
  - `wwwroot/index.html`, `service-worker.js`, `main.js`
  - Endpoints:
    - `GET /api/registerpush/vapidPublicKey` — returns the VAPID public key.
    - `POST /api/registerpush` — accepts browser `PushSubscription` to store.
    - `POST /api/webpush/send` — sends a notification to stored subscriptions.
- Configuration: `appsettings.json` contains `VapidKeys.PublicKey` and `VapidKeys.PrivateKey`.
- NuGet: `Lib.Net.Http.WebPush` is used for server-side push delivery.

### MauiPwaShell
- .NET MAUI app that:
  - Hosts the PWA in a `WebView`.
  - Initializes Firebase Cloud Messaging via `Plugin.Firebase.CloudMessaging`.
  - Retrieves FCM token, posts it to backend, injects it into the web app via JavaScript.

---

## How to run (Development)

### 1) Run the PWA (web)
1. Open a terminal and `cd PwaWeb`.
2. Restore packages: `dotnet restore`.
3. Run the app: `dotnet run --urls "http://localhost:5000"`.
4. Open `http://localhost:5000` in Chrome or Edge.
5. Click **Register for push (web)** to register using VAPID keys (you must replace keys in `appsettings.json` or provide them in `wwwroot/vapid-public.txt` and `vapid-private.txt`).

> Note: For testing locally, you may need to serve over HTTPS to get push working in some browsers. Consider using `dotnet dev-certs https --trust` and run with `https://localhost:5001`.

### 2) Run the MAUI app (Android)
1. Place your real `google-services.json` file into `MauiPwaShell/Platforms/Android/google-services.json`.
2. Open solution in Visual Studio with MAUI workloads or use `dotnet` CLI.
3. Adjust `MauiPwaShell/MainPage.xaml.cs` `PwaUrl` constant if your host mapping differs (for Android emulator use `http://10.0.2.2:5000/` to reach host machine `localhost`).
4. Run on emulator or device.
5. The MAUI app will initialize Firebase, obtain an FCM token, POST it to `http://10.0.2.2:5000/api/registerpush` and inject the token into the WebView.

### 3) Run the MAUI app (iOS)
1. Place your `GoogleService-Info.plist` into `MauiPwaShell/Platforms/iOS/`.
2. Enable Push Notifications and Background Modes (Remote notifications) for the iOS app in your App ID and provisioning profile.
3. Build and run from a macOS machine with Xcode installed.
4. MAUI will request permission for notifications on iOS; once granted, the FCM token will be sent to your backend and injected into the WebView.

---

## VAPID Keys

Generate VAPID keys (example using Node.js `web-push`):
```bash
npm install -g web-push
web-push generate-vapid-keys --json > vapid.json
```
Copy the keys into `PwaWeb/appsettings.json` (or place them into `wwwroot/vapid-public.txt` & `vapid-private.txt`).

---

## Testing Push Delivery

### Send browser push:
After a browser subscribes:
```bash
curl -X POST http://localhost:5000/api/webpush/send -H "Content-Type: application/json" \
 -d '{"title":"Hello","body":"This is a test push from server"}'
```

### Send native push:
Use Firebase Console to target the FCM token your MAUI app logs, or use your server to call FCM with stored tokens.

---

## Notes & Troubleshooting

- Browsers often require HTTPS for service workers and push. Use `dotnet dev-certs` or ngrok for tunneling.
- Android emulator uses `10.0.2.2` to reach host `localhost`. Adjust URLs accordingly.
- For production iOS push, upload your APNs key to Firebase and set `aps-environment` to `production` in entitlements.
- Securely store the VAPID private key; rotate periodically and protect access.

---

## Placeholder Firebase files

Placeholders are included:
- `MauiPwaShell/Platforms/Android/google-services.json`
- `MauiPwaShell/Platforms/iOS/GoogleService-Info.plist`

Replace them with your real Firebase files before running MAUI builds that require Firebase.

