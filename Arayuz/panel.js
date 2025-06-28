function toggleAyarlar() {
  const menu = document.getElementById("ayarlarMenu");
  menu.classList.toggle("gizle");
}

function sifreDegistirPaneliToggle() {
  document.getElementById("sifreDegistirAlani").classList.toggle("gizle");
}

function cikisYap() {
  localStorage.clear();
  alert("Çıkış yapıldı.");
  window.location.href = "index.html";
}

async function sifreDegistir() {
  const eski = document.getElementById("eskiSifre").value;
  const yeni = document.getElementById("yeniSifre").value;
  const token = localStorage.getItem("token");

  if (!eski || !yeni) {
    alert("Her iki şifre alanını da doldurun.");
    return;
  }

  try {
    const yanit = await fetch("http://localhost:5094/api/KullaniciDenetleyicisi/sifre", {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        "Authorization": "Bearer " + token
      },
      body: JSON.stringify({ eskiSifre: eski, yeniSifre: yeni })
    });

    if (yanit.ok) {
      alert("Şifre başarıyla değiştirildi!");
    } else {
      const hata = await yanit.text();
      alert("Hata: " + hata);
    }
  } catch (err) {
    alert("Bir hata oluştu: " + err.message);
  }
}

async function gorevEkle() {
  const token = localStorage.getItem("token");
  const kategori = document.getElementById("gorevBaslik").value.trim();
  const aciklama = document.getElementById("gorevAciklama").value.trim();

  if (!kategori || !aciklama) {
    alert("Lütfen kategori ve açıklama giriniz.");
    return;
  }

  try {
    const projelerRes = await fetch("http://localhost:5094/api/ProjeDenetleyicisi", {
      headers: { Authorization: "Bearer " + token }
    });
    const projeler = await projelerRes.json();
    let proje = projeler.find(p => p.baslik.toLowerCase() === kategori.toLowerCase());

    if (!proje) {
      const yeniProje = {
        baslik: kategori,
        aciklama: kategori + " kategorisine ait görevler",
        baslangicTarihi: new Date().toISOString(),
        bitisTarihi: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString()
      };
      const res = await fetch("http://localhost:5094/api/ProjeDenetleyicisi", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: "Bearer " + token
        },
        body: JSON.stringify(yeniProje)
      });
      proje = await res.json();
    }

    const yeniGorev = {
      baslik: kategori,
      aciklama: aciklama,
      durum: 0,
      projeId: proje.id
    };

    const yanit = await fetch("http://localhost:5094/api/GorevDenetleyicisi", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer " + token
      },
      body: JSON.stringify(yeniGorev)
    });

    if (yanit.ok) {
      alert("Görev eklendi!");
      yukleGorevler();
    } else {
      const hata = await yanit.text();
      alert("Görev eklenemedi: " + hata);
    }
  } catch (err) {
    alert("Hata: " + err.message);
  }
}

async function yukleGorevler() {
  const token = localStorage.getItem("token");

  try {
    const yanit = await fetch("http://localhost:5094/api/GorevDenetleyicisi", {
      headers: { Authorization: "Bearer " + token }
    });
    if (!yanit.ok) throw new Error("Görevler alınamadı.");

    const gorevler = await yanit.json();

    const bekleyenGruplar = {};
    const aktifGruplar = {};
    const tamamlananGruplar = {};
    const projeAdlari = {};

    gorevler.forEach(g => {
      const projeId = g.proje?.id;
      const projeAdi = g.proje?.baslik?.trim();
      if (!projeId || !projeAdi) return;

      if (!projeAdlari[projeId]) projeAdlari[projeId] = projeAdi;

      const hedef = g.durum === 0 ? bekleyenGruplar : g.durum === 1 ? aktifGruplar : tamamlananGruplar;
      if (!hedef[projeId]) hedef[projeId] = [];
      hedef[projeId].push({ id: g.id, aciklama: g.aciklama, baslik: g.baslik });
    });

    const todoDiv = document.getElementById("todo");
    todoDiv.innerHTML = "<h2>Bekleyen Görevler</h2>";
    for (const projeId in bekleyenGruplar) {
      const grupDiv = document.createElement("div");
      grupDiv.className = "gorevKart";
      grupDiv.innerHTML = `<strong>${projeAdlari[projeId]}</strong><ul>${bekleyenGruplar[projeId].map(g => `<li>${g.aciklama} <button onclick="gorevSil(${g.id})" class="sil-buton" title="Sil">❌</button> <button onclick="durumGuncelle(${g.id}, 1)" class="aktif-buton" title="Aktif Yap">🟡</button> <button onclick="gorevDuzenle(${g.id}, '${g.aciklama}', '${g.baslik}')" class="duzenle-buton" title="Düzenle">✏️</button>`).join("")}</ul>`;
      todoDiv.appendChild(grupDiv);
    }

    const doingDiv = document.getElementById("doing");
    doingDiv.innerHTML = "<h2>Aktif Görevler</h2>";
    for (const projeId in aktifGruplar) {
      const grupDiv = document.createElement("div");
      grupDiv.className = "gorevKart";
      grupDiv.innerHTML = `<strong>${projeAdlari[projeId]}</strong><ul>${aktifGruplar[projeId].map(g => `<li>${g.aciklama} <button onclick="gorevSil(${g.id})" class="sil-buton">❌</button> <button onclick="durumGuncelle(${g.id}, 2)" class="tamamla-buton">✔️</button>`).join("")}</ul>`;
      doingDiv.appendChild(grupDiv);
    }

    const doneDiv = document.getElementById("done");
    doneDiv.innerHTML = "<h2>Tamamlanan Görevler</h2>";
    for (const projeId in tamamlananGruplar) {
      const grupDiv = document.createElement("div");
      grupDiv.className = "gorevKart tamamlanan";
      grupDiv.innerHTML = `<strong>${projeAdlari[projeId]}</strong><ul>${tamamlananGruplar[projeId].map(g => `<li>${g.aciklama}</li>`).join("")}</ul>`;
      doneDiv.appendChild(grupDiv);
    }
  } catch (err) {
    alert("Hata: " + err.message);
  }
}

async function gorevSil(gorevId) {
  const token = localStorage.getItem("token");

  if (!confirm("Bu görevi silmek istediğinize emin misiniz?")) return;

  try {
    const yanit = await fetch(`http://localhost:5094/api/GorevDenetleyicisi/${gorevId}`, {
      method: "DELETE",
      headers: {
        "Authorization": "Bearer " + token
      }
    });

    if (yanit.ok) {
      alert("Görev silindi!");
      yukleGorevler();
    } else {
      const hata = await yanit.text();
      alert("Görev silinemedi: " + hata);
    }
  } catch (err) {
    alert("Hata: " + err.message);
  }
}

async function durumGuncelle(gorevId, yeniDurum) {
  const token = localStorage.getItem("token");
  try {
    const yanit = await fetch(`http://localhost:5094/api/GorevDenetleyicisi/${gorevId}/durum`, {
      method: "PATCH",
      headers: {
        "Authorization": "Bearer " + token,
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ Durum: yeniDurum })
    });

    if (yanit.ok) yukleGorevler();
    else {
      const hata = await yanit.text();
      alert("Durum güncellenemedi: " + hata);
    }
  } catch (err) {
    alert("Durum hatası: " + err.message);
  }
}
async function gorevDuzenle(id, eskiAciklama) {
  const yeniAciklama = prompt("Yeni açıklamayı girin:", eskiAciklama);
  if (yeniAciklama === null) return;

  const token = localStorage.getItem("token");

  try {
    const yanit = await fetch(`http://localhost:5094/api/GorevDenetleyicisi/${id}`, {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        "Authorization": "Bearer " + token
      },
      body: JSON.stringify({
        aciklama: yeniAciklama.trim()
      })
    });

    if (yanit.ok) {
      alert("Görev güncellendi.");
      yukleGorevler();
    } else {
      const hata = await yanit.text();
      alert("Görev güncellenemedi: " + hata);
    }
  } catch (err) {
    alert("Hata: " + err.message);
  }
}

async function kullanicilariGetir() {
  const token = localStorage.getItem("token");

  try {
    const yanit = await fetch("http://localhost:5094/api/KullaniciDenetleyicisi", {
      headers: {
        Authorization: "Bearer " + token
      }
    });

    if (!yanit.ok) throw new Error("Kullanıcılar alınamadı.");

    const kullanicilar = await yanit.json();
    const div = document.getElementById("kullanicilarListesi");
    div.innerHTML = `<h3>Kullanıcılar</h3><ul>
  ${kullanicilar.map(k => 
    `<li>
      ${k.ad} - ${k.eposta} - ${k.rol}
      <button onclick="kullaniciDetay(${k.id})" class="detay-buton">Detay</button>
    </li>`).join("")}
</ul>
<div id="kullaniciDetayAlani"></div>`;
  } catch (err) {
    alert("Hata: " + err.message);
  }
}

async function kullaniciDetay(kullaniciId) {
  const token = localStorage.getItem("token");

  try {
    const yanit = await fetch(`http://localhost:5094/api/KullaniciDenetleyicisi/${kullaniciId}`, {
      headers: {
        Authorization: "Bearer " + token
      }
    });

    if (!yanit.ok) throw new Error("Kullanıcı detayı alınamadı.");

    const kullanici = await yanit.json();
    const div = document.getElementById("kullanicilarListesi");

    const adSoyad = `${kullanici.ad} (${kullanici.eposta})`;
    const detayDiv = document.createElement("div");
    detayDiv.innerHTML = `<h4>${adSoyad}</h4>`;

    const projeler = {};

    for (const proje of kullanici.projeler) {
      if (!projeler[proje.baslik]) projeler[proje.baslik] = [];

      for (const gorev of proje.gorevler) {
        const durumMetni =
          gorev.durum === 0 ? "Bekliyor" :
          gorev.durum === 1 ? "Aktif" : "Tamamlandı";

        projeler[proje.baslik].push(`• ${gorev.aciklama} — <em>${durumMetni}</em>`);
      }
    }

    for (const baslik in projeler) {
      detayDiv.innerHTML += `<p><strong>${baslik}</strong><br/>${projeler[baslik].join("<br/>")}</p>`;
    }

    const silBtn = document.createElement("button");
    silBtn.innerText = "Kullanıcıyı Sil";
    silBtn.style.backgroundColor = "crimson";
    silBtn.style.color = "white";
    silBtn.onclick = async () => {
      if (confirm("Bu kullanıcı ve tüm görevleri silinecek. Emin misiniz?")) {
        const silYaniti = await fetch(`http://localhost:5094/api/KullaniciDenetleyicisi/${kullaniciId}`, {
          method: "DELETE",
          headers: {
            Authorization: "Bearer " + token
          }
        });

        if (silYaniti.ok) {
          alert("Kullanıcı silindi.");
          kullanicilariGetir();
        } else {
          alert("Kullanıcı silinemedi.");
        }
      }
    };

    detayDiv.appendChild(silBtn);
    div.appendChild(detayDiv);

  } catch (err) {
    alert("Hata: " + err.message);
  }
}
async function adminKullaniciEkle() {
  const ad = prompt("Yeni kullanıcının adını girin:");
  if (!ad) return;

  const eposta = prompt("Yeni kullanıcının epostasını girin:");
  if (!eposta) return;

  const sifre = prompt("Yeni kullanıcının şifresini girin:");
  if (!sifre) return;

  const rol = prompt("Rol girin (Admin veya Kullanici):");
  if (!rol || (rol !== "Admin" && rol !== "Kullanici")) {
    alert("Geçerli bir rol giriniz: Admin veya Kullanici");
    return;
  }

  const token = localStorage.getItem("token");

  try {
    const yanit = await fetch("http://localhost:5094/api/KullaniciDenetleyicisi/admin-ekle", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": "Bearer " + token
      },
      body: JSON.stringify({
        ad: ad.trim(),
        eposta: eposta.trim(),
        sifre: sifre.trim(),
        rol: rol
      })
    });

    if (yanit.ok) {
      alert("Kullanıcı başarıyla oluşturuldu.");
      kullanicilariGetir();
    } else {
      const hata = await yanit.text();
      alert("Kullanıcı oluşturulamadı: " + hata);
    }
  } catch (err) {
    alert("Hata: " + err.message);
  }
}
document.addEventListener("DOMContentLoaded", () => {
  yukleGorevler();
  const rol = localStorage.getItem("rol");
  if (rol === "Admin") {
    document.getElementById("kullanicilarBtn").style.display = "block";
  }
});