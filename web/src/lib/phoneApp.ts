// "BESTI på telefonen": install BESTI like an app (PWA) and push messages (Web Push), all in the browser and free.
// The bell (on every page for logged-in people) starts it with startPhoneApp(); /varsler shows the switches

type InstallPrompt = Event & { prompt: () => Promise<void>; userChoice: Promise<{ outcome: string }> };

const installChangedEvent = "besti-installer-endret";
let installPrompt: InstallPrompt | null = null;
let started = false;

export function startPhoneApp() {
    if (started || typeof window === "undefined") {
        return;
    }
    started = true;
    navigator.serviceWorker?.register("/sw.js").catch(() => {});
    // Chrome / Edge / Samsung on Android and computers: the browser's own "Install" question, saved for our button
    window.addEventListener("beforeinstallprompt", (event) => {
        event.preventDefault();
        installPrompt = event as InstallPrompt;
        window.dispatchEvent(new Event(installChangedEvent));
    });
    window.addEventListener("appinstalled", () => {
        installPrompt = null;
        window.dispatchEvent(new Event(installChangedEvent));
    });
}

// For useSyncExternalStore
export function subscribeToInstall(onChange: () => void) {
    window.addEventListener(installChangedEvent, onChange);
    return () => window.removeEventListener(installChangedEvent, onChange);
}

export function canInstall(): boolean {
    return installPrompt !== null;
}

export async function install() {
    const prompt = installPrompt;
    if (!prompt) {
        return;
    }
    await prompt.prompt();
    await prompt.userChoice.catch(() => null);
    installPrompt = null;
    window.dispatchEvent(new Event(installChangedEvent));
}

// Opened from the home screen icon (then there is nothing to install)
export function isInstalled(): boolean {
    return window.matchMedia("(display-mode: standalone)").matches
        || (navigator as Navigator & { standalone?: boolean }).standalone === true;
}

// iPhone / iPad in Safari: push only after "Legg til på Hjem-skjerm" (Apple's rule)
export function isIosBrowser(): boolean {
    const iPad = navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1;
    return (/iPhone|iPad|iPod/.test(navigator.userAgent) || iPad) && !isInstalled();
}

export function pushSupported(): boolean {
    return "serviceWorker" in navigator && "PushManager" in window && "Notification" in window;
}

export async function currentPushSubscription(): Promise<PushSubscription | null> {
    if (!pushSupported() || Notification.permission !== "granted") {
        return null;
    }
    const registration = await navigator.serviceWorker.ready;
    return registration.pushManager.getSubscription();
}

// Asks for permission (must run inside a tap) and subscribes; null = the person said no
export async function subscribeToPush(publicKey: string): Promise<{ endpoint: string; p256dh: string; auth: string } | null> {
    if ((await Notification.requestPermission()) !== "granted") {
        return null;
    }
    const registration = await navigator.serviceWorker.register("/sw.js").then(() => navigator.serviceWorker.ready);
    const subscription = await registration.pushManager.getSubscription()
        ?? await registration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: fromBase64Url(publicKey),
        });
    const json = subscription.toJSON();
    return { endpoint: subscription.endpoint, p256dh: json.keys?.p256dh ?? "", auth: json.keys?.auth ?? "" };
}

function fromBase64Url(text: string): Uint8Array<ArrayBuffer> {
    const base64 = (text + "=".repeat((4 - (text.length % 4)) % 4)).replace(/-/g, "+").replace(/_/g, "/");
    const bytes = atob(base64);
    const result = new Uint8Array(new ArrayBuffer(bytes.length));
    for (let i = 0; i < bytes.length; i++) {
        result[i] = bytes.charCodeAt(i);
    }
    return result;
}
