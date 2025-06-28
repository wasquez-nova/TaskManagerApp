using Microsoft.AspNetCore.Mvc;
using TaskManagerApp.Modeller;
using TaskManagerApp.Veri;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TaskManagerApp.Modeller.Giris;
using System.Security.Claims;

namespace TaskManagerApp.Denetleyiciler
{
    [ApiController]
    [Route("api/[controller]")]
    public class KullaniciDenetleyicisi : ControllerBase
    {
        private readonly UygulamaDbBaglami _baglam;

        public KullaniciDenetleyicisi(UygulamaDbBaglami baglam)
        {
            _baglam = baglam;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Kullanici>>> TumKullanicilariGetir()
        {
            return await _baglam.Kullanicilar.ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Kullanici>> KullaniciGetir(int id)
        {
            var kullanici = await _baglam.Kullanicilar
                .Include(k => k.Projeler!)
                    .ThenInclude(p => p.Gorevler)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kullanici == null)
                return NotFound();

            return kullanici;
        }

        // Mevcut kayıt olma (herkes erişebilir)
        [HttpPost]
        public async Task<ActionResult<Kullanici>> KullaniciEkle(Kullanici yeniKullanici)
        {
            // Yeni kayıtlar default olarak Kullanici rolünde olsun
            yeniKullanici.Rol = "Kullanici";
            _baglam.Kullanicilar.Add(yeniKullanici);
            await _baglam.SaveChangesAsync();
            return CreatedAtAction(nameof(KullaniciGetir), new { id = yeniKullanici.Id }, yeniKullanici);
        }

        // Yeni: Sadece admin yeni kullanıcı yaratıp rol atayabilsin
        [HttpPost("admin-ekle")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Kullanici>> AdminKullaniciEkle(Kullanici yeniKullanici)
        {
            if (string.IsNullOrWhiteSpace(yeniKullanici.Rol))
                yeniKullanici.Rol = "Kullanici"; // Eğer admin rol belirtmezse default Kullanici

            _baglam.Kullanicilar.Add(yeniKullanici);
            await _baglam.SaveChangesAsync();
            return CreatedAtAction(nameof(KullaniciGetir), new { id = yeniKullanici.Id }, yeniKullanici);
        }

        [HttpPatch("sifre")]
        [Authorize]
        public async Task<IActionResult> SifreDegistir([FromBody] SifreDegistirDto dto)
        {
            var kullaniciIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(kullaniciIdStr) || !int.TryParse(kullaniciIdStr, out int kullaniciId))
                return Unauthorized();

            var kullanici = await _baglam.Kullanicilar.FindAsync(kullaniciId);
            if (kullanici == null)
                return NotFound();

            if (kullanici.Sifre != dto.EskiSifre)
                return BadRequest("Mevcut şifre hatalı.");

            kullanici.Sifre = dto.YeniSifre;
            await _baglam.SaveChangesAsync();

            return Ok("Şifre güncellendi.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> KullaniciGuncelle(int id, Kullanici guncellenen)
        {
            if (id != guncellenen.Id)
                return BadRequest();

            _baglam.Entry(guncellenen).State = EntityState.Modified;
            await _baglam.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id}/detay")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> KullaniciDetay(int id)
        {
            var kullanici = await _baglam.Kullanicilar
                .Include(k => k.Projeler!)
                    .ThenInclude(p => p.Gorevler!)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kullanici == null)
                return NotFound();

            var detay = new
            {
                kullanici.Id,
                kullanici.Ad,
                kullanici.Eposta,
                kullanici.Rol,
                Projeler = kullanici.Projeler?.Select(p => new
                {
                    p.Baslik,
                    p.Aciklama,
                    Gorevler = p.Gorevler?.Select(g => new
                    {
                        g.Id,
                        g.Aciklama,
                        g.Durum
                    })
                })
            };

            return Ok(detay);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> KullaniciSil(int id)
        {
            var kullanici = await _baglam.Kullanicilar
                .Include(k => k.Projeler!)
                    .ThenInclude(p => p.Gorevler!)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kullanici == null)
                return NotFound();

            foreach (var proje in kullanici.Projeler ?? Enumerable.Empty<Proje>())
            {
                if (proje.Gorevler != null)
                    _baglam.Gorevler.RemoveRange(proje.Gorevler);
            }

            if (kullanici.Projeler != null)
                _baglam.Projeler.RemoveRange(kullanici.Projeler);

            _baglam.Kullanicilar.Remove(kullanici);

            await _baglam.SaveChangesAsync();
            return NoContent();
        }
    }
}