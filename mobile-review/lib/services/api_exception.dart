enum ApiFailure { connection, timeout, http, invalidResponse }

/// Arayüz, sunucu ayrıntıları yerine bu anlaşılır mesajı gösterebilir.
class ApiException implements Exception {
  const ApiException(this.kind, this.message, {this.statusCode});

  final ApiFailure kind;
  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}
