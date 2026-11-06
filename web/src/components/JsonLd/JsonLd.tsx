// Facts about the page in Google's own language (schema.org), so Google can show the address,
// opening hours and stars in the search results. "<" is escaped so the text can never end the script tag.
export default function JsonLd({ data }: { data: Record<string, unknown> }) {
    return (
        <script type="application/ld+json"
                dangerouslySetInnerHTML={{ __html: JSON.stringify(data).replace(/</g, "\\u003c") }}/>
    );
}
