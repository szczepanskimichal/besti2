import NextLink from "next/link";
import styles from "./VerifiedBadge.module.css";

type Props = {
    // With a link on the business page; without one inside a search card (the whole card is already a link)
    href?: string;
};

// "Verifisert bedrift": active in Brønnøysundregistrene (checked every week) and approved by BESTI
export default function VerifiedBadge({ href }: Props) {
    const content = (
        <>
            <svg className={styles.icon} viewBox="0 0 16 16" aria-hidden="true" focusable="false">
                <path d="M8 1.2 9.6 2.6l2.1-.2.5 2 1.8 1.1-.8 2 .8 2-1.8 1.1-.5 2-2.1-.2L8 14.8l-1.6-1.4-2.1.2-.5-2-1.8-1.1.8-2-.8-2 1.8-1.1.5-2 2.1.2Z"/>
                <path className={styles.check} d="m5.4 8.1 1.8 1.8 3.5-3.6"/>
            </svg>
            Verifisert bedrift
        </>
    );

    return href ? (
        <NextLink href={href} className={styles.badge}
                  title="Aktiv i Brønnøysundregistrene og godkjent av BESTI. Les mer">
            {content}
        </NextLink>
    ) : (
        <span className={styles.badge} title="Aktiv i Brønnøysundregistrene og godkjent av BESTI">{content}</span>
    );
}
