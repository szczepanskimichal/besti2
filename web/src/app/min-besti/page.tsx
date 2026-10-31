import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getMe, getMyBookings, getMyInvites, getMyRebookReminders, getMyWaitlist } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { statusLabels } from "@/lib/bookingStatus";
import { formatDate, formatDay, formatTime, isIsoDate, osloDay, shiftDay, todayInOslo } from "@/lib/format";
import { bookingsToReview, upcomingBookings } from "@/lib/minBesti";
import { canCancelOnline } from "@/lib/freeCancel";
import type { CustomerBooking, RebookReminder } from "@/lib/types";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import { answerMyNewTimeAction, leaveWaitlistAction, removeRebookReminderAction, shareInviteAgainAction } from "./actions";
import { withdrawInviteAction } from "../invitasjon/actions";
import CancelBookingForm from "./CancelBookingForm";
import Countdown, { FreeCancelLeft } from "./Countdown";
import MyCalendar, { type CalendarView, type Suggestion } from "./MyCalendar";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
// Same cards, status colours and buttons as the business panel
import styles from "../panel/page.module.css";
import waitlistStyles from "./waitlist.module.css";
import mine from "./bookings.module.css";
import EmptyState from "@/components/EmptyState/EmptyState";

export const metadata: Metadata = {
    title: "Min BESTI",
};

type MyBookingsPageProps = {
    searchParams: Promise<{ feil?: string; avbestilt?: string; svar?: string; invitasjon?: string; visning?: string; dag?: string }>;
};

// Only the coming visits: past and cancelled ones are gone (past visits can still be reviewed for 30 days)
export default async function MyBookingsPage({ searchParams }: MyBookingsPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    // Both at the same time instead of one after the other
    const [me, bookings, invites, waitlist, rebook] = await Promise.all([
        getMe(token), getMyBookings(token), getMyInvites(token), getMyWaitlist(token), getMyRebookReminders(token),
    ]);
    if (me?.isBusiness) {
        redirect("/panel");
    }
    if (!bookings) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const { feil, avbestilt, svar, invitasjon, visning, dag } = await searchParams;
    const upcoming = upcomingBookings(bookings);
    // Min kalender: all visits that were not cancelled (also the past ones, for "Bestill igjen"), soonest first
    const today = todayInOslo();
    const view: CalendarView = ["ar", "maned", "uke", "dag", "plan"].includes(visning ?? "") ? visning as CalendarView : "maned";
    const calendarDay = isIsoDate(dag) ? dag : today;
    const calendarVisits = bookings.filter((b) => b.status !== "Avlyst")
        .sort((a, b) => a.startUtc.localeCompare(b.startUtc));
    const calendarLink = (to: CalendarView, day: string) =>
        `/min-besti?${new URLSearchParams({ visning: to, dag: day })}`;
    const suggestions = rebookSuggestions(rebook, calendarVisits, today);
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
        <main className={`${styles.main} ${mine.page}`}>
            <h1 className={mine.title}>Min BESTI</h1>
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
            <MyCalendar view={view} day={calendarDay} today={today} visits={calendarVisits} waitlist={waitlist}
                        suggestions={suggestions} link={calendarLink}/>
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
                <Link href="/min-besti/vurderinger" className={mine.review}>
                    <CrystalIcon name="stjerne"/>
                    Du har {toReview === 1 ? "1 time" : `${toReview} timer`} du kan vurdere
                </Link>
            )}
            {waitlist.length > 0 && (
                <>
                    <h2>Venteliste <CrystalIcon name="bjelle"/></h2>
                    <ul className={styles.list}>
                        {waitlist.map((entry) => {
                            const times = `/${entry.businessSlug}/bestill?${new URLSearchParams({
                                tjeneste: entry.serviceId, maned: entry.date.slice(0, 7), dato: entry.date,
                                ...(entry.employeeId ? { ansatt: entry.employeeId } : {}),
                            })}`;
                            return (
                                <li key={entry.id} className={styles.card}>
                                    <div className={styles.cardHeader}>
                                        <p className={styles.when}>{formatDay(entry.date)}</p>
                                        <span className={styles.status}>{entry.notified ? "Det ble ledig!" : "Venter"}</span>
                                    </div>
                                    <p className={styles.service}>
                                        {entry.serviceName} hos <Link href={`/${entry.businessSlug}`}>{entry.businessName}</Link>
                                        {entry.employeeName && ` (${entry.employeeName})`}
                                    </p>
                                    <div className={styles.actions}>
                                        <Link href={times} className={`${styles.primaryButton} ${waitlistStyles.linkButton}`}>Se ledige tider</Link>
                                        <form action={leaveWaitlistAction}>
                                            <input type="hidden" name="id" value={entry.id}/>
                                            <SubmitButton className={styles.secondaryButton} pendingText="Fjerner…">Fjern</SubmitButton>
                                        </form>
                                    </div>
                                </li>
                            );
                        })}
                    </ul>
                </>
            )}
            <h2>Kommende</h2>
            {upcoming.length === 0 ? (
                <EmptyState icon="sparkles" title="Ingen kommende timer" action={{ href: "/", label: "Finn en bedrift" }}>
                    Når du bestiller, ser du timen her, med nedtelling, veibeskrivelse og avbestilling.
                </EmptyState>
            ) : (
                <ul className={styles.list}>
                    {upcoming.map((booking) => (
                        <li key={booking.id} id={`time-${booking.id}`} className={mine.card}>
                            <div className={booking.status === "VenterKunde" ? "" : mine.besideStamp}>
                                <p className={mine.date}>{formatDate(booking.startUtc)}</p>
                                <div className={mine.timeRow}>
                                    <span className={mine.time}>{formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}</span>
                                    <Countdown startUtc={booking.startUtc}/>
                                </div>
                            </div>
                            <StatusStamp status={booking.status}/>
                            <p className={mine.place}>
                                <Link href={`/${booking.businessSlug}`}>{booking.businessName}</Link>
                                <br/>
                                {booking.businessAddress}
                            </p>
                            <p className={mine.service}>
                                <CrystalIcon name="stjerne"/> {booking.serviceName} hos {booking.employeeName}
                            </p>
                            {booking.inviteId && together.has(booking.inviteId) && others(together.get(booking.inviteId)!.names) && (
                                <p className={styles.muted}>
                                    <span aria-hidden="true">👯</span> Sammen med {others(together.get(booking.inviteId)!.names)}
                                </p>
                            )}
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
                            <p className={mine.links}>
                                <a href={`/min-besti/kalender/${booking.id}`}>Legg til i kalender</a>
                                <a href={directionsUrl(booking.businessAddress)} target="_blank" rel="noopener noreferrer">
                                    Veibeskrivelse
                                </a>
                            </p>
                            {/* After the business's free-cancel deadline: only by phone */}
                            {canCancelOnline(booking) ? (
                                <div className={mine.cancelRow}>
                                    <details className={mine.cancel}>
                                        <summary>Avbestill</summary>
                                        <p>
                                            Avbestille {booking.serviceName} {formatDate(booking.startUtc)} kl. {formatTime(booking.startUtc)}?
                                            Tiden blir ledig for andre.
                                        </p>
                                        <CancelBookingForm bookingId={booking.id}/>
                                    </details>
                                    <FreeCancelLeft untilUtc={freeCancelUntil(booking)}/>
                                </div>
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
            {rebook.length > 0 && (
                <>
                    <h2>Påminnelser om ny time</h2>
                    <ul className={styles.list}>
                        {rebook.map((reminder) => (
                            <li key={reminder.id} className={styles.card}>
                                <p className={styles.service}>
                                    {reminder.serviceName} hos{" "}
                                    <Link href={`/${reminder.businessSlug}`}>{reminder.businessName}</Link>
                                </p>
                                <p className={styles.muted}>Vi minner deg på det {reminder.weeks} uker etter hver time.</p>
                                <form action={removeRebookReminderAction} className={styles.actions}>
                                    <input type="hidden" name="id" value={reminder.id}/>
                                    <SubmitButton className={styles.secondaryButton} pendingText="Fjerner…">Slå av</SubmitButton>
                                </form>
                            </li>
                        ))}
                    </ul>
                </>
            )}
        </main>
    );
}

// Only a confirmed visit with a deadline (0 = cancel any time, and a waiting one can always be cancelled)
function freeCancelUntil(booking: CustomerBooking): string | null {
    if (booking.status !== "Bekreftet" || booking.freeCancelHours === 0) {
        return null;
    }
    return new Date(new Date(booking.startUtc).getTime() - booking.freeCancelHours * 60 * 60 * 1000).toISOString();
}

// Google Maps route from where you are (opens the Maps app on the phone). Free, no API key
function directionsUrl(address: string): string {
    return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(address)}`;
}

// "Tid for ny time?" in the calendar: the last visit of the service + the reminder's weeks (today at the earliest),
// unless a new visit of that service is already booked
function rebookSuggestions(reminders: RebookReminder[], visits: CustomerBooking[], today: string): Suggestion[] {
    const nowUtc = new Date().toISOString();
    return reminders.flatMap((reminder) => {
        const same = visits.filter((b) => b.serviceId === reminder.serviceId && b.status !== "MotteIkke");
        const last = same.filter((b) => b.endUtc < nowUtc).at(-1);
        if (!last || same.some((b) => b.startUtc > nowUtc)) {
            return [];
        }
        const due = shiftDay(osloDay(last.startUtc), reminder.weeks * 7);
        const date = due < today ? today : due;
        const query = new URLSearchParams({ tjeneste: reminder.serviceId, maned: date.slice(0, 7), dato: date });
        if (reminder.employeeId) {
            query.set("ansatt", reminder.employeeId);
        }
        return [{
            date,
            serviceName: reminder.serviceName,
            businessName: reminder.businessName,
            bookHref: `/${reminder.businessSlug}/bestill?${query}`,
        }];
    });
}

// Bekreftet and Venter as a tilted glowing stamp in the corner; "Svar ønskes" stays a pink label, the note below asks
function StatusStamp({ status }: { status: string }) {
    if (status === "VenterKunde") {
        return <span className={`${styles.status} ${styles.VenterKunde}`}>Svar ønskes</span>;
    }
    const waiting = status !== "Bekreftet";
    return (
        <span className={`${mine.stamp} ${waiting ? mine.stampWaiting : ""}`} role="img"
              aria-label={`Status: ${statusLabels[status] ?? status}`}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.6} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                {waiting ? <><circle cx="12" cy="12" r="8.5"/><path d="M12 7.5v4.8l3.2 2"/></> : <path d="M5 12.5l4.5 4.5L19 7.5"/>}
            </svg>
            <span aria-hidden="true">{waiting ? <>VENTER PÅ<small>BEKREFTELSE</small></> : "BEKREFTET"}</span>
        </span>
    );
}
