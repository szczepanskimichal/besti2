"use client";
import { useSyncExternalStore } from "react";
import { isSoundOn, playNotificationSound, setSoundOn, subscribeToSoundSetting } from "@/lib/notificationSound";
import styles from "./page.module.css";

// "Lyd ved nye varsler" on / off for this device. Turning it on plays the sound once, so you hear what it is
export default function SoundToggle() {
    // Known only in the browser (the server renders "on", the default)
    const on = useSyncExternalStore(subscribeToSoundSetting, isSoundOn, () => true);

    function toggle() {
        setSoundOn(!on);
        if (!on) {
            playNotificationSound({ force: true });
        }
    }

    return (
        <button type="button" role="switch" aria-checked={on} onClick={toggle} className={styles.soundToggle}>
            <span aria-hidden="true">{on ? "🔊" : "🔇"}</span>
            Lyd ved nye varsler: <strong>{on ? "På" : "Av"}</strong>
        </button>
    );
}
