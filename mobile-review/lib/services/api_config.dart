/// Android emülatörü varsayılanı; gerçek cihazda API_BASE_URL değiştirilir.
class ApiConfig {
  ApiConfig({
    String baseUrl = const String.fromEnvironment(
      'API_BASE_URL',
      defaultValue: 'http://10.0.2.2:5168',
    ),
    this.timeout = const Duration(seconds: 10),
  }) : baseUri = _parseBaseUrl(baseUrl) {
    if (timeout <= Duration.zero) {
      throw ArgumentError.value(timeout, 'timeout', 'Pozitif olmalıdır.');
    }
  }

  final Uri baseUri;
  final Duration timeout;

  Uri endpoint(String path) => baseUri.resolve(path);

  static Uri _parseBaseUrl(String value) {
    final uri = Uri.tryParse(value.trim());
    if (uri == null ||
        !['http', 'https'].contains(uri.scheme) ||
        uri.host.isEmpty ||
        uri.userInfo.isNotEmpty ||
        uri.hasQuery ||
        uri.hasFragment) {
      throw ArgumentError(
        'API adresi geçerli bir HTTP/HTTPS adresi olmalıdır.',
      );
    }
    return uri.replace(
      path: uri.path.endsWith('/') ? uri.path : '${uri.path}/',
    );
  }
}
