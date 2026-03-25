import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  Image,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { AppColors, BorderRadius, Spacing } from '../../constants/theme';
import { adminService, type ReviewListItem } from '../../services/adminService';
import { useAuth } from '../../contexts/AuthContext';

function unwrapData<T>(payload: unknown): T {
  if (payload && typeof payload === 'object' && 'Data' in payload) {
    return (payload as { Data: T }).Data;
  }
  return payload as T;
}

function getStatusMeta(status: string) {
  if (status === 'PENDING') return { bg: '#FEF3C7', text: '#92400E', label: 'Chờ duyệt' };
  if (status === 'APPROVED') return { bg: '#D1FAE5', text: '#065F46', label: 'Đã duyệt' };
  if (status === 'HIDDEN') return { bg: '#F3F4F6', text: '#374151', label: 'Đã ẩn' };
  return { bg: '#F3F4F6', text: '#374151', label: status || 'N/A' };
}

export default function AdminReviewsScreen() {
  const { user } = useAuth();
  const isAdmin = user ? String(user.Role).toUpperCase() === 'ADMIN' || Number(user.Role) === 2 : false;
  const isStaff = user ? String(user.Role).toUpperCase() === 'STAFF' || Number(user.Role) === 1 : false;

  const [reviews, setReviews] = useState<ReviewListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [ratingFilter, setRatingFilter] = useState<number | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [updatingId, setUpdatingId] = useState<string | null>(null);

  const pageSize = 20;

  const canModerate = isAdmin || isStaff;

  const statusFilters = useMemo(
    () => [
      { value: '', label: 'Tất cả' },
      { value: 'PENDING', label: 'Chờ duyệt' },
      { value: 'APPROVED', label: 'Đã duyệt' },
      { value: 'HIDDEN', label: 'Đã ẩn' },
    ],
    [],
  );

  const ratingFilters = [undefined, 5, 4, 3, 2, 1];

  const fetchReviews = useCallback(async () => {
    if (!canModerate) return;

    setLoading(true);
    try {
      const res = await adminService.getReviews({
        status: statusFilter || undefined,
        rating: ratingFilter,
        page,
        pageSize,
      });

      const normalized = unwrapData<any>(res) ?? {};
      const items = Array.isArray(normalized.Items)
        ? normalized.Items
        : Array.isArray(normalized.items)
          ? normalized.items
          : Array.isArray(normalized)
            ? normalized
            : [];

      const totalItemsRaw = normalized.TotalItems ?? normalized.totalItems;
      const totalItems = typeof totalItemsRaw === 'number' ? totalItemsRaw : items.length;

      setReviews(items);
      setTotal(totalItems);
    } catch {
      setReviews([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  }, [canModerate, page, pageSize, ratingFilter, statusFilter]);

  useEffect(() => {
    fetchReviews();
  }, [fetchReviews]);

  const handleApprove = async (id: string) => {
    try {
      setUpdatingId(id);
      await adminService.approveReview(id);
      await fetchReviews();
    } finally {
      setUpdatingId(null);
    }
  };

  const handleHide = async (id: string) => {
    try {
      setUpdatingId(id);
      await adminService.hideReview(id);
      await fetchReviews();
    } finally {
      setUpdatingId(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  if (!canModerate) {
    return (
      <View style={styles.deniedWrap}>
        <Text style={styles.deniedTitle}>Khu vực đánh giá dành cho Staff/Admin</Text>
        <Text style={styles.deniedDesc}>Bạn không có quyền duyệt bình luận ở màn này.</Text>
      </View>
    );
  }

  return (
    <View style={styles.screen}>
      <FlatList
        data={reviews}
        keyExtractor={(item, idx) => String(item.Id || idx)}
        contentContainerStyle={styles.listContent}
        refreshControl={<RefreshControl refreshing={loading} onRefresh={fetchReviews} />}
        ListHeaderComponent={(
          <View style={styles.headerWrap}>
            <Text style={styles.title}>Duyệt bình luận</Text>
            <Text style={styles.subTitle}>Phê duyệt hoặc ẩn đánh giá của khách hàng</Text>

            <View style={styles.filterRow}>
              {statusFilters.map((f) => (
                <TouchableOpacity
                  key={f.value || 'all'}
                  style={[styles.filterChip, statusFilter === f.value && styles.filterChipActive]}
                  onPress={() => {
                    setPage(1);
                    setStatusFilter(f.value);
                  }}
                >
                  <Text style={[styles.filterText, statusFilter === f.value && styles.filterTextActive]}>{f.label}</Text>
                </TouchableOpacity>
              ))}
            </View>

            <View style={styles.filterRow}>
              {ratingFilters.map((r) => {
                const active = ratingFilter === r;
                return (
                  <TouchableOpacity
                    key={r ?? 0}
                    style={[styles.filterChip, active && styles.filterChipActive]}
                    onPress={() => {
                      setPage(1);
                      setRatingFilter(r);
                    }}
                  >
                    <Text style={[styles.filterText, active && styles.filterTextActive]}>
                      {r ? `${r} sao` : 'Tất cả sao'}
                    </Text>
                  </TouchableOpacity>
                );
              })}
            </View>
          </View>
        )}
        renderItem={({ item }) => {
          const status = String(item.Status ?? '');
          const meta = getStatusMeta(status);
          const dateText = item.CreatedAt ? new Date(item.CreatedAt).toLocaleDateString('vi-VN') : '--';
          const reviewerName = item.ReviewerName || 'Khách hàng';

          return (
            <View style={styles.card}>
              <View style={styles.cardHeader}>
                <View style={styles.avatar}>
                  <Text style={styles.avatarText}>{reviewerName.charAt(0).toUpperCase()}</Text>
                </View>
                <View style={styles.headerInfo}>
                  <Text style={styles.reviewerName}>{reviewerName}</Text>
                  <Text style={styles.reviewerEmail}>{item.ReviewerEmail}</Text>
                </View>
                <View style={[styles.statusBadge, { backgroundColor: meta.bg }]}>
                  <Text style={[styles.statusText, { color: meta.text }]}>{item.StatusLabel || meta.label}</Text>
                </View>
              </View>

              <View style={styles.ratingRow}>
                {[1, 2, 3, 4, 5].map((star) => (
                  <Ionicons
                    key={star}
                    name={star <= Number(item.Rating ?? 0) ? 'star' : 'star-outline'}
                    size={14}
                    color={star <= Number(item.Rating ?? 0) ? '#F59E0B' : '#D1D5DB'}
                  />
                ))}
                <Text style={styles.dateText}>{dateText}</Text>
              </View>

              <View style={styles.productRow}>
                {item.GiftBoxImage ? <Image source={{ uri: item.GiftBoxImage }} style={styles.productThumb} /> : null}
                <Text style={styles.productName} numberOfLines={1}>{item.GiftBoxName || 'Sản phẩm'}</Text>
              </View>

              <Text style={styles.content}>{item.Content}</Text>

              <View style={styles.actionRow}>
                {status !== 'APPROVED' && (
                  <TouchableOpacity
                    style={[styles.actionBtn, styles.approveBtn, updatingId === item.Id && styles.disabled]}
                    disabled={updatingId === item.Id}
                    onPress={() => handleApprove(item.Id)}
                  >
                    <Text style={styles.approveText}>{updatingId === item.Id ? 'Đang xử lý...' : 'Phê duyệt'}</Text>
                  </TouchableOpacity>
                )}
                {status !== 'HIDDEN' && (
                  <TouchableOpacity
                    style={[styles.actionBtn, styles.hideBtn, updatingId === item.Id && styles.disabled]}
                    disabled={updatingId === item.Id}
                    onPress={() => handleHide(item.Id)}
                  >
                    <Text style={styles.hideText}>{updatingId === item.Id ? 'Đang xử lý...' : 'Ẩn'}</Text>
                  </TouchableOpacity>
                )}
              </View>
            </View>
          );
        }}
        ListEmptyComponent={!loading ? <Text style={styles.emptyText}>Chưa có đánh giá nào.</Text> : null}
        ListFooterComponent={
          totalPages > 1 ? (
            <View style={styles.footerPager}>
              <Text style={styles.pagerLabel}>Trang {page} / {totalPages} ({total} đánh giá)</Text>
              <View style={styles.pagerActions}>
                <TouchableOpacity
                  style={[styles.pagerBtn, page <= 1 && styles.disabled]}
                  disabled={page <= 1}
                  onPress={() => setPage((p) => Math.max(1, p - 1))}
                >
                  <Text style={styles.pagerBtnText}>Trước</Text>
                </TouchableOpacity>
                <TouchableOpacity
                  style={[styles.pagerBtn, page >= totalPages && styles.disabled]}
                  disabled={page >= totalPages}
                  onPress={() => setPage((p) => Math.min(totalPages, p + 1))}
                >
                  <Text style={styles.pagerBtnText}>Sau</Text>
                </TouchableOpacity>
              </View>
            </View>
          ) : null
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: AppColors.background },
  listContent: { padding: Spacing.lg, paddingTop: 56, paddingBottom: 24 },
  headerWrap: { marginBottom: Spacing.md },
  title: { fontSize: 22, fontWeight: '800', color: AppColors.text },
  subTitle: { marginTop: 2, fontSize: 12, color: AppColors.textSecondary, marginBottom: Spacing.sm },
  filterRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 8 },
  filterChip: {
    borderWidth: 1,
    borderColor: AppColors.border,
    borderRadius: 999,
    paddingHorizontal: 10,
    paddingVertical: 6,
    backgroundColor: '#FFF',
  },
  filterChipActive: { borderColor: AppColors.primary, backgroundColor: 'rgba(139, 26, 26, 0.08)' },
  filterText: { fontSize: 12, color: AppColors.textSecondary, fontWeight: '600' },
  filterTextActive: { color: AppColors.primary },

  card: {
    backgroundColor: '#FFF',
    borderRadius: BorderRadius.md,
    borderWidth: 1,
    borderColor: AppColors.borderLight,
    padding: Spacing.md,
    marginBottom: Spacing.sm,
  },
  cardHeader: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  avatar: {
    width: 34,
    height: 34,
    borderRadius: 17,
    backgroundColor: 'rgba(139, 26, 26, 0.12)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: { color: AppColors.primary, fontWeight: '700' },
  headerInfo: { flex: 1 },
  reviewerName: { fontSize: 13, fontWeight: '700', color: AppColors.text },
  reviewerEmail: { fontSize: 11, color: AppColors.textMuted },
  statusBadge: { borderRadius: 999, paddingHorizontal: 8, paddingVertical: 3 },
  statusText: { fontSize: 10, fontWeight: '700' },

  ratingRow: { flexDirection: 'row', alignItems: 'center', gap: 2, marginTop: 8 },
  dateText: { marginLeft: 8, fontSize: 11, color: AppColors.textMuted },
  productRow: { marginTop: 8, flexDirection: 'row', alignItems: 'center', gap: 8 },
  productThumb: { width: 24, height: 24, borderRadius: 6 },
  productName: { flex: 1, fontSize: 12, color: AppColors.textSecondary },
  content: { marginTop: 8, fontSize: 13, color: AppColors.text, lineHeight: 18 },

  actionRow: { flexDirection: 'row', gap: 8, marginTop: 10 },
  actionBtn: { paddingHorizontal: 10, paddingVertical: 7, borderRadius: 8 },
  approveBtn: { backgroundColor: '#D1FAE5' },
  hideBtn: { backgroundColor: '#F3F4F6' },
  approveText: { fontSize: 12, fontWeight: '700', color: '#065F46' },
  hideText: { fontSize: 12, fontWeight: '700', color: '#374151' },

  emptyText: { textAlign: 'center', color: AppColors.textMuted, paddingVertical: 28, fontSize: 13 },
  footerPager: { marginTop: 8 },
  pagerLabel: { fontSize: 11, color: AppColors.textMuted, marginBottom: 8 },
  pagerActions: { flexDirection: 'row', gap: 8 },
  pagerBtn: {
    borderWidth: 1,
    borderColor: AppColors.border,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 7,
    backgroundColor: '#FFF',
  },
  pagerBtnText: { fontSize: 12, color: AppColors.textSecondary, fontWeight: '600' },

  deniedWrap: {
    flex: 1,
    backgroundColor: AppColors.background,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: Spacing.xl,
  },
  deniedTitle: { fontSize: 16, fontWeight: '700', color: AppColors.text, textAlign: 'center' },
  deniedDesc: { marginTop: 8, fontSize: 13, color: AppColors.textSecondary, textAlign: 'center' },
  disabled: { opacity: 0.6 },
});
