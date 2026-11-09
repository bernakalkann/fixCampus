import '../models/models.dart';

/// Oluşturma ve düzenleme formlarının ortak alan kuralları.
/// Güncel ilan formu/API sözleşmesindeki metin ve kategori kuralları.
class ListingValidator {
  const ListingValidator();

  String? title(String? value) => _text(
    value,
    min: 2,
    max: 150,
    shortMessage: 'Başlık en az 2 karakter olmalıdır.',
  );
  String? description(String? value) => _text(value, max: 3000);
  String? location(String? value) => _text(value, max: 200);

  bool acceptsCategory(IlanTuru type, Kategori category) =>
      category.tur == type;

  String? category(
    String? id, {
    required IlanTuru type,
    required Iterable<Kategori> categories,
  }) {
    if (id == null || id.trim().isEmpty) return 'Kategori seçin.';
    for (final item in categories) {
      if (item.id == id && acceptsCategory(type, item)) return null;
    }
    return 'İlan türüne uygun bir kategori seçin.';
  }

  String? meetingDate(IlanTuru type, DateTime? date, {DateTime? now}) =>
      type == IlanTuru.sosyal &&
          (date == null || !date.isAfter(now ?? DateTime.now()))
      ? 'Sosyal ilan için gelecekte bir buluşma zamanı seçin.'
      : null;

  String? _text(
    String? value, {
    int min = 1,
    required int max,
    String? shortMessage,
  }) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) return 'Bu alan zorunludur.';
    if (text.length < min) return shortMessage;
    if (text.length > max) return 'En fazla $max karakter girebilirsiniz.';
    return null;
  }
}
