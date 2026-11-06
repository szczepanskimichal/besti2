import { MiniStamp } from "../PendingBadge";
import PersonsIcon from "@/components/PersonsIcon/PersonsIcon";
import calendar from "../calendar.module.css";

// What the colours, the small stamps and the people icons mean (the same in every view)
// place: "top" above the calendar (hidden on phones), "bottom" under the phone list (phones only), so on a
// phone the day's visits come first
export default function CalendarLegend({ place }: { place: "top" | "bottom" }) {
    // On a phone the coloured frames are enough, so the small stamps only show in the laptop legend (Ada 2026-11-06)
    const stamps = place === "top";

    return (
        <div className={`${calendar.legend} ${place === "top" ? calendar.legendTop : calendar.legendBottom}`}
             aria-label="Forklaring">
            <ul>
                <li><span className={`${calendar.legendSwatch} ${calendar.swatchOk}`}/>{stamps && <MiniStamp status="Bekreftet"/>}Bekreftet</li>
                <li><span className={`${calendar.legendSwatch} ${calendar.swatchWait}`}/>{stamps && <MiniStamp status="Venter"/>}Venter på svar</li>
                <li><span className={`${calendar.legendSwatch} ${calendar.swatchCancel}`}/>Avlyst / Møtte ikke / Slettet kunde</li>
                <li><span className={`${calendar.legendSwatch} ${calendar.swatchOff}`}/>Pause / blokkert – ikke book</li>
            </ul>
            <ul>
                <li><PersonsIcon persons={1}/>Én person</li>
                <li><PersonsIcon persons={2}/>To sammen</li>
                <li><PersonsIcon persons={3}/>Gruppe</li>
            </ul>
        </div>
    );
}
