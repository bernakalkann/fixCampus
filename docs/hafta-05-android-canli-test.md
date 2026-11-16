# Android canlı kullanıcı testi — 17 Kasım 2026

**Ortam:** Pixel 7 Android emülatörü, Flutter Android debug APK, Mac'te çalışan yerel .NET API ve projenin Supabase ortamı. Akış iki öğrenci hesabıyla uygulama arayüzünden elle denendi.

| Akış | Gözlenen sonuç |
| --- | --- |
| Kayıt ve giriş | Öğrenci hesabı oluşturuldu ve uygulamaya giriş yapıldı. |
| İlan yayınlama | Kayıp ilanı kaydedildi. Sosyal buluşma oluşturuldu ve katılım testi için açıldı. |
| Buluşmaya katılma | İkinci hesap **Geliyorum**'a bastığında sayaç 0'dan 1'e çıktı. |
| Katılımı geri alma | İkinci hesap **Katılımı Geri Al**'a bastığında sayaç tekrar 0'a düştü. |

**Sonuç:** Canlı katılım ve geri alma senaryosu geçti (0 → 1 → 0).

## Açık kalan bulgular

- Kayıt formunda Ad Soyad ve Bölüm alanlarına Türkçe karakter girilemedi. Emülatör klavyesi ve uygulama giriş doğrulaması ayrı ayrı kontrol edilmeli.
- Uygulama ilk açıldığında ilanlar hemen görünmedi. Akışın kendiliğinden yüklenip yüklenmediği ve beklerken hangi durumun gösterildiği kontrol edilmeli.
- İlan detayında katılımcı sayısı görünüyor, katılan kişilerin listesi veya profil bilgisi açılamıyor.
- Sosyal ilan formunda geçerli bir tarih seçildikten sonra önceki kırmızı tarih uyarısı ekranda kalabiliyor. Tarih değişince uyarı güncellenmeli.
