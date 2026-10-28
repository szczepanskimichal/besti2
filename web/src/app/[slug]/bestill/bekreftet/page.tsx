import NextLink from "next/link";
import { notFound } from "next/navigation";
import { getBusinessBySlug } from "@/lib/api";
import { formatDate, formatTime } from "@/lib/format";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ slug: string }>;
    searchParams: Promise<{ tjeneste?: string; ansatt?: string; tid?: string }>;
};

export default async function BookingConfirmedPage({ params, searchParams }: Props) {
    const { slug } = await params;
    const { tjeneste, ansatt, tid } = await searchParams;

    const business = await getBusinessBySlug(slug);
    const service = business?.services.find((s) => s.id === tjeneste);
    const employee = business?.employees.find((e) => e.id === ansatt);

    // A cut-off or edited link (e.g. tid=abc) shows "not found" instead of crashing
    if (!business || !service || !employee || !tid || Number.isNaN(Date.parse(tid))) {
        notFound();
    }

    return (
        <main className={styles.main}>
            <div className={styles.card}>
                <div className={styles.badge} aria-hidden="true">
                    <span className={styles.glow} />
                    <svg className={styles.check} viewBox="0 0 92 92">
                        <circle cx="46" cy="46" r="41" />
                        <path d="M28 47 l12 12 l24 -26" />
                    </svg>
                </div>
                <h1 className={styles.title}>Vips! Så var det bestilt.</h1>
                <p className={styles.muted}>
                    {/* With "Bekreft automatisk" the visit is already confirmed */}
                    Klar for kvalitetstid med din besti? 💖 {business.autoConfirm
                    ? "Timen er bekreftet"
                    : `${business.name} bekrefter timen så snart som mulig`}, og du finner den under Min Besti.
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
