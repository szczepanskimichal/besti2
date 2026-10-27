"use client";
import { useActionState, useRef, useState } from "react";
import NextLink from "next/link";
import { categoryOptionGroups } from "@/lib/categories";
import { registerAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

// The business sign-up form: on an error every field stays filled in, only the password is typed again
export default function RegisterBusinessForm() {
    const [state, formAction] = useActionState<FormState, FormData>(registerAction, {});
    const value = (field: string) => state.values?.[field] ?? "";
    const formRef = useRef<HTMLFormElement>(null);
    const [registry, setRegistry] = useState<{ message: string; ok: boolean } | null>(null);
    const [looking, setLooking] = useState(false);

    // "Hent fra Brønnøysund": fills in name and address from Enhetsregisteret; everything can still be changed
    async function fillFromRegistry() {
        const form = formRef.current;
        const orgNumber = (form?.elements.namedItem("orgNumber") as HTMLInputElement | null)?.value.trim() ?? "";
        if (!/^\d{9}$/.test(orgNumber)) {
            setRegistry({ ok: false, message: "Skriv inn de 9 sifrene i organisasjonsnummeret først." });
            return;
        }
        setLooking(true);
        try {
            const res = await fetch(`/registrer-bedrift/brreg?orgnr=${orgNumber}`, { cache: "no-store" });
            const body = await res.json();
            if (!res.ok) {
                setRegistry({ ok: false, message: body.message });
                return;
            }
            for (const [field, text] of [["businessName", body.name], ["address", body.address],
                ["postalCode", body.postalCode], ["city", body.city]] as const) {
                const input = form?.elements.namedItem(field) as HTMLInputElement | null;
                if (input && text) input.value = text;
            }
            setRegistry({ ok: true, message: "Hentet fra Brønnøysund. Sjekk og endre om nødvendig (f.eks. adressen til salongen)." });
        } catch {
            setRegistry({ ok: false, message: "Brønnøysundregistrene svarer ikke akkurat nå. Fyll ut feltene selv." });
        } finally {
            setLooking(false);
        }
    }

    return (
        <form ref={formRef} action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <label className={styles.field}>
                Organisasjonsnummer (9 siffer)
                <input name="orgNumber" required inputMode="numeric" pattern="[0-9]{9}" maxLength={9}
                       defaultValue={value("orgNumber")} />
            </label>
            <button type="button" className={styles.registryButton} onClick={fillFromRegistry} disabled={looking}>
                {looking ? "Henter…" : "Hent fra Brønnøysund"}
            </button>
            {registry && (
                <p className={registry.ok ? styles.hint : styles.error} role="status">{registry.message}</p>
            )}
            <label className={styles.field}>
                Bedriftsnavn
                <input name="businessName" required maxLength={200} autoComplete="organization"
                       defaultValue={value("businessName")} />
            </label>
            <label className={styles.field}>
                Kategori
                {/* key: a select only takes a new defaultValue when it is created again */}
                <select key={value("category")} name="category" required defaultValue={value("category")}>
                    <option value="" disabled>Velg kategori</option>
                    {categoryOptionGroups.map((group) => (
                        <optgroup key={group.label} label={group.label}>
                            {group.options.map((option) => (
                                <option key={option.value} value={option.value}>{option.label}</option>
                            ))}
                        </optgroup>
                    ))}
                </select>
            </label>
            <label className={styles.field}>
                Adresse
                <input name="address" required maxLength={200} autoComplete="street-address" defaultValue={value("address")} />
            </label>
            <label className={styles.field}>
                Postnummer
                <input name="postalCode" required inputMode="numeric" pattern="[0-9]{4}" maxLength={4}
                       autoComplete="postal-code" defaultValue={value("postalCode")} />
            </label>
            <label className={styles.field}>
                By
                <input name="city" required autoComplete="address-level2" defaultValue={value("city")} />
            </label>
            <label className={styles.field}>
                Telefon
                <input name="phone" type="tel" required maxLength={20} autoComplete="tel" defaultValue={value("phone")} />
            </label>
            <label className={styles.field}>
                E-post til kunder (valgfritt)
                <input name="contactEmail" type="email" maxLength={250} defaultValue={value("contactEmail")} />
            </label>
            <p className={styles.hint}>Vises på siden til bedriften. La feltet stå tomt for å bare vise telefon.</p>
            <label className={styles.field}>
                E-post for innlogging
                <input name="email" type="email" required maxLength={250} autoComplete="email"
                       defaultValue={value("email")} />
            </label>
            <p className={styles.hint}>
                Vises aldri for kunder. Bruk en annen adresse enn e-posten til kunder, så kan ingen andre låse kontoen.
            </p>
            <label className={styles.field}>
                Passord
                <input name="password" type="password" required minLength={6} autoComplete="new-password" />
            </label>
            <p className={styles.hint}>Minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn.</p>
            <label className={styles.checkbox}>
                <input type="checkbox" name="acceptDataProcessing" required
                       defaultChecked={value("acceptDataProcessing") === "on"}/>
                <span>
                    Jeg godtar <NextLink href="/vilkar">vilkårene</NextLink>, også{" "}
                    <NextLink href="/vilkar#databehandleravtale">databehandleravtalen</NextLink>, og bekrefter at
                    jeg har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
                </span>
            </label>
            <SubmitButton className={styles.primaryButton} pendingText="Registrerer…">
                Registrer bedrift
            </SubmitButton>
        </form>
    );
}
