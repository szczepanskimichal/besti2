import { clockMinutes, formatDay, formatTime, osloDay, osloMinutes, shiftDay, weekdayOf } from "@/lib/format";
import type { BusinessDetails, PanelBooking, TimeOff } from "@/lib/types";
import { shortDay, daysBetween } from "./calendarDates";

// One hour in the grid is 120 px high (a 30 min visit = 60 px: time, customer and service fit)
export const PX_PER_MINUTE = 2;

// day = the date shown in the column (the same for every employee in the day view)
export type Column = { id: string; title: string; day: string; bookings: PanelBooking[]; blocks: TimeOff[] };

export type Range = { start: number; end: number };

// Links from a visit / a block to its details window
export type Href = (booking: PanelBooking) => string;

export type BlockHref = (block: TimeOff) => string;

// Day view: the employees as columns; a booking for an employee who was removed still gets a column
// A block of the whole business (employeeId null) is shown in every column
export function employeeColumns(business: BusinessDetails, bookings: PanelBooking[], blocks: TimeOff[], day: string): Column[] {
    const blocksFor = (employeeId: string) => blocksOn(blocks, day).filter((t) => !t.employeeId || t.employeeId === employeeId);
    const columns: Column[] = business.employees.map((e) => ({
        id: e.id,
        title: `${e.name} ${e.lastName}`.trim(),
        day,
        bookings: bookings.filter((b) => b.employeeId === e.id),
        blocks: blocksFor(e.id),
    }));
    for (const booking of bookings) {
        if (!columns.some((c) => c.id === booking.employeeId)) {
            columns.push({
                id: booking.employeeId,
                title: booking.employeeName,
                day,
                bookings: bookings.filter((b) => b.employeeId === booking.employeeId),
                blocks: blocksFor(booking.employeeId),
            });
        }
    }
    return columns;
}

// Week view: Monday to Sunday as columns; month view: every day of the shown weeks
export function dayColumns(first: string, last: string, bookings: PanelBooking[], blocks: TimeOff[]): Column[] {
    return daysBetween(first, last).map((date) => ({
        id: date,
        title: shortDay(date),
        day: date,
        bookings: bookings.filter((b) => osloDay(b.startUtc) === date),
        blocks: blocksOn(blocks, date),
    }));
}

// Blocks that touch this day in Oslo (a holiday over several days touches each of them)
export function blocksOn(blocks: TimeOff[], date: string): TimeOff[] {
    return blocks.filter((t) => osloDay(t.startUtc) <= date && (osloDay(t.endUtc) > date
        || (osloDay(t.endUtc) === date && osloMinutes(t.endUtc) > 0)));
}

// The part of a block on one day, cut to the shown hours (a whole day = from opening to closing in the grid)
export function blockMinutes(block: TimeOff, date: string, range: Range): Range {
    const start = osloDay(block.startUtc) < date ? 0 : osloMinutes(block.startUtc);
    const end = osloDay(block.endUtc) > date ? 24 * 60 : osloMinutes(block.endUtc);
    return { start: Math.max(start, range.start), end: Math.min(end, range.end) };
}

// "Ferie", in the week view with the employee: "Ingrid Hansen · Ferie"
export function blockLabel(block: TimeOff, withEmployee: boolean): string {
    const reason = block.reason ?? "Blokkert";
    const who = block.employeeName ?? "Hele bedriften";
    return withEmployee || !block.employeeId ? `${who} · ${reason}` : reason;
}

// The hours of a block on one day for the phone list: "10:00–12:00", "Hele dagen", "14:00–24:00"
export function blockHoursOn(block: TimeOff, date: string): string {
    const from = osloDay(block.startUtc) < date ? "00:00" : formatTime(block.startUtc);
    const to = osloDay(block.endUtc) > date ? "24:00" : formatTime(block.endUtc);
    return from === "00:00" && to === "24:00" ? "Hele dagen" : `${from}–${to}`;
}

// "torsdag 22. oktober, 10:00–12:00", "torsdag 22. oktober, hele dagen", "mandag 2. – fredag 6. november"
export function describeBlock(block: TimeOff): string {
    const startDay = osloDay(block.startUtc);
    const endDay = osloDay(block.endUtc);
    const wholeDays = osloMinutes(block.startUtc) === 0 && osloMinutes(block.endUtc) === 0;
    if (wholeDays) {
        const lastDay = shiftDay(endDay, -1);
        return lastDay === startDay ? `${formatDay(startDay)}, hele dagen` : `${formatDay(startDay)} – ${formatDay(lastDay)}`;
    }
    return startDay === endDay
        ? `${formatDay(startDay)}, ${formatTime(block.startUtc)}–${formatTime(block.endUtc)}`
        : `${formatDay(startDay)} ${formatTime(block.startUtc)} – ${formatDay(endDay)} ${formatTime(block.endUtc)}`;
}

// From the earliest opening to the latest closing of the shown days, widened to whole hours and to visits
export function visibleRange(business: BusinessDetails, days: string[], bookings: PanelBooking[]): Range {
    const hours = business.openingHours.filter((h) => days.some((d) => weekdayOf(d) === h.dayOfWeek));
    let start = hours.length ? Math.min(...hours.map((h) => clockMinutes(h.opensAt))) : 8 * 60;
    let end = hours.length ? Math.max(...hours.map((h) => clockMinutes(h.closesAt))) : 17 * 60;
    for (const booking of bookings) {
        start = Math.min(start, osloMinutes(booking.startUtc));
        end = Math.max(end, osloMinutes(booking.endUtc) || 24 * 60);
    }
    return { start: Math.floor(start / 60) * 60, end: Math.min(Math.ceil(end / 60) * 60, 24 * 60) };
}

// 480, 540, 600 ... for every whole hour in the range
export function hourMarks(range: Range): number[] {
    const marks: number[] = [];
    for (let minute = range.start; minute < range.end; minute += 60) {
        marks.push(minute);
    }
    return marks;
}

// Every half hour in the range: 480, 510, 540 ...
export function halfHours(range: Range): number[] {
    const slots: number[] = [];
    for (let minute = range.start; minute < range.end; minute += 30) {
        slots.push(minute);
    }
    return slots;
}

// 570 -> "09:30"
export function clock(minute: number): string {
    return `${String(Math.floor(minute / 60)).padStart(2, "0")}:${String(minute % 60).padStart(2, "0")}`;
}
