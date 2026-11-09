import 'dart:async';
import 'package:shared_preferences/shared_preferences.dart';

/// Kullanıcı bazlı yerel profil ve akademik bilgi (Bölüm, Sınıf) saklayıcısı.
/// Farklı hesapların birbirinin verilerini görmesini engellemek için anahtarlar
/// kullanıcı kimliği (userId) ile ayrıştırılır.
class LocalProfileStorage {
  const LocalProfileStorage();

  static final Map<String, String> _memoryFallback = {};

  static String _keyBolum(String userId) => 'fixcampus_profile_${userId}_bolum';
  static String _keySinif(String userId) => 'fixcampus_profile_${userId}_sinif';

  Future<SharedPreferences?> _getPrefs() async {
    try {
      return await SharedPreferences.getInstance()
          .timeout(const Duration(seconds: 2));
    } catch (_) {
      return null;
    }
  }

  /// Kullanıcının kaydedilmiş bölümünü getirir.
  Future<String?> getBolum(String userId) async {
    if (userId.trim().isEmpty) return null;
    final prefs = await _getPrefs();
    return prefs?.getString(_keyBolum(userId)) ?? _memoryFallback[_keyBolum(userId)];
  }

  /// Kullanıcının kaydedilmiş sınıfını getirir.
  Future<String?> getSinif(String userId) async {
    if (userId.trim().isEmpty) return null;
    final prefs = await _getPrefs();
    return prefs?.getString(_keySinif(userId)) ?? _memoryFallback[_keySinif(userId)];
  }

  /// Kullanıcının bölüm ve sınıf bilgilerini yerel depoya yazar.
  Future<void> saveAcademicInfo(
    String userId, {
    String? bolum,
    String? sinif,
  }) async {
    if (userId.trim().isEmpty) return;
    if (bolum != null && bolum.trim().isNotEmpty) {
      _memoryFallback[_keyBolum(userId)] = bolum.trim();
    }
    if (sinif != null && sinif.trim().isNotEmpty) {
      _memoryFallback[_keySinif(userId)] = sinif.trim();
    }
    final prefs = await _getPrefs();
    if (prefs != null) {
      if (bolum != null && bolum.trim().isNotEmpty) {
        await prefs.setString(_keyBolum(userId), bolum.trim());
      }
      if (sinif != null && sinif.trim().isNotEmpty) {
        await prefs.setString(_keySinif(userId), sinif.trim());
      }
    }
  }

  /// Belirli bir kullanıcının yerel akademik bilgilerini siler.
  Future<void> clearUser(String userId) async {
    if (userId.trim().isEmpty) return;
    _memoryFallback.remove(_keyBolum(userId));
    _memoryFallback.remove(_keySinif(userId));
    final prefs = await _getPrefs();
    if (prefs != null) {
      await prefs.remove(_keyBolum(userId));
      await prefs.remove(_keySinif(userId));
    }
  }

  /// Test temizliği için bellek havuzunu sıfırlar
  static void resetMemoryForTest() {
    _memoryFallback.clear();
  }
}
