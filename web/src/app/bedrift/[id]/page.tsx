import NextLink from "next/link";
import { notFound } from "next/navigation";
import { getBusiness } from "@/lib/api";
import { getCategoryLabel } from "@/lib/categories";
import { formatPrice } from "@/lib/format";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ id: string }>;
};

export default async function BusinessPage({ params }: Props) {
    const { id } = await params;
    const business = await getBusiness(id);

    if (!business) {
        notFound();
    }

    return (
        <main className={styles.main}>
            <NextLink href="/" className={styles.back}>← Tilbake til alle bedrifter</NextLink>

            <header className={styles.header}>
                <span className={styles.category}>{getCategoryLabel(business.category)}</span>
                <h1 className={styles.title}>{business.name}</h1>
                {business.description && <p className={styles.muted}>{business.description}</p>}
                <address className={styles.contact}>
                    <span>{business.address}, {business.postalCode} {business.city}</span>
                    <a href={`tel:${business.phoneNumber}`}>{business.phoneNumber}</a>
                    <a href={`mailto:${business.email}`}>{business.email}</a>
                </address>
            </header>

            <section>
                <h2 className={styles.sectionTitle}>Tjenester</h2>
                <ul className={styles.list}>
                    {business.services.map((service) => (
                        <li key={service.id} className={styles.service}>
                            <div>
                                <h3 className={styles.serviceName}>{service.name}</h3>
                                {service.description && <p className={styles.muted}>{service.description}</p>}
                            </div>
                            <div className={styles.serviceMeta}>
                                <span className={styles.price}>{formatPrice(service.priceNok, service.priceType)}</span>
                                <span className={styles.muted}>{service.durationMinutes} min</span>
                                <span className={styles.muted}>{service.durationMinutes} min</span>
                                <NextLink
                                    href={`/bedrift/${business.id}/bestill?tjeneste=${service.id}`}
                                    className={styles.bookButton}
                                >
                                    Bestill
                                </NextLink>
                            </div>
                        </li>
                    ))}
                </ul>
            </section>

            <section>
                <h2 className={styles.sectionTitle}>Ansatte</h2>
                <ul className={styles.list}>
                    {business.employees.map((employee) => (
                        <li key={employee.id} className={styles.employee}>
                            {employee.name} {employee.lastName}
                        </li>
                    ))}
                </ul>
            </section>
        </main>
    );
}