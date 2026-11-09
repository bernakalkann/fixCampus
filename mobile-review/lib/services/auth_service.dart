import 'dart:convert';
import 'package:dio/dio.dart';
import '../core/network/web_api_auth_client.dart';
import 'package:supabase/supabase.dart';
import '../core/network/api_client.dart';
import '../models/models.dart';

/// Auth SDK oturumu saklar ve yeniler; profil ve rol yalnızca API'den okunur.
abstract class AuthService {
  bool get hasSession;
  String? get email;
  Stream<void> get changes;
  Future<void> signIn(String email, String password);
  Future<void> signUp(String name, String email, String password);
  Future<void> signOut();
  Future<Profil> loadProfile();
  Future<Profil> saveProfile(
    String name, {
    String? bolum,
    String? sinif,
  });
}

class ApiAuthService implements AuthService {
  ApiAuthService(this.client, this.api, this.authApi);
  final WebApiAuthClient authApi;
  final SupabaseClient client;
  final ApiClient api;

  @override
  bool get hasSession => client.auth.currentSession != null;
  @override
  String? get email => client.auth.currentUser?.email;
  @override
  Stream<void> get changes => client.auth.onAuthStateChange.map((_) {});
  @override
  Future<void> signIn(String email, String password) async {
    final result = await authApi.authenticate('giris', {
      'email': email.trim(),
      'password': password,
    });
    await client.auth.recoverSession(jsonEncode(result));
  }

  @override
  Future<void> signUp(String name, String email, String password) async {
    final result = await authApi.authenticate('kayit', {
      'adSoyad': name.trim(),
      'email': email.trim(),
      'password': password,
    });
    if (result['access_token'] is String) {
      await client.auth.recoverSession(jsonEncode(result));
    }
  }

  @override
  // SDK yerel oturumu temizler; HTTP çıkışı WebApiAuthClient ile /api/auth/cikis olur.
  Future<void> signOut() => client.auth.signOut(scope: SignOutScope.local);
  @override
  Future<Profil> loadProfile() async {
    try {
      final response = await api.dio.get<Map<String, dynamic>>('/api/profil');
      return Profil.fromJson(response.data!);
    } on DioException catch (e) {
      // E-posta onayından sonraki ilk girişte profil henüz oluşmamış olabilir.
      final metadata = client.auth.currentUser?.userMetadata;
      final name = metadata?['ad_soyad'] ?? metadata?['adSoyad'];
      final bolum = metadata?['bolum'] as String?;
      final sinif = metadata?['sinif'] as String?;
      if (e.response?.statusCode == 404 &&
          name is String &&
          name.trim().length >= 2 &&
          name.trim().length <= 100) {
        return saveProfile(name.trim(), bolum: bolum, sinif: sinif);
      }
      rethrow;
    }
  }

  @override
  Future<Profil> saveProfile(
    String name, {
    String? bolum,
    String? sinif,
  }) async {
    final body = <String, dynamic>{
      'adSoyad': name,
    };
    if (bolum != null && bolum.trim().isNotEmpty) {
      body['bolum'] = bolum.trim();
    }
    if (sinif != null && sinif.trim().isNotEmpty) {
      body['sinif'] = sinif.trim();
    }
    final response = await api.dio.put<Map<String, dynamic>>(
      '/api/profil',
      data: body,
    );
    return Profil.fromJson(response.data!);
  }
}
