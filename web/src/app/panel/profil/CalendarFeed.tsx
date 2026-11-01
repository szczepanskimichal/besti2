import { SITE_URL } from "@/lib/site";
import CopyButton from "@/components/CopyButton/CopyButton";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { calendarFeedAction } from "./actions";
import styles from "./profil.module.css";

// "Kalender på telefonen": all bookings in Google, Apple or Outlook Calendar, updated by itself.
// feedToken = null while it is turned off
export default function CalendarFeed({ feedToken, status }: { feedToken: string | null; status?: string }) {
    const url = feedToken ? `${SITE_URL}/kalender/${feedToken}.ics` : null;
    // webcal:// opens "Abonner på kalender" on iPhone and Mac; Google takes the same address in ?cid=
    const webcal = url?.replace(/^https?:\/\//, "webcal://");

    return (
        <>
            <section id="kalender" className={styles.section}>
                <h2 className={styles.sectionTitle}>Timene i kalenderen på telefonen</h2>
                <p className={styles.text}>
                    BESTI-kalenderen finner du under Kalender. Her kan du i tillegg se timene i kalenderen du bruker
                    på telefonen.
                </p>
                {status === "lagret" && <p className={styles.notice} role="status">Lagret.</p>}
                {status === "feil" && <p className={styles.error} role="alert">Noe gikk galt. Prøv igjen.</p>}
                {!url || !webcal ? (
                    <>
                        <p className={styles.text}>
                            Det virker med Google, iPhone/Mac og Outlook. Nye, flyttede og avlyste timer
                            oppdateres av seg selv.
                        </p>
                        <form action={calendarFeedAction}>
                            <input type="hidden" name="action" value="pa"/>
                            <SubmitButton className={styles.saveButton} pendingText="Slår på…">Slå på</SubmitButton>
                        </form>
                    </>
                ) : (
                    <>
                        <p className={styles.text}>Trykk på kalenderen du bruker, og bekreft der:</p>
                        {/* Three equal choices, each with its own icon: it depends on the phone, not one main button */}
                        <div className={styles.calendarChoices}>
                            <a href={webcal} className={`${styles.calendarChoice} ${styles.calPhone}`}>iPhone / Mac</a>
                            <a href={`https://calendar.google.com/calendar/r?cid=${encodeURIComponent(webcal)}`}
                               className={`${styles.calendarChoice} ${styles.calGoogle}`}
                               target="_blank" rel="noopener noreferrer">
                                Google Kalender
                            </a>
                            <CopyButton text={url} label="Kopier lenken (Outlook)"
                                        className={`${styles.calendarChoice} ${styles.calCopy}`}/>
                        </div>
                        <p className={styles.muted}>
                            iPhone og Mac henter nye timer hvert kvarter. Google Kalender bestemmer selv og kan bruke
                            noen timer. Venter-timer vises som «Venter:».
                        </p>
                        <p className={styles.muted}>
                            Lenken viser kundenes navn og telefonnummer. Ikke del den med andre enn de ansatte.
                        </p>
                        <details className={styles.more}>
                            {/* Rarely needed (lost phone): a quiet text link, not a button */}
                            <summary className={styles.quietLink}>Lag ny lenke eller slå av</summary>
                            <p className={styles.text}>
                                En ny lenke gjør den gamle ugyldig (f.eks. hvis en telefon er mistet). Da må kalenderen
                                legges til på nytt.
                            </p>
                            <div className={styles.buttons}>
                                <form action={calendarFeedAction}>
                                    <input type="hidden" name="action" value="ny"/>
                                    <SubmitButton className={styles.secondaryButton} pendingText="Lager…">Lag ny lenke</SubmitButton>
                                </form>
                                <form action={calendarFeedAction}>
                                    <input type="hidden" name="action" value="av"/>
                                    <SubmitButton className={styles.dangerButton} pendingText="Slår av…">Slå av</SubmitButton>
                                </form>
                            </div>
                        </details>
                    </>
                )}
            </section>
        </>
    );
}
