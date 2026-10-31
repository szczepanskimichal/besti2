"use client";
import { useActionState, useState } from "react";
import NextLink from "next/link";
import { acceptInviteAction } from "../actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import QuickMessage from "./QuickMessage";
import styles from "./page.module.css";

// "Jeg blir med!": the friend's phone (from Min profil) and an optional message; name and e-mail come from the account
export default function AcceptForm({ token, fullName, phone, inviter }: { token: string; fullName: string | null; phone: string | null; inviter: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(acceptInviteAction, {});
    const value = (field: string, fallback = "") => state.values?.[field] ?? fallback;
    // The number from Min profil is shown on one line; the field only opens when it is missing or on "Endre"
    const [editPhone, setEditPhone] = useState(!phone);

    return (
        <form action={formAction} className={styles.form}>
            {state.error && <p className={styles.error} role="alert">{state.error}</p>}
            <input type="hidden" name="token" value={token}/>
            {editPhone ? (
                <>
                    <p className={styles.muted}>Du blir med som <strong>{fullName}</strong></p>
                    <label className={styles.field}>
                        Telefonen din
                        <input name="phone" type="tel" inputMode="tel" required maxLength={20}
                               autoComplete="tel" pattern="\+?[0-9 ]{8,20}" placeholder="912 34 567"
                               title="Bare tall, mellomrom og +, minst 8 sifre"
                               defaultValue={value("phone", phone ?? "")}/>
                        <span className={styles.fieldHint}>Bedriften bruker nummeret bare hvis noe må endres.</span>
                    </label>
                </>
            ) : (
                <div className={styles.whoLine}>
                    <input type="hidden" name="phone" value={phone ?? ""}/>
                    <p>Du blir med som <strong>{fullName}</strong></p>
                    <p>
                        Telefonen din <strong className={styles.nowrap}>{phone}</strong> ·{" "}
                        <button type="button" className={styles.textLink} onClick={() => setEditPhone(true)}>Endre</button>
                    </p>
                </div>
            )}
            <QuickMessage to={inviter} suggestions={[
                { text: "Takk for at du tenkte på meg!", icon: "hjerte" },
                { text: "Jeg blir med, gleder meg!", icon: "feiring" },
                { text: "Så hyggelig, takk!", icon: "tommel" },
                { text: "Endelig BESTI-tid!", icon: "stjerne" },
            ]}/>
            {/* The note to the business is still here, just tucked away: the friend has no other place to write it */}
            <details className={styles.businessNote} open={Boolean(value("note"))}>
                <summary>+ Melding til bedriften</summary>
                <label className={styles.field}>
                    <span className={styles.srOnly}>Melding til bedriften</span>
                    <textarea name="note" maxLength={500} rows={2} defaultValue={value("note")}/>
                    <span className={styles.fieldHint}>Bare bedriften ser dette.</span>
                </label>
            </details>
            <p className={styles.muted}>
                Ved å bli med godtar du <NextLink href="/vilkar">vilkårene</NextLink> og
                bekrefter at du har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
            </p>
            <SubmitButton className={styles.primaryButton} pendingText="Bestiller for dere…">
                Jeg blir med!
                <svg className={styles.duoIcon} viewBox="0 0 32 32" width="24" height="24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
                    <circle cx="11" cy="10" r="4"/><circle cx="21" cy="10" r="4"/><path d="M3 27c0-5 3.5-8 8-8M29 27c0-5-3.5-8-8-8M11 27c0-4 2-6 5-6s5 2 5 6"/>
                </svg>
            </SubmitButton>
        </form>
    );
}
