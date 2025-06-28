using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApp.Modeller;
using TaskManagerApp.Veri;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TaskManagerApp.Modeller.Dtos;

namespace TaskManagerApp.Denetleyiciler
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GorevDenetleyicisi : ControllerBase
    {
        private readonly UygulamaDbBaglami _baglam;

        public GorevDenetleyicisi(UygulamaDbBaglami baglam)
        {
            _baglam = baglam;
        }

        private int GetUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Gorev>>> TumGorevleriGetir()
        {
            var kullaniciId = GetUserId();
            if (kullaniciId == 0)
                return Unauthorized("Geçersiz token.");

            var gorevler = await _baglam.Gorevler
                .Include(g => g.Proje)
                .Where(g => g.Proje != null && g.Proje.KullaniciId == kullaniciId)
                .AsNoTracking()
                .ToListAsync();

            return Ok(gorevler);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Gorev>> GorevGetir(int id)
        {
            var gorev = await _baglam.Gorevler
                .Include(g => g.Proje)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gorev == null)
                return NotFound();

            return gorev;
        }

        [HttpPost]
        public async Task<ActionResult<Gorev>> GorevEkle(Gorev yeniGorev)
        {
            var kullaniciId = GetUserId();
            if (kullaniciId == 0)
                return Unauthorized("Geçersiz token.");

            var projeAdi = yeniGorev.Baslik?.Trim();
            if (string.IsNullOrWhiteSpace(projeAdi))
                return BadRequest("Görev başlığı boş olamaz.");

            var proje = await _baglam.Projeler
                .FirstOrDefaultAsync(p =>
                    p.KullaniciId == kullaniciId &&
                    p.Baslik != null &&
                    p.Baslik.ToLower() == projeAdi.ToLower());

            if (proje == null)
            {
                proje = new Proje
                {
                    Baslik = projeAdi,
                    Aciklama = $"{projeAdi} kategorisine ait görevler",
                    BaslangicTarihi = DateTime.Now,
                    BitisTarihi = DateTime.Now.AddDays(7),
                    KullaniciId = kullaniciId
                };

                _baglam.Projeler.Add(proje);
                await _baglam.SaveChangesAsync();
            }

            yeniGorev.ProjeId = proje.Id;
            yeniGorev.Proje = proje;

            _baglam.Gorevler.Add(yeniGorev);
            await _baglam.SaveChangesAsync();

            var gorevWithProje = await _baglam.Gorevler
                .Include(g => g.Proje)
                .FirstOrDefaultAsync(g => g.Id == yeniGorev.Id);

            return CreatedAtAction(nameof(GorevGetir), new { id = yeniGorev.Id }, gorevWithProje);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> GorevSil(int id)
        {
            var gorev = await _baglam.Gorevler.FindAsync(id);
            if (gorev == null)
                return NotFound();

            _baglam.Gorevler.Remove(gorev);
            await _baglam.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/aktif-yap")]
        public async Task<IActionResult> GoreviAktifYap(int id)
        {
            var kullaniciId = GetUserId();

            var gorev = await _baglam.Gorevler
                .Include(g => g.Proje)
                .FirstOrDefaultAsync(g => g.Id == id && g.Proje != null && g.Proje.KullaniciId == kullaniciId);

            if (gorev == null)
                return NotFound();

            gorev.Durum = GorevDurumu.Aktif;
            await _baglam.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/tamamla")]
        public async Task<IActionResult> GoreviTamamla(int id)
        {
            var kullaniciId = GetUserId();

            var gorev = await _baglam.Gorevler
                .Include(g => g.Proje)
                .FirstOrDefaultAsync(g => g.Id == id && g.Proje != null && g.Proje.KullaniciId == kullaniciId);

            if (gorev == null)
                return NotFound();

            gorev.Durum = GorevDurumu.Tamamlandi;
            await _baglam.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/durum")]
        public async Task<IActionResult> DurumGuncelle(int id, [FromBody] GorevDurumDto dto)
        {
            var kullaniciId = GetUserId();

            var gorev = await _baglam.Gorevler
                .Include(g => g.Proje)
                .FirstOrDefaultAsync(g => g.Id == id && g.Proje != null && g.Proje.KullaniciId == kullaniciId);

            if (gorev == null) return NotFound();

            gorev.Durum = dto.Durum;
            await _baglam.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> GorevGuncelle(int id, [FromBody] GorevGuncelleDto dto)
        {
            var kullaniciId = GetUserId();

            var gorev = await _baglam.Gorevler
                .Include(g => g.Proje)
                .FirstOrDefaultAsync(g => g.Id == id && g.Proje != null && g.Proje.KullaniciId == kullaniciId);

            if (gorev == null) return NotFound();

            gorev.Aciklama = dto.Aciklama;
            await _baglam.SaveChangesAsync();

            return NoContent();
        }
    }
}