using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicStore.Models
{
    public class UserCart
    {
        [Key, StringLength(128)] public string UserId { get; set; }
        [Required, StringLength(64), Index("UX_UserCart_CartId", IsUnique = true)] public string CartId { get; set; }
    }
}
