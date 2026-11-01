"use client";
import { useActionState } from "react";
import { updateProfileAction } from "./actions";
import type { FormState } from "@/lib/formState";
import { FREE_CANCEL_OPTIONS } from "@/lib/freeCancel";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./profil.module.css";

type ProfileFormProps = {
    // The saved profile
    initial: Record<string, string>;
};

// Description and contact info in calm glass sections: on an error the message shows here and everything typed stays
export default function ProfileForm({ initial }: ProfileFormProps) {
    const [state, formAction] = useActionState<FormState, FormData>(updateProfileAction, {});
    const value = (field: string) => state.values?.[field] ?? initial[field] ?? "";

    return (
        <form action={formAction} className={styles.profileForm}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <fieldset className={styles.section}>
                <legend className={styles.sectionTitle}>Om oss</legend>
                <label className={styles.field}>
                    <span>Fortell kort om bedriften (valgfritt)</span>
                    <textarea name="description" maxLength={1000} rows={5} defaultValue={value("description")}/>
                </label>
            </fieldset>

            <fieldset className={styles.section}>
                <legend className={styles.sectionTitle}>Kontakt</legend>
                <div className={styles.grid}>
                    <label className={styles.field}>
                        <span>Telefon</span>
                        <input name="phone" type="tel" required maxLength={20} defaultValue={value("phone")}/>
                    </label>
                    <label className={styles.field}>
                        <span>E-post til kunder (valgfritt)</span>
                        <input name="contactEmail" type="email" maxLength={250} defaultValue={value("contactEmail")}/>
                    </label>
                    <label className={`${styles.field} ${styles.wide}`}>
                        <span>Nettside (valgfritt)</span>
                        <input name="website" type="text" inputMode="url" maxLength={200} placeholder="klippogstil.no"
                               defaultValue={value("website")}/>
                    </label>
                </div>
            </fieldset>

            <fieldset className={styles.section}>
                <legend className={styles.sectionTitle}>Adresse</legend>
                <div className={styles.addressGrid}>
                    <label className={`${styles.field} ${styles.wide}`}>
                        <span>Gate og nummer</span>
                        <input name="address" required maxLength={200} defaultValue={value("address")}/>
                    </label>
                    <label className={styles.field}>
                        <span>Postnummer</span>
                        <input name="postalCode" required inputMode="numeric" pattern="[0-9]{4}" maxLength={4}
                               defaultValue={value("postalCode")}/>
                    </label>
                    <label className={styles.field}>
                        <span>By</span>
                        <input name="city" required maxLength={200} defaultValue={value("city")}/>
                    </label>
                </div>
            </fieldset>

            <fieldset className={styles.section}>
                <legend className={styles.sectionTitle}>Bestillinger</legend>
                {/* Two choices as two glass pills; the chosen one lit mint (same values as the old select) */}
                <p className={styles.question}>Nye bestillinger</p>
                <div className={styles.choices}>
                    <label className={styles.choice}>
                        <input type="radio" name="autoConfirm" value="true"
                               defaultChecked={value("autoConfirm") === "true"}/>
                        <span className={styles.choiceTitle}>Bekreft automatisk</span>
                        <span className={styles.choiceText}>Kunden får bekreftelse med en gang</span>
                    </label>
                    <label className={styles.choice}>
                        <input type="radio" name="autoConfirm" value="false"
                               defaultChecked={value("autoConfirm") !== "true"}/>
                        <span className={styles.choiceTitle}>Jeg bekrefter selv</span>
                        <span className={styles.choiceText}>Du godkjenner hver bestilling i panelet</span>
                    </label>
                </div>
                <label className={styles.field}>
                    <span>Gratis avbestilling inntil</span>
                    <select name="freeCancelHours" defaultValue={value("freeCancelHours")}>
                        {FREE_CANCEL_OPTIONS.map((hours) => (
                            <option key={hours} value={hours}>
                                {hours === 0 ? "Når som helst før timen" : `${hours} timer før timen`}
                            </option>
                        ))}
                    </select>
                </label>
            </fieldset>

            <SubmitButton className={styles.saveButton} pendingText="Lagrer…">
                Lagre profil
            </SubmitButton>
        </form>
    );
}
