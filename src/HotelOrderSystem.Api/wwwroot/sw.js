const CACHE_NAME = "hotel-order-shell-v4";
const ASSETS = ["/", "/index.html", "/assets/css/app.css?v=4", "/assets/js/app.js", "/manifest.webmanifest"];

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