"use client";

import { useEffect, useId, useRef, useState } from "react";
import glass from "../GlassSelect/GlassSelect.module.css";
import styles from "./CityPicker.module.css";
import { useNearMe } from "../NearMeButton/useNearMe";

type CityPickerProps = {
    name: string;
    cities: string[];
    defaultValue?: string;
    placeholder?: string;
};

// One letter for one letter, so "gjovik" finds "Gjøvik" and the matching letters can be lit up in the right place
const fold = (text: string) =>
    Array.from(text.toLowerCase(), (c) => c === "ø" ? "o" : c === "æ" || c === "å" ? "a" : c.normalize("NFD")[0]).join("");

// The city field in the search, Ada 2026-11-03: free text as before, but the suggestions are our glass list
// (instead of the white system list) with "Nær meg" on top and the cities that have BESTI businesses below
export default function CityPicker({ name, cities, defaultValue = "", placeholder }: CityPickerProps) {
    const [text, setText] = useState(defaultValue);
    const [open, setOpen] = useState(false);
    // Keyboard line: 0 = "Nær meg", 1.. = the cities, -1 = none
    const [active, setActive] = useState(-1);
    const { status, findMe } = useNearMe();
    const box = useRef<HTMLDivElement>(null);
    const id = useId();

    const typed = fold(text.trim());
    const matches = cities.filter((c) => fold(c).includes(typed));

    useEffect(() => {
        if (!open) return;
        const outside = (event: PointerEvent) => {
            if (!box.current?.contains(event.target as Node)) setOpen(false);
        };
        document.addEventListener("pointerdown", outside);
        return () => document.removeEventListener("pointerdown", outside);
    }, [open]);

    const choose = (line: number) => {
        setOpen(false);
        setActive(-1);
        if (line === 0) findMe();
        else setText(matches[line - 1]);
    };

    // Computer keyboard: arrows move between the lines, Enter chooses (and does not send the form), Esc closes
    const keys = (event: React.KeyboardEvent) => {
        const last = matches.length;
        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            setOpen(true);
            setActive(event.key === "ArrowDown" ? Math.min(active + 1, last) : Math.max(active - 1, 0));
        } else if (event.key === "Enter" && open && active >= 0) {
            event.preventDefault();
            choose(active);
        } else if (event.key === "Escape" || event.key === "Tab") {
            setOpen(false);
        }
    };

    // The matching letters in mint, e.g. "gjo" in "Gjøvik"
    const lit = (city: string) => {
        const at = typed ? fold(city).indexOf(typed) : -1;
        if (at < 0) return city;
        return <>{city.slice(0, at)}<b className={styles.match}>{city.slice(at, at + typed.length)}</b>{city.slice(at + typed.length)}</>;
    };

    return (
        <div className={styles.picker} ref={box}>
            <input name={name} value={text} placeholder={placeholder} autoComplete="off"
                   role="combobox" aria-expanded={open} aria-controls={`${id}-list`} aria-autocomplete="list"
                   aria-activedescendant={open && active >= 0 ? `${id}-${active}` : undefined}
                   onChange={(e) => { setText(e.target.value); setOpen(true); setActive(-1); }}
                   onFocus={() => setOpen(true)} onClick={() => setOpen(true)} onKeyDown={keys}/>
            {open && (
                <ul className={`${glass.list} ${glass.overlay}`} role="listbox" id={`${id}-list`} aria-label="Forslag">
                    <li role="option" id={`${id}-0`} aria-selected={active === 0}>
                        {/* onMouseDown: the field keeps the focus, so the list does not close before the click */}
                        <button type="button" tabIndex={-1} onMouseDown={(e) => e.preventDefault()} onClick={() => choose(0)}
                                className={active === 0 ? `${styles.near} ${styles.nearActive}` : styles.near}>
                            {/* eslint-disable-next-line @next/next/no-img-element -- static SVG with the glow baked in */}
                            <img src="/ikoner/pinne.svg" alt="" width={30} height={30} className={styles.pin}/>
                            <span className={styles.nearText}>{status === "finding" ? "Finner deg …" : "Nær meg"}</span>
                            <span className={styles.nearHint}>Bruk posisjonen min</span>
                        </button>
                    </li>
                    {/* No title above the cities: the field label "Hvor?" already says it (Ada 2026-11-03, label 2026-11-04) */}
                    {matches.length > 0 && (
                        <li role="presentation">
                            <ul role="presentation" className={glass.options}>
                                {matches.map((city, k) => (
                                    <li key={city} role="option" id={`${id}-${k + 1}`} aria-selected={active === k + 1}>
                                        <button type="button" tabIndex={-1} onMouseDown={(e) => e.preventDefault()}
                                                onClick={() => choose(k + 1)}
                                                className={active === k + 1 ? `${glass.option} ${glass.selected}` : glass.option}>
                                            {lit(city)}
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        </li>
                    )}
                </ul>
            )}
            {status === "denied" && (
                <p className={styles.denied} role="alert">Fikk ikke tilgang til posisjonen din. Skriv inn en by i stedet.</p>
            )}
        </div>
    );
}
