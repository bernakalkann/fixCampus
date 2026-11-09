import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/models.dart';
import '../../providers/auth_provider.dart';
import '../../providers/listings_provider.dart';
import '../../routes/app_routes.dart';
import '../../utils/listing_search_filter.dart';
import '../../widgets/category_chip.dart';
import '../../widgets/empty_state.dart';
import '../../widgets/listing_card.dart';
import '../../widgets/listing_search_bar.dart';
import '../../widgets/server_listing_results.dart';
import '../../services/listings_service.dart';
import '../../widgets/pagination_controls.dart';
import '../../widgets/role_session_guard.dart';

/// Hafta 4 — Kategori, Metin Arama ve Sayfalama Ekranı (Berke Mahmut Acar)
class ListingSearchScreen extends StatefulWidget {
  const ListingSearchScreen({
    super.key,
    this.initialTur,
    this.initialCategoryId,
    this.initialQuery = '',
    this.pageSize = ListingSearchFilter.defaultPageSize,
  });

  final IlanTuru? initialTur;
  final String? initialCategoryId;
  final String initialQuery;
  final int pageSize;

  @override
  State<ListingSearchScreen> createState() => _ListingSearchScreenState();
}

class _ListingSearchScreenState extends State<ListingSearchScreen> {
  late final TextEditingController _searchController;
  IlanTuru? _selectedTur;
  String? _selectedCategoryId;
  int _currentPage = 1;

  @override
  void initState() {
    super.initState();
    _selectedTur = widget.initialTur;
    _selectedCategoryId = widget.initialCategoryId;
    _searchController = TextEditingController(text: widget.initialQuery);
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  bool get _hasActiveFilters =>
      _searchController.text.trim().isNotEmpty ||
      _selectedTur != null ||
      _selectedCategoryId != null;

  void _resetFilters() {
    setState(() {
      _searchController.clear();
      _selectedTur = null;
      _selectedCategoryId = null;
      _currentPage = 1;
    });
  }

  void _selectTur(IlanTuru? tur, List<Kategori> allCategories) {
    setState(() {
      _selectedTur = tur;
      if (tur != null && _selectedCategoryId != null) {
        final stillValid = allCategories.any(
          (k) => k.id == _selectedCategoryId && k.tur == tur,
        );
        if (!stillValid) {
          _selectedCategoryId = null;
        }
      }
      _currentPage = 1;
    });
  }

  @override
  Widget build(BuildContext context) {
    return RoleSessionGuard(
      child: Builder(
        builder: (context) {
          final listingsProvider = context.watch<ListingsProvider>();
          final auth = context.watch<AuthProvider>();
          final currentUserId = auth.currentUser?.id ?? '';

          final availableCategories = _selectedTur == null
              ? listingsProvider.kategoriler
              : listingsProvider.kategoriler
                    .where((k) => k.tur == _selectedTur)
                    .toList();

          final activeCategoryId =
              availableCategories.any((k) => k.id == _selectedCategoryId)
              ? _selectedCategoryId
              : null;

          return ServerListingResults(
            provider: listingsProvider,
            query: ListingQuery(
              tur: _selectedTur,
              kategoriId: activeCategoryId,
              text: _searchController.text,
              page: _currentPage,
              pageSize: widget.pageSize,
              activeSocialOnly: true,
            ),
            onPageCorrected: (page) {
              if (mounted) {
                setState(() {
                  _currentPage = page;
                });
              }
            },
            builder: (pagination, queryLoading, queryError, queryRefresh) {
              return Scaffold(
                appBar: AppBar(
                  title: const Text('İlan Arama'),
                  actions: [
                    IconButton(
                      tooltip: 'Yenile',
                      icon: const Icon(Icons.refresh),
                      onPressed: listingsProvider.isLoading
                          ? null
                          : queryRefresh,
                    ),
                  ],
                ),
                body: RefreshIndicator(
                  onRefresh: () async {
                    await queryRefresh();
                  },
                  child: CustomScrollView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    slivers: [
                      SliverToBoxAdapter(
                        child: ListingQueryStatus(
                          loading: queryLoading || listingsProvider.isLoading,
                          error: queryError ?? listingsProvider.error,
                          onRetry: queryRefresh,
                        ),
                      ),
                      SliverToBoxAdapter(
                        child: Padding(
                          padding: const EdgeInsets.only(top: 8),
                          child: ListingSearchBar(
                            controller: _searchController,
                            hintText:
                                'Başlık, açıklama, konum veya kategori ara...',
                            hasActiveFilters: _hasActiveFilters,
                            totalResults: pagination.totalItems,
                            onResetFilters: _resetFilters,
                            onChanged: (_) {
                              setState(() {
                                _currentPage = 1;
                              });
                            },
                            onClear: () {
                              setState(() {
                                _searchController.clear();
                                _currentPage = 1;
                              });
                            },
                          ),
                        ),
                      ),

                      // İlan Türü Seçici
                      SliverToBoxAdapter(
                        child: Padding(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 6,
                          ),
                          child: Container(
                            decoration: BoxDecoration(
                              color: const Color(0xFFF4F4F5),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            padding: const EdgeInsets.all(3),
                            child: Row(
                              children: [
                                Expanded(
                                  child: _SearchTypeSegment(
                                    label: 'Tümü',
                                    isSelected: _selectedTur == null,
                                    onTap: () => _selectTur(
                                      null,
                                      listingsProvider.kategoriler,
                                    ),
                                  ),
                                ),
                                Expanded(
                                  child: _SearchTypeSegment(
                                    label: 'Sosyal',
                                    isSelected: _selectedTur == IlanTuru.sosyal,
                                    onTap: () => _selectTur(
                                      IlanTuru.sosyal,
                                      listingsProvider.kategoriler,
                                    ),
                                  ),
                                ),
                                Expanded(
                                  child: _SearchTypeSegment(
                                    label: 'Kayıp',
                                    isSelected: _selectedTur == IlanTuru.kayip,
                                    onTap: () => _selectTur(
                                      IlanTuru.kayip,
                                      listingsProvider.kategoriler,
                                    ),
                                  ),
                                ),
                                Expanded(
                                  child: _SearchTypeSegment(
                                    label: 'Buluntu',
                                    isSelected:
                                        _selectedTur == IlanTuru.buluntu,
                                    onTap: () => _selectTur(
                                      IlanTuru.buluntu,
                                      listingsProvider.kategoriler,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),

                      // Kategori Filtreleme Çipleri
                      SliverToBoxAdapter(
                        child: SizedBox(
                          height: 48,
                          child: ListView(
                            scrollDirection: Axis.horizontal,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 16,
                              vertical: 8,
                            ),
                            children: [
                              CategoryChip(
                                label: 'Tümü',
                                isSelected: activeCategoryId == null,
                                onTap: () {
                                  setState(() {
                                    _selectedCategoryId = null;
                                    _currentPage = 1;
                                  });
                                },
                              ),
                              ...availableCategories.map(
                                (cat) => CategoryChip(
                                  label: cat.ad,
                                  isSelected: activeCategoryId == cat.id,
                                  onTap: () {
                                    setState(() {
                                      _selectedCategoryId =
                                          activeCategoryId == cat.id
                                          ? null
                                          : cat.id;
                                      _currentPage = 1;
                                    });
                                  },
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),

                      // Sonuç Listesi veya Boş Durum
                      if (pagination.items.isEmpty &&
                          !queryLoading &&
                          queryError == null &&
                          !listingsProvider.isLoading &&
                          listingsProvider.error == null)
                        SliverFillRemaining(
                          hasScrollBody: false,
                          child: EmptyState(
                            icon: Icons.search_off_outlined,
                            title: _hasActiveFilters
                                ? 'Eşleşen İlan Bulunamadı'
                                : 'İlan Bulunamadı',
                            description: _hasActiveFilters
                                ? 'Arama veya kategori filtrelerine uygun sonuç yok.'
                                : 'Henüz yayınlanmış bir ilan bulunmuyor.',
                            actionLabel: _hasActiveFilters
                                ? 'Filtreleri Temizle'
                                : null,
                            onAction: _hasActiveFilters ? _resetFilters : null,
                          ),
                        )
                      else ...[
                        SliverPadding(
                          padding: const EdgeInsets.only(top: 4, bottom: 8),
                          sliver: SliverList(
                            delegate: SliverChildBuilderDelegate((
                              context,
                              index,
                            ) {
                              final item = pagination.items[index];
                              final isSocial = item.tur == IlanTuru.sosyal;
                              return ListingCard(
                                ilan: item,
                                attendanceCount: isSocial
                                    ? listingsProvider.getAttendanceCount(
                                        item.id,
                                      )
                                    : 0,
                                isAttending:
                                    isSocial &&
                                    currentUserId.isNotEmpty &&
                                    listingsProvider.isUserAttending(
                                      item.id,
                                      currentUserId,
                                    ),
                                onTap: () {
                                  Navigator.pushNamed(
                                    context,
                                    AppRoutes.listingDetail,
                                    arguments: item,
                                  );
                                },
                              );
                            }, childCount: pagination.items.length),
                          ),
                        ),
                        SliverToBoxAdapter(
                          child: PaginationControls(
                            pagination: pagination,
                            onPageChanged: (newPage) {
                              setState(() {
                                _currentPage = newPage;
                              });
                            },
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }
}

class _SearchTypeSegment extends StatelessWidget {
  const _SearchTypeSegment({
    required this.label,
    required this.isSelected,
    required this.onTap,
  });

  final String label;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 8),
        decoration: BoxDecoration(
          color: isSelected ? Colors.white : Colors.transparent,
          borderRadius: BorderRadius.circular(6),
        ),
        child: Center(
          child: Text(
            label,
            style: TextStyle(
              fontSize: 12,
              fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
              color: isSelected ? Colors.black : const Color(0xFF71717A),
            ),
          ),
        ),
      ),
    );
  }
}
