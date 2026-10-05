import NextLink from "next/link";
import { notFound } from "next/navigation";
import { getAvailability, getBusiness } from "@/lib/api";
import { formatPrice, formatTime, todayInOslo } from "@/lib/format";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ id: string }>;
    searchParams: Promise<{ tjeneste?: string; ansatt?: string; dato?: string }>;
};

export default async function BookingPage({ params, searchParams }: Props) {
    const { id } = await params;
    const { tjeneste, ansatt, dato } = await searchParams;

    const business = await getBusiness(id);
    const service = business?.services.find((s) => s.id === tjeneste);

    if (!business || !service) {
        notFound();
    }

    const employeeId = ansatt ?? business.employees[0]?.id;
    const date = dato ?? todayInOslo();
    const slots = employeeId ? await getAvailability(service.id, employeeId, date) : null;

    return (
        <main className={styles.main}>
            <NextLink href={`/bedrift/${business.id}`} className={styles.back}>← Tilbake til {business.name}</NextLink>

            <h1 className={styles.title}>Bestill {service.name}</h1>
            <p className={styles.muted}>
                {service.durationMinutes} min · {formatPrice(service.priceNok, service.priceType)}
            </p>

            <form method="get" className={styles.filters}>
                <input type="hidden" name="tjeneste" value={service.id} />

                <label className={styles.field}>
                    Ansatt
                    <select name="ansatt" defaultValue={employeeId}>
                        {business.employees.map((employee) => (
                            <option key={employee.id} value={employee.id}>
                                {employee.name} {employee.lastName}
                            </option>
                        ))}
                    </select>
                </label>

                <label className={styles.field}>
                    Dato
                    <input type="date" name="dato" defaultValue={date} min={todayInOslo()} />
                </label>

                <button type="submit" className={styles.secondaryButton}>Vis ledige tider</button>
            </form>

            <h2 className={styles.sectionTitle}>Ledige tider</h2>

            {!slots || slots.length === 0 ? (
                <p className={styles.muted}>Ingen ledige tider denne dagen. Prøv en annen dato.</p>
            ) : (
                <ul className={styles.slots}>
                    {slots.map((slot) => (
                        <li key={slot}>
                            <NextLink
                                href={`/bedrift/${business.id}/bestill?tjeneste=${service.id}&ansatt=${employeeId}&dato=${date}&tid=${encodeURIComponent(slot)}`}
                                className={styles.slot}
                            >
                                {formatTime(slot)}
                            </NextLink>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}
