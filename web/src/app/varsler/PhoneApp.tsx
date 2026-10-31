"use client";
import { useEffect, useState, useSyncExternalStore } from "react";
import {
    canInstall, currentPushSubscription, install, isIosBrowser, pushSupported, subscribeToInstall, subscribeToPush,
} from "@/lib/phoneApp";
import { removePushAction, savePushAction } from "./actions";
import panelStyles from "../panel/page.module.css";
import LineIcon from "./LineIcon";
import styles from "./page.module.css";

type PushState = "loading" | "on" | "off" | "blocked" | "unsupported";

const noChanges = () => () => {};

// "BESTI på telefonen": install BESTI as an app and get the notifications as push messages, free.
// publicKey = null when push is not set up on the server (then only the install part shows)
export default function PhoneApp({ publicKey }: { publicKey: string | null }) {
    const installable = useSyncExternalStore(subscribeToInstall, canInstall, () => false);
    const iosBrowser = useSyncExternalStore(noChanges, isIosBrowser, () => false);
    const supported = useSyncExternalStore(noChanges, pushSupported, () => false) && publicKey !== null;
    const [state, setState] = useState<PushState>("loading");
    const [error, setError] = useState<string | null>(null);
    const push: PushState = supported ? state : "unsupported";

    // Is this device already on? (asked from the browser, so only after the page has loaded)
    useEffect(() => {
        if (!supported) {
            return;
        }
        currentPushSubscription()
            .then((subscription) => setState(
                Notification.permission === "denied" ? "blocked" : subscription ? "on" : "off"))
            .catch(() => setState("off"));
    }, [supported]);

    async function togglePush() {
        setError(null);
        try {
            if (push === "on") {
                const subscription = await currentPushSubscription();
                if (subscription) {
                    await removePushAction(subscription.endpoint);
                    await subscription.unsubscribe();
                }
                setState("off");
                return;
            }
            const subscription = await subscribeToPush(publicKey!);
            if (!subscription) {
                setState(Notification.permission === "denied" ? "blocked" : "off");
                return;
            }
            const result = await savePushAction(subscription);
            if (!result.ok) {
                setError(result.message);
                return;
            }
            setState("on");
        } catch {
            setError("Varsler kunne ikke slås på i denne nettleseren.");
        }
    }

    if (!installable && !iosBrowser && push === "unsupported") {
        return null;
    }
    return (
        <section className={styles.phone}>
            <h2 className={styles.phoneTitle}><span className={styles.icon}><LineIcon name="phone"/></span>BESTI på telefonen</h2>
            {installable && (
                <p>
                    <button type="button" onClick={install} className={panelStyles.primaryButton}>
                        Installer BESTI som app
                    </button>
                </p>
            )}
            {iosBrowser && (
                <p className={panelStyles.muted}>
                    På iPhone: trykk på Del-knappen <span aria-hidden="true">⬆️</span> nederst i Safari og velg
                    «Legg til på Hjem-skjerm». Åpne BESTI fra ikonet, så kan du slå på varsler her.
                </p>
            )}
            {(push === "on" || push === "off") && (
                <>
                    <button type="button" role="switch" aria-checked={push === "on"} onClick={togglePush}
                            className={styles.row}>
                        <span className={styles.icon}><LineIcon name="bell"/></span>
                        <span className={styles.rowLabel}>Varsler på telefonen</span>
                        <span className={styles.switch} aria-hidden="true"/>
                    </button>
                    <p className={panelStyles.muted}>
                        Nye bestillinger, svar og endringer kommer på låseskjermen, også når BESTI er lukket.
                    </p>
                </>
            )}
            {push === "blocked" && (
                <p className={panelStyles.muted}>
                    Varsler er blokkert for BESTI. Slå dem på i innstillingene til nettleseren eller telefonen.
                </p>
            )}
            {error && <p className={panelStyles.error} role="alert">{error}</p>}
        </section>
    );
}
