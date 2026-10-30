import NextLink from "next/link";
import { toggleFavoriteAction } from "@/app/min-besti/favoritter/actions";
import FavoriteSparks from "./FavoriteSparks";
import styles from "./page.module.css";

type FavoriteButtonProps = {
    businessId: string;
    slug: string;
    // null = not logged in (the heart then leads to the login and back)
    saved: boolean | null;
};

// Our own heart instead of the emoji, so the saved one can be filled with the aurora colours
function Heart() {
    return (
        <svg className={styles.heart} viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <defs>
                <linearGradient id="besti-heart-aurora" x1="0" y1="0" x2="1" y2="1">
                    <stop offset="0" stopColor="#72d6bd"/>
                    <stop offset="0.6" stopColor="#3fa7d6"/>
                    <stop offset="1" stopColor="#8f7bd6"/>
                </linearGradient>
            </defs>
            <path d="M12 20.5s-7.5-4.6-9.3-9.4C1.5 7.9 3.6 4.5 7 4.5c2 0 3.3 1.1 5 3 1.7-1.9 3-3 5-3 3.4 0 5.5 3.4 4.3 6.6-1.8 4.8-9.3 9.4-9.3 9.4z"/>
        </svg>
    );
}

// "Lagre som favoritt": the business shows up in Min Besti → Favoritter with "Bestill igjen"
export default function FavoriteButton({ businessId, slug, saved }: FavoriteButtonProps) {
    if (saved === null) {
        return (
            <NextLink href={`/logg-inn?neste=${encodeURIComponent(`/${slug}`)}`} className={styles.favorite}>
                <Heart/> Lagre
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
                <Heart/> {saved ? "Favoritt" : "Lagre"}
                <FavoriteSparks businessId={businessId} saved={saved}/>
            </button>
        </form>
    );
}
