"use client";
import { useState } from "react";
import { REPLY_LINES } from "./replyLines";
import styles from "./page.module.css";

// The answer field with ready-made lines as pills, each with a crystal icon from the BESTI-chat (like
// "Skriv en liten hilsen" on the invitation). A tap fills the field; the business can still write its own words
export default function ReplyText({ answered, initial }: { answered: boolean; initial: string }) {
    const [text, setText] = useState(initial);
    return (
        <div className={styles.field}>
            <span>{answered ? "Ditt svar (kan endres)" : "Svar til kunden"}</span>
            <div className={styles.chips}>
                {REPLY_LINES.map((line) => (
                    <button key={line.text} type="button" aria-pressed={text === line.text}
                            className={text === line.text ? `${styles.chip} ${styles.chipOn}` : styles.chip}
                            onClick={() => setText(text === line.text ? "" : line.text)}>
                        {line.text}
                        {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                        <img src={`/ikoner/${line.icon}.svg`} alt="" aria-hidden="true" className={styles.chipIcon}/>
                    </button>
                ))}
            </div>
            <textarea name="reply" required maxLength={500} rows={3} value={text} onChange={(e) => setText(e.target.value)}
                      aria-label={answered ? "Ditt svar" : "Svar til kunden"} placeholder="…eller skriv ditt eget svar"/>
        </div>
    );
}
