using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApp.Modeller;
using TaskManagerApp.Veri;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TaskManagerApp.Denetleyiciler
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjeDenetleyicisi : ControllerBase
    {
        private readonly UygulamaDbBaglami _baglam;

        public ProjeDenetleyicisi(UygulamaDbBaglami baglam)
        {
            _baglam = baglam;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Proje>>> TumProjeleriGetir()
        {
            var kullaniciIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(kullaniciIdStr) || !int.TryParse(kullaniciIdStr, out int kullaniciId))
            {
                return Unauthorized("Geçersiz token.");
            }

            var projeler = await _baglam.Projeler
                .Include(p => p.Gorevler)
                .Where(p => p.KullaniciId == kullaniciId)
                .ToListAsync();

            return projeler;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Proje>> ProjeGetir(int id)
        {
            var proje = await _baglam.Projeler
                .Include(p => p.Gorevler)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proje == null)
                return NotFound();

            return proje;
        }

        [HttpPost]
        public async Task<ActionResult<Proje>> ProjeEkle(Proje proje)
        {
            var kullaniciIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(kullaniciIdStr) || !int.TryParse(kullaniciIdStr, out int kullaniciId))
            {
                return Unauthorized("Kullanıcı kimliği alınamadı.");
            }

            // Aynı kullanıcıda aynı başlığa sahip proje var mı?
            if (!string.IsNullOrWhiteSpace(proje.Baslik))
            {
                var mevcut = await _baglam.Projeler
                    .FirstOrDefaultAsync(p =>
                        p.KullaniciId == kullaniciId &&
                        p.Baslik != null &&
                        p.Baslik.ToLower() == proje.Baslik.ToLower());

                if (mevcut != null)
                {
                    return Ok(mevcut); // tekrar oluşturma → aynısını döndür
                }
            }

            proje.KullaniciId = kullaniciId;
            _baglam.Projeler.Add(proje);
            await _baglam.SaveChangesAsync();

            return CreatedAtAction(nameof(ProjeGetir), new { id = proje.Id }, proje);
        }

        // Dışarıdan kullanılmasa bile GorevEkle için yararlı olabilir
        private async Task<Proje> MevcutProjeVeyaYeniOlustur(string baslik, int kullaniciId)
        {
            var proje = await _baglam.Projeler
                .FirstOrDefaultAsync(p =>
                    p.KullaniciId == kullaniciId &&
                    p.Baslik != null &&
                    p.Baslik.ToLower() == baslik.ToLower());

            if (proje != null)
                return proje;

            proje = new Proje
            {
                Baslik = baslik,
                Aciklama = $"{baslik} kategorisine ait görevler",
                BaslangicTarihi = DateTime.Now,
                BitisTarihi = DateTime.Now.AddDays(7),
                KullaniciId = kullaniciId
            };

            _baglam.Projeler.Add(proje);
            await _baglam.SaveChangesAsync();

            return proje;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ProjeGuncelle(int id, Proje guncellenen)
        {
            if (id != guncellenen.Id)
                return BadRequest();

            _baglam.Entry(guncellenen).State = EntityState.Modified;
            await _baglam.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> ProjeSil(int id)
        {
            var proje = await _baglam.Projeler.FindAsync(id);
            if (proje == null)
                return NotFound();

            _baglam.Projeler.Remove(proje);
            await _baglam.SaveChangesAsync();
            return NoContent();
        }
    }
}