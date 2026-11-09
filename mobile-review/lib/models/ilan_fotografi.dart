import 'json_fields.dart';

/// Ortak API veri modeli: IlanFotografi. Kimlikler UUID metni olarak taşınır.
class IlanFotografi {
  const IlanFotografi({
    required this.id,
    required this.ilanId,
    required this.dosyaYolu,
    required this.yuklemeTarihi,
  });

  final String id;
  final String ilanId;
  final String dosyaYolu;
  final DateTime yuklemeTarihi;

  factory IlanFotografi.fromJson(Map<String, dynamic> json) => IlanFotografi(
        id: JsonFields.string(json, 'id'),
        ilanId: JsonFields.string(json, 'ilanId'),
        dosyaYolu: JsonFields.string(json, 'dosyaYolu'),
        yuklemeTarihi: JsonFields.dateTime(json, 'yuklemeTarihi'),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'ilanId': ilanId,
        'dosyaYolu': dosyaYolu,
        'yuklemeTarihi': yuklemeTarihi.toUtc().toIso8601String(),
      };
}
