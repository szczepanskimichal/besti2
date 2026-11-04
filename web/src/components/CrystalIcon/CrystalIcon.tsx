import styles from "./CrystalIcon.module.css";

const FILES = {
    stjerne: "/ikoner/stjerne-liten.svg",
    bjelle: "/ikoner/bjelle.svg",
    pinne: "/ikoner/pinne.svg",
    hjerte: "/ikoner/hjerte.svg",
    gave: "/ikoner/gave.svg",
    kompass: "/ikoner/kompass.svg",
    stempelkort: "/ikoner/stempelkort.svg",
    vinke: "/ikoner/vinke.svg",
    snakkeboble: "/ikoner/snakkeboble.svg",
    advarsel: "/ikoner/advarsel.svg",
    del: "/ikoner/del.svg",
    tommel: "/ikoner/tommel.svg",
    notatblokk: "/ikoner/notatblokk.svg",
    feiring: "/ikoner/feiring.svg",
    iskra: "/ikoner/iskra.svg",
};

// A crystal icon in the aurora colours instead of an emoji (★ 🔔 📍 🧭 👍 📝 💬 🎉 ✨), the size of the text around it.
// off = greyed out, e.g. a reminder that is turned off
export default function CrystalIcon({ name, off = false }: { name: keyof typeof FILES; off?: boolean }) {
    // eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed
    return <img src={FILES[name]} alt="" aria-hidden="true" className={off ? `${styles.icon} ${styles.off}` : styles.icon}/>;
}
