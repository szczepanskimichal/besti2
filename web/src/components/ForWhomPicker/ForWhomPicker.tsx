"use client";

import { useEffect, useId, useRef, useState } from "react";
import styles from "./ForWhomPicker.module.css";

type Choice = "egentid" | "duo" | "team" | "gave";

// "For hvem?": Egentid = only me, Duo and Team = BESTI sammen (2 or 3-8 people at the same time).
// Gave waits for gavekort, so it is shown but cannot be picked yet.
const options: { value: Choice; title: string; text: string; icon: React.ReactNode; soon?: boolean }[] = [
    {
        value: "egentid", title: "BESTI Egentid", text: "Vær din egen BESTI: bestill tid for deg selv",
        icon: <><circle cx="16" cy="9" r="4.5"/><path d="M7 28c0-6 4-10 9-10s9 4 9 10"/></>,
    },
    {
        value: "duo", title: "BESTI Duo", text: "Perfekt for en date eller en tur med en venn",
        icon: <><circle cx="11" cy="10" r="4"/><circle cx="21" cy="10" r="4"/><path d="M3 27c0-5 3.5-8 8-8M29 27c0-5-3.5-8-8-8M11 27c0-4 2-6 5-6s5 2 5 6"/></>,
    },
    {
        value: "team", title: "BESTI Team", text: "Gruppebestilling for hele gjengen",
        icon: <><circle cx="16" cy="9" r="3.6"/><circle cx="6.5" cy="12.5" r="3"/><circle cx="25.5" cy="12.5" r="3"/><path d="M9 27c0-5 3-8 7-8s7 3 7 8M1.5 25c0-4 2-6.5 5-6.5M30.5 25c0-4-2-6.5-5-6.5"/></>,
    },
    {
        value: "gave", title: "BESTI Gave", text: "Bestill eller kjøp en gave til noen", soon: true,
        icon: <><rect x="4" y="12" width="24" height="6" rx="1.5"/><path d="M6 18v9h20v-9M16 12v15M16 12c-2-5-8-5-8-2s5 2 8 2zM16 12c2-5 8-5 8-2s-5 2-8 2z"/></>,
    },
];

const teamSizes = [3, 4, 5, 6, 7, 8];

function choiceOf(persons: string | undefined): Choice {
    if (persons === "2") return "duo";
    return persons ? "team" : "egentid";
}

// Sends "personer" with the search form, like the old select did ("" = only me, 2-8 = BESTI sammen)
export default function ForWhomPicker({ persons }: { persons?: string }) {
    const [choice, setChoice] = useState<Choice>(choiceOf(persons));
    const [teamSize, setTeamSize] = useState(persons && persons !== "2" ? persons : "3");
    const [open, setOpen] = useState(false);
    const box = useRef<HTMLDivElement>(null);
    const listId = useId();
    const current = options.find((o) => o.value === choice)!;
    const value = choice === "duo" ? "2" : choice === "team" ? teamSize : "";

    // Close the list on a click outside it or on Escape
    useEffect(() => {
        if (!open) return;
        const outside = (event: PointerEvent) => {
            if (!box.current?.contains(event.target as Node)) setOpen(false);
        };
        const escape = (event: KeyboardEvent) => {
            if (event.key === "Escape") setOpen(false);
        };
        document.addEventListener("pointerdown", outside);
        document.addEventListener("keydown", escape);
        return () => {
            document.removeEventListener("pointerdown", outside);
            document.removeEventListener("keydown", escape);
        };
    }, [open]);

    return (
        <div className={styles.picker} ref={box}>
            <span className={styles.label} id={`${listId}-label`}>For hvem?</span>
            <button type="button" className={styles.trigger} aria-haspopup="listbox" aria-expanded={open}
                    aria-controls={listId} aria-labelledby={`${listId}-label ${listId}-value`}
                    onClick={() => setOpen(!open)}>
                <span id={`${listId}-value`}>{current.title}</span>
                <svg className={open ? styles.chevronUp : styles.chevron} viewBox="0 0 12 8" width="12" height="8" aria-hidden="true">
                    <path d="M1 1.5 6 6.5l5-5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
                </svg>
            </button>
            {open && (
                <ul className={styles.list} role="listbox" id={listId} aria-labelledby={`${listId}-label`}>
                    {options.map((option) => (
                        <li key={option.value} role="option" aria-selected={option.value === choice}
                            aria-disabled={option.soon || undefined}>
                            <button type="button" disabled={option.soon}
                                    className={option.value === choice ? `${styles.option} ${styles.selected}` : styles.option}
                                    onClick={() => {
                                        setChoice(option.value);
                                        setOpen(false);
                                    }}>
                                <svg className={styles.icon} viewBox="0 0 32 32" width="30" height="30" fill="none"
                                     stroke={`url(#${listId}-gradient)`} strokeWidth="2" strokeLinecap="round"
                                     strokeLinejoin="round" aria-hidden="true">
                                    <defs>
                                        <linearGradient id={`${listId}-gradient`} x1="0" y1="0" x2="1" y2="1">
                                            <stop offset="0" stopColor="#5af05e"/>
                                            <stop offset="1" stopColor="#28c9e8"/>
                                        </linearGradient>
                                    </defs>
                                    {option.icon}
                                </svg>
                                <span>
                                    <b className={styles.title}>
                                        {option.title}
                                        {option.soon && <span className={styles.soon}>Kommer snart</span>}
                                    </b>
                                    <span className={styles.text}>{option.text}</span>
                                </span>
                            </button>
                        </li>
                    ))}
                </ul>
            )}
            {choice === "team" && (
                <label className={styles.teamSize}>
                    Hvor mange?
                    <select value={teamSize} onChange={(event) => setTeamSize(event.target.value)}>
                        {teamSizes.map((size) => (
                            <option key={size} value={size}>{size} personer</option>
                        ))}
                    </select>
                </label>
            )}
            <input type="hidden" name="personer" value={value}/>
        </div>
    );
}
