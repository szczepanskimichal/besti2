"use client";
import { useEffect, useState } from "react";
import { currentPushSubscription, isIosBrowser, pushSupported, subscribeToPush } from "@/lib/phoneApp";
import { savePushAction } from "@/app/varsler/actions";
import LineIcon from "@/app/varsler/LineIcon";
import styles from "./page.module.css";

// Remembered on this device after "Ikke nå", so we ask only once
const NOT_NOW_KEY = "besti-push-ikke-na";

type Ask = "hidden" | "ask" | "ios" | "on";

// "Få varsel på telefonen" right after booking: the browser only allows push after the person says yes,
// so this is the best moment to ask. Hidden when push is already on, blocked, not possible, or after "Ikke nå"
export default function PushOptIn({ publicKey }: { publicKey: string | null }) {
    const [ask, setAsk] = useState<Ask>("hidden");
    const [error, setError] = useState<string | null>(null);

    // Asked from the browser, so only after the page has loaded
    useEffect(() => {
        firstAsk(publicKey).then(setAsk).catch(() => {});
    }, [publicKey]);

    function notNow() {
        try {
            localStorage.setItem(NOT_NOW_KEY, "1");
        } catch {
            // Then we ask again next time, which is fine
        }
        setAsk("hidden");
    }

    async function turnOn() {
        setError(null);
        try {
            const subscription = await subscribeToPush(publicKey!);
            if (!subscription) {
                setAsk("hidden");
                return;
            }
            const result = await savePushAction(subscription);
            if (!result.ok) {
                setError(result.message);
                return;
            }
            setAsk("on");
        } catch {
            setError("Varsler kunne ikke slås på i denne nettleseren.");
        }
    }

    if (ask === "hidden") {
        return null;
    }
    return (
        <section className={styles.push} aria-live="polite">
            <span className={styles.pushIcon}><LineIcon name="bell"/></span>
            <div>
                {ask === "on" ? (
                    <p className={styles.pushTitle}>Varsler er på. Du hører fra oss!</p>
                ) : (
                    <>
                        <p className={styles.pushTitle}>Få varsel på telefonen</p>
                        {ask === "ios" ? (
                            <p className={styles.pushText}>
                                På iPhone: trykk på Del-knappen nederst i Safari og velg «Legg til på Hjem-skjerm».
                                Åpne BESTI fra ikonet, så kan du slå på varsler.
                            </p>
                        ) : (
                            <p className={styles.pushText}>Vi gir beskjed når timen nærmer seg, eller hvis noe endres.</p>
                        )}
                        {error && <p className={styles.pushText} role="alert">{error}</p>}
                        <div className={styles.pushButtons}>
                            {ask === "ask" && (
                                <button type="button" onClick={turnOn} className={styles.pushYes}>Ja, slå på</button>
                            )}
                            <button type="button" onClick={notNow} className={styles.pushNo}>
                                {ask === "ios" ? "Skjønner" : "Ikke nå"}
                            </button>
                        </div>
                    </>
                )}
            </div>
        </section>
    );
}

// What to show when the page opens
async function firstAsk(publicKey: string | null): Promise<Ask> {
    try {
        if (publicKey === null || localStorage.getItem(NOT_NOW_KEY)) {
            return "hidden";
        }
    } catch {
        // No storage (private window): just ask
    }
    if (isIosBrowser()) {
        return "ios";
    }
    if (!pushSupported() || Notification.permission === "denied") {
        return "hidden";
    }
    return (await currentPushSubscription()) ? "hidden" : "ask";
}
