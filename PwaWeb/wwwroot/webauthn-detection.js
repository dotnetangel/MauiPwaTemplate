// webauthn-detection.js - WebAuthn availability detection and WebView handling

/**
 * Detects if we're running in a WebView context
 */
function isWebView() {
    const userAgent = navigator.userAgent || navigator.vendor || window.opera;
    
    // Check for MAUI WebView
    if (userAgent.includes('MauiPwaShell')) {
        return { isWebView: true, type: 'MAUI' };
    }
    
    // Check for common WebView indicators
    const webViewPatterns = [
        /wv/,                    // Android WebView
        /WebView/,               // Generic WebView
        /(iPhone|iPod|iPad).*AppleWebKit(?!.*Safari)/i  // iOS WebView
    ];
    
    for (const pattern of webViewPatterns) {
        if (pattern.test(userAgent)) {
            return { isWebView: true, type: 'Generic' };
        }
    }
    
    return { isWebView: false, type: 'Browser' };
}

/**
 * Checks if WebAuthn is available and functional
 */
async function checkWebAuthnAvailability() {
    const context = isWebView();
    
    const result = {
        available: false,
        context: context.type,
        isWebView: context.isWebView,
        features: {
            publicKeyCredential: false,
            conditionalMediation: false,
            userVerifyingPlatformAuthenticator: false
        },
        message: ''
    };
    
    // Check if PublicKeyCredential is available
    if (!window.PublicKeyCredential) {
        result.message = 'WebAuthn is not supported in this browser/WebView';
        return result;
    }
    
    result.features.publicKeyCredential = true;
    
    try {
        // Check for platform authenticator
        if (PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable) {
            result.features.userVerifyingPlatformAuthenticator = 
                await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
        }
        
        // Check for conditional mediation (autofill)
        if (PublicKeyCredential.isConditionalMediationAvailable) {
            result.features.conditionalMediation = 
                await PublicKeyCredential.isConditionalMediationAvailable();
        }
        
        result.available = true;
        result.message = context.isWebView 
            ? `WebAuthn is available in ${context.type} WebView` 
            : 'WebAuthn is fully supported';
            
    } catch (error) {
        result.message = `WebAuthn check failed: ${error.message}`;
        console.error('WebAuthn availability check error:', error);
    }
    
    return result;
}

/**
 * Gets platform-specific WebAuthn information
 */
function getWebAuthnInfo() {
    const platform = navigator.platform || 'Unknown';
    const userAgent = navigator.userAgent || 'Unknown';
    
    let platformType = 'Unknown';
    let minVersion = 'N/A';
    let recommendations = [];
    
    if (/Android/i.test(userAgent)) {
        platformType = 'Android';
        minVersion = 'Android 9.0 (API 28)';
        recommendations = [
            'Ensure Google Play Services is installed and updated',
            'Device must support biometric authentication (fingerprint/face)',
            'WebView must have JavaScript and DOM storage enabled'
        ];
    } else if (/iPhone|iPad|iPod/i.test(userAgent)) {
        platformType = 'iOS';
        minVersion = 'iOS 14.0';
        recommendations = [
            'Face ID or Touch ID must be configured',
            'iOS 15.0+ recommended for best compatibility',
            'Test on physical device, not just simulator'
        ];
    } else if (/Win/i.test(userAgent)) {
        platformType = 'Windows';
        minVersion = 'Windows 10 with WebView2';
        recommendations = [
            'Windows Hello must be configured',
            'WebView2 runtime required'
        ];
    } else if (/Mac/i.test(userAgent)) {
        platformType = 'macOS';
        minVersion = 'macOS 11.0';
        recommendations = [
            'Touch ID must be configured',
            'May have limited support in Catalyst apps'
        ];
    }
    
    return {
        platform: platformType,
        minVersion: minVersion,
        userAgent: userAgent,
        recommendations: recommendations
    };
}

/**
 * Displays WebAuthn availability information in the UI
 */
async function displayWebAuthnStatus(elementId = 'webauthn-status') {
    const statusElement = document.getElementById(elementId);
    if (!statusElement) return;
    
    const availability = await checkWebAuthnAvailability();
    const info = getWebAuthnInfo();
    
    let statusHTML = '<div class="webauthn-status">';
    
    if (availability.available) {
        statusHTML += `
            <div style="color: #28a745; font-weight: bold; margin-bottom: 10px;">
                ✅ WebAuthn Available
            </div>
            <div style="font-size: 0.9em; color: #666;">
                <strong>Context:</strong> ${availability.context}<br>
                <strong>Platform:</strong> ${info.platform}<br>
                <strong>Platform Authenticator:</strong> ${availability.features.userVerifyingPlatformAuthenticator ? 'Yes' : 'No'}<br>
                <strong>Conditional Mediation:</strong> ${availability.features.conditionalMediation ? 'Yes' : 'No'}
            </div>
        `;
        
        if (availability.isWebView) {
            statusHTML += `
                <div style="margin-top: 10px; padding: 10px; background: #fff3cd; border-left: 3px solid #ffc107; font-size: 0.85em;">
                    <strong>⚠️ WebView Mode</strong><br>
                    ${availability.message}
                </div>
            `;
        }
    } else {
        statusHTML += `
            <div style="color: #dc3545; font-weight: bold; margin-bottom: 10px;">
                ❌ WebAuthn Not Available
            </div>
            <div style="font-size: 0.9em; color: #666;">
                <strong>Context:</strong> ${availability.context}<br>
                <strong>Platform:</strong> ${info.platform}<br>
                <strong>Message:</strong> ${availability.message}
            </div>
        `;
        
        if (info.recommendations.length > 0) {
            statusHTML += `
                <div style="margin-top: 10px; padding: 10px; background: #f8d7da; border-left: 3px solid #dc3545; font-size: 0.85em;">
                    <strong>Requirements:</strong>
                    <ul style="margin: 5px 0 0 20px; padding: 0;">
                        ${info.recommendations.map(r => `<li>${r}</li>`).join('')}
                    </ul>
                </div>
            `;
        }
    }
    
    statusHTML += '</div>';
    statusElement.innerHTML = statusHTML;
    
    // Log to console for debugging
    console.log('WebAuthn Availability:', availability);
    console.log('Platform Info:', info);
    
    return availability;
}

/**
 * Initialize WebAuthn detection on page load
 */
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        displayWebAuthnStatus().catch(err => 
            console.error('Failed to display WebAuthn status:', err)
        );
    });
} else {
    displayWebAuthnStatus().catch(err => 
        console.error('Failed to display WebAuthn status:', err)
    );
}

// Export functions for use in other scripts
window.webAuthnDetection = {
    isWebView,
    checkWebAuthnAvailability,
    getWebAuthnInfo,
    displayWebAuthnStatus
};
