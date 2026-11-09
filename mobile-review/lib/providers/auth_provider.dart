import 'dart:async';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import '../models/models.dart';
import '../services/auth_service.dart';
import '../services/local_profile_storage.dart';

/// Kimlik Doğrulama, Kullanıcı Oturum ve Rol Yönetimi Sağlayıcısı
class AuthProvider extends ChangeNotifier {
  AuthProvider({
    AuthService? service,
    LocalProfileStorage? storage,
  })  : _service = service,
        _storage = storage ?? const LocalProfileStorage() {
    _subscription = service?.changes.listen(
      (_) {
        if (_isLoading) return; // İşlemin son durumunu tek yerde uygula.
        if (!service.hasSession) {
          _currentUser = null;
          _cachedBolum = null;
          _cachedSinif = null;
          _message = null;
          _hasSessionError = false;
          notifyListeners();
        } else {
          unawaited(initialize());
        }
      },
      onError: (Object error) {
        _hasSessionError = true;
        _message =
            'Oturum yenilenemedi. Bağlantınızı kontrol edip tekrar deneyin.';
        _notify();
      },
    );
  }

  final AuthService? _service;
  final LocalProfileStorage _storage;
  StreamSubscription<void>? _subscription;
  Profil? _currentUser;
  bool _isLoading = false;
  bool _disposed = false;
  String? _message;
  bool _hasSessionError = false;

  // Aktif oturum için hafızada tutulan bölüm ve sınıf
  String? _cachedBolum;
  String? _cachedSinif;

  bool get hasSessionError => _hasSessionError;
  Profil? get currentUser => _currentUser;
  bool get isLoggedIn => _service?.hasSession ?? false;
  bool get isLoading => _isLoading;
  bool get isAdmin =>
      isLoggedIn &&
      !_hasSessionError &&
      _currentUser?.rol == KullaniciRolu.yonetici;
  bool get isStudent => !isAdmin;

  String? get email => _service?.email;
  String? get currentEmail => _service?.email;
  String? get message => _message;
  String? get errorMessage => _message;
  bool get isConfigured => _service != null;

  void clearError() {
    _message = null;
    _notify();
  }

  void _notify() {
    if (!_disposed) notifyListeners();
  }

  Future<bool> _run(
    Future<void> Function(AuthService) action, {
    bool sessionOperation = true,
  }) async {
    var succeeded = false;
    if (_isLoading) return false;
    final service = _service;
    _message = null;
    if (sessionOperation) _hasSessionError = false;
    if (service == null) {
      _message =
          'Supabase bağlantı ayarları eksik. Kurulum belgesini kontrol edin.';
      _notify();
      return false;
    }
    _isLoading = true;
    _notify();
    try {
      await action(service);
      succeeded = true;
      return true;
    } on AuthException catch (e) {
      _message = e.code == 'invalid_credentials'
          ? 'E-posta veya parola hatalı.'
          : 'Kimlik doğrulama tamamlanamadı. Bilgilerinizi ve e-posta onayını kontrol edin.';
    } on DioException catch (e) {
      if (e.response?.statusCode == 401 || e.response?.statusCode == 403) {
        _hasSessionError = true;
      }
      _message = e.response?.statusCode == 401
          ? 'Oturum doğrulanamadı. Çıkış yapıp yeniden giriş yapın.'
          : 'Profil API’sine ulaşılamadı. Bağlantıyı kontrol edip tekrar deneyin.';
    } catch (_) {
      _message =
          'İşlem tamamlanamadı. Bağlantınızı kontrol edip tekrar deneyin.';
    } finally {
      if (sessionOperation) _hasSessionError = !succeeded;
      if (!service.hasSession) {
        _currentUser = null;
        _cachedBolum = null;
        _cachedSinif = null;
        _hasSessionError = false;
      }
      _isLoading = false;
      _notify();
    }
    return false;
  }

  Future<bool> initialize() => _run((s) async {
        final profile = s.hasSession ? await s.loadProfile() : null;
        if (profile != null) {
          final userId = profile.id;
          final savedBolum = await _storage.getBolum(userId);
          final savedSinif = await _storage.getSinif(userId);

          // GET'ten gelen sunucu değerleri öncelikli kullanılır
          final serverBolum =
              (profile.bolum != null && profile.bolum!.trim().isNotEmpty)
                  ? profile.bolum!.trim()
                  : null;
          final serverSinif =
              (profile.sinif != null && profile.sinif!.trim().isNotEmpty)
                  ? profile.sinif!.trim()
                  : null;

          final finalBolum = serverBolum ??
              savedBolum ??
              _cachedBolum ??
              (profile.rol == KullaniciRolu.yonetici
                  ? 'Rektörlük / Yönetim'
                  : 'Bilgisayar Mühendisliği');

          final finalSinif = serverSinif ??
              savedSinif ??
              _cachedSinif ??
              (profile.rol == KullaniciRolu.yonetici ? '-' : '4. Sınıf');

          if (savedBolum != finalBolum || savedSinif != finalSinif) {
            await _storage.saveAcademicInfo(
              userId,
              bolum: finalBolum,
              sinif: finalSinif,
            );
          }

          _cachedBolum = finalBolum;
          _cachedSinif = finalSinif;

          _currentUser = profile.copyWith(
            bolum: finalBolum,
            sinif: finalSinif,
          );
        } else {
          _currentUser = null;
          _cachedBolum = null;
          _cachedSinif = null;
        }
      });

  Future<bool> login({
    required String email,
    required String password,
    bool asAdmin = false,
  }) =>
      _run((s) async {
        // Yeni oturum öncesi önceki hesaptan kalan hafızayı temizle
        _currentUser = null;
        _cachedBolum = null;
        _cachedSinif = null;

        await s.signIn(email.trim(), password);
        final profile = await s.loadProfile();
        final userId = profile.id;

        final savedBolum = await _storage.getBolum(userId);
        final savedSinif = await _storage.getSinif(userId);

        // GET'ten gelen sunucu değerleri öncelikli kullanılır
        final serverBolum =
            (profile.bolum != null && profile.bolum!.trim().isNotEmpty)
                ? profile.bolum!.trim()
                : null;
        final serverSinif =
            (profile.sinif != null && profile.sinif!.trim().isNotEmpty)
                ? profile.sinif!.trim()
                : null;

        final finalBolum = serverBolum ??
            savedBolum ??
            (asAdmin ? 'Rektörlük / Yönetim' : 'Bilgisayar Mühendisliği');
        final finalSinif = serverSinif ??
            savedSinif ??
            (asAdmin ? '-' : '4. Sınıf');

        await _storage.saveAcademicInfo(
          userId,
          bolum: finalBolum,
          sinif: finalSinif,
        );

        _cachedBolum = finalBolum;
        _cachedSinif = finalSinif;

        _currentUser = profile.copyWith(
          bolum: finalBolum,
          sinif: finalSinif,
        );
      });

  Future<bool> register({
    required String adSoyad,
    required String email,
    required String password,
    String? bolum,
    String? sinif,
  }) {
    final selectedBolum =
        (bolum != null && bolum.trim().isNotEmpty) ? bolum.trim() : null;
    final selectedSinif =
        (sinif != null && sinif.trim().isNotEmpty) ? sinif.trim() : null;

    // Önceki hesaptan kalan bilgileri temizle
    _cachedBolum = null;
    _cachedSinif = null;

    return _run((s) async {
      _currentUser = null;
      await s.signUp(adSoyad.trim(), email.trim(), password);
      if (s.hasSession) {
        final profile = await s.saveProfile(
          adSoyad.trim(),
          bolum: selectedBolum,
          sinif: selectedSinif,
        );
        final userId = profile.id;

        final serverBolum =
            (profile.bolum != null && profile.bolum!.trim().isNotEmpty)
                ? profile.bolum!.trim()
                : null;
        final serverSinif =
            (profile.sinif != null && profile.sinif!.trim().isNotEmpty)
                ? profile.sinif!.trim()
                : null;

        final finalBolum = serverBolum ?? selectedBolum ?? 'Bilgisayar Mühendisliği';
        final finalSinif = serverSinif ?? selectedSinif ?? '1. Sınıf';

        await _storage.saveAcademicInfo(
          userId,
          bolum: finalBolum,
          sinif: finalSinif,
        );

        _cachedBolum = finalBolum;
        _cachedSinif = finalSinif;

        _currentUser = profile.copyWith(
          bolum: finalBolum,
          sinif: finalSinif,
        );
      } else {
        _message = 'Hesap oluşturuldu. E-postanızı onaylayıp giriş yapın.';
      }
    });
  }

  Future<bool> updateProfile(
    String name, {
    String? bolum,
    String? sinif,
  }) async {
    final trimmed = name.trim();
    if (trimmed.length < 2 || trimmed.length > 100) {
      _message = 'Ad soyad 2–100 karakter olmalıdır.';
      _notify();
      return false;
    }

    final targetBolum = (bolum != null && bolum.trim().isNotEmpty)
        ? bolum.trim()
        : _cachedBolum ?? _currentUser?.bolum;
    final targetSinif = (sinif != null && sinif.trim().isNotEmpty)
        ? sinif.trim()
        : _cachedSinif ?? _currentUser?.sinif;

    return _run((s) async {
      // PUT /api/profil isteğine adSoyad yanında bolum ve sinif de gönderilsin
      final updated = await s.saveProfile(
        trimmed,
        bolum: targetBolum,
        sinif: targetSinif,
      );
      final userId = updated.id;

      // Sunucudan (GET/PUT yanıtından) dönen değerler önceliklidir
      final serverBolum =
          (updated.bolum != null && updated.bolum!.trim().isNotEmpty)
              ? updated.bolum!.trim()
              : null;
      final serverSinif =
          (updated.sinif != null && updated.sinif!.trim().isNotEmpty)
              ? updated.sinif!.trim()
              : null;

      final finalBolum = serverBolum ??
          targetBolum ??
          _cachedBolum ??
          _currentUser?.bolum ??
          'Bilgisayar Mühendisliği';
      final finalSinif = serverSinif ??
          targetSinif ??
          _cachedSinif ??
          _currentUser?.sinif ??
          '4. Sınıf';

      await _storage.saveAcademicInfo(
        userId,
        bolum: finalBolum,
        sinif: finalSinif,
      );

      _cachedBolum = finalBolum;
      _cachedSinif = finalSinif;

      _currentUser = updated.copyWith(
        bolum: finalBolum,
        sinif: finalSinif,
      );
    }, sessionOperation: false);
  }

  Future<bool> updateProfileDetails({
    required String adSoyad,
    String? bolum,
    String? sinif,
  }) =>
      updateProfile(adSoyad, bolum: bolum, sinif: sinif);

  /// Demo rol değiştirme (Öğrenci <-> Yönetici)
  void toggleRoleForDemo() {
    if (_currentUser == null) return;
    final newRole = _currentUser!.rol == KullaniciRolu.ogrenci
        ? KullaniciRolu.yonetici
        : KullaniciRolu.ogrenci;

    _currentUser = _currentUser!.copyWith(
      rol: newRole,
      bolum: newRole == KullaniciRolu.yonetici
          ? 'Rektörlük / Yönetim'
          : (_cachedBolum ?? 'Bilgisayar Mühendisliği'),
      sinif: newRole == KullaniciRolu.yonetici ? '-' : (_cachedSinif ?? '4. Sınıf'),
    );
    notifyListeners();
  }

  Future<bool> logout() => _run((s) async {
        await s.signOut();
        _currentUser = null;
        _cachedBolum = null;
        _cachedSinif = null;
      });

  @override
  void dispose() {
    _disposed = true;
    _subscription?.cancel();
    super.dispose();
  }
}
