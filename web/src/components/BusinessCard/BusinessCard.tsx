import NextLink from "next/link";
import type { BusinessListItem } from "@/lib/types";
import { getCategoryLabel } from "@/lib/categories";
import styles from "./BusinessCard.module.css";
import BusinessLogo from "@/components/BusinessLogo/BusinessLogo";

type Props = {
    business: BusinessListItem;
};

export default function BusinessCard({ business }: Props ) {
    return (
    <NextLink href={`/bedrift/${business.id}`}  className={styles.link}> 
        <article className={styles.card}>
            <div className={styles.top}>
                <BusinessLogo name={business.name} category={business.category} />
                <span className={styles.category}>{getCategoryLabel(business.category)}</span>
            </div>
            <h2 className={styles.name}>{business.name}</h2>
            <p className={styles.city}><span aria-hidden="true">📍</span> {business.city}</p>
            {business.description && <p className={styles.description}>{business.description}</p>}
            <span className={styles.more}>Se tjenester →</span>
        </article>
    </NextLink>
    );
}