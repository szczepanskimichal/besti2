import NextLink from "next/link";
import { categoryOptions } from "@/lib/categories";
import { registerAction } from "./actions";
import styles from "./page.module.css";

type RegisterPageProps = {
    searchParams: Promise<{ feil?: string }>;
};

export default async function RegisterBusinessPage({ searchParams }: RegisterPageProps) {
    const { feil } = await searchParams;

    return (
        <main className={styles.main}>
        <h1 className={styles.title}>Registrer bedriften din</h1>
    <p className={styles.muted}>Få kundene dine til å bestille time på nett.</p>

    {feil && (
        <p className={styles.error} role="alert">
        {feil}
        </p>
    )}
    <form action={registerAction} className={styles.form}>
    <label className={styles.field}>
        Bedriftsnavn
        <input name="businessName" required maxLength={200} autoComplete="organization" />
        </label>

        <label className={styles.field}>
        Organisasjonsnummer (9 siffer)
    <input name="orgNumber" required inputMode="numeric" pattern="[0-9]{9}" maxLength={9} />
    </label>

    <label className={styles.field}>
        Kategori
        <select name="category" required defaultValue="">
    <option value="" disabled>Velg kategori</option>
    {categoryOptions.map((option) => (
        <option key={option.value} value={option.value}>{option.label}</option>
    ))}
    </select>
    </label>

    <label className={styles.field}>
        Adresse
        <input name="address" required autoComplete="street-address" />
        </label>

        <label className={styles.field}>
        Postnummer
        <input name="postalCode" required inputMode="numeric" pattern="[0-9]{4}" maxLength={4} autoComplete="postal-code" />
        </label>

        <label className={styles.field}>
        By
        <input name="city" required autoComplete="address-level2" />
        </label>

        <label className={styles.field}>
        Telefon
        <input name="phone" type="tel" required maxLength={20} autoComplete="tel" />
        </label>

        <label className={styles.field}>
        E-post (brukes til innlogging)
    <input name="email" type="email" required maxLength={250} autoComplete="email" />
        </label>

        <label className={styles.field}>
        Passord
        <input name="password" type="password" required minLength={6} autoComplete="new-password" />
        </label>
        <p className={styles.hint}>Minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn.</p>
        <p className={styles.hint}>
            Ved å registrere bedriften godtar du <NextLink href="/vilkar">vilkårene</NextLink> og
            bekrefter at du har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
        </p>
    <button type="submit" className={styles.primaryButton}>
        Registrer bedrift
    </button>
    </form>
    </main>
);
}
