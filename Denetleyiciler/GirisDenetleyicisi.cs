using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagerApp.Modeller;
using TaskManagerApp.Modeller.Giris;
using TaskManagerApp.Veri;

namespace TaskManagerApp.Denetleyiciler
{
    [ApiController]
    [Route("api/[controller]")]
    public class GirisDenetleyicisi : ControllerBase
    {
        private readonly UygulamaDbBaglami _baglam;
        private readonly IConfiguration _konfig;

        public GirisDenetleyicisi(UygulamaDbBaglami baglam, IConfiguration konfig)
        {
            _baglam = baglam;
            _konfig = konfig;
        }

        [HttpPost]
        public IActionResult GirisYap([FromBody] GirisIsteği istek)
        {
            var kullanici = _baglam.Kullanicilar.FirstOrDefault(k =>
                k.Eposta == istek.Eposta && k.Sifre == istek.Sifre);

            if (kullanici == null)
                return Unauthorized("Hatalı e-posta veya şifre.");

            var jwtAyar = _konfig.GetSection("JwtAyarlar");
            var gizliAnahtar = jwtAyar["GizliAnahtar"];

            if (string.IsNullOrWhiteSpace(gizliAnahtar))
                return StatusCode(500, "Sunucu hatası: GizliAnahtar appsettings.json içinde boş veya tanımsız.");

            var anahtar = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(gizliAnahtar));
            var kimlik = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new Claim(ClaimTypes.Name, kullanici.Ad ?? ""),
                new Claim(ClaimTypes.Email, kullanici.Eposta ?? ""),
                new Claim(ClaimTypes.Role, kullanici.Rol ?? "Kullanici")
            };

            var token = new JwtSecurityToken(
                issuer: jwtAyar["Yayimci"],
                audience: jwtAyar["Hedef"],
                claims: kimlik,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtAyar["GecerlilikSuresiDakika"] ?? "60")),
                signingCredentials: new SigningCredentials(anahtar, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new GirisYaniti
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Rol = kullanici.Rol,
                Ad = kullanici.Ad
            });
        }
    }
}
