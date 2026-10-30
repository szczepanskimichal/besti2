using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace besti2.Infrastructure.Services.Email;

// The HTML version of every e-mail, built from the plain text that the services already write, so the texts stay
// in one place. Same look as the site: night blue, mint button, Besti logo. The plain text is still sent next to it
// for mail programs without HTML. Tables and inline styles only: that is what Outlook and Gmail understand
public static partial class EmailHtml
{
    // "Tid: 14. nov. kl. 10:00" -> the label in bold. Short labels only, so a normal sentence with a colon stays as it is
    [GeneratedRegex(@"^([A-ZÆØÅ][\wæøåÆØÅ\- ]{1,18}):\s+(\S.*)$")]
    private static partial Regex LabelLine();

    // "Bekreft eller avlys dem her: https://..." -> a button with that text
    [GeneratedRegex(@"^(.{2,60}?):?\s+(https?://\S+)$")]
    private static partial Regex LinkLine();

    [GeneratedRegex(@"https?://[^\s<]+")]
    private static partial Regex Url();

    public static string Render(string subject, string body, string webBaseUrl)
    {
        var html = new StringBuilder();
        var paragraphs = body.Replace("\r\n", "\n").Trim().Split("\n\n");
        foreach (var paragraph in paragraphs)
        {
            var lines = paragraph.Split('\n');
            // A paragraph that is only one link becomes a button
            if (lines.Length == 1 && LinkLine().Match(lines[0]) is { Success: true } link)
            {
                html.Append(Button(link.Groups[1].Value.TrimEnd(':'), link.Groups[2].Value));
                continue;
            }

            html.Append("<p style=\"margin:0 0 16px;font-size:15px;line-height:1.6;color:#eef7f4\">");
            html.Append(string.Join("<br>", lines.Select(Line)));
            html.Append("</p>");
        }

        var logo = $"{webBaseUrl.TrimEnd('/')}/logo/besti-napis.png";
        return $"""
            <!doctype html>
            <html lang="nb">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><meta name="color-scheme" content="dark"><title>{Encode(subject)}</title></head>
            <body style="margin:0;padding:0;background:#04101a">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#04101a" style="background:#04101a">
            <tr><td align="center" style="padding:24px 12px">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px">
            <tr><td style="padding:0 8px 16px"><a href="{Encode(webBaseUrl)}"><img src="{Encode(logo)}" alt="Besti" width="119" height="50" style="display:block;border:0"></a></td></tr>
            <tr><td bgcolor="#0b1f29" style="background:#0b1f29;border:1px solid #1d4a48;border-radius:16px;padding:28px 24px;font-family:Inter,Segoe UI,Helvetica,Arial,sans-serif">
            {html}
            </td></tr>
            <tr><td style="padding:16px 8px;font-family:Inter,Segoe UI,Helvetica,Arial,sans-serif;font-size:12px;line-height:1.5;color:#8fb3aa">
            BESTI · Finn og bestill time · <a href="{Encode(webBaseUrl)}" style="color:#72d6bd">bestiapp.no</a>
            </td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """;
    }

    private static string Line(string line)
    {
        var label = LabelLine().Match(line);
        if (label.Success && !Url().IsMatch(label.Groups[2].Value))
        {
            return $"<span style=\"color:#9fc9bd\">{Encode(label.Groups[1].Value)}:</span> " +
                   $"<strong style=\"color:#ffffff\">{Encode(label.Groups[2].Value)}</strong>";
        }

        // Links inside a sentence stay links
        var result = new StringBuilder();
        var last = 0;
        foreach (Match url in Url().Matches(line))
        {
            result.Append(Encode(line[last..url.Index]));
            result.Append($"<a href=\"{Encode(url.Value)}\" style=\"color:#72d6bd\">{Encode(url.Value)}</a>");
            last = url.Index + url.Length;
        }

        result.Append(Encode(line[last..]));
        return result.ToString();
    }

    private static string Button(string text, string url) =>
        "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:4px 0 20px\"><tr>" +
        "<td bgcolor=\"#72d6bd\" style=\"background:#72d6bd;border-radius:999px\">" +
        $"<a href=\"{Encode(url)}\" style=\"display:inline-block;padding:12px 22px;font-size:15px;font-weight:700;" +
        $"color:#06201f;text-decoration:none\">{Encode(text)}</a></td></tr></table>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
