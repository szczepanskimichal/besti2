import BusinessCard from '@/components/BusinessCard/BusinessCard';
import {getBusinesses, getCities} from '@/lib/api';
import {categoryOptionGroups} from '@/lib/categories';
import styles from "./page.module.css";

type HomeProps = {
    searchParams: Promise<{ category?: string; city?: string; q?: string }>;
};

export default async function Home({searchParams}: HomeProps) {
    const {category, city, q} = await searchParams;
    const businesses = await getBusinesses(category, city, q);
    const cities = await getCities();
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

                    <button type="submit" className={styles.searchButton}>Søk</button>
                </form>
            </section>

            {businesses.length === 0 && (
                <p className={styles.subtitle}>Ingen bedrifter funnet. Prøv et annet søk.</p>
            )}

            <ul className={styles.grid}>
                {businesses.map((business) => (
                    <li key={business.id}>
                        <BusinessCard business={business}/>
                    </li>
                ))}
            </ul>
        </main>
    )
}