self.addEventListener('install', e => self.skipWaiting());
self.addEventListener('activate', e => clients.claim());
self.addEventListener('push', e => { const data = e.data?.json() || { title: 'PWA Push', body: 'Hello!' }; e.waitUntil(self.registration.showNotification(data.title, { body: data.body })); });
self.addEventListener('notificationclick', e => { e.notification.close(); e.waitUntil(clients.openWindow('/')); });
