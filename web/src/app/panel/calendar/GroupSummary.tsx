import Link from "next/link";
import { formatTime } from "@/lib/format";
import type { PanelBooking, PanelGroup } from "@/lib/types";
import styles from "../page.module.css";
import calendar from "../calendar.module.css";

// BESTI sammen seen by the business: the whole group as one event, so the salon knows how many come.
// "Gruppe 👯 5 plasser: 3 med, 2 venter på svar, svarfrist kl. 18:00"
export function groupLine(group: PanelGroup): string {
    const waiting = group.persons - group.joined;
    if (!group.open) {
        return `${group.joined} ${group.joined === 1 ? "person" : "personer"} kommer${waiting > 0 ? ` (${waiting} plass${waiting === 1 ? "" : "er"} ble ikke tatt)` : ""}`;
    }
    return `${group.persons} plasser: ${group.joined} med, ${waiting} venter på svar, svarfrist kl. ${formatTime(group.expiresUtc)}`;
}

// The group box in the details window: the count, who organizes and pays, and every member with a link
export default function GroupSummary({ group, members, current }: {
    group: PanelGroup; members: { booking: PanelBooking; href: string }[]; current: string
}) {
    return (
        <div className={calendar.group}>
            <p className={calendar.groupTitle}>
                <span aria-hidden="true">👯</span> Gruppe{group.open ? "" : " klar"} · {groupLine(group)}
            </p>
            <p className={styles.muted}>
                Organisert av {group.organizerName}
                {!group.organizerComes && " (kommer ikke selv)"}
                {group.organizerPays ? " · betaler for hele gruppa 🎁" : " · hver betaler selv"}
            </p>
            <ol className={calendar.groupList}>
                {group.names.map((name, i) => {
                    const member = members.find((m) => m.booking.customerName === name);
                    return (
                        <li key={`${name}-${i}`}>
                            {member && member.booking.id !== current
                                ? <Link href={member.href} scroll={false}>{name}</Link>
                                : <strong>{name}</strong>}
                            {member && ` · ${member.booking.employeeName}`}
                        </li>
                    );
                })}
            </ol>
        </div>
    );
}

// Above the calendar: every group in this view once, with the count, so nothing gets lost among the columns
export function GroupStrip({ bookings, hrefFor }: { bookings: PanelBooking[]; hrefFor: (booking: PanelBooking) => string }) {
    const first = new Map<string, PanelBooking>();
    for (const booking of bookings) {
        if (booking.group && booking.status !== "Avlyst" && !first.has(booking.group.id)) {
            first.set(booking.group.id, booking);
        }
    }
    if (first.size === 0) {
        return null;
    }
    return (
        <ul className={calendar.groupStrip} aria-label="BESTI sammen-grupper">
            {[...first.values()].map((booking) => (
                <li key={booking.group!.id}>
                    <Link href={hrefFor(booking)} scroll={false} className={calendar.groupChip}>
                        <span aria-hidden="true">👯</span>{" "}
                        <strong>{formatTime(booking.startUtc)} {booking.serviceName}</strong>
                        {" · "}gruppa til {booking.group!.organizerName} · {groupLine(booking.group!)}
                    </Link>
                </li>
            ))}
        </ul>
    );
}
