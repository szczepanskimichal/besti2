"use client";
import { useState } from "react";
import type { ReactionKind } from "@/lib/types";
import styles from "./page.module.css";

export type Suggestion = { text: string; icon: ReactionKind };

// "Melding til Ada": tap a ready-made line (or write your own) that goes to the friend who invited you, together with the answer.
// Each ready line carries one of the crystal icons from the BESTI-chat instead of an emoji
export default function QuickMessage({ to, suggestions }: { to: string; suggestions: Suggestion[] }) {
    const [text, setText] = useState("");
    const picked = suggestions.find((s) => s.text === text);
    return (
        <div className={styles.quickMessage}>
            <p className={styles.quickTitle}>Skriv en liten hilsen til {to}</p>
            <div className={styles.chips}>
                {suggestions.map((s) => (
                    <button key={s.text} type="button" aria-pressed={text === s.text}
                            className={text === s.text ? `${styles.chip} ${styles.chipOn}` : styles.chip}
                            onClick={() => setText(text === s.text ? "" : s.text)}>
                        {s.text}
                        {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                        <img src={`/ikoner/${s.icon}.svg`} alt="" aria-hidden="true" className={styles.chipIcon}/>
                    </button>
                ))}
            </div>
            <textarea name="friendMessage" aria-label={`Melding til ${to}`} maxLength={200} rows={2}
                      placeholder="…eller skriv din egen" value={text} onChange={(e) => setText(e.target.value)}/>
            {/* The crystal goes along with a ready line, so the inviter sees the same icon in the chat */}
            <input type="hidden" name="friendMessageIcon" value={picked?.icon ?? ""}/>
            <span className={styles.fieldHint}>Bare {to} ser dette.</span>
        </div>
    );
}
