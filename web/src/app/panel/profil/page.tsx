import { redirect } from "next/navigation";
import { getCalendarFeedToken, getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import BusinessLogo from "@/components/BusinessLogo/BusinessLogo";
import NextLink from "next/link";
import AccountForms from "./AccountForms";
import CalendarFeed from "./CalendarFeed";
import LogoUploadForm from "./LogoUploadForm";
import ProfileForm from "./ProfileForm";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import styles from "./profil.module.css";

type ProfilePageProps = {
    searchParams: Promise<{ lagret?: string, logo?: string, kalender?: string }>;
};
export default async function ProfilePage({ searchParams }: ProfilePageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const [business, feedToken] = await Promise.all([getPanelBusiness(token), getCalendarFeedToken(token)]);
    if (!business) {
        redirect("/logg-inn");
    }
    const { lagret, logo, kalender } = await searchParams;

    return (
        <main className={`${panel.main} ${styles.page}`}>
            <PanelNav title="Profil"/>
            {lagret && <p className={styles.notice} role="status">Profilen er lagret.</p>}
            {logo && <p className={styles.notice} role="status">Logoen er lagret.</p>}
            {business.latitude === null && (
                <p className={styles.error} role="status">
                    Vi fant ikke adressen på kartet – sjekk adressen og postnummeret.
                    Uten kart vises dere ikke i «Nær meg».
                </p>
            )}

            {/* One glass card: the logo in an aurora ring with Bytt logo under it, the name and the Verifisert
                stamp next to it. What customers see at the top of the business page */}
            <section className={styles.hero}>
                <div className={styles.logoRing}>
                    <BusinessLogo name={business.name} category={business.category} logoUrl={business.logoUrl}
                                  size="large"/>
                </div>
                <div className={styles.heroText}>
                    <p className={styles.businessName}>{business.name}</p>
                    {/* The API checks Brønnøysund again every week */}
                    {business.verifiedUtc ? (
                        <div className={styles.verified}>
                            {/* Crystal seal in a mint neon pill: mint = done, like every other confirmed thing */}
                            <NextLink href="/aerlig-booking#verifisert" className={styles.verifiedStamp}
                                      title="Aktiv i Brønnøysundregistrene og godkjent av BESTI. Les mer">
                                <span className={styles.verifiedSeal} aria-hidden="true"/>
                                Verifisert bedrift
                            </NextLink>
                            <span className={styles.verifiedWhen}>Sjekket i Brønnøysundregistrene {formatDate(business.verifiedUtc)}</span>
                        </div>
                    ) : (
                        <p className={styles.verifiedWaiting}>
                            Merket «Verifisert bedrift» kommer når bedriften er godkjent og sjekket i
                            Brønnøysundregistrene (senest innen et døgn).
                        </p>
                    )}
                </div>
                {/* Under the logo circle; when open, the upload sheet takes the whole width of the card */}
                <div className={styles.heroUpload}>
                    <LogoUploadForm hasLogo={business.logoUrl !== null}/>
                </div>
            </section>

            <ProfileForm
                initial={{
                    description: business.description ?? "",
                    phone: business.phoneNumber,
                    contactEmail: business.email ?? "",
                    address: business.address,
                    postalCode: business.postalCode,
                    city: business.city,
                    freeCancelHours: String(business.freeCancelHours),
                    website: business.website ?? "",
                    autoConfirm: String(business.autoConfirm),
                }}
            />
            <CalendarFeed feedToken={feedToken} status={kalender}/>
            <AccountForms/>
        </main>
    );
}
