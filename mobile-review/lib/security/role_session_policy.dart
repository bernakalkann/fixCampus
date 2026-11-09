import '../models/profil.dart';
import '../models/model_enums.dart';

enum RoleAccess {
  signInRequired,
  checking,
  profileRequired,
  forbidden,
  allowed,
}

/// Ekran erişimi için API'den doğrulanmış profil gerekir.
/// Sunucudaki yetkilendirme denetiminin yerine geçmez.
class RoleSessionPolicy {
  const RoleSessionPolicy();

  RoleAccess evaluate({
    required bool hasSession,
    required bool isLoading,
    required Profil? profile,
    required bool hasError,
    bool adminOnly = false,
  }) {
    if (!hasSession) return RoleAccess.signInRequired;
    if (isLoading) return RoleAccess.checking;
    if (hasError || profile == null) return RoleAccess.profileRequired;
    if (adminOnly && profile.rol != KullaniciRolu.yonetici) {
      return RoleAccess.forbidden;
    }
    return RoleAccess.allowed;
  }
}
