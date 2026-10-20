import BusinessCard from '@/components/BusinessCard/BusinessCard';
import {getBusinesses, getCities} from '@/lib/api';
import {categoryOptionGroups} from '@/lib/categories';
import {todayInOslo} from '@/lib/format';
import {Suspense} from "react";
import Form from "next/form";
import Link from "next/link";
import styles from "./page.module.css";

type HomeProps = {
    searchParams: Promise<{ category?: string; city?: string; q?: string; date?: string; tid?: string }>;
};

// Time of day in the search -> from/to for the API (Oslo time)
const timeWindows: Record<string, { from?: string; to?: string }> = {
    formiddag: {to: "12:00"},
    ettermiddag: {from: "12:00", to: "17:00"},
    kveld: {from: "17:00"},
};

export default async function Home({searchParams}: HomeProps) {
    const {category, city, q, date, tid} = await searchParams;
    // Only the cities here (fast); the businesses load inside Suspense below
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
                </Form>
            </section>
            {/* key: every new search shows the skeleton again */}
            <Suspense key={JSON.stringify({category, city, q, date, tid})} fallback={<ResultsSkeleton/>}>
                <Results category={category} city={city} q={q} date={date} tid={tid}/>
            </Suspense>
        </main>
    )
}

type SearchProps = { category?: string; city?: string; q?: string; date?: string; tid?: string };

// The slow part: with a date the API calculates free times for every business
async function Results({category, city, q, date, tid}: SearchProps) {
    const timeWindow = date ? timeWindows[tid ?? ""] : undefined;
    const businesses = await getBusinesses(category, city, q, date, timeWindow?.from, timeWindow?.to);
    if (businesses.length === 0) {
        if (date) {
// Same search, one day later
            const params = new URLSearchParams({date: nextDay(date)});
            if (category) params.set("category", category);
            if (city) params.set("city", city);
            if (q) params.set("q", q);
            if (tid) params.set("tid", tid);
            return (
                <p className={styles.subtitle}>
                    Ingen ledige tider da.{" "}
                    <Link href={`/?${params}`}>Prøv dagen etter →</Link>
                </p>
            );
        }
        return <p className={styles.subtitle}>Ingen bedrifter funnet. Prøv et annet søk.</p>;
    }

    return (
        <ul className={styles.grid}>
            {businesses.map((business) => (
                <li key={business.id}>
                    <BusinessCard business={business} date={date}/>
                </li>
            ))}
        </ul>
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