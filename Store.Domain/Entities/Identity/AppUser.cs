using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Store.Domain.Common;
 
namespace Store.Domain.Entities.Identity
{
    public class AppUser : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
        public ICollection<Address>? Address;
        public bool IsPermanentlyBanned { get; set; } = false;
        public int LockoutCount { get; set; } = 0;
    }
    public class Address
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
        public string State { get; set; } = null!;
        public string? AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
    }
}
