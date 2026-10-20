import { Metadata } from "next";
import { Fragment } from "react";
import NextLink from "next/link";
import { notFound } from "next/navigation";
import { getBusiness, getBusinessReviews } from "@/lib/api";
import { getCategoryLabel } from "@/lib/categories";
import { formatPrice } from "@/lib/format";
import { weekdays } from "@/lib/weekdays";
import BusinessLogo from "@/components/BusinessLogo/BusinessLogo";
import Stars from "@/components/Stars/Stars";

import styles from "./page.module.css";

type Props = {
    params: Promise<{ id: string }>;
};

// Title, description and link preview per business (for Google, Messenger, SMS)
export async function generateMetadata({ params }: Props): Promise<Metadata> {
    const { id } = await params;
    const business = await getBusiness(id);
    if (!business) {
        return { title: "Fant ikke bedriften" };
    }
    const title = `${business.name} – Bestill time i ${business.city}`;
    const description = business.description?.slice(0, 155)
        ?? `Se tjenester, priser og ledige tider hos ${business.name} i ${business.city}, og bestill time på nett.`;
    return {
        title,
        description,
        openGraph: {
            title,
            description,
            images: business.logoUrl ? [business.logoUrl] : undefined,
        },
    };
}
export default async function BusinessPage({ params }: Props) {
    const { id } = await params;
    // Business and reviews at the same time
    const [business, reviews] = await Promise.all([getBusiness(id), getBusinessReviews(id)]);

    if (!business) {
        notFound();
    }

    // OpenStreetMap embed: small box around the business with a marker
    const d = 0.005;
    const lat = business.latitude;
    const lon = business.longitude;
    const mapUrl =
        lat !== null && lon !== null
            ? `https://www.openstreetmap.org/export/embed.html?bbox=${[lon - d, lat - d, lon + d, lat + d].join(",")}&layer=mapnik&marker=${lat},${lon}`
            : null;

    return (
        <main className={styles.main}>
            <NextLink href="/" className={styles.back}>← Tilbake til alle bedrifter</NextLink>

            <header className={styles.header}>
                <div className={styles.top}>
                    <BusinessLogo name={business.name} category={business.category} logoUrl={business.logoUrl} size="large" />
                    <span className={styles.category}>{getCategoryLabel(business.category)}</span>
                </div>
                <h1 className={styles.title}>{business.name}</h1>
                {reviews.average !== null && (
                    <a href="#vurderinger" className={styles.rating}>
                        <Stars value={reviews.average}/> {reviews.average.toString().replace(".", ",")} · {reviews.count} {reviews.count === 1 ? "vurdering" : "vurderinger"}
                    </a>
                )}
                {business.description && <p className={styles.muted}>{business.description}</p>}
                <address className={styles.contact}>
                    <span>{business.address}, {business.postalCode} {business.city}</span>
                    <a
                        href={`https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(`${business.address}, ${business.postalCode} ${business.city}`)}`}
                        target="_blank"
                        rel="noopener noreferrer"
                    >
                        Vis vei 🧭
                    </a>
                    <a href={`tel:${business.phoneNumber}`}>{business.phoneNumber}</a>
                    <a href={`mailto:${business.email}`}>{business.email}</a>
                </address>
            </header>
            {business.gallery.length > 0 && (
                <section>
                    <h2 className={styles.sectionTitle}>Bilder</h2>
                    <ul className={styles.gallery}>
                        {business.gallery.map((image) => (
                            <li key={image.id}>
                                {/* Opens the full photo in a new tab */}
                                <a href={image.url} target="_blank" rel="noreferrer">
                                    {/* eslint-disable-next-line @next/next/no-img-element -- our own resized WebP from the API */}
                                    <img src={image.url} alt={`Bilde fra ${business.name}`} loading="lazy" className={styles.galleryImage}/>
                                </a>
                            </li>
                        ))}
                    </ul>
                </section>
            )}
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

            <section id="vurderinger">
                <h2 className={styles.sectionTitle}>Vurderinger</h2>
                {reviews.count === 0 ? (
                    <p className={styles.muted}>Ingen vurderinger ennå. Bare kunder som har vært her, kan gi vurdering.</p>
                ) : (
                    <ul className={styles.reviews}>
                        {reviews.reviews.map((review) => (
                            <li key={review.id} className={styles.review}>
                                <p className={styles.reviewHead}>
                                    <Stars value={review.stars}/> <strong>{review.customerName}</strong>
                                </p>
                                {review.comment && <p>{review.comment}</p>}
                                {review.businessReply && (
                                    <p className={styles.reply}>
                                        <strong>Svar fra {business.name}:</strong> {review.businessReply}
                                    </p>
                                )}
                            </li>
                        ))}
                    </ul>
                )}
            </section>

            <section>
                <h2 className={styles.sectionTitle}>Åpningstider</h2>
                <dl className={styles.hours}>
                    {weekdays.map((w) => {
                        const hours = business.openingHours.find((h) => h.dayOfWeek === w.day);
                        return (
                            <Fragment key={w.day}>
                                <dt>{w.label}</dt>
                                <dd>{hours ? `${hours.opensAt.slice(0, 5)}–${hours.closesAt.slice(0, 5)}` : "Stengt"}</dd>
                            </Fragment>
                        );
                    })}
                </dl>
            </section>
            {mapUrl && (
                <section>
                    <h2 className={styles.sectionTitle}>Kart</h2>
                    <iframe
                        title={`Kart som viser ${business.name}`}
                        src={mapUrl}
                        className={styles.map}
                        loading="lazy"
                    />
                </section>
            )}
        </main>
    );
}