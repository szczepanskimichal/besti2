import NextLink from "next/link";
import { toggleHelpfulAction } from "./actions";
import styles from "./page.module.css";

type HelpfulButtonProps = {
    reviewId: string;
    slug: string;
    count: number;
    marked: boolean;
    loggedIn: boolean;
};

// "Nyttig": readers show which reviews helped them (the most useful ones are easy to trust)
export default function HelpfulButton({ reviewId, slug, count, marked, loggedIn }: HelpfulButtonProps) {
    const label = <><span aria-hidden="true">👍</span> Nyttig{count > 0 && ` · ${count}`}</>;
    if (!loggedIn) {
        return (
            <NextLink href={`/logg-inn?neste=${encodeURIComponent(`/${slug}#vurderinger`)}`} className={styles.helpful}>
                {label}
            </NextLink>
        );
    }
    return (
        <form action={toggleHelpfulAction}>
            <input type="hidden" name="reviewId" value={reviewId}/>
            <input type="hidden" name="slug" value={slug}/>
            <button type="submit" aria-pressed={marked}
                    className={marked ? `${styles.helpful} ${styles.helpfulOn}` : styles.helpful}>
                {label}
            </button>
        </form>
    );
}
