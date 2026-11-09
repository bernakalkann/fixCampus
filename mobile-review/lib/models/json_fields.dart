/// API alanları eksik veya hatalıysa sessizce varsayılan değer üretmez.
class JsonFields {
  JsonFields._();

  static String string(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is! String) {
      throw FormatException('$key metin olmalıdır.');
    }
    return value;
  }

  static DateTime dateTime(Map<String, dynamic> json, String key) {
    final value = string(json, key);
    // UTC ya da açık saat dilimi zorunlu; cihazın yerel saat dilimine bağlı okuma yok.
    if (!RegExp(r'(Z|[+-]\d{2}:\d{2})$', caseSensitive: false).hasMatch(value)) {
      throw FormatException('$key saat dilimi içermelidir.');
    }
    final parsed = DateTime.tryParse(value);
    if (parsed == null) throw FormatException('$key geçerli bir tarih olmalıdır.');
    return parsed.toUtc();
  }

  static DateTime? nullableDateTime(Map<String, dynamic> json, String key) {
    if (json[key] == null) return null;
    return dateTime(json, key);
  }
}
