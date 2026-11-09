import '../models/models.dart';
import 'role_session_policy.dart';

enum ListingMutation { create, update, delete }

/// İlan değiştirme işlemleri için istemci ön kontrolü.
/// API her istekte oturum ve sahipliği ayrıca doğrulamalıdır.
class ListingMutationPolicy {
  const ListingMutationPolicy();

  RoleAccess evaluate({
    required ListingMutation action,
    required bool hasSession,
    required bool isLoading,
    required Profil? profile,
    required bool hasSessionError,
    Ilan? listing,
  }) {
    final access = const RoleSessionPolicy().evaluate(
      hasSession: hasSession,
      isLoading: isLoading,
      profile: profile,
      hasError: hasSessionError,
    );
    if (access != RoleAccess.allowed) return access;
    if (profile!.id.trim().isEmpty) return RoleAccess.profileRequired;
    if (action == ListingMutation.create) return RoleAccess.allowed;
    // Yönetici gizleme işlemi ayrı bir moderasyon görevidir. Başkasının
    // ilanını normal düzenleme/silme akışından değiştirmeye izin verilmez.
    if (listing == null || listing.sahipId != profile.id) {
      return RoleAccess.forbidden;
    }
    return RoleAccess.allowed;
  }
}
