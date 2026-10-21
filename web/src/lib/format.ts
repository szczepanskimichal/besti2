const nok = new Intl.NumberFormat("nb-NO", {
    style: "currency",
    currency: "NOK",
    maximumFractionDigits: 0,
});

export function formatPrice(priceNok: number | null, priceType: string): string {
    if (priceNok === null || priceType === "EtterAvtale") {
        return "Pris etter avtale";
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

const osloDate = new Intl.DateTimeFormat("nb-NO", {
    timeZone: "Europe/Oslo",
    weekday: "long",
    day: "numeric",
    month: "long",
});

export function formatDate(utc: string): string {
    return osloDate.format(new Date(utc));
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