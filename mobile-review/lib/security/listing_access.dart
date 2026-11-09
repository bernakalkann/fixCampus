import '../models/models.dart';
import '../providers/auth_provider.dart';
import 'listing_mutation_policy.dart';
import 'role_session_policy.dart';

/// Her işlemde AuthProvider'ın o anki doğrulama durumunu kullanır.
RoleAccess listingAccess(
  AuthProvider auth,
  ListingMutation action, {
  Ilan? listing,
}) => const ListingMutationPolicy().evaluate(
  action: action,
  hasSession: auth.isLoggedIn,
  isLoading: auth.isLoading,
  profile: auth.currentUser,
  hasSessionError: auth.hasSessionError,
  listing: listing,
);

String listingAccessMessage(RoleAccess access) => access == RoleAccess.forbidden
    ? 'İlanı yalnızca sahibi değiştirebilir.'
    : 'İlan işlemi için oturumunuzu doğrulayın.';
