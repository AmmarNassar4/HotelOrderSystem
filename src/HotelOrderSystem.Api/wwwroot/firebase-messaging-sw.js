// firebase-messaging-sw.js — Auto-loaded by Firebase messaging SDK
importScripts("https://www.gstatic.com/firebasejs/10.7.0/firebase-app-compat.js");
importScripts("https://www.gstatic.com/firebasejs/10.7.0/firebase-messaging-compat.js");

firebase.initializeApp({
  apiKey: "AIzaSyAHI21qVUztDZ9RuMt8H1VHD0c7hiwy4DQ",
  authDomain: "hotelordersystem-b9b60.firebaseapp.com",
  projectId: "hotelordersystem-b9b60",
  storageBucket: "hotelordersystem-b9b60.firebasestorage.app",
  messagingSenderId: "739295170683",
  appId: "1:739295170683:web:989a93e00a497e051852de"
});

var messaging = firebase.messaging();

messaging.onBackgroundMessage(function(payload) {
  console.log("[FCM-SW] Background message:", payload);
  if (!payload.notification) return;
  var notificationTitle = payload.notification.title || "Hotel Order";
  var notificationOptions = {
    body: payload.notification.body || "You have a new notification",
    icon: "/assets/img/icon-192.png",
    badge: "/assets/img/icon-192.png",
    data: payload.data || {},
    requireInteraction: true,
    tag: "hotel-order-notification"
  };
  self.registration.showNotification(notificationTitle, notificationOptions);
});

self.addEventListener("notificationclick", function(event) {
  event.notification.close();
  event.waitUntil(clients.matchAll({ type: "window" }).then(function(clientList) {
    for (var i = 0; i < clientList.length; i++) {
      var client = clientList[i];
      if (client.url == "/" && "focus" in client) return client.focus();
    }
    return clients.openWindow("/");
  }));
});