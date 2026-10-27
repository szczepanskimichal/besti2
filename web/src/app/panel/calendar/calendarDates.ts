import { shiftDay, weekdayOf } from "@/lib/format";

// "2026-10-19" -> "man. 19.10." (short enough for a column head)
export function shortDay(date: string): string {
    return new Intl.DateTimeFormat("nb-NO", { timeZone: "UTC", weekday: "short", day: "numeric", month: "numeric" })
        .format(new Date(`${date}T12:00:00Z`));
}

export function weekDays(monday: string): string[] {
    return [0, 1, 2, 3, 4, 5, 6].map((n) => shiftDay(monday, n));
}

// Every day from first to last, both included
export function daysBetween(first: string, last: string): string[] {
    const days: string[] = [];
    for (let date = first; date <= last; date = shiftDay(date, 1)) {
        days.push(date);
    }
    return days;
}

export function mondayOf(date: string): string {
    return shiftDay(date, -((weekdayOf(date) + 6) % 7));
}

export function lastDayOfMonth(date: string): string {
    return shiftDay(shiftMonth(date, 1), -1);
}

// The 1st of the month n months later: ("2026-10-22", 1) -> "2026-11-01"
export function shiftMonth(date: string, n: number): string {
    const d = new Date(Date.UTC(Number(date.slice(0, 4)), Number(date.slice(5, 7)) - 1 + n, 1));
    return d.toISOString().slice(0, 10);
}

// "2026-10-22" -> "Oktober 2026"
export function monthTitle(date: string): string {
    const title = new Intl.DateTimeFormat("nb-NO", { timeZone: "UTC", month: "long", year: "numeric" })
        .format(new Date(`${date.slice(0, 7)}-15T12:00:00Z`));
    return title.charAt(0).toUpperCase() + title.slice(1);
}

// ISO week number (week 1 has the first Thursday of the year), as on Norwegian calendars
export function isoWeek(monday: string): number {
    const thursday = new Date(`${shiftDay(monday, 3)}T12:00:00Z`);
    const yearStart = new Date(Date.UTC(thursday.getUTCFullYear(), 0, 1));
    return Math.ceil(((thursday.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
}
