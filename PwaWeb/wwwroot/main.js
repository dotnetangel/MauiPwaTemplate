// main.js: VAPID Web Push + WebAuthn integration (client)

// Utility functions for VAPID
async function getVapidPublicKey() {
    const resp = await fetch('/api/registerpush/vapidPublicKey');
    if (!resp.ok) throw new Error('Failed to get VAPID key');
    const data = await resp.json();
    return data.key;
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = atob(base64);
    const outputArray = new Uint8Array(rawData.length);
    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

async function registerServiceWorker() {
    if ('serviceWorker' in navigator) {
        return await navigator.serviceWorker.register('/service-worker.js');
    }
    throw new Error('Service workers are not supported in this browser');
}

async function subscribeForPush(reg, vapidPublicKey) {
    return await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
    });
}

async function sendSubscriptionToServer(sub) {
    const resp = await fetch('/api/registerpush', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(sub)
    });
    if (!resp.ok) throw new Error('Failed to register subscription');
    return await resp.json();
}

// UI Helper functions
function showError(elementId, message) {
    const el = document.getElementById(elementId);
    el.textContent = message;
    el.style.display = 'block';
    setTimeout(() => el.style.display = 'none', 5000);
}

function showSuccess(elementId, message) {
    const el = document.getElementById(elementId);
    el.textContent = message;
    el.style.display = 'block';
    setTimeout(() => el.style.display = 'none', 5000);
}

function updatePushStatus(text, icon = '⚪') {
    document.getElementById('pushStatusText').textContent = text;
    document.querySelector('.status-icon').textContent = icon;
}

function logPasskey(message, isError = false) {
    const log = document.getElementById('log');
    const timestamp = new Date().toLocaleTimeString();
    const prefix = isError ? '❌' : '✅';
    log.textContent += `[${timestamp}] ${prefix} ${message}\n`;
    log.scrollTop = log.scrollHeight;
}

// Web Push Registration
document.getElementById('registerBtn').addEventListener('click', async () => {
    const btn = document.getElementById('registerBtn');
    btn.disabled = true;
    
    try {
        updatePushStatus('Registering...', '⏳');
        
        // Check if notifications are supported
        if (!('Notification' in window)) {
            throw new Error('Notifications are not supported in this browser');
        }
        
        // Request notification permission
        const permission = await Notification.requestPermission();
        if (permission !== 'granted') {
            throw new Error('Notification permission denied');
        }
        
        const reg = await registerServiceWorker();
        const vapidKey = await getVapidPublicKey();
        const sub = await subscribeForPush(reg, vapidKey);
        await sendSubscriptionToServer(sub);
        
        updatePushStatus('Push notifications active', '🔔');
        showSuccess('pushSuccess', 'Successfully registered for push notifications!');
    } catch (e) {
        console.error('Push registration error:', e);
        updatePushStatus('Registration failed', '❌');
        showError('pushError', `Error: ${e.message}`);
    } finally {
        btn.disabled = false;
    }
});

// Send Test Push
document.getElementById('sendTestBtn').addEventListener('click', async () => {
    const btn = document.getElementById('sendTestBtn');
    btn.disabled = true;
    
    try {
        const resp = await fetch('/api/webpush/send', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                title: 'Test Notification',
                body: 'Hello from the PWA! This is a test push notification.'
            })
        });
        
        if (!resp.ok) throw new Error('Failed to send notification');
        
        showSuccess('pushSuccess', 'Test notification sent successfully!');
    } catch (e) {
        console.error('Send push error:', e);
        showError('pushError', `Error: ${e.message}`);
    } finally {
        btn.disabled = false;
    }
});

// WebAuthn - Register Passkey
document.getElementById('registerPasskey').addEventListener('click', async () => {
    const btn = document.getElementById('registerPasskey');
    btn.disabled = true;
    
    try {
        const username = document.getElementById('username').value.trim();
        const displayName = document.getElementById('displayName').value.trim();
        
        if (!username) {
            throw new Error('Username is required');
        }
        
        logPasskey(`Starting passkey registration for: ${username}`);
        
        const resp = await fetch('/api/fido/register/options', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, displayName: displayName || username })
        });
        
        if (!resp.ok) {
            const error = await resp.json();
            throw new Error(error.error || 'Failed to get registration options');
        }
        
        const options = await resp.json();
        
        // Convert base64url to ArrayBuffer
        options.publicKey.challenge = base64UrlToBuffer(options.publicKey.challenge);
        options.publicKey.user.id = base64UrlToBuffer(options.publicKey.user.id);
        if (options.publicKey.excludeCredentials) {
            options.publicKey.excludeCredentials = options.publicKey.excludeCredentials.map(c => ({
                ...c,
                id: base64UrlToBuffer(c.id)
            }));
        }
        
        logPasskey('Creating credential...');
        const attestation = await navigator.credentials.create(options);
        
        if (!attestation) {
            throw new Error('Failed to create credential');
        }
        
        const attResp = {
            id: attestation.id,
            rawId: bufferToBase64Url(attestation.rawId),
            response: {
                clientDataJSON: bufferToBase64Url(attestation.response.clientDataJSON),
                attestationObject: bufferToBase64Url(attestation.response.attestationObject)
            },
            type: attestation.type
        };
        
        const completeResp = await fetch('/api/fido/register/complete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Attestation: attResp, Options: options })
        });
        
        if (!completeResp.ok) {
            const error = await completeResp.json();
            throw new Error(error.error || 'Failed to complete registration');
        }
        
        logPasskey('Passkey registered successfully!');
        showSuccess('passkeySuccess', 'Passkey registered successfully!');
    } catch (e) {
        console.error('Passkey registration error:', e);
        logPasskey(`Registration failed: ${e.message}`, true);
        showError('passkeyError', `Error: ${e.message}`);
    } finally {
        btn.disabled = false;
    }
});

// WebAuthn - Login with Passkey
document.getElementById('loginPasskey').addEventListener('click', async () => {
    const btn = document.getElementById('loginPasskey');
    btn.disabled = true;
    
    try {
        const username = document.getElementById('loginUser').value.trim();
        
        if (!username) {
            throw new Error('Username is required');
        }
        
        logPasskey(`Starting passkey login for: ${username}`);
        
        const resp = await fetch('/api/fido/login/options', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username })
        });
        
        if (!resp.ok) {
            const error = await resp.json();
            throw new Error(error.error || 'Failed to get login options');
        }
        
        const options = await resp.json();
        
        options.publicKey.challenge = base64UrlToBuffer(options.publicKey.challenge);
        if (options.publicKey.allowCredentials) {
            options.publicKey.allowCredentials = options.publicKey.allowCredentials.map(c => ({
                ...c,
                id: base64UrlToBuffer(c.id)
            }));
        }
        
        logPasskey('Requesting authentication...');
        const assertion = await navigator.credentials.get(options);
        
        if (!assertion) {
            throw new Error('Failed to get credential');
        }
        
        const assertionResp = {
            id: assertion.id,
            rawId: bufferToBase64Url(assertion.rawId),
            response: {
                clientDataJSON: bufferToBase64Url(assertion.response.clientDataJSON),
                authenticatorData: bufferToBase64Url(assertion.response.authenticatorData),
                signature: bufferToBase64Url(assertion.response.signature),
                userHandle: assertion.response.userHandle ? bufferToBase64Url(assertion.response.userHandle) : null
            },
            type: assertion.type
        };
        
        const completeResp = await fetch('/api/fido/login/complete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Assertion: assertionResp, Options: options, Username: username })
        });
        
        if (!completeResp.ok) {
            const error = await completeResp.json();
            throw new Error(error.error || 'Authentication failed');
        }
        
        logPasskey('Login successful!');
        showSuccess('passkeySuccess', '🎉 Successfully logged in with passkey!');
    } catch (e) {
        console.error('Passkey login error:', e);
        logPasskey(`Login failed: ${e.message}`, true);
        showError('passkeyError', `Error: ${e.message}`);
    } finally {
        btn.disabled = false;
    }
});

// Check if already registered on load
(async () => {
    if ('serviceWorker' in navigator && 'PushManager' in window) {
        const registration = await navigator.serviceWorker.getRegistration();
        if (registration) {
            const subscription = await registration.pushManager.getSubscription();
            if (subscription) {
                updatePushStatus('Push notifications active', '🔔');
            }
        }
    }
})();
