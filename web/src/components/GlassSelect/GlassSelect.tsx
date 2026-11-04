"use client";

import { useEffect, useId, useRef, useState } from "react";
import { flushSync } from "react-dom";
import styles from "./GlassSelect.module.css";

export type GlassOption = { value: string; label: string };
export type GlassGroup = { label?: string; options: GlassOption[] };

type GlassSelectProps = {
    name?: string;
    // Text above the field; leave it out when the field already sits inside a <label> with its own text
    label?: string;
    // One flat list, or groups with a small mint title (e.g. categories)
    options?: GlassOption[];
    groups?: GlassGroup[];
    defaultValue?: string;
    // Controlled use (the parent keeps the value), e.g. the column choice in customer import
    value?: string;
    onChange?: (value: string) => void;
    // Shown while nothing is chosen yet (value ""), like "Velg kategori"
    placeholder?: string;
    required?: boolean;
    // Sends the form right after a choice, like AutoSubmitSelect did, so the customer does not need the "Vis" button
    autoSubmit?: boolean;
    id?: string;
    ariaLabel?: string;
    // The open list floats over the page instead of pushing it down (for a field in a row next to a button)
    overlay?: boolean;
    className?: string;
};

// A list in BESTI style (frosted glass, the chosen line in mint), the same as "For hvem?", instead of the system list.
// The system list is painted by the phone or computer in its own colour (on a Mac often bright green), Ada 2026-11-03.
// Sends the chosen value with the form as `name`, like a select did
export default function GlassSelect({
    name, label, options, groups, defaultValue = "", value: controlled, onChange, placeholder, required, autoSubmit,
    id: givenId, ariaLabel, overlay, className,
}: GlassSelectProps) {
    const allGroups: GlassGroup[] = groups ?? [{ options: options ?? [] }];
    const all = allGroups.flatMap((g) => g.options);
    const firstValue = placeholder !== undefined ? "" : all[0]?.value ?? "";
    const [own, setOwn] = useState(all.some((o) => o.value === defaultValue) ? defaultValue : firstValue);
    const value = controlled ?? own;
    const [open, setOpen] = useState(false);
    const box = useRef<HTMLDivElement>(null);
    const listRef = useRef<HTMLUListElement>(null);
    const triggerRef = useRef<HTMLButtonElement>(null);
    // Opened with an arrow key: then the keyboard focus goes into the list
    const focusList = useRef(false);
    const ownId = useId();
    const id = givenId ?? ownId;
    const current = all.find((o) => o.value === value);

    // Close on a click outside or on Escape; when it opens, the chosen line is scrolled into view
    useEffect(() => {
        if (!open) return;
        const chosen = listRef.current?.querySelector<HTMLElement>("[aria-selected='true'] button")
            ?? listRef.current?.querySelector<HTMLElement>("button");
        chosen?.scrollIntoView({ block: "nearest" });
        if (focusList.current) {
            focusList.current = false;
            chosen?.focus();
        }
        const outside = (event: PointerEvent) => {
            if (!box.current?.contains(event.target as Node)) setOpen(false);
        };
        const escape = (event: KeyboardEvent) => {
            if (event.key !== "Escape") return;
            if (listRef.current?.contains(document.activeElement)) triggerRef.current?.focus();
            setOpen(false);
        };
        document.addEventListener("pointerdown", outside);
        document.addEventListener("keydown", escape);
        return () => {
            document.removeEventListener("pointerdown", outside);
            document.removeEventListener("keydown", escape);
        };
    }, [open]);

    // Computer keyboard (the phone does not use this): arrows open the list and move between the lines,
    // Enter chooses, Home/End jump to the first/last line, Tab leaves the list
    const triggerKeys = (event: React.KeyboardEvent) => {
        if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
        event.preventDefault();
        focusList.current = true;
        if (open) {
            listRef.current?.querySelector<HTMLElement>("[aria-selected='true'] button, button")?.focus();
        } else {
            setOpen(true);
        }
    };

    const listKeys = (event: React.KeyboardEvent) => {
        const buttons = Array.from(listRef.current?.querySelectorAll<HTMLButtonElement>("button") ?? []);
        const at = buttons.indexOf(document.activeElement as HTMLButtonElement);
        const to = event.key === "ArrowDown" ? Math.min(at + 1, buttons.length - 1)
            : event.key === "ArrowUp" ? Math.max(at - 1, 0)
            : event.key === "Home" ? 0
            : event.key === "End" ? buttons.length - 1
            : -1;
        if (event.key === "Tab") setOpen(false);
        if (to < 0) return;
        event.preventDefault();
        buttons[to]?.focus();
    };

    const choose = (next: string) => {
        // Keyboard users stay on the field after choosing
        if (listRef.current?.contains(document.activeElement)) triggerRef.current?.focus();
        setOpen(false);
        if (next === value) return;
        // flushSync: the hidden input gets the new value before an auto submit reads the form
        flushSync(() => {
            if (controlled === undefined) setOwn(next);
            onChange?.(next);
        });
        if (autoSubmit) box.current?.closest("form")?.requestSubmit();
    };

    return (
        <div className={className ? `${styles.picker} ${className}` : styles.picker} ref={box}>
            {label && <span className={styles.label} id={`${id}-label`}>{label}</span>}
            <button type="button" id={id} ref={triggerRef} className={styles.trigger} aria-haspopup="listbox" aria-expanded={open}
                    aria-controls={`${id}-list`} aria-label={ariaLabel}
                    aria-labelledby={label ? `${id}-label ${id}-value` : undefined}
                    onClick={() => setOpen(!open)} onKeyDown={triggerKeys}>
                <span id={`${id}-value`} className={current ? styles.value : `${styles.value} ${styles.placeholder}`}>
                    {current?.label ?? placeholder}
                </span>
                <svg className={open ? styles.chevronUp : styles.chevron} viewBox="0 0 12 8" width="12" height="8" aria-hidden="true">
                    <path d="M1 1.5 6 6.5l5-5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
                </svg>
            </button>
            {open && (
                <ul className={overlay ? `${styles.list} ${styles.overlay}` : styles.list} role="listbox" id={`${id}-list`}
                    aria-labelledby={label ? `${id}-label` : undefined} aria-label={label ? undefined : ariaLabel}
                    ref={listRef} onKeyDown={listKeys}>
                    {allGroups.map((group, g) => (
                        <li key={group.label ?? g} role="presentation">
                            {group.label && <span className={styles.group}>{group.label}</span>}
                            <ul role="presentation" className={styles.options}>
                                {group.options.map((option) => (
                                    <li key={option.value} role="option" aria-selected={option.value === value}>
                                        <button type="button"
                                                className={option.value === value ? `${styles.option} ${styles.selected}` : styles.option}
                                                onClick={() => choose(option.value)}>
                                            {option.label}
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        </li>
                    ))}
                </ul>
            )}
            {name && (required ? (
                // A required field: an invisible input the browser can check, so "Velg ..." still stops the form
                <input className={styles.validator} name={name} value={value} required tabIndex={-1} aria-hidden="true"
                       onChange={() => {}} onFocus={() => setOpen(true)}/>
            ) : (
                <input type="hidden" name={name} value={value}/>
            ))}
        </div>
    );
}
