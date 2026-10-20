import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import GalleryUploadForm from "./GalleryUploadForm";
import { deleteGalleryImageAction } from "./actions";
import panelStyles from "../tjenester/page.module.css";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Galleri",
};

type GalleryPageProps = {
    searchParams: Promise<{ feil?: string; lagt?: string; slettet?: string }>;
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
    const { feil, lagt, slettet } = await searchParams;
    return (
        <main className={panelStyles.main}>
            <Link href="/panel" className={panelStyles.back}>← Tilbake til bestillinger</Link>
            <h1 className={panelStyles.title}>Galleri</h1>
            <p className={panelStyles.muted}>
                Vis frem lokalet og arbeidet ditt. Maks 12 bilder. Bildene vises på bedriftssiden din.
            </p>
            {feil && (
                <p className={panelStyles.error} role="alert">
                    {feil === "1" ? "Noe gikk galt. Prøv igjen." : feil}
                </p>
            )}
            {lagt && <p className={panelStyles.success} role="status">Bildet er lagt til.</p>}
            {slettet && <p className={panelStyles.success} role="status">Bildet er slettet.</p>}
            {business.gallery.length < 12 && <GalleryUploadForm/>}
            {business.gallery.length === 0 ? (
                <p className={panelStyles.muted}>Ingen bilder ennå.</p>
            ) : (
                <ul className={styles.grid}>
                    {business.gallery.map((image) => (
                        <li key={image.id} className={styles.item}>
                            {/* eslint-disable-next-line @next/next/no-img-element -- our own resized WebP from the API */}
                            <img src={image.url} alt="" className={styles.image}/>
                            <form action={deleteGalleryImageAction}>
                                <input type="hidden" name="imageId" value={image.id}/>
                                <SubmitButton className={panelStyles.dangerButton} pendingText="Sletter…">
                                    Slett
                                </SubmitButton>
                            </form>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}