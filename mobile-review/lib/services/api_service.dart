import 'package:dio/dio.dart';

import 'api_config.dart';
import 'api_exception.dart';
import 'api_responses.dart';

export 'api_config.dart';
export 'api_exception.dart';
export 'api_responses.dart';

/// Birinci hafta salt okunur API bağlantısı; Auth ve CRUD sonraki görevlerdir.
class ApiService {
  ApiService({ApiConfig? config}) : config = config ?? ApiConfig() {
    _client = Dio(
      BaseOptions(
        connectTimeout: this.config.timeout,
        receiveTimeout: this.config.timeout,
        sendTimeout: this.config.timeout,
        headers: {'Accept': 'application/json'},
        responseType: ResponseType.json,
        followRedirects: false,
        validateStatus: (status) => status == 200,
      ),
    );
  }

  final ApiConfig config;
  late final Dio _client;

  Future<ApiStatus> getStatus() => _get('api/durum', ApiStatus.fromJson);

  Future<SampleListingsResponse> getSampleListings() =>
      _get('api/ornek/ilanlar', SampleListingsResponse.fromJson);

  Future<DatabaseDemoListing> getDatabaseDemoListing() =>
      _get('api/veritabani/ornek-ilan', DatabaseDemoListing.fromJson);

  Future<T> _get<T>(
    String path,
    T Function(Map<String, dynamic>) decode,
  ) async {
    try {
      final response = await _client.get<Object>(
        config.endpoint(path).toString(),
      );
      final data = response.data;
      if (data is! Map<String, dynamic>) {
        throw const FormatException('API bir JSON nesnesi döndürmelidir.');
      }
      return decode(data);
    } on FormatException {
      throw const ApiException(
        ApiFailure.invalidResponse,
        'API yanıtı beklenen biçimde değil.',
      );
    } on DioException catch (error) {
      if (error.type == DioExceptionType.connectionTimeout ||
          error.type == DioExceptionType.sendTimeout ||
          error.type == DioExceptionType.receiveTimeout) {
        throw const ApiException(
          ApiFailure.timeout,
          'API yanıtı zamanında gelmedi. Tekrar deneyin.',
        );
      }
      if (error.type == DioExceptionType.badResponse) {
        throw ApiException(
          ApiFailure.http,
          'API isteği başarısız oldu (HTTP ${error.response?.statusCode}).',
          statusCode: error.response?.statusCode,
        );
      }
      if (error.error is FormatException) {
        throw const ApiException(
          ApiFailure.invalidResponse,
          'API yanıtı geçerli JSON değil.',
        );
      }
      throw const ApiException(
        ApiFailure.connection,
        'API’ye bağlanılamadı. API adresini ve bağlantıyı kontrol edin.',
      );
    }
  }

  /// Servisi oluşturan ekran/uygulama artık kullanmayacağı zaman çağırır.
  void close() => _client.close(force: true);
}
