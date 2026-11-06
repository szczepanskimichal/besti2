import BusinessCard from '@/components/BusinessCard/BusinessCard';
import {getBusinesses, getCities} from '@/lib/api';
import {categoryOptionGroups} from '@/lib/categories';
import {isDecimal, isIsoDate, isPageNumber, todayInOslo} from '@/lib/format';
import {Suspense} from "react";
import Form from "next/form";
import Link from "next/link";
import NearMeButton from "@/components/NearMeButton/NearMeButton";
import ForWhomPicker from "@/components/ForWhomPicker/ForWhomPicker";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import styles from "./page.module.css";
import GlassSelect from "@/components/GlassSelect/GlassSelect";
import CityPicker from "@/components/CityPicker/CityPicker";
import {JumpToSearchButton, ScrollToResults} from "@/components/MobileSearchJump/MobileSearchJump";
import HomeSky from "@/components/HomeSky/HomeSky";

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
    const {category, city, tid} = query;
    // The API reads at most 100 characters; a longer text (pasted by mistake) is cut instead of failing
    const q = query.q?.slice(0, 100);
    // Broken or edited links (e.g. ?date=abc) must not crash the page: invalid values are ignored
    const date = isIsoDate(query.date) ? query.date : undefined;
    // A position outside the map (?lat=999) is refused by the API: ignore it like any other broken value
    const lat = isDecimal(query.lat) && Math.abs(Number(query.lat)) <= 90 ? query.lat : undefined;
    const lng = isDecimal(query.lng) && Math.abs(Number(query.lng)) <= 180 ? query.lng : undefined;
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
            <HomeSky/>
            <section className={styles.hero}>
                {/* On a phone the title fills the first screen alone, with a button down to the search */}
                <div className={styles.heroIntro}>
                    <h1 className={styles.heroTitle}>Magiske opplevelser med din BESTI</h1>
                    <p className={styles.heroLead}>Finn ledig time og bestill på under ett minutt</p>
                    <JumpToSearchButton className={styles.jumpButton}/>
                </div>
                <Form action="/" id="sok" className={styles.filters}>
                    <label className={styles.field}>
                        Hva vil du oppleve med BESTI?
                        <input
                            name="q"
                            type="search"
                            defaultValue={q ?? ""}
                            placeholder="Frisør, dekkskift, massasje"
                        />
                    </label>
                    <ForWhomPicker key={personer ?? ""} persons={personer}/>
                    <GlassSelect name="category" label="Kategori" defaultValue={category ?? ""}
                                 groups={[{ options: [{ value: "", label: "Alle kategorier" }] }, ...categoryOptionGroups]}/>
                    <label className={styles.field}>
                        Hvor?
                        <CityPicker name="city" cities={cities} defaultValue={city ?? ""} placeholder="Gjøvik, Oslo, Bergen"/>
                    </label>
                    {/* On the computer "Når har du tid?" and "Beste tidspunkt" side by side, so the card is shorter */}
                    <label className={`${styles.field} ${styles.half}`}>
                        Når har du tid?
                        <input name="date" type="date" defaultValue={date ?? ""} min={todayInOslo()}/>
                    </label>
                    <GlassSelect name="tid" label="Beste tidspunkt" defaultValue={tid ?? ""} className={styles.half}
                                 groups={[{ options: [
                                     { value: "", label: "Hele dagen" },
                                     { value: "formiddag", label: "Før kl. 12" },
                                     { value: "ettermiddag", label: "Kl. 12–17" },
                                     { value: "kveld", label: "Etter kl. 17" },
                                 ] }]}/>
                    <button type="submit" className={styles.searchButton}>
                        Finn BESTI-time
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
            <h2 id="resultater" className={styles.listTitle}>Lokale BESTI-bedrifter</h2>
            <ScrollToResults search={JSON.stringify({...search, page})}/>
            {lat && lng && (
                <p className={styles.subtitle}>
                    <CrystalIcon name="pinne"/> {sorter ? "Nær deg" : "Sortert etter avstand fra deg"} ·{" "}
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

// "Sorter:" A–Å / Nærmest, Best vurdert and 4★ og over. Shown from MIN_FOR_SORTING results
// (with only a few businesses it is just noise), and always while one of them is turned on.
const MIN_FOR_SORTING = 30;
function SortLinks({search}: {search: SearchProps}) {
    const {lat, lng, sorter, stjerner} = search;
    return (
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
                {stjerner ? "✓ " : ""}4<CrystalIcon name="stjerne"/> og over
            </Link>
        </p>
    );
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
                    Ingen bedrifter med 4 stjerner og over her.{" "}
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
            {(result.totalCount >= MIN_FOR_SORTING || sorter || stjerner) && <SortLinks search={search}/>}
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