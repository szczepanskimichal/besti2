import Script from "next/script";
import { SITE_URL, UMAMI_SCRIPT_URL, UMAMI_WEBSITE_ID } from "@/lib/site";

// Runs before every visit is sent: private pages are not counted at all, and from the address only
// the utm_ tags are kept (never booking ids, times or e-mail links)
const filter = `
window.bestiBeforeSend = function (type, payload) {
  var privatePages = /^\\/(panel|min-besti|varsler|ny-tid|godkjenn|glemt-passord|bekreft-epost|konto-slettet|invitasjon)(\\/|$)/;
  function clean(value) {
    if (!value) return value;
    var url = new URL(value, location.origin);
    if (url.origin !== location.origin) return value;
    var keep = new URLSearchParams();
    url.searchParams.forEach(function (v, k) { if (k.indexOf("utm_") === 0) keep.append(k, v); });
    var query = keep.toString();
    var path = url.pathname + (query ? "?" + query : "");
    return /^https?:/.test(value) ? url.origin + path : path;
  }
  if (privatePages.test(new URL(payload.url, location.origin).pathname)) return false;
  payload.url = clean(payload.url);
  payload.referrer = clean(payload.referrer);
  return payload;
};`;

// The filter first, then Umami's script, in this order (Umami looks the filter up by name)
function loader(websiteId: string) {
    const settings = JSON.stringify({
        src: UMAMI_SCRIPT_URL,
        websiteId,
        domains: new URL(SITE_URL).hostname,
    });
    return `${filter}
(function (s) {
  var script = document.createElement("script");
  script.defer = true;
  script.src = s.src;
  script.dataset.websiteId = s.websiteId;
  script.dataset.domains = s.domains;
  script.dataset.beforeSend = "bestiBeforeSend";
  script.dataset.doNotTrack = "true";
  document.head.appendChild(script);
})(${settings});`;
}

// Visit statistics with Umami: no cookies and nothing stored in the browser, so no cookie banner is needed.
// Only on bestiapp.no (data-domains), and only when UMAMI_WEBSITE_ID is set
export default function Analytics() {
    if (!UMAMI_WEBSITE_ID) {
        return null;
    }

    return <Script id="besti-analytics" strategy="afterInteractive">{loader(UMAMI_WEBSITE_ID)}</Script>;
}
