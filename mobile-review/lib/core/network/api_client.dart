import 'package:dio/dio.dart';
import '../constants/app_constants.dart';

/// Ortak API İstemcisi İskeleti
/// ASP.NET Core Web API ile haberleşmeyi ve Auth token yönetimini sağlar.
class ApiClient {
  ApiClient({String? baseUrl, Dio? dio, String? Function()? tokenProvider})
    : _tokenProvider = tokenProvider,
      _dio =
          dio ??
          Dio(
            BaseOptions(
              baseUrl: baseUrl ?? AppConstants.defaultApiBaseUrl,
              connectTimeout: const Duration(seconds: 10),
              receiveTimeout: const Duration(seconds: 10),
              headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
              },
            ),
          ) {
    _initInterceptors();
  }

  final Dio _dio;
  String? _authToken;
  final String? Function()? _tokenProvider;

  Dio get dio => _dio;

  /// Oturum açıldığında JWT erişim belirtecini ayarlar
  void setAuthToken(String? token) {
    _authToken = token;
  }

  void _initInterceptors() {
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          // Eğer token mevcutsa Bearer Authorization header eklenir
          final tokenProvider = _tokenProvider;
          final token = tokenProvider != null ? tokenProvider() : _authToken;
          options.headers.removeWhere(
            (name, _) => name.toLowerCase() == 'authorization',
          );
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          return handler.next(options);
        },
        onError: (DioException error, handler) {
          // Ortak hata yakalama ve loglama iskeleti
          return handler.next(error);
        },
      ),
    );
  }
}
