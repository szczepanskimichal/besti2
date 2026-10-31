"use client";
import { useSyncExternalStore } from "react";
import { isSoundOn, playNotificationSound, setSoundOn, subscribeToSoundSetting } from "@/lib/notificationSound";
import LineIcon from "./LineIcon";
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
        <button type="button" role="switch" aria-checked={on} onClick={toggle} className={styles.row}>
            <span className={styles.icon}><LineIcon name="speaker"/></span>
            <span className={styles.rowLabel}>Lyd ved nye varsler</span>
            <span className={styles.switch} aria-hidden="true"/>
        </button>
    );
}
