import BusinessCard from '@/components/BusinessCard/BusinessCard';
import { getBusinesses, getCities } from '@/lib/api';
import { categoryOptions } from '@/lib/categories';
import styles from "./page.module.css";

type HomeProps = {
    searchParams: Promise<{ category?: string; city?: string }>;
};

export default async function Home({ searchParams }: HomeProps) {
    const { category, city } = await searchParams;
    const businesses = await getBusinesses(category, city);
    const cities = await getCities();
    return (
        <section className={styles.main}>
            <section className={styles.hero}>
                <h1 className={styles.heroTitle}>Bestill time hos lokale bedrifter</h1>
                <p className={styles.heroLead}>
                    Frisør, håndverker eller psykolog – finn ledig tid og bestill på under ett minutt.
                </p>           
                <form method="get" className={styles.filters}>
                <label className={styles.field}>
                    Kategori
                    <select name="category" defaultValue={category ?? ""}>
                        <option value="">Alle kategorier</option>
                        {categoryOptions.map((option) => (
                            <option key={option.value} value={option.value}>{option.label}</option>
                        ))}
                    </select>
                </label>

                <label className={styles.field}>
                    By
                    <select name="city" defaultValue={city ?? ""}>
                        <option value="">Alle byer</option>
                        {cities.map((c) => (
                            <option key={c} value={c}>{c}</option>
                        ))}
                    </select>
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
                        <BusinessCard business={business} />
                    </li>
                ))}
            </ul>
        </section>
    )
}