using besti2.Infrastructure.Services.Email;

namespace besti2.Tests;

public class EmailHtmlTests
{
    // check if a paragraph that is only a link becomes a button with the text in front of the link
    [Fact]
    public void Render_LinkParagraph_BecomesButton()
    {
        var html = EmailHtml.Render("Ny tid", "Hei Kari!\n\nSvar her: https://bestiapp.no/ny-tid?t=abc", "https://bestiapp.no");

        Assert.Contains("href=\"https://bestiapp.no/ny-tid?t=abc\"", html);
        Assert.Contains(">Svar her</a>", html);
    }

    // check if "Tid: ..." lines get the label in bold and the customer's own text is escaped
    [Fact]
    public void Render_LabelLines_AreBoldAndEscaped()
    {
        var html = EmailHtml.Render("Bestilling", "Tjeneste: Klipp <b>\nTid: 14. nov. kl. 10:00", "https://bestiapp.no");

        Assert.Contains("<strong style=\"color:#ffffff\">Klipp &lt;b&gt;</strong>", html);
        Assert.Contains("Tid:</span>", html);
        Assert.DoesNotContain("<b>", html);
    }

    // check if the logo comes from the site, so it shows in the mail program
    [Fact]
    public void Render_UsesLogoFromSite()
    {
        var html = EmailHtml.Render("Hei", "Hei!", "https://bestiapp.no/");

        Assert.Contains("src=\"https://bestiapp.no/logo/besti-napis.png\"", html);
    }
}
