// The small "pling" when a new notification arrives. Made by the browser itself (Web Audio), so there is no
// sound file to download and nothing to pay for. On or off is kept per device (one person may want sound on
// the salon's iPad but not on the phone), and it is on until someone turns it off under Varsler

const storageKey = "besti-varsellyd";
const changedEvent = "besti-varsellyd-endret";
// Two tabs get the same notification at the same moment: only the first one plays
const lastPlayedKey = "besti-varsellyd-sist";

let context: AudioContext | null = null;

export function isSoundOn(): boolean {
    try {
        return localStorage.getItem(storageKey) !== "av";
    } catch {
        return true;
    }
}

export function setSoundOn(on: boolean) {
    try {
        localStorage.setItem(storageKey, on ? "på" : "av");
    } catch {
        // Private mode without storage: the choice lasts until the page is closed
    }
    window.dispatchEvent(new Event(changedEvent));
}

// For useSyncExternalStore: this tab (the switch) and other tabs ("storage") tell about a change
export function subscribeToSoundSetting(onChange: () => void) {
    window.addEventListener(changedEvent, onChange);
    window.addEventListener("storage", onChange);
    return () => {
        window.removeEventListener(changedEvent, onChange);
        window.removeEventListener("storage", onChange);
    };
}

// Browsers only let a page make sound after a click or a key press on it. The bell calls this once, so the
// first tap anywhere on the page wakes the sound up for later (iPhone needs it to happen inside the tap)
export function unlockSoundOnFirstTap() {
    const stop = () => {
        window.removeEventListener("pointerdown", unlock);
        window.removeEventListener("keydown", unlock);
    };
    const unlock = () => {
        getContext()?.resume().catch(() => {});
        stop();
    };
    window.addEventListener("pointerdown", unlock);
    window.addEventListener("keydown", unlock);
    return stop;
}

// A soft two-note chime (E6 then A6), quiet and under half a second. Phones that can also buzz briefly
export function playNotificationSound({ force = false } = {}) {
    if (!force && (!isSoundOn() || playedJustNowInAnotherTab())) {
        return;
    }
    navigator.vibrate?.(60);
    const audio = getContext();
    if (!audio) {
        return;
    }
    audio.resume().catch(() => {});
    const start = audio.currentTime + 0.02;
    note(audio, 1318.5, start, 0.35);
    note(audio, 1760, start + 0.12, 0.45);
}

function note(audio: AudioContext, frequency: number, start: number, length: number) {
    const oscillator = audio.createOscillator();
    const gain = audio.createGain();
    oscillator.type = "sine";
    oscillator.frequency.value = frequency;
    // Fades in fast and out slowly, so it sounds like a little bell and not a beep
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(0.12, start + 0.015);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + length);
    oscillator.connect(gain).connect(audio.destination);
    oscillator.start(start);
    oscillator.stop(start + length + 0.05);
}

function getContext(): AudioContext | null {
    if (!context && typeof AudioContext !== "undefined") {
        context = new AudioContext();
    }
    return context;
}

function playedJustNowInAnotherTab(): boolean {
    try {
        const now = Date.now();
        if (now - Number(localStorage.getItem(lastPlayedKey) ?? 0) < 2000) {
            return true;
        }
        localStorage.setItem(lastPlayedKey, String(now));
    } catch {
        // No storage: just play
    }
    return false;
}
