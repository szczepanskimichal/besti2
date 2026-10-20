import Link from 'next/link';
import { redirect } from "next/navigation";
import { getPanelBookings } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate, formatTime } from "@/lib/format";
import { statusLabels } from "@/lib/bookingStatus";
import { logoutAction } from "../logg-inn/actions";
import { cancelBookingAction, confirmBookingAction } from "./actions";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

type PanelPageProps = {
    searchParams: Promise<{ feil?: string }>;
};

export default async function PanelPage({ searchParams }: PanelPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const bookings = await getPanelBookings(token);
    if (!bookings) {
        redirect("/logg-inn");
    }

    const { feil } = await searchParams;

    return (
        <main className={styles.main}>
            <div className={styles.topBar}>
                <h1 className={styles.title}>Bestillinger</h1>
                <div className={styles.topActions}>
                    <Link href="/panel/tjenester" className={styles.secondaryButton}>
                        Tjenester
                    </Link>
                    <Link href="/panel/ansatte" className={styles.secondaryButton}>
                        Ansatte
                    </Link>
                    <Link href={"/panel/profil"} className={styles.secondaryButton}>
                        Profil
                    </Link>
                    <Link href="/panel/galleri" className={styles.secondaryButton}>
                        Galleri
                    </Link>
                    <Link href="/panel/apningstider" className={styles.secondaryButton}>
                        Åpningstider
                    </Link>
                    <form action={logoutAction}>
                        <button type="submit" className={styles.secondaryButton}>
                            Logg ut
                        </button>
                    </form>
                </div>
            </div>

            {feil && (
                <p className={styles.error} role="alert">
                    Kunne ikke endre bestillingen. Prøv igjen.
                </p>
            )}

            {bookings.length === 0 ? (
                <p className={styles.muted}>Ingen bestillinger ennå.</p>
            ) : (
                <ul className={styles.list}>
                    {bookings.map((booking) => (
                        <li key={booking.id} className={styles.card}>
                            <div className={styles.cardHeader}>
                                <p className={styles.when}>
                                    {formatDate(booking.startUtc)}, {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                                </p>
                                <span className={`${styles.status} ${styles[booking.status] ?? ""}`}>
                                    {statusLabels[booking.status] ?? booking.status}
                                </span>
                            </div>

                            <p className={styles.service}>
                                {booking.serviceName} hos {booking.employeeName}
                            </p>

                            <p className={styles.customer}>
                                {booking.customerName}
                                <br />
                                <a href={`tel:${booking.customerPhone}`}>{booking.customerPhone}</a>
                                {" · "}
                                <a href={`mailto:${booking.customerEmail}`}>{booking.customerEmail}</a>
                            </p>
                            {booking.customerNote && (
                                <div className={styles.note}>
                                    <p><span aria-hidden="true">💬</span> {booking.customerNote}</p>
                                    <a href={`mailto:${booking.customerEmail}?subject=${encodeURIComponent("Svar på bestillingen din")}`}>
                                        Svar på e-post
                                    </a>
                                </div>
                            )}
                            {booking.status !== "Avlyst" && (
                                <div className={styles.actions}>
                                    {booking.status === "Venter" && (
                                        <form action={confirmBookingAction}>
                                            <input type="hidden" name="bookingId" value={booking.id} />
                                            <SubmitButton className={styles.primaryButton} pendingText="Bekrefter…">
                                                Bekreft
                                            </SubmitButton>
                                        </form>
                                    )}
                                    <details>
                                    <summary className={styles.dangerButton}>Avlys</summary>
                                    <p>Er du sikker? Kunden får beskjed på e-post.</p>
                                    <form action={cancelBookingAction}>
                                        <input type="hidden" name="bookingId" value={booking.id} />
                                        <SubmitButton className={styles.dangerButton} pendingText="Avlyser…">
                                            Ja, avlys
                                        </SubmitButton>
                                    </form>
                                </details>
                                </div>
                            )}
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}
