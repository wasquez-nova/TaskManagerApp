// Kullanıcı Girişi
async function girisYap() {
  const eposta = document.getElementById("girisEposta").value;
  const sifre = document.getElementById("girisSifre").value;

  if (!eposta || !sifre) {
    alert("Lütfen e-posta ve şifre giriniz.");
    return;
  }

  const girisVerisi = {
    eposta: eposta,
    sifre: sifre
  };

  try {
    const yanit = await fetch("http://localhost:5094/api/GirisDenetleyicisi", {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(girisVerisi)
    });

    if (yanit.ok) {
      const veri = await yanit.json();
      localStorage.setItem("token", veri.token);
      localStorage.setItem("rol", veri.rol);
      localStorage.setItem("ad", veri.ad || ""); // ad varsa kaydet
      alert("Hoş geldin, " + (veri.ad || eposta) + "!");
      window.location.href = "panel.html";
    } else {
      const hata = await yanit.text();
      alert("Giriş başarısız: " + hata);
    }
  } catch (err) {
    alert("Hata oluştu: " + err.message);
  }
}

// Kullanıcı Kaydı
async function kayitOl() {
  const ad = document.getElementById("kayitAd").value;
  const eposta = document.getElementById("kayitEposta").value;
  const sifre = document.getElementById("kayitSifre").value;

  if (!ad || !eposta || !sifre) {
    alert("Lütfen tüm alanları doldurun.");
    return;
  }

  const kullanici = {
    ad: ad,
    eposta: eposta,
    sifre: sifre,
    rol: "Kullanici"
  };

  try {
    const yanit = await fetch("http://localhost:5094/api/KullaniciDenetleyicisi", {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(kullanici)
    });

    if (yanit.ok) {
      alert("Kayıt başarılı! Artık giriş yapabilirsiniz.");
    } else {
      const hata = await yanit.text();
      alert("Kayıt başarısız: " + hata);
    }
  } catch (err) {
    alert("Hata oluştu: " + err.message);
  }
}
