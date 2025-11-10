# PwaWeb - Progressive Web Application Sample

A modern Progressive Web App (PWA) built with ASP.NET Core 8.0 demonstrating:
- ✅ **Web Push Notifications** using VAPID protocol
- ✅ **Passkeys (WebAuthn/FIDO2)** for biometric authentication
- ✅ **Service Worker** with offline support and caching
- ✅ **Native Integration** ready for MAUI app wrapping

## Features

### 🔔 Web Push Notifications (VAPID)
- Browser-based push notifications using VAPID keys
- Service worker integration for background notifications
- Support for notification actions and interactions
- Works on Chrome, Firefox, Edge, and other modern browsers

### 🔐 Passkeys (WebAuthn/FIDO2)
- Passwordless authentication using biometrics or security keys
- Support for Windows Hello, Face ID, Touch ID, and hardware keys
- Secure registration and login flows
- In-memory credential storage (replace with database for production)

### 📱 Progressive Web App
- Installable on desktop and mobile devices
- Offline capability with service worker caching
- Responsive design for all screen sizes
- Modern, professional UI

## Prerequisites

- .NET 8.0 SDK
- Modern web browser (Chrome, Edge, Firefox)
- HTTPS for production (required for Service Workers and WebAuthn)

## Setup

### 1. Generate VAPID Keys

You need to generate VAPID keys for Web Push notifications. Install the `web-push` tool:

```bash
npm install -g web-push
web-push generate-vapid-keys
```

### 2. Configure Application

Update `appsettings.json` with your VAPID keys:

```json
{
  "VapidKeys": {
    "PublicKey": "YOUR_PUBLIC_KEY_HERE",
    "PrivateKey": "YOUR_PRIVATE_KEY_HERE",
    "Subject": "mailto:your-email@example.com"
  },
  "Fido2": {
    "ServerDomain": "localhost",
    "ServerName": "PWA Sample Application",
    "Origin": "http://localhost:5000"
  }
}
```

For production, update the `Origin` to your actual domain (must use HTTPS).

### 3. Create PWA Icons (Optional)

The application references icon files in the manifest. You can create them using:
- Online tools like [favicon.io](https://favicon.io/)
- ImageMagick: `convert -size 192x192 xc:#4CAF50 -gravity center -pointsize 96 -fill white -annotate +0+0 "🔔" icon-192.png`
- Or use the provided `create-icons.html` file in wwwroot

Required icons:
- `icon-192.png` (192x192)
- `icon-512.png` (512x512)
- `badge-72.png` (72x72)

## Running the Application

### Development Mode

```bash
cd PwaWeb
dotnet restore
dotnet run
```

The application will be available at `http://localhost:5000`

### Production Mode

For WebAuthn and Service Workers to work properly in production, HTTPS is required:

```bash
dotnet dev-certs https --trust
dotnet run --urls "https://localhost:5001"
```

## Usage

### Register for Push Notifications

1. Open the application in a browser
2. Click "Register for Push" button
3. Grant notification permission when prompted
4. Your device is now registered for push notifications

### Send Test Notification

1. After registering, click "Send Test Notification"
2. You should receive a browser notification

### Register a Passkey

1. Enter a username and display name
2. Click "Register Passkey"
3. Follow your device's biometric authentication prompt
4. Your passkey is now registered

### Login with Passkey

1. Enter the username you registered
2. Click "Login with Passkey"
3. Authenticate using your biometric or security key
4. You're logged in!

## API Endpoints

### Push Notifications

- `GET /api/registerpush/vapidPublicKey` - Get VAPID public key
- `POST /api/registerpush` - Register push subscription
- `GET /api/registerpush/stats` - Get subscription statistics
- `POST /api/webpush/send` - Send push notification

### WebAuthn/FIDO2

- `POST /api/fido/register/options` - Get credential creation options
- `POST /api/fido/register/complete` - Complete credential registration
- `POST /api/fido/login/options` - Get assertion options
- `POST /api/fido/login/complete` - Complete authentication

### Health & Status

- `GET /health` - Application health check

## Project Structure

```
PwaWeb/
├── Controllers/          # API Controllers
│   ├── FidoController.cs        # WebAuthn endpoints
│   ├── RegisterPushController.cs # Push registration
│   └── WebPushController.cs     # Push sending
├── Services/             # Business Logic
│   ├── WebAuthnService.cs       # FIDO2 logic
│   └── WebPushService.cs        # Push notification logic
├── wwwroot/              # Static Files
│   ├── index.html               # Main UI
│   ├── main.js                  # Client-side logic
│   ├── webauthn.js              # WebAuthn helpers
│   ├── service-worker.js        # PWA service worker
│   ├── styles.css               # Styling
│   └── manifest.json            # PWA manifest
├── appsettings.json      # Configuration
└── Program.cs            # Application startup
```

## Architecture

### Backend (ASP.NET Core 8.0)

- **Controllers**: RESTful API endpoints with validation and error handling
- **Services**: Business logic for push notifications and authentication
- **Dependency Injection**: Singleton services registered in Program.cs
- **Logging**: Structured logging using ILogger
- **Configuration**: appsettings.json with environment-specific overrides

### Frontend (Progressive Web App)

- **HTML5**: Semantic markup with modern structure
- **CSS3**: Responsive design with CSS Grid and Flexbox
- **JavaScript (ES6+)**: Modern async/await patterns
- **Service Worker**: Offline caching and push notification handling
- **Web APIs**: Notifications API, Service Worker API, WebAuthn API

## Security Considerations

### Production Deployment

1. **HTTPS Required**: Both Service Workers and WebAuthn require HTTPS
2. **VAPID Keys**: Store private key securely (use Key Vault, environment variables, or secrets manager)
3. **CORS**: Configure appropriate CORS policies for your domain
4. **Credential Storage**: Replace in-memory storage with a secure database
5. **Origin Validation**: Ensure Fido2:Origin matches your actual domain
6. **Rate Limiting**: Implement rate limiting for API endpoints
7. **Input Validation**: All user inputs are validated server-side

## Testing

### Local Testing

The application can be tested locally at `http://localhost:5000`. For full functionality:

1. **Web Push**: Works in Chrome, Edge, Firefox
2. **WebAuthn**: Requires HTTPS in most browsers (use `https://localhost:5001`)
3. **Service Worker**: May need to open DevTools > Application > Service Workers to debug

### Browser Compatibility

- **Chrome/Edge**: Full support for all features
- **Firefox**: Full support for all features  
- **Safari**: WebAuthn support, limited Push API support
- **Mobile Browsers**: Full support on Android Chrome, Safari iOS

## Integration with MAUI

This PWA is designed to be wrapped in a .NET MAUI application. The MAUI app can:

1. Host the PWA in a WebView
2. Register for native FCM push tokens
3. Inject native tokens into the PWA
4. Provide native app store deployment

See the `MauiPwaShell` project in the solution for the MAUI wrapper implementation.

## Troubleshooting

### Push Notifications Not Working

- Ensure VAPID keys are configured correctly
- Check browser console for errors
- Verify notification permission is granted
- Test with HTTPS (required for some browsers)

### WebAuthn Errors

- WebAuthn requires HTTPS (except localhost)
- Ensure Fido2:Origin matches your URL
- Check browser DevTools for specific error messages
- Some browsers require user gesture for credential creation

### Service Worker Not Updating

- Hard refresh (Ctrl+F5 or Cmd+Shift+R)
- Clear browser cache
- Check Application > Service Workers in DevTools
- Update cache version in service-worker.js

## Resources

- [Web Push Protocol](https://web.dev/push-notifications-overview/)
- [WebAuthn Guide](https://webauthn.guide/)
- [Progressive Web Apps](https://web.dev/progressive-web-apps/)
- [Fido2NetLib Documentation](https://github.com/passwordless-lib/fido2-net-lib)
- [Service Worker API](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API)

## License

This is a sample application for demonstration purposes.
