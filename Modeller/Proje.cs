using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace TaskManagerApp.Modeller
{
    public class Proje
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Baslik { get; set; }

        public string? Aciklama { get; set; }

        public DateTime BaslangicTarihi { get; set; }

        public DateTime BitisTarihi { get; set; }

        public int KullaniciId { get; set; }
        public Kullanici? Kullanici { get; set; }

        public ICollection<Gorev>? Gorevler { get; set; }
    }
}
