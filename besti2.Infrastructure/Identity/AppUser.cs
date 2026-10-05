using Microsoft.AspNetCore.Identity;

namespace besti2.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public Guid? BusinessId { get; set; }
}