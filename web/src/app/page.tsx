import BusinessCard from '@/components/BusinessCard/BusinessCard';
import { getBusinesses } from '@/lib/api';
import { getCategoryLabel } from '@/lib/categories';
import styles from "./page.module.css";

export default async function Home() {
    const businesses = await getBusinesses();
    
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Finn en bedrift</h1>
            <ul className={styles.grid}>
                {businesses.map((business) => (
                    <li key={business.id}>
                        <BusinessCard business={business} />
                    </li>
                ))}
            </ul>
        </main>
    )
}