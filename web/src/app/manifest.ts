import type { MetadataRoute } from "next";

// "Legg til på Hjem-skjerm": BESTI opens like an app with its own icon
export default function manifest(): MetadataRoute.Manifest {
    return {
        name: "BESTI – Finn og bestill time",
        short_name: "BESTI",
        description: "Finn lokale bedrifter i Norge, se ledige tider og bestill time på nett.",
        lang: "nb",
        start_url: "/",
        display: "standalone",
        background_color: "#050d1c",
        theme_color: "#050d1c",
        icons: [
            { src: "/logo/besti-ikon-192.png", sizes: "192x192", type: "image/png" },
            { src: "/logo/besti-ikon-512.png", sizes: "512x512", type: "image/png" },
            // Square without rounded corners: Android cuts its own shape out of it
            { src: "/logo/besti-ikon-maskable-512.png", sizes: "512x512", type: "image/png", purpose: "maskable" },
        ],
    };
}
