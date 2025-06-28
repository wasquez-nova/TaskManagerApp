using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskManagerApp.Veri;
using TaskManagerApp.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("GenelCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Veritabanı bağlantısını yapılandır (SQLite)
builder.Services.AddDbContext<UygulamaDbBaglami>(secenekler =>
    secenekler.UseSqlite("Data Source=gorevveritabani.db"));

// Controller'ları aktif et
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    );

// JWT Ayarlarını al
var jwtAyarlar = builder.Configuration.GetSection("JwtAyarlar");
var gizliAnahtar = jwtAyarlar["GizliAnahtar"];

if (string.IsNullOrEmpty(gizliAnahtar))
    throw new Exception("GizliAnahtar appsettings.json içinde tanımlı değil.");

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtAyarlar["Yayimci"],
        ValidAudience = jwtAyarlar["Hedef"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(gizliAnahtar))
    };
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("GenelCors");
app.UseMiddleware<HataYakalaMiddleware>();

// JWT Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Sabit admin kullanıcıyı ekle (eğer yoksa)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UygulamaDbBaglami>();

    if (!db.Kullanicilar.Any(k => k.Eposta == "admin@admin.com"))
    {
        db.Kullanicilar.Add(new TaskManagerApp.Modeller.Kullanici
        {
            Ad = "Sistem Admin",
            Eposta = "admin@admin.com",
            Sifre = "12345",
            Rol = "Admin"
        });
        db.SaveChanges();
    }
}

app.Run();