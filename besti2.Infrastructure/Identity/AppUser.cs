using Microsoft.AspNetCore.Identity;

namespace besti2.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public Guid? BusinessId { get; set; }

    // Customer's full name; business owners leave it empty
    public string? FullName { get; set; }

    // When the account was made (accounts from before this was saved have none)
    public DateTime? CreatedUtc { get; set; }

    // "Varsler" settings of a customer: no reminder e-mail the day before, no "Om 2 timer" notice
    public bool ReminderEmailOff { get; set; }
    public bool SoonNoticeOff { get; set; }
}