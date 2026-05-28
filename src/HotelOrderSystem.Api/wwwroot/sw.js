const CACHE_NAME = "hotel-order-shell-v2";
const ASSETS = ["/", "/index.html", "/assets/css/app.css", "/assets/js/app.js", "/manifest.webmanifest"];

self.addEventListener("install", event => {
  event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(ASSETS)).then(() => self.skipWaiting()));
});

self.addEventListener("activate", event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key)))).then(() => self.clients.claim()));
});

self.addEventListener("fetch", event => {
  const url = new URL(event.request.url);
  if (url.pathname.startsWith("/api/") || url.pathname.startsWith("/hubs/")) return;
  event.respondWith(caches.match(event.request).then(cached => cached || fetch(event.request)));
});

importScripts("https://www.gstatic.com/firebasejs/10.7.0/firebase-app-compat.js");
importScripts("https://www.gstatic.com/firebasejs/10.7.0/firebase-messaging-compat.js");

firebase.initializeApp({
  apiKey: "AIzaSyAHI21qVUztDZ9RuMt8H1VHD0c7hiwy4DQ",
  authDomain: "hotelordersystem-b9b60.firebaseapp.com",
  projectId: "hotelordersystem-b9b60",
  storageBucket: "hotelordersystem-b9b60.firebasestorage.app",
  messagingSenderId: "739295170683",
  appId: "1:739295170683:web:989a93e00a497e051852de",
  measurementId: "G-EDS6J6LXBM"
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage((payload) => {
  if (!payload.notification) return;
  const { title, body } = payload.notification;
  const options = {
    body,
    icon: "/assets/img/icon-192.svg",
    badge: "/assets/img/icon-192.svg",
    data: payload.data || {},
    requireInteraction: true,
    tag: "hotel-order-notification"
  };
  self.registration.showNotification(title || "New Order", options);
});

self.addEventListener("notificationclick", event => {
  event.notification.close();
  event.waitUntil(clients.matchAll({ type: "window" }).then(windowClients => {
    for (const client of windowClients) {
      if (client.url === "/" && "focus" in client) return client.focus();
    }
    return clients.openWindow("/");
  }));
});