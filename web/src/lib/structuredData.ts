import type { BusinessDetails } from "@/lib/types";
import { SITE_URL } from "@/lib/site";

// Our category -> the closest schema.org type; everything else is a plain LocalBusiness
const schemaTypes: Record<string, string> = {
    Frisor: "HairSalon",
    Barber: "HairSalon",
    Skjonnhet: "BeautySalon",
    Vipper: "BeautySalon",
    Negler: "NailSalon",
    Massasje: "HealthAndBeautyBusiness",
    Fotpleie: "HealthAndBeautyBusiness",
    Spa: "DaySpa",
    Tatovering: "TattooParlor",
    PersonligTrener: "ExerciseGym",
    Yoga: "ExerciseGym",
    Bilverksted: "AutoRepair",
    Dekkskift: "AutoRepair",
    EuKontroll: "AutoRepair",
    Bilpleie: "AutoWash",
    Trafikkskole: "DrivingSchool",
    Bygg: "GeneralContractor",
    Rorlegger: "Plumber",
    Elektriker: "Electrician",
    Maler: "HousePainter",
    Taktekker: "RoofingContractor",
    Laasesmed: "Locksmith",
    Rengjoring: "HomeAndConstructionBusiness",
    Flyttehjelp: "MovingCompany",
    Hage: "HomeAndConstructionBusiness",
    Dyrlege: "VeterinaryCare",
};

const dayNames = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

type Reviews = { average: number | null; count: number };

// The business page: name, address, phone, opening hours, map position and (from 3 reviews, same as on the page) stars
export function businessJsonLd(business: BusinessDetails, reviews: Reviews): Record<string, unknown> {
    const url = `${SITE_URL}/${business.slug}`;
    const image = business.gallery[0]?.url ?? business.logoUrl;
    return {
        "@context": "https://schema.org",
        "@type": schemaTypes[business.category] ?? "LocalBusiness",
        "@id": url,
        name: business.name,
        url,
        description: business.description ?? undefined,
        telephone: business.phoneNumber,
        email: business.email ?? undefined,
        image: image ? new URL(image, SITE_URL).href : undefined,
        address: {
            "@type": "PostalAddress",
            streetAddress: business.address,
            postalCode: business.postalCode,
            addressLocality: business.city,
            addressCountry: "NO",
        },
        geo: business.latitude !== null && business.longitude !== null
            ? { "@type": "GeoCoordinates", latitude: business.latitude, longitude: business.longitude }
            : undefined,
        openingHoursSpecification: business.openingHours.map((h) => ({
            "@type": "OpeningHoursSpecification",
            dayOfWeek: dayNames[h.dayOfWeek],
            opens: h.opensAt.slice(0, 5),
            closes: h.closesAt.slice(0, 5),
        })),
        sameAs: business.website ? [business.website] : undefined,
        aggregateRating: reviews.average !== null && reviews.count >= 3
            ? { "@type": "AggregateRating", ratingValue: reviews.average, reviewCount: reviews.count, bestRating: 5, worstRating: 1 }
            : undefined,
    };
}

// The start page: who runs the site and its logo (the name and logo Google shows next to the results)
export const siteJsonLd: Record<string, unknown> = {
    "@context": "https://schema.org",
    "@graph": [
        {
            "@type": "Organization",
            "@id": `${SITE_URL}/#organisasjon`,
            name: "BESTI",
            url: SITE_URL,
            logo: `${SITE_URL}/logo/besti-ikon-v3-512.png`,
            email: "post@bestiapp.no",
        },
        {
            "@type": "WebSite",
            "@id": `${SITE_URL}/#nettsted`,
            name: "BESTI",
            url: SITE_URL,
            inLanguage: "nb-NO",
            publisher: { "@id": `${SITE_URL}/#organisasjon` },
        },
    ],
};
