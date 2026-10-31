"use client";
import { useActionState } from "react";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import type { MyProfile } from "@/lib/api";
import { changePasswordAction, deleteAccountAction, type ProfileFormState, updateProfileAction } from "./actions";
import formStyles from "../../[slug]/bestill/page.module.css";
import panelStyles from "../../panel/page.module.css";
import styles from "./page.module.css";

// "Lagret" as a glowing pill with a check
function Saved({ children }: { children: React.ReactNode }) {
    return (
        <p className={styles.saved} role="status">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round"
                 strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="9"/><path d="M8 12.5l2.6 2.6L16 9.5"/>
            </svg>
            {children}
        </p>
    );
}

// Separate forms, so an error in one never empties the others. Password and account are folded away
export default function ProfileForms({ profile }: { profile: MyProfile }) {
    const [details, detailsAction] = useActionState<ProfileFormState, FormData>(updateProfileAction, {});
    const [password, passwordAction] = useActionState<ProfileFormState, FormData>(changePasswordAction, {});
    const [deletion, deleteAction] = useActionState<ProfileFormState, FormData>(deleteAccountAction, {});
    const value = (field: string, fallback: string) => details.values?.[field] ?? fallback;

    return (
        <>
            <section className={styles.card}>
                <h2 className={styles.heading}>Mine opplysninger</h2>
                <form action={detailsAction} className={styles.form}>
                    {details.error && <p className={formStyles.error} role="alert">{details.error}</p>}
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
                    <SubmitButton className={`${formStyles.primaryButton} ${styles.glow}`} pendingText="Lagrer…">Lagre</SubmitButton>
                    {details.saved && <Saved>Lagret</Saved>}
                </form>
            </section>

            {/* Open after saving, so the "Lagret" message is seen */}
            <details className={styles.fold} open={password.saved || !!password.error}>
                <summary>{profile.hasPassword ? "Endre passord" : "Lag passord"}</summary>
                <div className={styles.foldBody}>
                    {!profile.hasPassword && (
                        <p className={panelStyles.muted}>
                            Du logger inn med Google. Med et passord kan du også logge inn med e-post.
                        </p>
                    )}
                    <form action={passwordAction} className={styles.form}>
                        {password.error && <p className={formStyles.error} role="alert">{password.error}</p>}
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
                        <SubmitButton className={`${formStyles.primaryButton} ${styles.glow}`} pendingText="Lagrer…">
                            {profile.hasPassword ? "Endre passord" : "Lag passord"}
                        </SubmitButton>
                        {password.saved && <Saved>Passordet er lagret</Saved>}
                    </form>
                </div>
            </details>

            <details className={styles.fold} open={!!deletion.error}>
                <summary>Personvern og konto</summary>
                <div className={styles.foldBody}>
                    <h3 className={styles.subheading}>Mine data</h3>
                    <p className={panelStyles.muted}>
                        Last ned alt BESTI lagrer om deg: konto, timer, vurderinger og varsler (en JSON-fil).
                    </p>
                    {/* A plain link: the browser saves the file */}
                    <a href="/min-besti/profil/eksport" className={panelStyles.secondaryButton} download>
                        Last ned mine data
                    </a>
                    <div className={styles.danger}>
                        <h3 className={styles.subheading}>Slett konto</h3>
                        <p>
                            Kommende timer blir avlyst, og bedriftene får beskjed. Tidligere timer beholdes uten navnet ditt,
                            og vurderingene dine vises som «Tidligere kunde». Dette kan ikke angres.
                        </p>
                        <details>
                            <summary className={`${panelStyles.dangerButton} ${styles.pink}`}>Slett kontoen min</summary>
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
                                <SubmitButton className={`${panelStyles.dangerButton} ${styles.pink}`} pendingText="Sletter…">
                                    Ja, slett kontoen min
                                </SubmitButton>
                            </form>
                        </details>
                    </div>
                </div>
            </details>
        </>
    );
}
