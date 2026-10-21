import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { updateProfileAction } from "./actions";
import LogoUploadForm from "./LogoUploadForm";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../tjenester/page.module.css";

type ProfilePageProps = {
    searchParams: Promise<{ feil?: string; lagret?: string, logo?: string }>;
};
export default async function ProfilePage({ searchParams }: ProfilePageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const business = await getPanelBusiness(token);
    if (!business) {
        redirect("/logg-inn");
    }
    const { feil, lagret, logo } = await searchParams
    
    return (
        <main className={styles.main}>
            <Link href="/panel" className={styles.back}>← Tilbake til bestillinger</Link>
            <h1 className={styles.title}>Bedriftsprofil</h1>
            <p className={styles.muted}>{business.name}</p>
            {feil && (
                <p className={styles.error} role="alert">
                    {feil}
                </p>
            )}
            {lagret && (
                <p className={styles.success} role="status">
                    Profilen er lagret.
                </p>
            )}
            {logo && (
                <p className={styles.success} role="status">
                    Logoen er lagret.
                </p>
            )}
            {business.latitude === null && (
                <p className={styles.error} role="status">
                    Vi fant ikke adressen på kartet – sjekk adressen og postnummeret.
                    Uten kart vises dere ikke i «Nær meg».
                </p>
            )}
            <LogoUploadForm name={business.name} category={business.category} logoUrl={business.logoUrl} />
            <form action={updateProfileAction} className={styles.form}>
                <label className={styles.field}>
                    Om oss (valgfritt)
                    <textarea name="description" maxLength={1000} rows={5} defaultValue={business.description ?? ""} />
                </label>
                <label className={styles.field}>
                    Telefon
                    <input name="phone" type="tel" required maxLength={20} defaultValue={business.phoneNumber} />
                </label>
                <label className={styles.field}>
                    Adresse
                    <input name="address" required defaultValue={business.address} />
                </label>
                <label className={styles.field}>
                    Postnummer
                    <input name="postalCode" required inputMode="numeric" pattern="[0-9]{4}" maxLength={4} defaultValue={business.postalCode} />
                </label>
                <label className={styles.field}>
                    By
                    <input name="city" required maxLength={200} defaultValue={business.city} />
                </label>
                <SubmitButton className={styles.primaryButton}>
                    Lagre profil
                </SubmitButton>
            </form>
        </main>
    );
}