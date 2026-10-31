const nok = new Intl.NumberFormat("nb-NO", {
    style: "currency",
    currency: "NOK",
    maximumFractionDigits: 0,
});

export function formatPrice(priceNok: number | null, priceType: string): string {
    if (priceNok === null || priceType === "EtterAvtale") {
        return "Pris etter avtale";
    }
    if (priceNok === 0 && priceType === "FastPris") {
        return "Gratis";
    }

    const amount = nok.format(priceNok);

    switch (priceType) {
        case "PerTime":
            return `${amount}/time`;
        case "PerKvadratmeter":
            return `Fra ${amount}/m²`;
        case "PerLøpendeMeter":
            return `Fra ${amount}/lm`;
        default:
            return amount;
    }
}
// Length of a visit: 180 -> "3 t", 90 -> "1 t 30 min", 45 -> "45 min"
export function formatLength(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const rest = minutes % 60;
    return [hours > 0 ? `${hours} t` : "", rest > 0 ? `${rest} min` : ""].filter(Boolean).join(" ");
}

const osloTime = new Intl.DateTimeFormat("nb-NO", {
    timeZone: "Europe/Oslo",
    hour: "2-digit",
    minute: "2-digit",
});

export function formatTime(utc: string): string {
    return osloTime.format(new Date(utc));
}

export function todayInOslo(): string {
    return new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Oslo" }).format(new Date());
}
// The Oslo day of a time from the API: "2026-10-29T16:00:00Z" -> "2026-10-29"
export function osloDay(utc: string): string {
    return new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Oslo" }).format(new Date(utc));
}
const osloDate = new Intl.DateTimeFormat("nb-NO", {
    timeZone: "Europe/Oslo",
    weekday: "long",
    day: "numeric",
    month: "long",
});

export function formatDate(utc: string): string {
    return osloDate.format(new Date(utc));
}

const osloWeekday = new Intl.DateTimeFormat("nb-NO", { timeZone: "Europe/Oslo", weekday: "long" });

// How long ago, like a phone: "for 18 min siden", "for 2 timer siden", "i går kl. 23:16", "onsdag kl. 03:16",
// and the full date after a week
export function formatAgo(utc: string, now = new Date()): string {
    const minutes = Math.floor((now.getTime() - new Date(utc).getTime()) / 60000);
    if (minutes < 1) {
        return "nå nettopp";
    }
    if (minutes < 60) {
        return `for ${minutes} min siden`;
    }
    const today = osloDay(now.toISOString());
    const day = osloDay(utc);
    if (day === today) {
        const hours = Math.floor(minutes / 60);
        return `for ${hours} ${hours === 1 ? "time" : "timer"} siden`;
    }
    if (day === shiftDay(today, -1)) {
        return `i går kl. ${formatTime(utc)}`;
    }
    if (day > shiftDay(today, -7)) {
        return `${osloWeekday.format(new Date(utc))} kl. ${formatTime(utc)}`;
    }
    return formatDateTime(utc);
}

// "torsdag 29. oktober kl. 14:05" (logs and system status, where the time matters)
export function formatDateTime(utc: string): string {
    return `${formatDate(utc)} kl. ${formatTime(utc)}`;
}
// Values from the address bar can be anything (old, cut-off or edited links): only real values pass
export function isIsoDate(value: string | undefined): value is string {
// "2026-02-31" would roll over to March, so the date must come back unchanged
    return !!value && /^\d{4}-\d{2}-\d{2}$/.test(value)
        && !Number.isNaN(Date.parse(value)) && new Date(`${value}T12:00:00Z`).toISOString().startsWith(value);
}
// A whole page number (1..999999); "1.5" or "99999999999" would make the API answer 400
export function isPageNumber(value: string | undefined): value is string {
    return !!value && /^\d{1,6}$/.test(value);
}
// A plain decimal like 59.912, -5.3 or 25 (not "0x10", "1e3" or huge numbers the API cannot read)
export function isDecimal(value: string | undefined): value is string {
    return !!value && /^-?\d{1,3}(\.\d{1,6})?$/.test(value);
}

const osloClock = new Intl.DateTimeFormat("en-GB", {
    timeZone: "Europe/Oslo",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
});
// Minutes since midnight in Oslo: "2026-10-26T08:30:00Z" -> 570 (09:30 in winter)
export function osloMinutes(utc: string): number {
    const [hours, minutes] = osloClock.format(new Date(utc)).split(":").map(Number);
    return hours * 60 + minutes;
}

// "09:30:00" -> 570
export function clockMinutes(time: string): number {
    const [hours, minutes] = time.split(":").map(Number);
    return hours * 60 + minutes;
}

// "2026-10-26" + 1 -> "2026-10-27" (noon UTC, so summer time never moves the day)
export function shiftDay(date: string, days: number): string {
    const day = new Date(`${date}T12:00:00Z`);
    day.setUTCDate(day.getUTCDate() + days);
    return day.toISOString().slice(0, 10);
}

// "2026-10-26" -> 1 (Monday); 0 = Sunday, like DayOfWeek in the API
export function weekdayOf(date: string): number {
    return new Date(`${date}T12:00:00Z`).getUTCDay();
}

const dayHeading = new Intl.DateTimeFormat("nb-NO", {
    timeZone: "UTC",
    weekday: "long",
    day: "numeric",
    month: "long",
});

// "2026-10-26" -> "mandag 26. oktober"
export function formatDay(date: string): string {
    return dayHeading.format(new Date(`${date}T12:00:00Z`));
}

// "2026-11" (the month calendar on Bestill); edited links like "2026-13" do not pass
export function isIsoMonth(value: string | undefined): value is string {
    return !!value && /^\d{4}-(0[1-9]|1[0-2])$/.test(value);
}

// "2026-11" + 2 -> "2027-01"
export function shiftMonth(month: string, months: number): string {
    const day = new Date(`${month}-15T12:00:00Z`);
    day.setUTCMonth(day.getUTCMonth() + months);
    return day.toISOString().slice(0, 7);
}

// Whole days from one date to another: "2026-11-01" -> "2026-11-03" = 2
export function daysBetween(from: string, to: string): number {
    return Math.round((Date.parse(to) - Date.parse(from)) / 86_400_000);
}

const monthHeading = new Intl.DateTimeFormat("nb-NO", {
    timeZone: "UTC",
    month: "long",
    year: "numeric",
});

// "2026-11" -> "november 2026"
export function formatMonth(month: string): string {
    return monthHeading.format(new Date(`${month}-15T12:00:00Z`));
}

// A month calendar in whole weeks: from the Monday before the 1st to the Sunday after the last day (max 42 days)
export function monthGrid(month: string): { gridStart: string; gridEnd: string } {
    const firstDay = `${month}-01`;
    const lastDay = shiftDay(`${shiftMonth(month, 1)}-01`, -1);
    return {
        gridStart: shiftDay(firstDay, -((weekdayOf(firstDay) + 6) % 7)),
        gridEnd: shiftDay(lastDay, (7 - weekdayOf(lastDay)) % 7),
    };
}
