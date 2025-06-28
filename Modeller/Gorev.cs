using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagerApp.Modeller
{
    public class Gorev
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Baslik { get; set; }

        public string? Aciklama { get; set; }

        public DateTime SonTarih { get; set; }

        // Eski: public bool Tamamlandi { get; set; } = false;
        public GorevDurumu Durum { get; set; } = GorevDurumu.Bekleyen;

        public int ProjeId { get; set; }
        public Proje? Proje { get; set; }
    }
}