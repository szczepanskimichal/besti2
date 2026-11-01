"use client";
import { useActionState, type CSSProperties } from "react";
import { formatDay } from "@/lib/format";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { joinWaitlistAction, type WaitlistFormState } from "./actions";
import styles from "./page.module.css";

// The same spark colours as the header bell
const SPARK_COLOURS = ["#ffffff", "#bff8e8", "#72d6bd", "#8fc8ff", "#c9bfff"];

// "Si fra hvis det blir ledig": the customer picks one of the days in the strip and gets a message in the bell
// and on the phone as soon as someone cancels. Nobody else sees the list
export default function WaitlistForm({ serviceId, employeeId, days, selectedDay }: {
    serviceId: string;
    employeeId: string | null;
    days: string[];
    selectedDay: string | undefined;
}) {
    const [state, action] = useActionState<WaitlistFormState, FormData>(joinWaitlistAction, {});
    const date = state.values?.date ?? selectedDay ?? days[0];

    return (
        <section className={styles.waitlist}>
            {/* Ada 2026-11-01: a glass card like the calendar, a big crystal bell in a glowing ring on top */}
            {/* After "Si fra hvis det blir ledig": the same ring as the header bell (shake, clapper, waves, sparks) */}
            <div className={state.saved ? `${styles.waitlistBell} ${styles.waitlistRinging}` : styles.waitlistBell}
                 aria-hidden="true">
                <span className={styles.waitlistBellIcon}>
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon */}
                    <img src="/ikoner/bjelle.svg" alt=""/>
                    <span className={styles.waitlistClapper}/>
                </span>
                {state.saved && (
                    <>
                        <span className={`${styles.waitlistWave} ${styles.waitlistWaveLeft}`}/>
                        <span className={`${styles.waitlistWave} ${styles.waitlistWaveRight}`}/>
                        <span className={`${styles.waitlistWave} ${styles.waitlistWaveLeft} ${styles.waitlistWaveOuter}`}/>
                        <span className={`${styles.waitlistWave} ${styles.waitlistWaveRight} ${styles.waitlistWaveOuter}`}/>
                        <span className={styles.waitlistSparks}>
                            {Array.from({ length: 10 }, (_, k) => (
                                <i key={k} style={{
                                    "--a": `${k * 36 + 15}deg`,
                                    "--d": `${70 + (k % 2) * 24}px`,
                                    color: SPARK_COLOURS[k % 5],
                                } as CSSProperties}>{k % 2 ? "✦" : "✧"}</i>
                            ))}
                        </span>
                    </>
                )}
            </div>
            <h2 className={styles.waitlistTitle}>{state.saved ? "Du er på ventelisten!" : "Passer ingen av tidene?"}</h2>
            {state.saved ? (
                <p className={styles.waitlistDone} role="status">
                    Vi sier fra med en gang noe blir ledig {formatDay(date)}, i varslene og på telefonen. Du finner den i Min BESTI.
                </p>
            ) : (
                <form action={action} className={styles.waitlistForm}>
                    <p className={styles.waitlistLead}>Få beskjed med en gang noen avbestiller. Først til mølla!</p>
                    {state.error && <p className={styles.error} role="alert">{state.error}</p>}
                    <input type="hidden" name="serviceId" value={serviceId}/>
                    <input type="hidden" name="employeeId" value={employeeId ?? ""}/>
                    <div className={styles.waitlistRow}>
                        <label className={styles.waitlistField}>
                            Hvilken dag?
                            <select name="date" defaultValue={date}>
                                {days.map((day) => <option key={day} value={day}>{formatDay(day)}</option>)}
                            </select>
                        </label>
                        <SubmitButton className={`${styles.primaryButton} ${styles.waitlistButton}`} pendingText="Lagrer…">
                            Si fra hvis det blir ledig
                        </SubmitButton>
                    </div>
                </form>
            )}
        </section>
    );
}
