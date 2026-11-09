import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:supabase/supabase.dart';

/// Supabase SDK yalnızca oturumu saklar/yeniler. Auth ağı bizim Web API'mizdir.
/// Otomatik yenileme ve SDK çıkışı da bu istemciden geçer; doğrudan Auth
/// sunucusuna geri dönüş yolu yoktur. Storage gibi diğer SDK servisleri etkilenmez.
class WebApiAuthClient extends http.BaseClient {
  WebApiAuthClient({
    required String apiBaseUrl,
    required String supabaseUrl,
    http.Client? inner,
    this.onAuthRequest,
  }) : _apiBase = Uri.parse(apiBaseUrl),
       _supabaseBase = Uri.parse(supabaseUrl),
       _inner = inner ?? http.Client();

  final Uri _apiBase;
  final Uri _supabaseBase;
  final http.Client _inner;

  /// Test kanıtı için yalnızca uç adı; parola/token/boyutlu gövde kaydedilmez.
  final void Function(String path)? onAuthRequest;

  Uri _endpoint(String action) => _apiBase.replace(
    path: '${_apiBase.path.replaceFirst(RegExp(r'/+$'), '')}/api/auth/$action',
    query: null,
    fragment: null,
  );

  Future<http.Response> _post(
    String action,
    Map<String, dynamic> body, {
    String? authorization,
  }) async {
    final uri = _endpoint(action);
    onAuthRequest?.call(uri.path);
    final request = http.Request('POST', uri)
      ..headers.addAll({
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      })
      ..body = jsonEncode(body);
    if (authorization != null) request.headers['Authorization'] = authorization;
    return http.Response.fromStream(
      await _inner.send(request).timeout(const Duration(seconds: 20)),
    ).timeout(const Duration(seconds: 20));
  }

  Map<String, dynamic> _json(http.Response response) {
    if (response.bodyBytes.isEmpty) return {};
    final value = jsonDecode(utf8.decode(response.bodyBytes));
    if (value is! Map<String, dynamic>) {
      throw const FormatException('API yanıtı geçersiz.');
    }
    return value;
  }

  /// Kayıt ve girişte uygulama doğrudan kendi DTO/uçlarını kullanır.
  Future<Map<String, dynamic>> authenticate(
    String action,
    Map<String, dynamic> body,
  ) async {
    if (action != 'giris' && action != 'kayit') {
      throw ArgumentError('Desteklenmeyen Auth işlemi.');
    }
    final response = await _post(action, body);
    final data = _json(response);
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw AuthException(
        data['message'] as String? ?? 'Kimlik doğrulama tamamlanamadı.',
        statusCode: '${response.statusCode}',
        code: data['code'] as String?,
      );
    }
    return data;
  }

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final uri = request.url;
    if (uri.origin != _supabaseBase.origin ||
        !uri.path.startsWith('/auth/v1/')) {
      return _inner.send(request);
    }
    // SDK'nın desteklenmeyen Auth işlemlerini Supabase'e doğrudan göndermeyiz.
    final path = uri.path.substring('/auth/v1/'.length);
    final bytes = await request.finalize().toBytes();
    final body = bytes.isEmpty
        ? <String, dynamic>{}
        : jsonDecode(utf8.decode(bytes)) as Map<String, dynamic>;
    final String action;
    final Map<String, dynamic> payload;
    if (request.method == 'POST' &&
        path == 'token' &&
        uri.queryParameters['grant_type'] == 'refresh_token') {
      action = 'yenile';
      payload = {'refreshToken': body['refresh_token']};
    } else if (request.method == 'POST' &&
        path == 'logout' &&
        (uri.queryParameters['scope'] ?? 'local') == 'local') {
      action = 'cikis';
      payload = {};
    } else {
      throw UnsupportedError(
        'Auth işlemi Web API üzerinden tanımlanmalıdır: $path',
      );
    }
    final response = await _post(
      action,
      payload,
      authorization:
          request.headers['Authorization'] ?? request.headers['authorization'],
    );
    var responseBytes = response.bodyBytes;
    // SDK yenileme/çıkış hatalarını kendi hata modeliyle işler. API'nin
    // kodunu koruyarak kalıcı hata ile geçici 429/5xx hatasını ayırmasını sağlarız.
    if (response.statusCode >= 400) {
      Map<String, dynamic> error;
      try {
        error = _json(response);
      } catch (_) {
        error = {};
      }
      responseBytes = utf8.encode(
        jsonEncode({
          'msg': error['message'] ?? 'Kimlik doğrulama servisine ulaşılamadı.',
          'error_code': error['code'] ?? 'auth_request_failed',
        }),
      );
    }
    return http.StreamedResponse(
      Stream.value(responseBytes),
      response.statusCode,
      headers: {
        'content-type': 'application/json',
        if (response.headers['retry-after'] != null)
          'retry-after': response.headers['retry-after']!,
      },
      request: request,
    );
  }

  @override
  void close() => _inner.close();
}
