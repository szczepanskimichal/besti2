import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import GalleryUploadForm from "./GalleryUploadForm";
import DeleteImageForm from "./DeleteImageForm";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Galleri",
};

type GalleryPageProps = {
    searchParams: Promise<{ lagt?: string; slettet?: string }>;
};

const MAX_IMAGES = 12;

// "Galleri" (Ada 2026-11-01): the dark glass look of the other panel pages. On top how many photos are used
// with the crystal camera and the upload field; under it the photos in glass frames, the first one marked
// as the big photo on the business page. Slett is the blue cancel pill (blue = delete)
export default async function GalleryPage({ searchParams }: GalleryPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const business = await getPanelBusiness(token);
    if (!business) {
        redirect("/logg-inn");
    }
    const { lagt, slettet } = await searchParams;
    const count = business.gallery.length;
    const full = count >= MAX_IMAGES;

    return (
        <main className={panel.main}>
            <PanelNav title="Galleri"/>
            {lagt && <p className={styles.notice} role="status">Bildet er lagt til.</p>}
            {slettet && <p className={styles.notice} role="status">Bildet er slettet.</p>}

            <section className={styles.hero}>
                <div className={styles.heroHead}>
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon */}
                    <img src="/ikoner/kamera.svg" alt="" aria-hidden="true" className={styles.camera}/>
                    <div className={styles.heroText}>
                        <p className={styles.used}>
                            <span className={styles.usedNumber}>{count}</span> av {MAX_IMAGES} bilder
                        </p>
                        <p className={styles.small}>
                            Vis frem lokalet og arbeidet ditt. Bildene vises på bedriftssiden din, og det første bildet
                            blir det store bildet øverst.
                        </p>
                    </div>
                </div>
                {full ? (
                    <p className={styles.full}>Galleriet er fullt. Slett et bilde for å legge til et nytt.</p>
                ) : (
                    <GalleryUploadForm/>
                )}
            </section>

            <section className={styles.section}>
                <h2 className={styles.sectionTitle}>
                    Bildene dine
                    <span className={count > 0 ? `${styles.count} ${styles.countLit}` : styles.count}>{count}</span>
                </h2>
                {count === 0 ? (
                    <p className={styles.small}>
                        Ingen bilder ennå. Bilder av lokalet eller arbeidet ditt gir kundene tillit.
                    </p>
                ) : (
                    <ul className={styles.grid}>
                        {business.gallery.map((image, index) => (
                            <li key={image.id} className={index === 0 ? `${styles.item} ${styles.itemCover}` : styles.item}>
                                <div className={styles.frame}>
                                    {/* eslint-disable-next-line @next/next/no-img-element -- our own resized WebP from the API */}
                                    <img src={image.url} alt="" className={styles.image}/>
                                    {index === 0 && <span className={styles.coverStamp}>Forsidebilde</span>}
                                </div>
                                <DeleteImageForm imageId={image.id}/>
                            </li>
                        ))}
                    </ul>
                )}
            </section>
        </main>
    );
}
