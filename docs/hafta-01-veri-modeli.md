# Hafta 1 — Berna Kalkan: veri modeli

Plan belgesindeki ilk hafta veri modeli görevi için EF Core/Npgsql altyapısı,
altı varlık ve ilişkileri hazırlandı.

| Tablo | Ana alanlar | İlişkiler |
|---|---|---|
| Profiller | Id, AdSoyad, Rol, OlusturulmaTarihi | İlan, katılım ve şikayet sahibi |
| Kategoriler | Id, Ad, Tur | Bir kategorinin çok ilanı |
| Ilanlar | Id, SahipId, KategoriId, Baslik, Aciklama, Tur, Yer, BulusmaTarihi, Durum | Sahip ve kategoriye bağlı |
| Katilimlar | Id, IlanId, ProfilId, KatilimTarihi | İlan ve profile bağlı |
| IlanFotograflari | Id, IlanId, DosyaYolu, YuklemeTarihi | İlana bağlı, ilan başına en fazla bir |
| Sikayetler | Id, IlanId, BildirenId, Neden, Durum, OlusturulmaTarihi | İlan ve bildiren profile bağlı |

## Kararlar

- Kimlikler UUID. Profil kimliği ikinci haftada Supabase Auth kullanıcısının
  UUID değeriyle doldurulacak; uygulama parola saklamaz. Bu aşamada auth.users
  tablosuna doğrudan foreign key veya otomatik profil trigger'ı eklenmedi.
- Roller: Ogrenci / Yonetici; ilan türleri: Sosyal / Kayip / Buluntu;
  ilan durumları: Aktif / Cozuldu / Gizli; şikayet durumları: Bekliyor / Incelendi.
- DateTimeOffset alanları PostgreSQL timestamptz; uygulama UTC değer yazar.
- Sosyal ilanın buluşma tarihi zorunlu, kayıp/buluntu için boş.
- İlan/profil katılım çifti, ilan/bildiren şikayet çifti, kategori adı/türü
  ve fotoğrafın ilan kimliği benzersiz.
- İlan silinince katılım, fotoğraf kaydı ve şikayet silinir.
  Referans verilen profil/kategori silinemez. Storage dosyasının silinmesi
  ileride fotoğraf servisi tarafından yapılacak.
- Süresi geçen buluşmayı gizleme, yalnız sosyal ilana katılma,
  kategori/ilan türü eşleşmesi ve sahiplik/rol yetkileri sonraki API işlerinde
  denetlenecek. Yetkilendirme sonraki API çalışmalarında eklenecektir.

## Kapsam

Bu katkı Berna Kalkan'ın 1. hafta veri modeli görevidir. Model sınıfları
Models/Entities altında, tablo eşlemeleri ve ilişkiler Data/FixCampusDbContext.cs
içindedir. Npgsql EF Core paketi PostgreSQL eşlemesi için eklendi.

Veritabanı bağlantısı, migration uygulama, Auth ve CRUD bu katkıya dahil değildir.
Profil kimliği Supabase Auth kullanıcı kimliğiyle eşleşecek şekilde tasarlandı;
parola alanı yoktur. İlan başına tek fotoğraf kuralı benzersiz indeksle sağlanır.

Bu ortamda .NET SDK bulunmadığından derleme çalıştırılamadı.
