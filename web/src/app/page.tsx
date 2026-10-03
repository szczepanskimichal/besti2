import { getBusinesses } from '@/lib/api';
import { getCategoryLabel } from '@/lib/categories';

export default async function Home() {
    const businesses = await getBusinesses();
    
    return (
        <main>
            <h1>Finn en bedrift</h1>
            <ul>
                {businesses.map((business) => (
                    <li key={business.id}>
                        <h2>{business.name}</h2>
                        <p>{getCategoryLabel(business.category)} · {business.city}</p>
                        {business.description && <p>{business.description}</p>}
                    </li>
                ))}
            </ul>
        </main>
    )
}