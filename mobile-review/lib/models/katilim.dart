import 'json_fields.dart';

/// Ortak API veri modeli: Katilim. Kimlikler UUID metni olarak taşınır.
class Katilim {
  const Katilim({
    required this.id,
    required this.ilanId,
    required this.profilId,
    required this.katilimTarihi,
  });

  final String id;
  final String ilanId;
  final String profilId;
  final DateTime katilimTarihi;

  factory Katilim.fromJson(Map<String, dynamic> json) => Katilim(
        id: JsonFields.string(json, 'id'),
        ilanId: JsonFields.string(json, 'ilanId'),
        profilId: JsonFields.string(json, 'profilId'),
        katilimTarihi: JsonFields.dateTime(json, 'katilimTarihi'),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'ilanId': ilanId,
        'profilId': profilId,
        'katilimTarihi': katilimTarihi.toUtc().toIso8601String(),
      };
}
