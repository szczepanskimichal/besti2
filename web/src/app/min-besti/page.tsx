import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getMe, getMyBookings, getMyInvites } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { statusLabels } from "@/lib/bookingStatus";
import { formatDate, formatTime } from "@/lib/format";
import { bookingsToReview, upcomingBookings } from "@/lib/minBesti";
import { canCancelOnline } from "@/lib/freeCancel";
import { answerMyNewTimeAction, shareInviteAgainAction } from "./actions";
import { withdrawInviteAction } from "../invitasjon/actions";
import CancelBookingForm from "./CancelBookingForm";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
// Same cards, status colours and buttons as the business panel
import styles from "../panel/page.module.css";

export const metadata: Metadata = {
    title: "Min Besti",
};

type MyBookingsPageProps = {
    searchParams: Promise<{ feil?: string; avbestilt?: string; svar?: string; invitasjon?: string }>;
};

// Only the coming visits: past and cancelled ones are gone (past visits can still be reviewed for 30 days)
export default async function MyBookingsPage({ searchParams }: MyBookingsPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    // Both at the same time instead of one after the other
    const [me, bookings, invites] = await Promise.all([getMe(token), getMyBookings(token), getMyInvites(token)]);
    if (me?.isBusiness) {
        redirect("/panel");
    }
    if (!bookings) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const { feil, avbestilt, svar, invitasjon } = await searchParams;
    const upcoming = upcomingBookings(bookings);
    // BESTI sammen: my invitations others can still join, and who each shared visit is with
    const waiting = (invites ?? []).filter((i) => i.open && i.isInviter);
    const together = new Map((invites ?? []).map((i) => [i.id, i]));
    const myFirstName = me?.fullName?.split(" ")[0];
    const others = (names: string[]) => {
        const rest = [...names];
        const mine = rest.indexOf(myFirstName ?? "");
        if (mine >= 0) {
            rest.splice(mine, 1);
        }
        return rest.length <= 1 ? rest.join("") : `${rest.slice(0, -1).join(", ")} og ${rest[rest.length - 1]}`;
    };
    const toReview = bookingsToReview(bookings).length;
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Min Besti</h1>
            <MinBestiTabs/>
            {feil === "ny-tid" && (
                <p className={styles.error} role="alert">
                    Svaret kunne ikke lagres. Timen kan allerede være endret eller ha startet.
                </p>
            )}
            {svar === "godtatt" && (
                <p className={styles.muted} role="status">Takk! Vi har gitt beskjed til bedriften.</p>
            )}
            {svar === "avslatt" && (
                <p className={styles.muted} role="status">Timen er avlyst, og bedriften har fått beskjed.</p>
            )}
            {avbestilt && <p className={styles.muted} role="status">Bestillingen er avbestilt.</p>}
            {invitasjon === "trukket" && (
                <p className={styles.muted} role="status">Invitasjonen er trukket tilbake. Ingenting ble bestilt.</p>
            )}
            {feil === "invitasjon" && (
                <p className={styles.error} role="alert">Invitasjonen kunne ikke endres. Kanskje noen nettopp svarte?</p>
            )}
            {waiting.length > 0 && (
                <>
                    <h2>Venter på svar <span aria-hidden="true">👯</span></h2>
                    <ul className={styles.list}>
                        {waiting.map((invite) => (
                            <li key={invite.id} className={styles.card}>
                                <div className={styles.cardHeader}>
                                    <p className={styles.when}>
                                        {formatDate(invite.startUtc)}, {formatTime(invite.startUtc)}
                                    </p>
                                    <span className={styles.status}>
                                        Svar innen {formatTime(invite.expiresUtc)}
                                    </span>
                                </div>
                                <p className={styles.service}>
                                    {invite.serviceName} hos <Link href={`/${invite.businessSlug}`}>{invite.businessName}</Link>
                                </p>
                                <p className={styles.muted}>
                                    {invite.joined === 0
                                        ? invite.persons === 2 ? "Ingenting er bestilt før vennen din svarer." : `${invite.persons} plasser. Ingenting er bestilt før noen svarer.`
                                        : `${invite.joined} av ${invite.persons} er med: ${invite.names.join(", ")}.`}
                                </p>
                                <div className={styles.actions}>
                                    <form action={shareInviteAgainAction}>
                                        <input type="hidden" name="inviteId" value={invite.id}/>
                                        <SubmitButton className={styles.primaryButton} pendingText="Lager lenke…">Del igjen</SubmitButton>
                                    </form>
                                    {/* After the first yes the visits are booked: then it just runs until the deadline */}
                                    {invite.status === "Venter" && (
                                        <form action={withdrawInviteAction}>
                                            <input type="hidden" name="inviteId" value={invite.id}/>
                                            <SubmitButton className={styles.dangerButton} pendingText="Trekker tilbake…">Trekk tilbake</SubmitButton>
                                        </form>
                                    )}
                                </div>
                            </li>
                        ))}
                    </ul>
                </>
            )}
            {toReview > 0 && (
                <p>
                    <Link href="/min-besti/vurderinger">
                        Du har {toReview === 1 ? "1 time" : `${toReview} timer`} du kan vurdere ⭐
                    </Link>
                </p>
            )}
            <h2>Kommende</h2>
            {upcoming.length === 0 ? (
                <p className={styles.muted}>
                    Du har ingen kommende bestillinger. <Link href="/">Finn en bedrift</Link>
                </p>
            ) : (
                <ul className={styles.list}>
                    {upcoming.map((booking) => (
                        <li key={booking.id} className={styles.card}>
                            <div className={styles.cardHeader}>
                                <p className={styles.when}>
                                    {formatDate(booking.startUtc)}, {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                                </p>
                                <span className={`${styles.status} ${styles[booking.status] ?? ""}`}>
                                    {booking.status === "VenterKunde"
                                        ? "Svar ønskes"
                                        : statusLabels[booking.status] ?? booking.status}
                                </span>
                            </div>
                            <p className={styles.service}>
                                {booking.serviceName} hos {booking.employeeName}
                            </p>
                            {booking.inviteId && together.has(booking.inviteId) && others(together.get(booking.inviteId)!.names) && (
                                <p className={styles.muted}>
                                    <span aria-hidden="true">👯</span> Sammen med {others(together.get(booking.inviteId)!.names)}
                                </p>
                            )}
                            <p className={styles.customer}>
                                <Link href={`/${booking.businessSlug}`}>{booking.businessName}</Link>
                                <br/>
                                {booking.businessAddress}
                            </p>
                            {/* The business moved the visit: the customer accepts or declines the new time */}
                            {booking.status === "VenterKunde" && (
                                <div className={styles.note}>
                                    <p>
                                        Bedriften ønsker å flytte timen
                                        {booking.previousStartUtc &&
                                            ` fra ${formatDate(booking.previousStartUtc)} kl. ${formatTime(booking.previousStartUtc)}`}.
                                        Passer den nye tiden?
                                    </p>
                                    <div className={styles.actions}>
                                        <form action={answerMyNewTimeAction}>
                                            <input type="hidden" name="bookingId" value={booking.id}/>
                                            <input type="hidden" name="svar" value="ja"/>
                                            <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">Godta ny tid</SubmitButton>
                                        </form>
                                        <form action={answerMyNewTimeAction}>
                                            <input type="hidden" name="bookingId" value={booking.id}/>
                                            <input type="hidden" name="svar" value="nei"/>
                                            <SubmitButton className={styles.dangerButton} pendingText="Lagrer…">Passer ikke</SubmitButton>
                                        </form>
                                    </div>
                                </div>
                            )}
                            <p>
                                <a href={`/min-besti/kalender/${booking.id}`}>Legg til i kalender</a>
                            </p>
                            {/* After the business's free-cancel deadline: only by phone */}
                            {canCancelOnline(booking) ? (
                                <details className={styles.actions}>
                                    <summary>Avbestill</summary>
                                    <p>
                                        Avbestille {booking.serviceName} {formatDate(booking.startUtc)} kl. {formatTime(booking.startUtc)}?
                                        Tiden blir ledig for andre.
                                    </p>
                                    <CancelBookingForm bookingId={booking.id}/>
                                </details>
                            ) : (
                                <p className={styles.muted}>
                                    Fristen for gratis avbestilling er ute.{" "}
                                    <a href={`tel:${booking.businessPhone}`}>Ring bedriften</a> hvis du ikke kan komme.
                                </p>
                            )}
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}
