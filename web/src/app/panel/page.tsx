import { cookies } from "next/headers";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBookings, getPanelBusiness, getPanelTimeOff } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDay, formatTime, isIsoDate, osloDay, shiftDay, todayInOslo, weekdayOf } from "@/lib/format";
import MoveBookingForm from "./MoveBookingForm";
import NewBookingForm from "./NewBookingForm";
import PanelNav from "./PanelNav";
import TimeOffForm from "./TimeOffForm";
import FirstSteps from "./FirstSteps";
import { FIRST_STEPS_COOKIE } from "./firstStepsCookie";
import styles from "./page.module.css";
import calendar from "./calendar.module.css";
import { employeeColumns, dayColumns, blocksOn, visibleRange, clock } from "./calendar/calendarLayout";
import { shortDay, weekDays, mondayOf, lastDayOfMonth, shiftMonth, monthTitle, isoWeek } from "./calendar/calendarDates";
import TimeGrid from "./calendar/TimeGrid";
import MonthGrid from "./calendar/MonthGrid";
import PhoneList from "./calendar/PhoneList";
import BookingDetails, { allowedActions } from "./calendar/BookingDetails";
import { GroupStrip } from "./calendar/GroupSummary";
import TimeOffDetails from "./calendar/TimeOffDetails";

type CalendarPageProps = {
    searchParams: Promise<{
        dag?: string; visning?: string; ansatt?: string; bestilling?: string; flytt?: string; feil?: string;
        ny?: string; tid?: string; for?: string; blokker?: string; blokkering?: string; varsel?: string;
    }>;
};

// The business calendar: one day with a column per employee, or one week with a column per day
export default async function CalendarPage({ searchParams }: CalendarPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const query = await searchParams;
    const day = isIsoDate(query.dag) ? query.dag : todayInOslo();
    const week = query.visning === "uke";
    const month = query.visning === "maned";
    const view = week ? "uke" : month ? "maned" : undefined;
    // Week = Monday to Sunday around the chosen day; month = whole weeks from the 1st to the last day
    const monthStart = `${day.slice(0, 7)}-01`;
    const first = week ? mondayOf(day) : month ? mondayOf(monthStart) : day;
    const last = week ? shiftDay(first, 6) : month ? shiftDay(mondayOf(lastDayOfMonth(day)), 6) : day;

    const [business, allBookings, allBlocks] = await Promise.all([
        getPanelBusiness(token), getPanelBookings(token, first, last), getPanelTimeOff(token, first, last),
    ]);
    if (!business || !allBookings || !allBlocks) {
        redirect("/logg-inn");
    }

    const employeeFilter = (week || month) && business.employees.some((e) => e.id === query.ansatt) ? query.ansatt : undefined;
    const bookings = allBookings
        // Cancelled visits are gone from the calendar (a link from a notification can still open one)
        .filter((b) => b.status !== "Avlyst")
        .filter((b) => !employeeFilter || b.employeeId === employeeFilter);
    // Week or month view with one employee: that employee's blocks and the closed days of the whole business
    const blocks = allBlocks.filter((t) => !employeeFilter || !t.employeeId || t.employeeId === employeeFilter);
    const columns = view ? dayColumns(first, last, bookings, blocks) : employeeColumns(business, bookings, blocks, day);
    const range = visibleRange(business, week ? weekDays(first) : [day], bookings);

    // The same view with one value changed (keeps the other choices)
    const link = (change: Record<string, string | undefined>) => {
        const params = new URLSearchParams();
        const values = { dag: day, visning: view, ansatt: employeeFilter, ...change };
        for (const [key, value] of Object.entries(values)) {
            if (value) params.set(key, value);
        }
        return `/panel?${params}`;
    };
    const step = week ? 7 : 1;
    const previous = month ? shiftMonth(day, -1) : shiftDay(day, -step);
    const next = month ? shiftMonth(day, 1) : shiftDay(day, step);
    const unit = month ? "måned" : week ? "uke" : "dag";
    // The visit opened in the details window (also a cancelled one, even when those are hidden)
    const selected = allBookings.find((b) => b.id === query.bestilling);
    const moving = allBookings.find((b) => b.id === query.flytt && allowedActions(b).canMove);
    // BESTI sammen: every visit of the same group, shown and linked in the details window
    const members = selected?.inviteId
        ? allBookings.filter((b) => b.inviteId === selected.inviteId && b.status !== "Avlyst")
            .map((b) => ({ booking: b, href: link({ bestilling: b.id }) }))
        : [];
    const here = link({}); // this view without the details window
    // "+ Ny bestilling", or a click on an empty place in the grid (time and employee filled in)
    const newBooking = query.ny === "1";
    const newTime = query.tid && /^\d{2}:\d{2}$/.test(query.tid) ? query.tid : "09:00";
    const newEmployee = business.employees.some((e) => e.id === query.for) ? query.for : undefined;
    const newTimeOff = query.blokker === "1";
    const selectedBlock = allBlocks.find((t) => t.id === query.blokkering);
    // Visits already in a time that was just blocked (they are kept)
    const overlapping = Number(query.varsel);
    const firstStepsHidden = (await cookies()).has(FIRST_STEPS_COOKIE);

    return (
        <main className={`${styles.main} ${calendar.wide}`}>
            <PanelNav title="Kalender"/>
            {business.isHidden && (
                <p className={calendar.notice} role="status">
                    Bedriften venter på godkjenning – vanligvis innen én arbeidsdag. Du kan allerede legge inn
                    tjenester, ansatte og åpningstider. Kundene ser bedriften når den er godkjent.
                </p>
            )}
            {!firstStepsHidden && <FirstSteps business={business}/>}
            {query.feil && (
                <p className={styles.error} role="alert">
                    {query.feil === "for-sent"
                        ? "Bestillingen kan ikke avlyses lenger – timen har allerede startet."
                        : "Kunne ikke endre bestillingen. Prøv igjen."}
                </p>
            )}
            {Number.isInteger(overlapping) && overlapping > 0 && (
                <p className={calendar.notice} role="status">
                    Tiden er blokkert, men det finnes allerede {overlapping === 1 ? "1 bestilling" : `${overlapping} bestillinger`} i
                    perioden. De er ikke avlyst – åpne dem i kalenderen hvis kunden må få beskjed.
                </p>
            )}

            <div className={calendar.toolbar}>
                <div className={calendar.segmented} role="group" aria-label="Visning">
                    <Link href={link({ visning: undefined, ansatt: undefined })} aria-current={view ? undefined : "page"}>Dag</Link>
                    <Link href={link({ visning: "uke" })} aria-current={week ? "page" : undefined}>Uke</Link>
                    <Link href={link({ visning: "maned" })} aria-current={month ? "page" : undefined}>Måned</Link>
                </div>
                <Link href={link({ ny: "1" })} scroll={false} className={styles.primaryButton}>+ Ny bestilling</Link>
                <Link href={link({ blokker: "1" })} scroll={false} className={styles.secondaryButton}>Blokker tid</Link>
                <Link href={link({ dag: previous })} className={styles.secondaryButton} aria-label={`Forrige ${unit}`}>←</Link>
                <Link href={link({ dag: todayInOslo() })} className={styles.secondaryButton}>I dag</Link>
                <Link href={link({ dag: next })} className={styles.secondaryButton} aria-label={`Neste ${unit}`}>→</Link>
                <h2 className={calendar.dayTitle}>
                    {month ? monthTitle(day)
                        : week ? `Uke ${isoWeek(first)} · ${shortDay(first)} – ${shortDay(last)}` : formatDay(day)}
                </h2>
                <form method="get" className={calendar.datePicker}>
                    {view && <input type="hidden" name="visning" value={view}/>}
                    {employeeFilter && <input type="hidden" name="ansatt" value={employeeFilter}/>}
                    <label>
                        <span className={calendar.srOnly}>Velg dato</span>
                        <input type="date" name="dag" defaultValue={day}/>
                    </label>
                    <button type="submit" className={styles.secondaryButton}>Vis</button>
                </form>
            </div>

            {view && (
                <div className={calendar.filters}>
                    <form method="get" className={calendar.employeeFilter}>
                        <input type="hidden" name="visning" value={view}/>
                        <input type="hidden" name="dag" value={day}/>
                        <label>
                            Ansatt{" "}
                            <select name="ansatt" defaultValue={employeeFilter ?? ""}>
                                <option value="">Alle ansatte</option>
                                {business.employees.map((e) => (
                                    <option key={e.id} value={e.id}>{e.name} {e.lastName}</option>
                                ))}
                            </select>
                        </label>
                        <button type="submit" className={styles.secondaryButton}>Vis</button>
                    </form>
                </div>
            )}

            {!view && !business.openingHours.some((h) => h.dayOfWeek === weekdayOf(day)) && (
                <p className={styles.muted}>Stengt denne dagen.</p>
            )}

            <GroupStrip bookings={bookings} hrefFor={(booking) => link({ bestilling: booking.id })}/>

            {month ? (
                <MonthGrid days={columns} month={day.slice(0, 7)} business={business}
                           dayHref={(date) => link({ dag: date, visning: undefined, ansatt: undefined })}
                           newHref={(date) => link({ dag: date, ny: "1" })}
                           hrefFor={(booking) => link({ bestilling: booking.id })}/>
            ) : (
                <TimeGrid columns={columns} range={range} showEmployee={week}
                          hrefFor={(booking) => link({ bestilling: booking.id })}
                          blockHref={(block) => link({ blokkering: block.id })}
                          newHref={(column, minute) => link({
                              ny: "1",
                              tid: clock(minute),
                              // day view: the column is an employee; week view: the column is a day
                              ...(week ? { dag: column.id } : { for: column.id }),
                          })}/>
            )}

            {/* Phone: the same visits as a simple list (the grid is too narrow there) */}
            <PhoneList columns={month ? columns.filter((c) => c.day.startsWith(day.slice(0, 7)))
                : week ? columns : [{ id: day, title: formatDay(day), day, bookings, blocks: blocksOn(blocks, day) }]}
                       hrefFor={(booking) => link({ bestilling: booking.id })}
                       blockHref={(block) => link({ blokkering: block.id })}/>

            {selected && <BookingDetails booking={selected} closeHref={here} moveHref={link({ flytt: selected.id })}
                                         members={members}/>}
            {moving && (
                <MoveBookingForm booking={moving} employees={business.employees} date={osloDay(moving.startUtc)}
                                 time={formatTime(moving.startUtc)} view={view} closeHref={link({ bestilling: moving.id })}/>
            )}            {selectedBlock && <TimeOffDetails block={selectedBlock} closeHref={here}/>}
            {newTimeOff && (
                <TimeOffForm employees={business.employees} date={day} employeeId={employeeFilter} view={view}
                             closeHref={here}/>
            )}
            {newBooking && (
                <NewBookingForm services={business.services} employees={business.employees} date={day}
                                time={newTime} employeeId={newEmployee ?? employeeFilter} view={view} closeHref={here}/>
            )}
        </main>
    );
}
