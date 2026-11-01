import styles from "./PersonsIcon.module.css";

// One, two or several people in the glowing BESTI line style (the same figures as "For hvem?": Egentid, Duo, Team),
// instead of the 👯 emoji. persons = how many come together (1 = an ordinary visit)
export default function PersonsIcon({ persons }: { persons: number }) {
    const file = persons >= 3 ? "personer-gruppe" : persons === 2 ? "personer-to" : "person-en";
    const label = persons >= 3 ? `Gruppe, ${persons} personer` : persons === 2 ? "To personer sammen" : "Én person";
    // eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed
    return <img src={`/ikoner/${file}.svg`} alt={label} title={label} className={styles.icon}/>;
}

// How many come together for this visit: the group's places, 2 for a pair without group data, else 1
export function personsFor(booking: { inviteId: string | null; group?: { persons: number } | null }): number {
    if (booking.group) {
        return booking.group.persons;
    }
    return booking.inviteId ? 2 : 1;
}
