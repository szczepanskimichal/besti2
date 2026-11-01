"use client";
import { useActionState } from "react";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { customerNoteAction } from "./actions";
import kunder from "./kunder.module.css";

// "Notat" as one more line under the customer's phone and e-mail. A tap opens the note like the notes app on a phone:
// anyone at the business (also a colleague who takes over the customer) reads it and writes in it right here.
// Faint "Skriv notat" when there is no note yet
export default function NotePad({ customerKey, name, note, open = false }: {
    customerKey: string; name: string; note: string; open?: boolean;
}) {
    const [state, formAction] = useActionState<FormState, FormData>(customerNoteAction, {});
    const id = `notat-${customerKey}`;
    return (
        <details className={kunder.noteDetails} open={open || Boolean(state.error)}>
            <summary className={`${kunder.contact} ${kunder.noteSummary} ${note ? "" : kunder.noteEmpty}`}
                     aria-label={note ? `Notat om ${name}` : `Skriv notat om ${name}`}>
                <BookIcon/>
                <span>{note ? "Notat" : "Skriv notat"}</span>
            </summary>
            <form action={formAction} className={kunder.sheet}>
                <label className={kunder.sheetTitle} htmlFor={id}>Notat · {name}</label>
                {open && !state.error && <p className={kunder.sheetSaved} role="status">Lagret ✓</p>}
                {state.error && <p className={kunder.sheetError} role="alert">{state.error}</p>}
                <input type="hidden" name="key" value={customerKey}/>
                <input type="hidden" name="tilbake" value="kunder"/>
                <textarea id={id} name="note" rows={5} maxLength={1000} className={kunder.sheetText}
                          defaultValue={state.values?.note ?? note}
                          placeholder="F.eks. «Liker det kort i nakken. Vil ha te, ikke kaffe.»"/>
                {/* Allergies and other health details are special category data (GDPR art. 9) */}
                <p className={kunder.sheetHint}>Bare for dere. Helseopplysninger, som allergier, bare hvis kunden har sagt ja.</p>
                <SubmitButton className={kunder.sheetSave} pendingText="Lagrer…">Lagre</SubmitButton>
            </form>
        </details>
    );
}

// Open book, drawn like the phone and envelope icons on the card
export function BookIcon() {
    return (
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
             strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M2 5c3-1.3 6.5-1.3 10 1 3.5-2.3 7-2.3 10-1v14c-3-1.3-6.5-1.3-10 1-3.5-2.3-7-2.3-10-1z"/>
            <path d="M12 6v14"/>
        </svg>
    );
}
