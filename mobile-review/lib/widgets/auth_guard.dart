import 'package:flutter/material.dart';
import 'role_session_guard.dart';

class AuthGuard extends StatelessWidget {
  const AuthGuard({super.key, required this.child, this.adminOnly = false});
  final Widget child;
  final bool adminOnly;
  @override
  Widget build(BuildContext context) {
    return RoleSessionGuard(adminOnly: adminOnly, child: child);
  }
}
