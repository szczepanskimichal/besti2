"use client";
import { useActionState } from "react";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import type { MyProfile } from "@/lib/api";
import { changePasswordAction, deleteAccountAction, type ProfileFormState, updateProfileAction } from "./actions";
import formStyles from "../../[slug]/bestill/page.module.css";
import panelStyles from "../../panel/page.module.css";
import styles from "./page.module.css";

// Three separate forms, so an error in one never empties the others
export default function ProfileForms({ profile }: { profile: MyProfile }) {
    const [details, detailsAction] = useActionState<ProfileFormState, FormData>(updateProfileAction, {});
    const [password, passwordAction] = useActionState<ProfileFormState, FormData>(changePasswordAction, {});
    const [deletion, deleteAction] = useActionState<ProfileFormState, FormData>(deleteAccountAction, {});
    const value = (field: string, fallback: string) => details.values?.[field] ?? fallback;

    return (
        <>
            <section className={panelStyles.card}>
                <h2 className={styles.heading}>Mine opplysninger</h2>
                <form action={detailsAction} className={styles.form}>
                    {details.error && <p className={formStyles.error} role="alert">{details.error}</p>}
                    {details.saved && <p className={styles.saved} role="status">Lagret ✓</p>}
                    <label className={formStyles.field}>
                        Navn
                        <input name="fullName" required maxLength={100} autoComplete="name"
                               defaultValue={value("fullName", profile.fullName)}/>
                    </label>
                    <label className={formStyles.field}>
                        Telefon (valgfritt – fylles inn når du bestiller)
                        <input name="phone" type="tel" inputMode="tel" maxLength={20} autoComplete="tel"
                               pattern="\+?[0-9 ]{8,20}" placeholder="912 34 567"
                               defaultValue={value("phone", profile.phone ?? "")}/>
                    </label>
                    <p className={panelStyles.muted}>E-post: {profile.email}</p>
                    <SubmitButton className={formStyles.primaryButton} pendingText="Lagrer…">Lagre</SubmitButton>
                </form>
            </section>

            <section className={panelStyles.card}>
                <h2 className={styles.heading}>{profile.hasPassword ? "Endre passord" : "Lag passord"}</h2>
                {!profile.hasPassword && (
                    <p className={panelStyles.muted}>
                        Du logger inn med Google. Med et passord kan du også logge inn med e-post.
                    </p>
                )}
                <form action={passwordAction} className={styles.form}>
                    {password.error && <p className={formStyles.error} role="alert">{password.error}</p>}
                    {password.saved && <p className={styles.saved} role="status">Passordet er lagret ✓</p>}
                    {profile.hasPassword && (
                        <label className={formStyles.field}>
                            Nåværende passord
                            <input name="currentPassword" type="password" required autoComplete="current-password"/>
                        </label>
                    )}
                    <label className={formStyles.field}>
                        Nytt passord (minst 6 tegn)
                        <input name="newPassword" type="password" required minLength={6} autoComplete="new-password"/>
                    </label>
                    <label className={formStyles.field}>
                        Gjenta nytt passord
                        <input name="repeatPassword" type="password" required minLength={6} autoComplete="new-password"/>
                    </label>
                    <SubmitButton className={formStyles.primaryButton} pendingText="Lagrer…">
                        {profile.hasPassword ? "Endre passord" : "Lag passord"}
                    </SubmitButton>
                </form>
            </section>
            <section className={panelStyles.card}>
                <h2 className={styles.heading}>Mine data</h2>
                <p className={panelStyles.muted}>
                    Last ned alt BESTI lagrer om deg: konto, timer, vurderinger og varsler (en JSON-fil).
                </p>
                {/* A plain link: the browser saves the file */}
                <a href="/min-besti/profil/eksport" className={panelStyles.secondaryButton} download>
                    Last ned mine data
                </a>
            </section>
            <section className={`${panelStyles.card} ${styles.danger}`}>
                <h2 className={styles.heading}>Slett konto</h2>
                <p>
                    Kommende timer blir avlyst, og bedriftene får beskjed. Tidligere timer beholdes uten navnet ditt,
                    og vurderingene dine vises som «Tidligere kunde». Dette kan ikke angres.
                </p>
                <details>
                    <summary className={panelStyles.dangerButton}>Slett kontoen min</summary>
                    <form action={deleteAction} className={styles.form}>
                        {deletion.error && <p className={formStyles.error} role="alert">{deletion.error}</p>}
                        {/* An account from "Fortsett med Google" has no password: the word SLETT confirms instead */}
                        {profile.hasPassword ? (
                            <label className={formStyles.field}>
                                Skriv passordet ditt for å bekrefte
                                <input name="password" type="password" required autoComplete="current-password"/>
                            </label>
                        ) : (
                            <label className={formStyles.field}>
                                Skriv SLETT for å bekrefte
                                <input name="password" type="text" required autoComplete="off"/>
                            </label>
                        )}
                        <SubmitButton className={panelStyles.dangerButton} pendingText="Sletter…">
                            Ja, slett kontoen min
                        </SubmitButton>
                    </form>
                </details>
            </section>
        </>
    );
}
