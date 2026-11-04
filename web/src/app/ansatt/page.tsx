import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getStaffSchedule } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDay, formatTime, isIsoDate, osloDay, shiftDay, todayInOslo } from "@/lib/format";
import { privatePage } from "@/lib/site";
import type { StaffPlace } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { leaveStaffPlaceAction } from "./actions";
import panelStyles from "../panel/page.module.css";
import styles from "./page.module.css";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";

export const metadata: Metadata = {
    title: "Arbeidsplan",
    robots: privatePage,
};

const Days = 7;

type StaffPageProps = {
    searchParams: Promise<{ fra?: string; velkommen?: string }>;
};

type Entry = { key: string; startUtc: string; endUtc: string; content: React.ReactNode };

// The visits and blocked times of one place, day by day (only days that have something)
function daysOf(place: StaffPlace, from: string): { day: string; entries: Entry[] }[] {
    const entries: (Entry & { day: string })[] = [
        ...place.bookings.map((b) => ({
            key: b.id, day: osloDay(b.startUtc), startUtc: b.startUtc, endUtc: b.endUtc,
            content: (
                <>
                    <strong><CrystalIcon name="stjerne"/> {b.serviceName}</strong> · {b.customerName}{" "}
                    <a href={`tel:${b.customerPhone}`}>{b.customerPhone}</a>
                    {b.status === "Venter" && <span className={styles.waiting}> Venter på svar</span>}
                    {b.customerNote && <span className={styles.note}>Kunden: {b.customerNote}</span>}
                    {b.salonNote && <span className={styles.note}><CrystalIcon name="notatblokk"/> {b.salonNote}</span>}
                </>
            ),
        })),
        ...place.blocks.map((b) => ({
            key: `blokk-${b.startUtc}`, day: osloDay(b.startUtc), startUtc: b.startUtc, endUtc: b.endUtc,
            content: <span className={panelStyles.muted}>Blokkert{b.reason ? `: ${b.reason}` : ""}</span>,
        })),
    ];
    return Array.from({ length: Days }, (_, i) => shiftDay(from, i))
        .map((day) => ({
            day,
            entries: entries.filter((e) => e.day === day).sort((a, b) => a.startUtc.localeCompare(b.startUtc)),
        }))
        .filter((d) => d.entries.length > 0);
}

// "10:00–11:00"; a day blocked from midnight to midnight is "Hele dagen", not "00:00–00:00"
function clockRange(startUtc: string, endUtc: string): string {
    const start = formatTime(startUtc);
    const end = formatTime(endUtc);
    return start === "00:00" && end === "00:00" ? "Hele dagen" : `${start}–${end}`;
}

// An employee's own week: only their own visits, with the phone number and notes; no prices or settings
export default async function StaffPage({ searchParams }: StaffPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/ansatt");
    }
    const { fra, velkommen } = await searchParams;
    const today = todayInOslo();
    const from = isIsoDate(fra) ? fra : today;
    const places = await getStaffSchedule(token, from, Days);
    if (!places) {
        redirect("/logg-inn?neste=/ansatt");
    }

    return (
        <main className={panelStyles.main}>
            <h1 className={panelStyles.title}>Arbeidsplan</h1>
            {velkommen && <p className={styles.welcome} role="status">Velkommen! Nå ser du timene dine her. <CrystalIcon name="feiring"/></p>}
            {places.length === 0 ? (
                <p className={panelStyles.muted}>
                    Du er ikke koblet til noen bedrift. Be bedriften sende deg en invitasjon på e-post.
                </p>
            ) : (
                <>
                    <nav className={styles.weekNav} aria-label="Velg uke">
                        <Link href={`/ansatt?fra=${shiftDay(from, -Days)}`}>← Forrige</Link>
                        <span>{formatDay(from)} – {formatDay(shiftDay(from, Days - 1))}</span>
                        <Link href={`/ansatt?fra=${shiftDay(from, Days)}`}>Neste →</Link>
                    </nav>
                    {from !== today && <p><Link href="/ansatt">Til i dag</Link></p>}
                    {places.map((place) => {
                        const days = daysOf(place, from);
                        return (
                            <section key={place.employeeId} className={styles.place}>
                                <h2 className={styles.placeTitle}>
                                    <Link href={`/${place.businessSlug}`}>{place.businessName}</Link>
                                    <span className={panelStyles.muted}> som {place.employeeName}</span>
                                </h2>
                                {days.length === 0 ? (
                                    <p className={panelStyles.muted}>Ingen timer denne uka.</p>
                                ) : (
                                    days.map(({ day, entries }) => (
                                        <div key={day} className={panelStyles.card}>
                                            <h3 className={styles.day}>{formatDay(day)}</h3>
                                            <ul className={styles.entries}>
                                                {entries.map((e) => (
                                                    <li key={e.key}>
                                                        <span className={styles.time}>
                                                            {clockRange(e.startUtc, e.endUtc)}
                                                        </span>
                                                        <span className={styles.text}>{e.content}</span>
                                                    </li>
                                                ))}
                                            </ul>
                                        </div>
                                    ))
                                )}
                                <details className={styles.leave}>
                                    <summary>Slutt å se planen</summary>
                                    <p className={panelStyles.muted}>
                                        Du ser ikke lenger timene hos {place.businessName}. Bedriften kan invitere deg
                                        igjen.
                                    </p>
                                    <form action={leaveStaffPlaceAction}>
                                        <input type="hidden" name="employeeId" value={place.employeeId}/>
                                        <SubmitButton className={panelStyles.dangerButton} pendingText="Lagrer…">
                                            Ja, slutt å se planen
                                        </SubmitButton>
                                    </form>
                                </details>
                            </section>
                        );
                    })}
                </>
            )}
        </main>
    );
}
