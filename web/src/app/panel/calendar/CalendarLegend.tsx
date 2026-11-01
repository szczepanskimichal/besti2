import { MiniStamp } from "../PendingBadge";
import PersonsIcon from "@/components/PersonsIcon/PersonsIcon";
import calendar from "../calendar.module.css";

// What the colours, the small stamps and the people icons mean (the same in every view)
export default function CalendarLegend() {
    return (
        <div className={calendar.legend} aria-label="Forklaring">
            <ul>
                <li><span className={`${calendar.legendSwatch} ${calendar.swatchOk}`}/><MiniStamp status="Bekreftet"/>Bekreftet</li>
                <li><span className={`${calendar.legendSwatch} ${calendar.swatchWait}`}/><MiniStamp status="Venter"/>Venter på svar</li>
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
