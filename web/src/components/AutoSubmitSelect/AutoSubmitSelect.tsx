"use client";
import type { ComponentProps } from "react";

// A select that sends its form right after a change, so the customer does not need the "Vis" button
export default function AutoSubmitSelect(props: ComponentProps<"select">) {
    return <select {...props} onChange={(event) => event.currentTarget.form?.requestSubmit()}/>;
}
