import 'package:flutter/material.dart';

/// İlan akışları ve arama ekranı için minimalist arama çubuğu ve aktif filtre özeti.
class ListingSearchBar extends StatelessWidget {
  const ListingSearchBar({
    super.key,
    required this.controller,
    required this.onChanged,
    required this.onClear,
    this.hintText = 'Başlık, açıklama veya konum ara...',
    this.hasActiveFilters = false,
    this.totalResults,
    this.onResetFilters,
  });

  final TextEditingController controller;
  final ValueChanged<String> onChanged;
  final VoidCallback onClear;
  final String hintText;
  final bool hasActiveFilters;
  final int? totalResults;
  final VoidCallback? onResetFilters;

  @override
  Widget build(BuildContext context) {
    final hasText = controller.text.trim().isNotEmpty;

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          TextField(
            controller: controller,
            onChanged: onChanged,
            textInputAction: TextInputAction.search,
            style: const TextStyle(fontSize: 13, color: Color(0xFF18181B)),
            decoration: InputDecoration(
              hintText: hintText,
              hintStyle: const TextStyle(
                fontSize: 13,
                color: Color(0xFF71717A),
              ),
              prefixIcon: const Icon(
                Icons.search,
                size: 18,
                color: Color(0xFF71717A),
              ),
              suffixIcon: hasText
                  ? IconButton(
                      tooltip: 'Aramayı temizle',
                      icon: const Icon(
                        Icons.close,
                        size: 18,
                        color: Color(0xFF71717A),
                      ),
                      onPressed: onClear,
                    )
                  : null,
              isDense: true,
              filled: true,
              fillColor: const Color(0xFFF8FAFC),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 12,
                vertical: 10,
              ),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(color: Color(0xFFE4E4E7)),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(color: Color(0xFFE4E4E7)),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(
                  color: Color(0xFF18181B),
                  width: 1.2,
                ),
              ),
            ),
          ),
          if (hasActiveFilters && totalResults != null) ...[
            const SizedBox(height: 6),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Flexible(
                  child: Text(
                    '$totalResults ilan bulundu',
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: Color(0xFF52525B),
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                if (onResetFilters != null)
                  TextButton.icon(
                    onPressed: onResetFilters,
                    style: TextButton.styleFrom(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 2,
                      ),
                      minimumSize: Size.zero,
                      tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                    ),
                    icon: const Icon(Icons.filter_alt_off_outlined, size: 14),
                    label: const Text(
                      'Filtreleri Temizle',
                      style: TextStyle(fontSize: 12),
                    ),
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

