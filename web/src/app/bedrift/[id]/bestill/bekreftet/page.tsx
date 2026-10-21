import NextLink from "next/link";
import { notFound } from "next/navigation";
import { getBusiness } from "@/lib/api";
import { formatDate, formatTime } from "@/lib/format";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ id: string }>;
    searchParams: Promise<{ tjeneste?: string; ansatt?: string; tid?: string }>;
};

export default async function BookingConfirmedPage({ params, searchParams }: Props) {
    const { id } = await params;
    const { tjeneste, ansatt, tid } = await searchParams;

    const business = await getBusiness(id);
    const service = business?.services.find((s) => s.id === tjeneste);
    const employee = business?.employees.find((e) => e.id === ansatt);


    // A cut-off or edited link (e.g. tid=abc) shows "not found" instead of crashing
    if (!business || !service || !employee || !tid || Number.isNaN(Date.parse(tid))) {        notFound();
    }

    return (
        <main className={styles.main}>
            <div className={styles.card}>
                <p className={styles.icon}>✓</p>
                <h1 className={styles.title}>Så var det bestilt!</h1>
                <p className={styles.muted}>
                    Klar for kvalitetstid med din besti? 💖 {business.name} bekrefter timen så snart som mulig,
                    og du finner den under Min Besti.
                </p>

                <dl className={styles.details}>
                    <dt>Tjeneste</dt>
                    <dd>{service.name}</dd>
                    <dt>Hos</dt>
                    <dd>{employee.name} {employee.lastName}</dd>
                    <dt>Tid</dt>
                    <dd>{formatDate(tid)} kl. {formatTime(tid)}</dd>
                    <dt>Adresse</dt>
                    <dd>{business.address}, {business.postalCode} {business.city}</dd>
                </dl>

                <NextLink href="/" className={styles.back}>← Tilbake til forsiden</NextLink>
            </div>
        </main>
    );
}
