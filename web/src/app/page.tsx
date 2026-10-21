import BusinessCard from '@/components/BusinessCard/BusinessCard';
import {getBusinesses, getCities} from '@/lib/api';
import {categoryOptionGroups} from '@/lib/categories';
import {isIsoDate, isNumber, todayInOslo} from '@/lib/format';
import {Suspense} from "react";
import Form from "next/form";
import Link from "next/link";
import NearMeButton from "@/components/NearMeButton/NearMeButton";
import styles from "./page.module.css";

type HomeProps = {
    searchParams: Promise<{ category?: string; city?: string; q?: string; date?: string; tid?: string; lat?: string; lng?: string; km?: string, page?: string }>;
};

// Time of day in the search -> from/to for the API (Oslo time)
const timeWindows: Record<string, { from?: string; to?: string }> = {
    formiddag: {to: "12:00"},
    ettermiddag: {from: "12:00", to: "17:00"},
    kveld: {from: "17:00"},
};
// "Nær meg" radius choices in km (0 = all distances)
const radiusOptions = [0, 2, 5, 10, 25, 50, 75, 100];
export default async function Home({searchParams}: HomeProps) {
    const query = await searchParams;
    const {category, city, q, tid} = query;
    // Broken or edited links (e.g. ?date=abc) must not crash the page: invalid values are ignored
    const date = isIsoDate(query.date) ? query.date : undefined;
    const lat = isNumber(query.lat) ? query.lat : undefined;
    const lng = isNumber(query.lng) ? query.lng : undefined;
    const km = isNumber(query.km) ? query.km : undefined;
    const page = isNumber(query.page) ? query.page : undefined;
    const withoutNearMe = new URLSearchParams(
        Object.entries({category, city, q, date, tid}).filter((entry): entry is [string, string] => !!entry[1]));    // Only the cities here (fast); the businesses load inside Suspense below
    const cities = await getCities();
    return (
        <main className={styles.main}>
            <section className={styles.hero}>
                <h1 className={styles.heroTitle}>Bestill time hos lokale bedrifter</h1>
                <p className={styles.heroLead}>
                    Frisør, håndverker eller psykolog – finn ledig tid og bestill på under ett minutt.
                </p>
                <Form action="/" className={styles.filters}>
                    <label className={styles.field}>
                        Hva leter du etter?
                        <input
                            name="q"
                            type="search"
                            defaultValue={q ?? ""}
                            placeholder="F.eks. herreklipp eller fliser"
                        />
                    </label>
                    <label className={styles.field}>
                        Kategori
                        <select name="category" defaultValue={category ?? ""}>
                            <option value="">Alle kategorier</option>
                            {categoryOptionGroups.map((group) => (
                                <optgroup key={group.label} label={group.label}>
                                    {group.options.map((option) => (
                                        <option key={option.value} value={option.value}>{option.label}</option>
                                    ))}
                                </optgroup>
                            ))}
                        </select>
                    </label>
                    <label className={styles.field}>
                        By
                        <input
                            name="city"
                            list="city-list"
                            defaultValue={city ?? ""}
                            placeholder="F.eks. Oslo"
                            autoComplete="off"
                        />
                        <datalist id="city-list">
                            {cities.map((c) => (
                                <option key={c} value={c}/>
                            ))}
                        </datalist>
                    </label>
                    <label className={styles.field}>
                        Når
                        <input name="date" type="date" defaultValue={date ?? ""} min={todayInOslo()}/>
                    </label>
                    <label className={styles.field}>
                        Tid på dagen
                        <select name="tid" defaultValue={tid ?? ""}>
                            <option value="">Hele dagen</option>
                            <option value="formiddag">Før kl. 12</option>
                            <option value="ettermiddag">Kl. 12–17</option>
                            <option value="kveld">Etter kl. 17</option>
                        </select>
                    </label>
                    <button type="submit" className={styles.searchButton}>Søk</button>
                    {lat && lng && (
                        <>
                            <input type="hidden" name="lat" value={lat}/>
                            <input type="hidden" name="lng" value={lng}/>
                            {km && <input type="hidden" name="km" value={km}/>}
                        </>
                    )}
                    <NearMeButton/>
                </Form>
            </section>
            {lat && lng && (
                <p className={styles.subtitle}>
                    📍 Sortert etter avstand fra deg · <Link href={`/?${withoutNearMe}`}>Vis alle</Link>
                </p>
            )}
            {lat && lng && (
                <p className={styles.subtitle}>
                    Innen:{" "}
                    {radiusOptions.map((option) => {
                        const params = new URLSearchParams(withoutNearMe);
                        params.set("lat", lat);
                        params.set("lng", lng);
                        if (option) params.set("km", String(option));
                        const active = String(option) === (km ?? "0");
                        return (
                            <Link key={option} href={`/?${params}`} className={styles.radius}
                                  aria-current={active ? "true" : undefined}>
                                {option ? `${option} km` : "Alle"}
                            </Link>
                        );
                    })}
                </p>
            )}
            {/* key: every new search shows the skeleton again */}
            <Suspense key={JSON.stringify({category, city, q, date, tid, lat, lng, km, page})} fallback={<ResultsSkeleton/>}>
                <Results category={category} city={city} q={q} date={date} tid={tid} lat={lat} lng={lng} km={km} page={page}/>
            </Suspense>
        </main>
    )
}

type SearchProps = {
    category?: string;
    city?: string;
    q?: string;
    date?: string;
    tid?: string;
    lat?: string;
    lng?: string;
    km?: string;
    page?: string;
};

// The slow part: with a date the API calculates free times for every business
async function Results({category, city, q, date, tid, lat, lng, km, page}: SearchProps) {
    // "Tid på dagen" without a date means today
    const day = date || (tid ? todayInOslo() : undefined);
    const timeWindow = day ? timeWindows[tid ?? ""] : undefined;
    const result = await getBusinesses(category, city, q, day, timeWindow?.from, timeWindow?.to, lat, lng, km, page);
    const businesses = result.items;    if (businesses.length === 0) {
        if (day) {
// Same search, one day later
            const params = new URLSearchParams({date: nextDay(day)});
            if (category) params.set("category", category);
            if (city) params.set("city", city);
            if (q) params.set("q", q);
            if (tid) params.set("tid", tid);
            if (lat) params.set("lat", lat);
            if (lng) params.set("lng", lng);
            if (km) params.set("km", km);
            return (
                <p className={styles.subtitle}>
                    Ingen ledige tider da.{" "}
                    <Link href={`/?${params}`}>Prøv dagen etter →</Link>
                </p>
            );
        }
        return <p className={styles.subtitle}>Ingen bedrifter funnet. Prøv et annet søk.</p>;
    }

    // The same search on another page (all filters stay)
    const pageHref = (target: number) => {
        const params = new URLSearchParams(
            Object.entries({category, city, q, date, tid, lat, lng, km})
                .filter((entry): entry is [string, string] => !!entry[1]));
        if (target > 1) params.set("page", String(target));
        return `/?${params}`;
    };

    return (
        <>
            <ul className={styles.grid}>
                {businesses.map((business) => (
                    <li key={business.id}>
                        <BusinessCard business={business} date={day}/>
                    </li>
                ))}
            </ul>
            {result.pageCount > 1 && (
                <nav className={styles.pagination} aria-label="Sider">
                    {result.page > 1 ? <Link href={pageHref(result.page - 1)}>← Forrige</Link> : <span/>}
                    <span>Side {result.page} av {result.pageCount}</span>
                    {result.page < result.pageCount ? <Link href={pageHref(result.page + 1)}>Neste →</Link> : <span/>}
                </nav>
            )}
        </>
    );
}

// Grey placeholder cards while the results load
function ResultsSkeleton() {
    return (
        <ul className={styles.grid} aria-busy="true" aria-label="Laster bedrifter">
            {[1, 2, 3].map((n) => (
                <li key={n} className={styles.skeleton}/>
            ))}
        </ul>
    );
}

// "2026-10-24" -> "2026-10-25" (noon UTC, so summer time never moves the day)
function nextDay(date: string): string {
    const day = new Date(`${date}T12:00:00Z`);
    day.setUTCDate(day.getUTCDate() + 1);
    return day.toISOString().slice(0, 10);
}