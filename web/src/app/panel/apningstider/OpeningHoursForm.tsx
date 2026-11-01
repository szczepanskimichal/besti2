"use client";
import { useActionState, useState } from "react";
import { weekdays } from "@/lib/weekdays";
import { updateOpeningHoursAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./apningstider.module.css";

type OpeningHoursFormProps = {
    // The saved hours, e.g. { "open-1": "on", "opensAt-1": "09:00", "closesAt-1": "17:00", "breakStart-1-0": "12:00" }
    initial: Record<string, string>;
};

// One break a day for now (michal 2026-10-31: more only if beta testers ask); the rows are ready for more
const maxBreaks = 1;

type Break = { key: number; start: string; end: string };

// All 7 days in one form: on an error the message shows here and every day stays as typed
export default function OpeningHoursForm({ initial }: OpeningHoursFormProps) {
    const [state, formAction] = useActionState<FormState, FormData>(updateOpeningHoursAction, {});
    const values = state.values ?? initial;

    return (
        // key: after an error the form is drawn again from the typed values (checkboxes included)
        <form key={JSON.stringify(values)} action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <ul className={styles.days}>
                {weekdays.map((w) => (
                    <DayCard key={w.day} day={w.day} label={w.label} values={values}/>
                ))}
            </ul>
            <SubmitButton className={styles.saveButton} pendingText="Lagrer…">
                Lagre åpningstider
            </SubmitButton>
        </form>
    );
}

// One day: the Åpen switch, the opening hours and the break.
// A closed day (switch off) turns calm and dashed by CSS alone.
function DayCard({ day, label, values }: { day: number; label: string; values: Record<string, string> }) {
    const [breaks, setBreaks] = useState<Break[]>(() => {
        const saved: Break[] = [];
        for (let i = 0; i < maxBreaks; i++) {
            const start = values[`breakStart-${day}-${i}`] ?? "";
            const end = values[`breakEnd-${day}-${i}`] ?? "";
            if (start || end) {
                saved.push({ key: i, start, end });
            }
        }
        // Always one row to fill in, even on a day without a break
        return saved.length > 0 ? saved : [{ key: 0, start: "", end: "" }];
    });

    // The last row is emptied instead of removed, so one break row always stays (× = no break that day)
    function removeBreak(key: number) {
        const rest = breaks.filter((x) => x.key !== key);
        setBreaks(rest.length > 0 ? rest : [{ key: key + 1, start: "", end: "" }]);
    }

    function addBreak() {
        const key = Math.max(-1, ...breaks.map((b) => b.key)) + 1;
        setBreaks([...breaks, { key, start: "", end: "" }]);
    }

    return (
        <li className={styles.day}>
            <fieldset className={styles.dayFields}>
                <legend className={styles.dayName}>{label}</legend>
                <label className={styles.toggle}>
                    <input type="checkbox" name={`open-${day}`} defaultChecked={values[`open-${day}`] === "on"}/>
                    <span className={styles.switch} aria-hidden="true"/>
                    <span className={styles.onText}>Åpen</span>
                    <span className={styles.offText}>Stengt</span>
                </label>
                <div className={styles.times}>
                    <TimeField label="Fra" name={`opensAt-${day}`} value={values[`opensAt-${day}`] ?? "09:00"}/>
                    <span className={styles.dash} aria-hidden="true">–</span>
                    <TimeField label="Til" name={`closesAt-${day}`} value={values[`closesAt-${day}`] ?? "17:00"}/>
                </div>
                <div className={styles.breaks}>
                    {breaks.map((b, i) => (
                        <div key={b.key} className={`${styles.times} ${styles.pause}`}>
                            <TimeField label={breaks.length > 1 ? `Pause ${i + 1} fra` : "Pause fra"}
                                       name={`breakStart-${day}-${i}`} value={b.start}/>
                            <span className={styles.dash} aria-hidden="true">–</span>
                            <TimeField label="til" name={`breakEnd-${day}-${i}`} value={b.end}/>
                            <button type="button" className={styles.removeBreak}
                                    aria-label={`Fjern pause ${i + 1} på ${label.toLowerCase()}`}
                                    onClick={() => removeBreak(b.key)}>
                                ×
                            </button>
                        </div>
                    ))}
                    {breaks.length < maxBreaks && (
                        <button type="button" className={styles.addBreak} onClick={addBreak}>
                            <span aria-hidden="true">+</span> Legg til en pause til
                        </button>
                    )}
                </div>
            </fieldset>
        </li>
    );
}

// A time field without the browser's clock icon; a click still opens the time picker where the browser has one
function TimeField({ label, name, value }: { label: string; name: string; value: string }) {
    return (
        <label className={styles.time}>
            <span>{label}</span>
            <input type="time" name={name} defaultValue={value} step={300}
                   onClick={(e) => {
                       try {
                           e.currentTarget.showPicker();
                       } catch {
                           // Not supported or not allowed here: typing the time still works
                       }
                   }}/>
        </label>
    );
}
