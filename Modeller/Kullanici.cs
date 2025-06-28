using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TaskManagerApp.Modeller
{
    public class Kullanici
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Ad { get; set; }

        [Required]
        public string? Eposta { get; set; }

        [Required]
        public string? Sifre { get; set; }

        public string? Rol { get; set; } = "Kullanici"; // Admin olabilir

        public ICollection<Proje>? Projeler { get; set; }
    }
}
