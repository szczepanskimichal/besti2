"use client";
import { Fragment, useActionState, useState, type ChangeEvent, type CSSProperties } from "react";
import { createReviewAction } from "../actions";
import { MaxSendBytes, shrinkImage } from "../../panel/profil/LogoUploadForm";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import panelStyles from "../../panel/page.module.css";
import styles from "./page.module.css";

const SPARK_COLOURS = ["#ffffff", "#bff8e8", "#72d6bd", "#8fc8ff", "#c9bfff"];

// Stars and comment for one visit; the API's own message (e.g. already reviewed) shows here when it fails
export default function ReviewForm({ bookingId }: { bookingId: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(createReviewAction, {});
    const [preparing, setPreparing] = useState(false);
    const [tooBig, setTooBig] = useState(false);
    const [photoName, setPhotoName] = useState("");
    // The chosen number of stars; burst starts the light-up animation again on every new choice
    const [chosen, setChosen] = useState(Number(state.values?.stars ?? 0));
    const [burst, setBurst] = useState(0);

    // A phone photo is shrunk in the browser first, so it fits in one request
    async function handlePhoto(event: ChangeEvent<HTMLInputElement>) {
        const input = event.currentTarget;
        const file = input.files?.[0];
        setPhotoName(file?.name ?? "");
        if (!file) {
            setTooBig(false);
            return;
        }
        setPreparing(true);
        try {
            const smaller = await shrinkImage(file);
            const transfer = new DataTransfer();
            transfer.items.add(smaller);
            input.files = transfer.files;
        } catch {
            // Browser could not read the file; send the original and let the API answer
        } finally {
            setPreparing(false);
            setTooBig((input.files?.[0]?.size ?? 0) > MaxSendBytes);
        }
    }

    return (
        <form action={formAction} className={styles.form}>
            {state.error && (
                <p className={panelStyles.error} role="alert">
                    {state.error}
                </p>
            )}
            <input type="hidden" name="bookingId" value={bookingId}/>
            <p className={styles.label}>Gi vurdering</p>
            {/* Stars 1..5 for keyboard and screen readers; the chosen one and those before it light up one after another */}
            <div className={styles.stars} role="radiogroup" aria-label="Antall stjerner">
                {[1, 2, 3, 4, 5].map((n) => {
                    const lit = n <= chosen;
                    const delay = { "--dl": `${(n - 1) * 0.09}s`, "--gd": `${(n - 1) * 0.5}s` } as CSSProperties;
                    return (
                        <Fragment key={n}>
                            <input type="radio" id={`s${n}-${bookingId}`} name="stars" value={n} required
                                   defaultChecked={state.values?.stars === String(n)}
                                   onChange={() => { setChosen(n); setBurst(burst + 1); }}/>
                            <label htmlFor={`s${n}-${bookingId}`} title={`${n} av 5`}
                                   className={lit ? styles.lit : undefined} style={delay}>
                                {/* eslint-disable @next/next/no-img-element -- small SVG icons */}
                                <img src="/ikoner/stjerne-kontur.svg" alt="" className={styles.off}/>
                                <img key={`on-${burst}`} src="/ikoner/stjerne.svg" alt="" className={styles.on}/>
                                {/* eslint-enable @next/next/no-img-element */}
                                {lit && burst > 0 && (
                                    <span key={burst} aria-hidden="true">
                                        <span className={styles.burst}/>
                                        <span className={styles.sparks}>
                                            {Array.from({ length: 12 }, (_, k) => (
                                                <i key={k} style={{
                                                    "--a": `${k * 30 + n * 13}deg`,
                                                    "--d": `${34 + (k % 3) * 12}px`,
                                                    color: SPARK_COLOURS[k % 5],
                                                } as CSSProperties}>{k % 2 ? "✦" : "✧"}</i>
                                            ))}
                                        </span>
                                    </span>
                                )}
                                {lit && <span className={styles.glint} aria-hidden="true"/>}
                                <span className={styles.srOnly}>{n} av 5 stjerner</span>
                            </label>
                        </Fragment>
                    );
                })}
            </div>
            <label className={styles.label}>
                Kommentar (valgfritt)
                <textarea
                    name="comment"
                    maxLength={500}
                    rows={3}
                    className={styles.textarea}
                    placeholder="Hva var bra? Hva kan bli bedre?"
                    defaultValue={state.values?.comment}
                />
            </label>
            <div className={styles.label}>
                Bilde av resultatet (valgfritt)
                {/* The browser's own "Choose File" is hidden behind a mint pill with a camera */}
                <label className={styles.file}>
                    <input type="file" name="photo" accept="image/png,image/jpeg,image/webp" onChange={handlePhoto}
                           className={styles.srOnly}/>
                    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2"
                         strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                        <path d="M4 8h3l2-3h6l2 3h3v11H4z"/>
                        <circle cx="12" cy="13" r="3.5"/>
                    </svg>
                    {photoName ? "Bytt bilde" : "Legg til bilde"}
                </label>
                {photoName && <span className={styles.fileName}>{photoName}</span>}
            </div>
            {tooBig && (
                <p className={panelStyles.error} role="alert">
                    Bildet er for stort. Velg et bilde under 3,5 MB.
                </p>
            )}
            <SubmitButton className={panelStyles.primaryButton} pendingText="Sender…" disabled={preparing || tooBig}>
                Send vurdering
            </SubmitButton>
        </form>
    );
}
