// BESTI's service worker: shows the bell's notifications as push messages ("Varsler på telefonen"), also when
// BESTI is closed, and opens the right page on a tap. It does not cache pages: BESTI always shows live times
self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", (event) => event.waitUntil(self.clients.claim()));

self.addEventListener("push", (event) => {
    let data = {};
    try {
        data = event.data ? event.data.json() : {};
    } catch {
        data = { body: event.data ? event.data.text() : "" };
    }
    event.waitUntil(self.registration.showNotification(data.title || "BESTI", {
        body: data.body || "",
        icon: "/logo/besti-ikon-v2-192.png",
        badge: "/logo/besti-ikon-v2-192.png",
        lang: "nb",
        // The same notification twice (two devices of one business) replaces itself instead of piling up
        tag: data.tag,
        data: { url: data.url || "/varsler" },
    }));
});

// A tap: an open BESTI window goes to the page, otherwise a new one opens
self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    const url = new URL(event.notification.data?.url || "/varsler", self.location.origin).href;
    event.waitUntil((async () => {
        const windows = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
        for (const client of windows) {
            if (new URL(client.url).origin === self.location.origin) {
                await client.focus();
                try {
                    await client.navigate(url);
                    return;
                } catch {
                    break; // a window not controlled by us yet: open a new one instead
                }
            }
        }
        await self.clients.openWindow(url);
    })());
});
