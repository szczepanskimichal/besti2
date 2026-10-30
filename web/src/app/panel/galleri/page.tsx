import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import GalleryUploadForm from "./GalleryUploadForm";
import DeleteImageForm from "./DeleteImageForm";
import panelStyles from "../tjenester/page.module.css";
import styles from "./page.module.css";
import EmptyState from "@/components/EmptyState/EmptyState";

export const metadata: Metadata = {
    title: "Galleri",
};

type GalleryPageProps = {
    searchParams: Promise<{ lagt?: string; slettet?: string }>;
};

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
    return (
        <main className={panelStyles.main}>
            <Link href="/panel" className={panelStyles.back}>← Tilbake til bestillinger</Link>
            <h1 className={panelStyles.title}>Galleri</h1>
            <p className={panelStyles.muted}>
                Vis frem lokalet og arbeidet ditt. Maks 12 bilder. Bildene vises på bedriftssiden din.
            </p>
            {lagt && <p className={panelStyles.success} role="status">Bildet er lagt til.</p>}
            {slettet && <p className={panelStyles.success} role="status">Bildet er slettet.</p>}
            {business.gallery.length < 12 && <GalleryUploadForm/>}
            {business.gallery.length === 0 ? (
                <EmptyState icon="📷" title="Ingen bilder ennå">
                    Bilder av lokalet eller arbeidet ditt gir kundene tillit. Det første bildet blir det store
                    bildet øverst på siden din.
                </EmptyState>
            ) : (
                <ul className={styles.grid}>
                    {business.gallery.map((image) => (
                        <li key={image.id} className={styles.item}>
                            {/* eslint-disable-next-line @next/next/no-img-element -- our own resized WebP from the API */}
                            <img src={image.url} alt="" className={styles.image}/>
                            <DeleteImageForm imageId={image.id}/>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}
