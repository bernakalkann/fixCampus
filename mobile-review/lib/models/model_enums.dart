/// Değerler web modellerindeki metin alanlarıyla aynıdır.
enum KullaniciRolu {
  ogrenci('Ogrenci'),
  yonetici('Yonetici');

  const KullaniciRolu(this.apiValue);
  final String apiValue;

  static KullaniciRolu fromJson(String value) => values.firstWhere(
        (item) => item.apiValue == value,
        orElse: () => throw FormatException('Geçersiz KullaniciRolu: $value'),
      );
}

enum IlanTuru {
  sosyal('Sosyal'),
  kayip('Kayip'),
  buluntu('Buluntu');

  const IlanTuru(this.apiValue);
  final String apiValue;

  static IlanTuru fromJson(String value) => values.firstWhere(
        (item) => item.apiValue == value,
        orElse: () => throw FormatException('Geçersiz IlanTuru: $value'),
      );
}

enum IlanDurumu {
  aktif('Aktif'),
  cozuldu('Cozuldu'),
  gizli('Gizli');

  const IlanDurumu(this.apiValue);
  final String apiValue;

  static IlanDurumu fromJson(String value) => values.firstWhere(
        (item) => item.apiValue == value,
        orElse: () => throw FormatException('Geçersiz IlanDurumu: $value'),
      );
}

enum SikayetDurumu {
  bekliyor('Bekliyor'),
  incelendi('Incelendi');

  const SikayetDurumu(this.apiValue);
  final String apiValue;

  static SikayetDurumu fromJson(String value) => values.firstWhere(
        (item) => item.apiValue == value,
        orElse: () => throw FormatException('Geçersiz SikayetDurumu: $value'),
      );
}
