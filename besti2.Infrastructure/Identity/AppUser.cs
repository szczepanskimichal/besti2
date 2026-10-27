using Microsoft.AspNetCore.Identity;

namespace besti2.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public Guid? BusinessId { get; set; }

    // Customer's full name; business owners leave it empty
    public string? FullName { get; set; }
}