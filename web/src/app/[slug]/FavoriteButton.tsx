import NextLink from "next/link";
import { toggleFavoriteAction } from "@/app/min-besti/favoritter/actions";
import styles from "./page.module.css";

type FavoriteButtonProps = {
    businessId: string;
    slug: string;
    // null = not logged in (the heart then leads to the login and back)
    saved: boolean | null;
};

// "Lagre som favoritt": the business shows up in Min Besti → Favoritter with "Bestill igjen"
export default function FavoriteButton({ businessId, slug, saved }: FavoriteButtonProps) {
    if (saved === null) {
        return (
            <NextLink href={`/logg-inn?neste=${encodeURIComponent(`/${slug}`)}`} className={styles.favorite}>
                <span aria-hidden="true">♡</span> Lagre
            </NextLink>
        );
    }
    return (
        <form action={toggleFavoriteAction} className={styles.favoriteForm}>
            <input type="hidden" name="businessId" value={businessId}/>
            <input type="hidden" name="tilbake" value={`/${slug}`}/>
            <input type="hidden" name="on" value={saved ? "0" : "1"}/>
            <button type="submit" aria-pressed={saved}
                    className={saved ? `${styles.favorite} ${styles.favoriteSaved}` : styles.favorite}>
                <span aria-hidden="true">{saved ? "♥" : "♡"}</span> {saved ? "Favoritt" : "Lagre"}
            </button>
        </form>
    );
}
