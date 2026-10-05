import NextLink from "next/link";
import type { BusinessListItem } from "@/lib/types";
import { getCategoryLabel } from "@/lib/categories";
import styles from "./BusinessCard.module.css";

type Props = {
    business: BusinessListItem;
};

export default function BusinessCard({ business }: Props ) {
    return (
    <NextLink href={`/bedrift/${business.id}`}  className={styles.link}> 
        <article className={styles.card}>
            <span className={styles.category}>{getCategoryLabel(business.category)}</span> 
            <h2 className={styles.name}>{business.name}</h2>
            <p className={styles.city}>{business.city}</p>
            {business.description && <p className={styles.description}>{business.description}</p>}
        </article>
    </NextLink>
    );
}