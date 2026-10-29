import { SITE_URL } from "@/lib/site";
import CopyButton from "@/components/CopyButton/CopyButton";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { calendarFeedAction } from "./actions";
import styles from "../tjenester/page.module.css";
import own from "./CalendarFeed.module.css";

// "Kalender på telefonen": all bookings in Google, Apple or Outlook Calendar, updated by itself.
// feedToken = null while it is turned off
export default function CalendarFeed({ feedToken, status }: { feedToken: string | null; status?: string }) {
    const url = feedToken ? `${SITE_URL}/kalender/${feedToken}.ics` : null;
    // webcal:// opens "Abonner på kalender" on iPhone and Mac; Google takes the same address in ?cid=
    const webcal = url?.replace(/^https?:\/\//, "webcal://");

    return (
        <>
            <h2 id="kalender" className={styles.subtitle}>Kalender på telefonen <span aria-hidden="true">📅</span></h2>
            <div className={styles.card}>
                {status === "lagret" && <p className={styles.success} role="status">Lagret.</p>}
                {status === "feil" && <p className={styles.error} role="alert">Noe gikk galt. Prøv igjen.</p>}
                {!url || !webcal ? (
                    <>
                        <p>
                            Se alle timene i kalenderen du allerede bruker: Google, iPhone/Mac eller Outlook.
                            Nye, flyttede og avlyste timer oppdateres av seg selv.
                        </p>
                        <form action={calendarFeedAction}>
                            <input type="hidden" name="action" value="pa"/>
                            <SubmitButton className={own.primary} pendingText="Slår på…">Slå på</SubmitButton>
                        </form>
                    </>
                ) : (
                    <>
                        <p>Trykk på kalenderen du bruker, og bekreft der:</p>
                        <div className={styles.cardActions}>
                            <a href={webcal} className={own.primary}>iPhone / Mac</a>
                            <a href={`https://calendar.google.com/calendar/r?cid=${encodeURIComponent(webcal)}`}
                               className={styles.secondaryButton} target="_blank" rel="noopener noreferrer">
                                Google Kalender
                            </a>
                            <CopyButton text={url} label="Kopier lenken (Outlook)" className={styles.secondaryButton}/>
                        </div>
                        <p className={styles.muted}>
                            iPhone og Mac henter nye timer hvert kvarter. Google Kalender bestemmer selv og kan bruke
                            noen timer. Venter-timer vises som «Venter:».
                        </p>
                        <p className={styles.muted}>
                            Lenken viser kundenes navn og telefonnummer. Ikke del den med andre enn de ansatte.
                        </p>
                        <details className={styles.deleteBox}>
                            <summary>Lag ny lenke eller slå av</summary>
                            <p>
                                En ny lenke gjør den gamle ugyldig (f.eks. hvis en telefon er mistet). Da må kalenderen
                                legges til på nytt.
                            </p>
                            <div className={styles.cardActions}>
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
            </div>
        </>
    );
}
