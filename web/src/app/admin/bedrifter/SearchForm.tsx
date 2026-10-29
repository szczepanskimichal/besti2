import styles from "../admin.module.css";

// Search by name, city, org number or the owner's e-mail (a normal GET form: the search is in the address)
export default function SearchForm({ status, q }: { status: string; q: string }) {
    return (
        <form action="/admin/bedrifter" className={styles.search} role="search">
            {status && <input type="hidden" name="status" value={status}/>}
            <label className={styles.srOnly} htmlFor="admin-sok">Søk</label>
            <input id="admin-sok" name="q" type="search" defaultValue={q} maxLength={100}
                   placeholder="Navn, sted, org.nr. eller e-post" className={styles.searchInput}/>
            <button type="submit" className={styles.secondaryButton}>Søk</button>
        </form>
    );
}
