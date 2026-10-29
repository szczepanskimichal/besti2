import BusinessCard from '@/components/BusinessCard/BusinessCard';
import {getBusinesses, getCities} from '@/lib/api';
import {categoryOptionGroups} from '@/lib/categories';
import {isDecimal, isIsoDate, isPageNumber, todayInOslo} from '@/lib/format';
import {Suspense} from "react";
import Form from "next/form";
import Link from "next/link";
import NearMeButton from "@/components/NearMeButton/NearMeButton";
import styles from "./page.module.css";

type HomeProps = {
    searchParams: Promise<{ category?: string; city?: string; q?: string; date?: string; tid?: string; lat?: string; lng?: string; km?: string, page?: string; sorter?: string; stjerner?: string; personer?: string }>;
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
    const lat = isDecimal(query.lat) ? query.lat : undefined;
    const lng = isDecimal(query.lng) ? query.lng : undefined;
    const km = isDecimal(query.km) ? query.km : undefined;
    const page = isPageNumber(query.page) ? query.page : undefined;
    // "Best vurdert" and "4★ og over": any other value is ignored
    const sorter = query.sorter === "vurdering" ? query.sorter : undefined;
    const stjerner = query.stjerner === "4" ? query.stjerner : undefined;
    // "For hvem?": BESTI sammen shows only times when that many employees are free together (2-8)
    const personer = query.personer && /^[2-8]$/.test(query.personer) ? query.personer : undefined;
    const search: SearchProps = {category, city, q, date, tid, lat, lng, km, sorter, stjerner, personer};
    // Only the cities here (fast); the businesses load inside Suspense below
    const cities = await getCities();
    return (
        <main className={styles.main}>
            <section className={styles.hero}>
                <div>
                    <h1 className={styles.heroTitle}>Bestill time hos lokale bedrifter</h1>
                    <p className={styles.heroLead}>
                        Frisør, håndverker eller psykolog – finn ledig tid og bestill på under ett minutt.
                    </p>
                </div>
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
                        For hvem?
                        <select name="personer" defaultValue={personer ?? ""}>
                            <option value="">Bare meg</option>
                            <option value="2">Meg + en venn 👯</option>
                            {[3, 4, 5, 6, 7, 8].map((count) => (
                                <option key={count} value={count}>{count} personer 👯</option>
                            ))}
                        </select>
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
                    <button type="submit" className={styles.searchButton}>
                        {/* eslint-disable-next-line @next/next/no-img-element -- small SVG logo, next/image not needed */}
                        <img src="/logo/besti-pin.svg" alt="" width={28} height={30} className={styles.searchIcon}/>
                        Finn besti time
                    </button>
                    {lat && lng && (
                        <>
                            <input type="hidden" name="lat" value={lat}/>
                            <input type="hidden" name="lng" value={lng}/>
                            {km && <input type="hidden" name="km" value={km}/>}
                        </>
                    )}
                    {sorter && <input type="hidden" name="sorter" value={sorter}/>}
                    {stjerner && <input type="hidden" name="stjerner" value={stjerner}/>}
                    <NearMeButton/>
                </Form>
            </section>
            <h2 className={styles.listTitle}>Bedrifter</h2>
            {lat && lng && (
                <p className={styles.subtitle}>
                    📍 {sorter ? "Nær deg" : "Sortert etter avstand fra deg"} ·{" "}
                    <Link href={searchHref(search, {lat: undefined, lng: undefined, km: undefined})}>Vis alle</Link>
                </p>
            )}
            {lat && lng && (
                <p className={styles.subtitle}>
                    Innen:{" "}
                    {radiusOptions.map((option) => {
                        const active = String(option) === (km ?? "0");
                        return (
                            <Link key={option} href={searchHref(search, {km: option ? String(option) : undefined})}
                                  className={styles.radius}
                                  aria-current={active ? "true" : undefined}>
                                {option ? `${option} km` : "Alle"}
                            </Link>
                        );
                    })}
                </p>
            )}
            <p className={styles.subtitle}>
                Sorter:{" "}
                <Link href={searchHref(search, {sorter: undefined})} className={styles.radius}
                      aria-current={!sorter ? "true" : undefined}>
                    {lat && lng ? "Nærmest" : "A–Å"}
                </Link>
                <Link href={searchHref(search, {sorter: "vurdering"})} className={styles.radius}
                      aria-current={sorter ? "true" : undefined}>
                    Best vurdert
                </Link>
                {/* One click on, one click off */}
                <Link href={searchHref(search, {stjerner: stjerner ? undefined : "4"})} className={styles.radius}
                      aria-current={stjerner ? "true" : undefined}>
                    {stjerner ? "✓ " : ""}4★ og over
                </Link>
            </p>
            {/* key: every new search shows the skeleton again */}
            <Suspense key={JSON.stringify({...search, page})} fallback={<ResultsSkeleton/>}>
                <Results {...search} page={page}/>
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
    sorter?: string;
    stjerner?: string;
    personer?: string;
};

// The same search with some values changed (undefined = removed); a new search always starts on page 1
function searchHref(search: SearchProps, changes: Partial<SearchProps>): string {
    const merged: SearchProps = {...search, page: undefined, ...changes};
    const params = new URLSearchParams(
        Object.entries(merged).filter((entry): entry is [string, string] => !!entry[1]));
    return `/?${params}`;
}

// The slow part: with a date or a time of day the API calculates free times for every business
async function Results(search: SearchProps) {
    const {category, city, q, date, tid, lat, lng, km, page, sorter, stjerner, personer} = search;
    // "Tid på dagen" without a date means today
    // "Tid på dagen" without a date: the API finds the soonest day for each business
    const timeWindow = timeWindows[tid ?? ""];
    const result = await getBusinesses(category, city, q, date, timeWindow?.from, timeWindow?.to, lat, lng, km, page,
        sorter ? "rating" : undefined, stjerner, personer);
    const businesses = result.items;
    if (businesses.length === 0) {
        if (stjerner) {
            return (
                <p className={styles.subtitle}>
                    Ingen bedrifter med 4★ og over her.{" "}
                    <Link href={searchHref(search, {stjerner: undefined})}>Vis alle →</Link>
                </p>
            );
        }
        if (date) {
            // Same search, one day later
            return (
                <p className={styles.subtitle}>
                    Ingen ledige tider da.{" "}
                    <Link href={searchHref(search, {date: nextDay(date)})}>Prøv dagen etter →</Link>
                </p>
            );
        }
        if (tid || personer) {
            return <p className={styles.subtitle}>Ingen ledige tider{personer ? ` for ${personer === "2" ? "to" : `${personer} samtidig`}` : ""} de neste 14 dagene.</p>;
        }
        return <p className={styles.subtitle}>Ingen bedrifter funnet. Prøv et annet søk.</p>;
    }

    // The same search on another page (all filters stay)
    const pageHref = (target: number) => searchHref(search, {page: target > 1 ? String(target) : undefined});

    return (
        <>
            <ul className={styles.grid}>
                {businesses.map((business) => (
                    <li key={business.id}>
                        <BusinessCard business={business} persons={personer ? Number(personer) : 1}/>
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