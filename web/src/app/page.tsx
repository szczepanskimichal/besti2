import BusinessCard from '@/components/BusinessCard/BusinessCard';
import {getBusinesses, getCities} from '@/lib/api';
import {categoryOptionGroups} from '@/lib/categories';
import {todayInOslo} from '@/lib/format';
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
    const timeWindow = date ? timeWindows[tid ?? ""] : undefined;
    // Both requests at the same time instead of one after the other
    const [businesses, cities] = await Promise.all([
        getBusinesses(category, city, q, date, timeWindow?.from, timeWindow?.to),
        getCities(),
    ]);
    return (
        <main className={styles.main}>
            <section className={styles.hero}>
                <h1 className={styles.heroTitle}>Bestill time hos lokale bedrifter</h1>
                <p className={styles.heroLead}>
                    Frisør, håndverker eller psykolog – finn ledig tid og bestill på under ett minutt.
                </p>
                <form method="get" className={styles.filters}>
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
                </form>
            </section>
            {businesses.length === 0 && (
                <p className={styles.subtitle}>
                    {date
                        ? "Ingen ledige tider da. Prøv en annen dag eller tid på dagen."
                        : "Ingen bedrifter funnet. Prøv et annet søk."}
                </p>
            )}
            <ul className={styles.grid}>
                {businesses.map((business) => (
                    <li key={business.id}>
                        <BusinessCard business={business} date={date}/>
                    </li>
                ))}
            </ul>
        </main>
    )
}