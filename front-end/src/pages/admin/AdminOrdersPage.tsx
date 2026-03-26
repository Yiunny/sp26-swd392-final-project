import { useState, useEffect } from "react";
import { adminService, type OrderStatusSummary, type AdminOrderListItem } from "../../services/adminService";
import { FiEdit2, FiX, FiEye } from "react-icons/fi";
import { orderService, type OrderDto } from "../../services/orderService";

const STATUS_CONFIG: { key: keyof OrderStatusSummary; label: string; color: string; bgColor: string }[] = [
    { key: "PendingPayment", label: "Chờ thanh toán", color: "text-amber-700", bgColor: "bg-amber-500" },
    { key: "Preparing", label: "Đang chuẩn bị", color: "text-blue-700", bgColor: "bg-blue-500" },
    { key: "Shipping", label: "Đang giao", color: "text-indigo-700", bgColor: "bg-indigo-500" },
    { key: "DeliveryFailed", label: "Giao thất bại", color: "text-red-700", bgColor: "bg-red-500" },
    { key: "PartiallyDelivered", label: "Giao một phần", color: "text-orange-700", bgColor: "bg-orange-500" },
    { key: "Refunding", label: "Đang hoàn tiền", color: "text-rose-700", bgColor: "bg-rose-500" },
    { key: "Refunded", label: "Đã hoàn tiền", color: "text-purple-700", bgColor: "bg-purple-500" },
    { key: "Completed", label: "Hoàn tất", color: "text-emerald-700", bgColor: "bg-emerald-500" },
    { key: "Cancelled", label: "Đã hủy", color: "text-gray-700", bgColor: "bg-gray-500" },
];

const ALL_STATUSES = [
    { value: "", label: "Tất cả trạng thái" },
    { value: "PAYMENT_CONFIRMING", label: "Chờ thanh toán" },
    { value: "PREPARING", label: "Đang chuẩn bị" },
    { value: "SHIPPING", label: "Đang giao" },
    { value: "COMPLETED", label: "Hoàn tất" },
    { value: "CANCELLED", label: "Đã hủy" },
    { value: "REFUNDING", label: "Đang hoàn tiền" },
    { value: "REFUNDED", label: "Đã hoàn tiền" },
    { value: "DELIVERY_FAILED", label: "Giao thất bại" },
    { value: "PARTIAL_DELIVERY", label: "Giao một phần" },
];

const VALID_TRANSITIONS: Record<string, { value: string; label: string }[]> = {
    PAYMENT_CONFIRMING: [
        { value: "PREPARING", label: "Đang chuẩn bị" },
        { value: "CANCELLED", label: "Đã hủy" },
    ],
    PREPARING: [
        { value: "SHIPPING", label: "Đang giao" },
        { value: "CANCELLED", label: "Đã hủy" },
    ],
    SHIPPING: [
        { value: "COMPLETED", label: "Hoàn tất" },
        { value: "PARTIAL_DELIVERY", label: "Giao một phần" },
        { value: "DELIVERY_FAILED", label: "Giao thất bại" },
        { value: "CANCELLED", label: "Đã hủy" },
    ],
    DELIVERY_FAILED: [
        { value: "SHIPPING", label: "Giao lại" },
        { value: "CANCELLED", label: "Đã hủy" },
    ],
    PARTIAL_DELIVERY: [
        { value: "SHIPPING", label: "Giao lại phần còn lại" },
        { value: "COMPLETED", label: "Hoàn tất" },
        { value: "CANCELLED", label: "Đã hủy" },
    ],
    REFUNDING: [
        { value: "REFUNDED", label: "Đã hoàn tiền" },
    ],
};

const STATUS_BADGE: Record<string, { text: string; cls: string }> = {
    PAYMENT_CONFIRMING: { text: "Chờ thanh toán", cls: "bg-amber-100 text-amber-700" },
    PREPARING: { text: "Đang chuẩn bị", cls: "bg-blue-100 text-blue-700" },
    SHIPPING: { text: "Đang giao", cls: "bg-indigo-100 text-indigo-700" },
    PARTIAL_DELIVERY: { text: "Giao một phần", cls: "bg-orange-100 text-orange-700" },
    DELIVERY_FAILED: { text: "Giao thất bại", cls: "bg-red-100 text-red-700" },
    COMPLETED: { text: "Hoàn tất", cls: "bg-emerald-100 text-emerald-700" },
    CANCELLED: { text: "Đã hủy", cls: "bg-gray-100 text-gray-600" },
    PAYMENT_EXPIRED_INTERNAL: { text: "Hết hạn TT", cls: "bg-gray-100 text-gray-500" },
    REFUNDING: { text: "Đang hoàn tiền", cls: "bg-red-100 text-red-700" },
    REFUNDED: { text: "Đã hoàn tiền", cls: "bg-purple-100 text-purple-700" },
};

function formatPrice(v: number) { return v.toLocaleString("vi-VN") + "₫"; }
function formatDate(d: string) { return new Date(d).toLocaleString("vi-VN"); }
function getStatusInfo(s: string) { return STATUS_BADGE[s] ?? { text: s, cls: "bg-gray-100 text-gray-600" }; }

function getErrorMessage(err: unknown): string {
    if (typeof err === "object" && err !== null) {
        const maybeErr = err as { response?: { data?: { message?: string } }; message?: string };
        return maybeErr.response?.data?.message || maybeErr.message || "Cập nhật thất bại.";
    }
    return "Cập nhật thất bại.";
}

export default function AdminOrdersPage() {
    const [statusSummary, setStatusSummary] = useState<OrderStatusSummary | null>(null);
    const [summaryLoading, setSummaryLoading] = useState(true);

    // Order list
    const [orders, setOrders] = useState<AdminOrderListItem[]>([]);
    const [totalOrders, setTotalOrders] = useState(0);
    const [totalPages, setTotalPages] = useState(0);
    const [page, setPage] = useState(1);
    const [keyword, setKeyword] = useState("");
    const [statusFilter, setStatusFilter] = useState("");
    const [typeFilter, setTypeFilter] = useState("");
    const [listLoading, setListLoading] = useState(true);

    // Status update modal
    const [showUpdate, setShowUpdate] = useState(false);
    const [selectedOrder, setSelectedOrder] = useState<AdminOrderListItem | null>(null);
    const [newStatus, setNewStatus] = useState("PREPARING");
    const [statusNote, setStatusNote] = useState("");
    const [updating, setUpdating] = useState(false);
    const [updateResult, setUpdateResult] = useState<{ success: boolean; message: string } | null>(null);

    // Delivery update form
    const [showDelivery, setShowDelivery] = useState(false);
    const [deliveryId, setDeliveryId] = useState("");
    const [deliveryStatus, setDeliveryStatus] = useState("DELIVERED");
    const [failureReason, setFailureReason] = useState("");
    const [updatingDelivery, setUpdatingDelivery] = useState(false);

    // Order Details Modal
    const [showDetail, setShowDetail] = useState(false);
    const [detailLoading, setDetailLoading] = useState(false);
    const [detailOrder, setDetailOrder] = useState<OrderDto | null>(null);

    const pageSize = 20;

    const fetchSummary = async () => {
        setSummaryLoading(true);
        try {
            const res = await adminService.getOrderStatusSummary();
            setStatusSummary(res);
        } catch { setStatusSummary(null); }
        finally { setSummaryLoading(false); }
    };

    const fetchOrders = async () => {
        setListLoading(true);
        try {
            const res = await adminService.getAdminOrders({
                status: statusFilter || undefined,
                orderType: typeFilter || undefined,
                keyword: keyword || undefined,
                page,
                pageSize,
            });
            setOrders(res.Data);
            setTotalOrders(res.TotalItems);
            setTotalPages(res.TotalPages);
        } catch { setOrders([]); }
        finally { setListLoading(false); }
    };

    useEffect(() => { fetchSummary(); }, []);
    useEffect(() => { fetchOrders(); }, [page, keyword, statusFilter, typeFilter]);


    const openUpdateForOrder = (order: AdminOrderListItem) => {
        const options = VALID_TRANSITIONS[order.Status] ?? [];
        setSelectedOrder(order);
        setNewStatus(options.length > 0 ? options[0].value : "");
        setStatusNote("");
        setUpdateResult(null);
        setShowUpdate(true);
        setShowDelivery(false);
    };

    const openOrderDetail = async (orderId: string) => {
        setShowDetail(true);
        setDetailLoading(true);
        setDetailOrder(null);
        try {
            const data = await orderService.getOrderDetailById(orderId);
            setDetailOrder(data);
        } catch {
            // ignore
        } finally {
            setDetailLoading(false);
        }
    };

    const handleUpdateStatus = async () => {
        if (!selectedOrder) return;
        setUpdating(true);
        setUpdateResult(null);
        try {
            await adminService.updateOrderStatus(selectedOrder.Id, newStatus, statusNote || undefined);
            setUpdateResult({ success: true, message: "Đã cập nhật thành công!" });
            fetchOrders();
            fetchSummary();
        } catch (err: unknown) {
            setUpdateResult({ success: false, message: getErrorMessage(err) });
        } finally { setUpdating(false); }
    };

    const handleUpdateDelivery = async () => {
        if (!deliveryId.trim()) return;
        setUpdatingDelivery(true);
        try {
            if (deliveryStatus === "RESHIP") {
                await adminService.reshipDelivery(deliveryId.trim());
            } else {
                await adminService.updateDeliveryStatus(deliveryId.trim(), deliveryStatus, failureReason || undefined);
            }
            setShowDelivery(false);
            setDeliveryId("");
            setFailureReason("");
            fetchOrders();
            fetchSummary();
        } catch { /* ignore */ }
        finally { setUpdatingDelivery(false); }
    };

    return (
        <div className="p-6 space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Quản lý đơn hàng</h1>
                    <p className="text-sm text-gray-500">Danh sách đơn hàng, trạng thái và cập nhật</p>
                </div>
                <button onClick={() => { setShowDelivery(true); setShowUpdate(false); }} className="px-4 py-2 border border-[#8B1A1A] text-[#8B1A1A] text-sm font-semibold rounded-lg hover:bg-[#8B1A1A]/5 transition-colors cursor-pointer">
                    Cập nhật giao hàng
                </button>
            </div>

            {/* Status summary cards */}
            {summaryLoading ? (
                <div className="text-center py-4 text-gray-400 text-sm">Đang tải tổng quan...</div>
            ) : statusSummary && (
                <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-9 gap-3">
                    {STATUS_CONFIG.map(({ key, label, color, bgColor }) => {
                        const count = statusSummary[key] ?? 0;
                        return (
                            <div key={key} className="bg-white rounded-xl p-4 shadow-sm cursor-pointer hover:shadow-md transition-shadow" onClick={() => { setStatusFilter(key === "PendingPayment" ? "PAYMENT_CONFIRMING" : key === "DeliveryFailed" ? "DELIVERY_FAILED" : key === "PartiallyDelivered" ? "PARTIAL_DELIVERY" : key.toUpperCase()); setPage(1); }}>
                                <div className="flex items-center gap-2 mb-1">
                                    <div className={`w-2.5 h-2.5 rounded-full ${bgColor}`} />
                                    <span className="text-[10px] text-gray-400 uppercase tracking-wider">{label}</span>
                                </div>
                                <p className={`text-xl font-bold ${color}`}>{count}</p>
                            </div>
                        );
                    })}
                </div>
            )}

            {/* Filters */}
            <div className="flex flex-wrap gap-3 items-center">
                <input type="text" placeholder="Tìm mã đơn, tên, email..." value={keyword} onChange={(e) => { setKeyword(e.target.value); setPage(1); }} className="px-3 py-2 border border-gray-200 rounded-lg text-sm w-64 focus:outline-none focus:ring-2 focus:ring-[#8B1A1A]/20 focus:border-[#8B1A1A]" />
                <select value={statusFilter} onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }} className="px-3 py-2 border border-gray-200 rounded-lg text-sm cursor-pointer">
                    {ALL_STATUSES.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
                </select>
                <select value={typeFilter} onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }} className="px-3 py-2 border border-gray-200 rounded-lg text-sm cursor-pointer">
                    <option value="">Tất cả loại</option>
                    <option value="B2C">B2C</option>
                    <option value="B2B">B2B</option>
                </select>
                {(statusFilter || typeFilter || keyword) && (
                    <button onClick={() => { setStatusFilter(""); setTypeFilter(""); setKeyword(""); setPage(1); }} className="text-xs text-[#8B1A1A] hover:underline cursor-pointer">
                        Xóa bộ lọc
                    </button>
                )}
                <span className="ml-auto text-xs text-gray-400">{totalOrders} đơn hàng</span>
            </div>

            {/* Orders table */}
            <div className="bg-white rounded-xl shadow-sm overflow-hidden">
                <div className="overflow-x-auto">
                    <table className="w-full text-sm min-w-[800px]">
                        <thead className="bg-gray-50 border-b">
                        <tr className="text-left text-xs text-gray-400 uppercase">
                            <th className="px-4 py-3 font-medium">Mã đơn</th>
                            <th className="px-4 py-3 font-medium">Khách hàng</th>
                            <th className="px-4 py-3 font-medium">Loại</th>
                            <th className="px-4 py-3 font-medium">Trạng thái</th>
                            <th className="px-4 py-3 font-medium text-right">Tổng tiền</th>
                            <th className="px-4 py-3 font-medium text-center">SP</th>
                            <th className="px-4 py-3 font-medium">Ngày tạo</th>
                            <th className="px-4 py-3 font-medium text-right">Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        {listLoading ? (
                            <tr><td colSpan={8} className="text-center py-8 text-gray-400">Đang tải...</td></tr>
                        ) : orders.length === 0 ? (
                            <tr><td colSpan={8} className="text-center py-8 text-gray-400">Không có đơn hàng nào</td></tr>
                        ) : orders.map((order) => {
                            const badge = getStatusInfo(order.Status);
                            const isRefunding = order.Status === "REFUNDING";
                            return (
                                <tr key={order.Id} className={`border-b border-gray-50 transition-colors ${isRefunding ? "bg-rose-50 hover:bg-rose-100" : "hover:bg-gray-50/50"}`}>
                                    <td className="px-4 py-3">
                                        <button onClick={() => openOrderDetail(order.Id)} className="font-mono font-medium text-[#8B1A1A] text-xs hover:underline cursor-pointer text-left">
                                            {order.OrderCode}
                                        </button>
                                    </td>
                                    <td className="px-4 py-3">
                                        <div>
                                            <p className="font-medium text-gray-900 text-sm">{order.CustomerName}</p>
                                            <p className="text-[11px] text-gray-400">{order.CustomerEmail}</p>
                                        </div>
                                    </td>
                                    <td className="px-4 py-3">
                                        <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${order.OrderType === "B2B" ? "bg-purple-100 text-purple-700" : "bg-sky-100 text-sky-700"}`}>{order.OrderType}</span>
                                    </td>
                                    <td className="px-4 py-3">
                                        <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${badge.cls}`}>{badge.text}</span>
                                    </td>
                                    <td className="px-4 py-3 text-right font-bold text-gray-900">{formatPrice(order.TotalAmount)}</td>
                                    <td className="px-4 py-3 text-center">
                                        <span className="bg-gray-100 text-gray-600 px-2 py-0.5 rounded-full text-xs font-medium">{order.TotalItems}</span>
                                    </td>
                                    <td className="px-4 py-3 text-xs text-gray-500">{formatDate(order.CreatedAt)}</td>
                                    <td className="px-4 py-3 text-right">
                                        <div className="flex items-center justify-end gap-2">
                                            <button onClick={() => openOrderDetail(order.Id)} className="p-1 text-gray-400 hover:text-blue-600 cursor-pointer" title="Xem chi tiết">
                                                <FiEye className="w-4 h-4" />
                                            </button>
                                            <button onClick={() => openUpdateForOrder(order)} className="p-1 text-gray-400 hover:text-[#8B1A1A] cursor-pointer" title="Cập nhật trạng thái">
                                                <FiEdit2 className="w-4 h-4" />
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            );
                        })}
                    </tbody>
                    </table>
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                    <div className="flex items-center justify-between px-4 py-3 border-t">
                        <span className="text-xs text-gray-400">Trang {page} / {totalPages} ({totalOrders} đơn hàng)</span>
                        <div className="flex gap-1">
                            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1} className="px-3 py-1 rounded border text-xs disabled:opacity-30 cursor-pointer">Trước</button>
                            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page >= totalPages} className="px-3 py-1 rounded border text-xs disabled:opacity-30 cursor-pointer">Sau</button>
                        </div>
                    </div>
                )}
            </div>

            {/* Update Status Modal */}
            {showUpdate && selectedOrder && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={() => { setShowUpdate(false); setUpdateResult(null); }}>
                    <div className="bg-white rounded-2xl p-6 shadow-xl w-full max-w-lg" onClick={e => e.stopPropagation()}>
                        <div className="flex items-center justify-between mb-4">
                            <h3 className="text-lg font-bold text-gray-900">
                                Cập nhật trạng thái — <span className="text-[#8B1A1A] font-mono">{selectedOrder.OrderCode}</span>
                            </h3>
                            <button onClick={() => { setShowUpdate(false); setUpdateResult(null); }} className="text-gray-400 hover:text-gray-600 cursor-pointer">
                                <FiX className="w-5 h-5" />
                            </button>
                        </div>
                        <p className="text-sm text-gray-500 mb-4">Khách hàng: <span className="font-semibold">{selectedOrder.CustomerName}</span></p>

                        <div className="flex items-center gap-2 mb-6">
                            <span className="text-sm font-medium text-gray-600">Trạng thái hiện tại:</span>
                            <span className={`px-2 py-0.5 rounded-full text-xs font-bold ${getStatusInfo(selectedOrder.Status).cls}`}>{getStatusInfo(selectedOrder.Status).text}</span>
                        </div>

                        {selectedOrder.Status === "REFUNDING" && (
                            <div className="mb-6 p-4 border border-red-200 rounded-lg bg-red-50 flex flex-col items-center">
                                <p className="text-sm font-bold text-red-800 mb-2">Quét mã QR để hoàn tiền</p>
                                {selectedOrder.BankName && selectedOrder.BankAccountNumber ? (
                                    <>
                                        <p className="text-xs text-red-700 mb-3 text-center">
                                            Ngân hàng: <strong>{selectedOrder.BankName}</strong><br/>
                                            STK: <strong>{selectedOrder.BankAccountNumber}</strong><br/>
                                            Số tiền: <strong>{formatPrice(selectedOrder.TotalAmount)}</strong>
                                        </p>
                                        <div className="p-2 bg-white rounded-lg shadow-sm">
                                            <img
                                                src={`https://qr.sepay.vn/img?acc=${selectedOrder.BankAccountNumber}&bank=${selectedOrder.BankName}&amount=${selectedOrder.TotalAmount}&des=Hoan tien don hang ${selectedOrder.OrderCode}`}
                                                alt="QR Code Hoàn Tiền"
                                                className="w-48 h-48 object-contain"
                                            />
                                        </div>
                                        <p className="text-xs text-red-600 mt-2 text-center">Scan mã bằng App Ngân Hàng / ZaloPay / MoMo</p>
                                    </>
                                ) : (
                                    <p className="text-sm text-red-700 text-center">
                                        Khách hàng chưa cung cấp thông tin tài khoản ngân hàng trong hồ sơ cá nhân. Vui lòng liên hệ trực tiếp để hoàn tiền.
                                    </p>
                                )}
                            </div>
                        )}

                        <div className="space-y-4">
                            <div>
                                <label className="block text-xs font-medium text-gray-500 mb-1">Trạng thái mới</label>
                                {(() => {
                                    const options = VALID_TRANSITIONS[selectedOrder.Status] ?? [];
                                    if (options.length === 0) return <p className="text-sm text-gray-400">Không có thao tác chuyển trạng thái khả dụng.</p>;
                                    return (
                                        <select value={newStatus} onChange={e => setNewStatus(e.target.value)} className="w-full px-3 py-2 border rounded-lg text-sm cursor-pointer focus:outline-none focus:ring-2 focus:ring-[#8B1A1A]/20">
                                            {options.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
                                        </select>
                                    );
                                })()}
                            </div>
                            <div>
                                <label className="block text-xs font-medium text-gray-500 mb-1">Ghi chú (Tùy chọn)</label>
                                <textarea rows={2} value={statusNote} onChange={e => setStatusNote(e.target.value)} placeholder="Nhập thêm ghi chú với khách hàng hoặc nhân viên..." className="w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-[#8B1A1A]/20" />
                            </div>
                        </div>

                        <div className="flex items-center gap-3 mt-8">
                            <button onClick={() => { setShowUpdate(false); setUpdateResult(null); }} className="flex-1 px-4 py-2 border rounded-lg text-sm font-medium text-gray-600 hover:bg-gray-50 cursor-pointer">
                                Hủy
                            </button>
                            <button onClick={handleUpdateStatus} disabled={updating} className="flex-1 px-4 py-2 bg-[#8B1A1A] text-white rounded-lg text-sm font-bold hover:bg-[#701515] disabled:opacity-50 cursor-pointer">
                                {updating ? "Đang cập nhật..." : "Cập nhật"}
                            </button>
                        </div>

                        {updateResult && (
                            <div className={`mt-4 p-3 rounded-lg text-sm font-medium text-center ${updateResult.success ? "bg-emerald-50 text-emerald-700" : "bg-red-50 text-red-700"}`}>
                                {updateResult.message}
                            </div>
                        )}
                    </div>
                </div>
            )}

            {/* Update Delivery Modal */}
            {showDelivery && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={() => setShowDelivery(false)}>
                    <div className="bg-white rounded-2xl p-6 shadow-xl w-full max-w-md" onClick={e => e.stopPropagation()}>
                        <div className="flex items-center justify-between mb-6">
                            <h3 className="text-lg font-bold text-gray-900">Cập nhật Giao Hàng</h3>
                            <button onClick={() => setShowDelivery(false)} className="text-gray-400 hover:text-gray-600 cursor-pointer">
                                <FiX className="w-5 h-5" />
                            </button>
                        </div>
                        
                        <div className="space-y-4">
                            <div>
                                <label className="block text-xs font-medium text-gray-500 mb-1">Mã vận đơn (Delivery ID)</label>
                                <input type="text" value={deliveryId} onChange={e => setDeliveryId(e.target.value)} placeholder="GHTK... VNPOST..." className="w-full px-4 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-200 focus:border-indigo-400" />
                            </div>

                            <div>
                                <label className="block text-xs font-medium text-gray-500 mb-1">Trạng thái vận chuyển</label>
                                <select value={deliveryStatus} onChange={e => setDeliveryStatus(e.target.value)} className="w-full px-4 py-2 border rounded-lg text-sm cursor-pointer focus:outline-none focus:ring-2 focus:ring-indigo-200 focus:border-indigo-400">
                                    <option value="SHIPPING">Đang giao</option>
                                    <option value="DELIVERED">Đã giao tận nơi</option>
                                    <option value="FAILED">Giao thất bại</option>
                                    <option value="RESHIP">Yêu cầu Giao lại</option>
                                </select>
                            </div>

                            {deliveryStatus === "FAILED" && (
                                <div>
                                    <label className="block text-xs font-medium text-gray-500 mb-1">Lý do thất bại / Hoàn hàng</label>
                                    <textarea rows={2} value={failureReason} onChange={e => setFailureReason(e.target.value)} placeholder="Khách không nghe máy, sai địa chỉ..." className="w-full px-4 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-200 focus:border-red-400" />
                                </div>
                            )}
                        </div>

                        <div className="flex items-center gap-3 mt-8">
                            <button onClick={() => setShowDelivery(false)} className="flex-1 px-4 py-2 border rounded-lg text-sm font-medium text-gray-600 hover:bg-gray-50 cursor-pointer">
                                Hủy
                            </button>
                            <button onClick={handleUpdateDelivery} disabled={updatingDelivery || !deliveryId.trim()} className="flex-1 px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm font-bold hover:bg-indigo-700 disabled:opacity-50 cursor-pointer">
                                {updatingDelivery ? "Đang xử lý..." : "Cập nhật"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Order Detail Modal */}
            {showDetail && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" onClick={() => setShowDetail(false)}>
                    <div className="bg-white rounded-2xl shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden" onClick={e => e.stopPropagation()}>
                        <div className="flex items-center justify-between p-6 border-b border-gray-100">
                            <h3 className="text-lg font-bold text-gray-900">Chi tiết đơn hàng</h3>
                            <button onClick={() => setShowDetail(false)} className="text-gray-400 hover:text-gray-600 cursor-pointer">
                                <FiX className="w-5 h-5" />
                            </button>
                        </div>
                        
                        <div className="flex-1 overflow-y-auto p-6 bg-gray-50">
                            {detailLoading ? (
                                <div className="flex justify-center items-center py-20">
                                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-[#8B1A1A]"></div>
                                </div>
                            ) : !detailOrder ? (
                                <div className="text-center py-20 text-gray-500">Không thể tải thông tin đơn hàng</div>
                            ) : (
                                <div className="space-y-6">
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                        <div className="bg-white p-4 rounded-xl shadow-sm border border-gray-100">
                                            <h4 className="font-bold text-gray-900 mb-3 text-sm">Thông cấu chung</h4>
                                            <div className="space-y-2 text-sm">
                                                <div className="flex justify-between"><span className="text-gray-500">Mã đơn:</span> <span className="font-mono font-bold text-[#8B1A1A]">{detailOrder.OrderCode}</span></div>
                                                <div className="flex justify-between"><span className="text-gray-500">Trạng thái:</span> <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${getStatusInfo(detailOrder.Status).cls}`}>{getStatusInfo(detailOrder.Status).text}</span></div>
                                                <div className="flex justify-between"><span className="text-gray-500">Loại đơn:</span> <span className="font-medium text-gray-900">{detailOrder.OrderType}</span></div>
                                                <div className="flex justify-between"><span className="text-gray-500">Ngày đặt:</span> <span className="font-medium text-gray-900">{detailOrder.CreatedAt ? formatDate(detailOrder.CreatedAt) : ""}</span></div>
                                            </div>
                                        </div>
                                        <div className="bg-white p-4 rounded-xl shadow-sm border border-gray-100">
                                            <h4 className="font-bold text-gray-900 mb-3 text-sm">Khách hàng</h4>
                                            <div className="space-y-2 text-sm">
                                                <div className="flex justify-between"><span className="text-gray-500">Email:</span> <span className="font-medium text-gray-900">{detailOrder.Email}</span></div>
                                                {detailOrder.CustomerBankName && (
                                                    <div className="flex justify-between"><span className="text-gray-500">Ngân hàng:</span> <span className="font-medium text-gray-900">{detailOrder.CustomerBankName}</span></div>
                                                )}
                                                {detailOrder.CustomerBankAccount && (
                                                    <div className="flex justify-between"><span className="text-gray-500">STK:</span> <span className="font-medium text-gray-900">{detailOrder.CustomerBankAccount}</span></div>
                                                )}
                                                {detailOrder.GreetingMessage && (
                                                    <div className="flex justify-between"><span className="text-gray-500">Lời chúc:</span> <span className="font-medium text-gray-900 text-right max-w-xs truncate" title={detailOrder.GreetingMessage}>{detailOrder.GreetingMessage}</span></div>
                                                )}
                                            </div>
                                        </div>
                                    </div>
                                    
                                    <div className="bg-white p-4 rounded-xl shadow-sm border border-gray-100">
                                        <h4 className="font-bold text-gray-900 mb-4 text-sm">Sản phẩm</h4>
                                        <div className="space-y-3">
                                            {detailOrder.Items.map((item, idx) => (
                                                <div key={idx} className="flex items-center gap-3 py-2 border-b border-gray-50 last:border-0">
                                                    <div className="w-12 h-12 rounded-lg bg-gray-100 overflow-hidden flex-shrink-0">
                                                        {item.Image ? <img src={item.Image} alt={item.Name || "Product"} className="w-full h-full object-cover" /> : <div className="w-full h-full flex items-center justify-center text-gray-400 text-xs">No img</div>}
                                                    </div>
                                                    <div className="flex-1 min-w-0">
                                                        <p className="font-medium text-sm text-gray-900 truncate">{item.Name || "Sản phẩm"}</p>
                                                        <p className="text-xs text-gray-500">Loại: {item.Type}</p>
                                                    </div>
                                                    <div className="text-right">
                                                        <p className="font-bold text-sm text-gray-900">{formatPrice(item.UnitPrice ?? item.Price ?? 0)}</p>
                                                        <p className="text-xs text-gray-500">x{item.Quantity}</p>
                                                    </div>
                                                </div>
                                            ))}
                                            <div className="pt-3 flex justify-between items-center bg-gray-50 px-3 py-2 rounded-lg mt-2">
                                                <span className="font-bold text-gray-900 text-sm">Tổng cộng</span>
                                                <span className="font-bold text-[#8B1A1A] text-lg">{formatPrice(detailOrder.TotalAmount)}</span>
                                            </div>
                                        </div>
                                    </div>

                                    {detailOrder.DeliveryAddresses && detailOrder.DeliveryAddresses.length > 0 && (
                                        <div className="bg-white p-4 rounded-xl shadow-sm border border-gray-100">
                                            <h4 className="font-bold text-gray-900 mb-3 text-sm">Thông tin giao hàng</h4>
                                            <div className="space-y-3">
                                                {detailOrder.DeliveryAddresses.map((addr, idx) => (
                                                    <div key={addr.Id || idx} className="p-3 bg-gray-50 rounded-lg text-sm border border-gray-100">
                                                        <p className="font-medium text-gray-900">{addr.ReceiverName} - {addr.ReceiverPhone}</p>
                                                        <p className="text-gray-600 mt-1">{addr.FullAddress}</p>
                                                        {addr.GreetingMessage && (
                                                            <p className="text-xs text-gray-500 mt-2 bg-white px-2 py-1.5 rounded border border-gray-200 italic">"{addr.GreetingMessage}"</p>
                                                        )}
                                                    </div>
                                                ))}
                                            </div>
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
