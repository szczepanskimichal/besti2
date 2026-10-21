import NextLink from "next/link";
import type { BusinessListItem } from "@/lib/types";
import { getCategoryLabel } from "@/lib/categories";
import { formatTime } from "@/lib/format";
import styles from "./BusinessCard.module.css";
import BusinessLogo from "@/components/BusinessLogo/BusinessLogo";
import Stars from "@/components/Stars/Stars";


type Props = {
    business: BusinessListItem;
    date?: string; // the searched day, needed for the booking link
};

export default function BusinessCard({ business, date }: Props) {
    const href = `/bedrift/${business.id}`;
    const slots = date ? business.freeSlots ?? [] : [];

    return (
        <article className={styles.card}>
            <NextLink href={href} className={styles.link}>
                <div className={styles.top}>
                    <BusinessLogo name={business.name} category={business.category} logoUrl={business.logoUrl}/>
                    <span className={styles.category}>{getCategoryLabel(business.category)}</span>
                </div>
                <h2 className={styles.name}>{business.name}</h2>
                <p className={styles.city}>
                    <span aria-hidden="true">📍</span> {business.city}
                    {business.distanceKm != null && ` · ${formatDistance(business.distanceKm)} unna`}
                </p>
                {business.averageRating !== null && (
                    <p className={styles.rating}>
                    <Stars value={business.averageRating}/>{" "}
                    {business.averageRating.toString().replace(".", ",")} · {business.reviewCount}{" "}
                    {business.reviewCount === 1 ? "vurdering" : "vurderinger"}
                </p>
            )}
                {business.description && <p className={styles.description}>{business.description}</p>}
            </NextLink>
            {slots.length > 0 && (
                <div className={styles.slots}>
                    <p className={styles.slotsLabel}>{slots[0].serviceName} · Ledig:</p>
                    {/* One click: the booking page opens with service, employee, day and time chosen */}
                    {slots.map((slot) => (
                        <NextLink
                            key={slot.startUtc}
                            href={`${href}/bestill?tjeneste=${slot.serviceId}&ansatt=${slot.employeeId}&dato=${date}&tid=${encodeURIComponent(slot.startUtc)}`}
                            className={styles.slot}
                        >
                            {formatTime(slot.startUtc)}
                        </NextLink>
                    ))}
                </div>
            )}
            <NextLink href={href} className={styles.more}>Se tjenester →</NextLink>
        </article>
    );
}

// 0.45 -> "450 m", 3.27 -> "3,3 km" (Norwegian decimal comma)
function formatDistance(km: number): string {
    return km < 1 ? `${Math.round(km * 1000)} m` : `${km.toFixed(1).replace(".", ",")} km`;
}