using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicStore.Models
{
    public class LookupForm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Escribe un nombre."), StringLength(160), Display(Name = "Nombre")]
        public string Name { get; set; }
        [StringLength(1000), Display(Name = "Descripción")]
        public string Description { get; set; }
        public string Version { get; set; }
        public int AlbumCount { get; set; }
    }
    public class LookupList
    {
        public List<LookupForm> Items { get; set; }
        public int Page { get; set; }
        public bool HasNext { get; set; }
        public string Search { get; set; }
    }
    public class UserRow
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool IsAdministrator { get; set; }
        public bool IsOwner { get; set; }
        public bool IsCurrentUser { get; set; }
        public string Stamp { get; set; }
    }
    public class UserList
    {
        public List<UserRow> Items { get; set; }
        public int Page { get; set; }
        public bool HasNext { get; set; }
        public string Search { get; set; }
    }
    public class RoleChangeForm
    {
        [Required, StringLength(128)] public string Id { get; set; }
        [Required, StringLength(128)] public string Stamp { get; set; }
        public bool MakeAdministrator { get; set; }
        [Required, StringLength(128), DataType(DataType.Password)] public string Password { get; set; }
    }
}
