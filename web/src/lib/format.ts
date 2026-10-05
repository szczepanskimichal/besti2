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