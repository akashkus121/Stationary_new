import React, { useState, useEffect, useCallback } from 'react';
import {
  X,
  ShoppingBag,
  Calendar,
  CreditCard,
  RefreshCw,
  PackageCheck,
  Search,
  ChevronDown,
  ChevronUp,
  Tag,
  CheckCircle2,
  Clock,
  Sparkles,
  ArrowUpRight
} from 'lucide-react';
import type { Order } from '../types';
import { api } from '../services/api';

interface MyOrdersModalProps {
  isOpen: boolean;
  onClose: () => void;
  onBrowseStore?: () => void;
}

export const MyOrdersModal: React.FC<MyOrdersModalProps> = ({ isOpen, onClose, onBrowseStore }) => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [expandedOrders, setExpandedOrders] = useState<Record<number, boolean>>({});

  const fetchOrders = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await api.getMyOrders();
      setOrders(Array.isArray(data) ? data : []);
    } catch (err: any) {
      setError(err.message || 'Failed to load order history.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isOpen) {
      fetchOrders();
      setSearchQuery('');
    }
  }, [isOpen, fetchOrders]);

  if (!isOpen) return null;

  const toggleExpand = (orderId: number) => {
    setExpandedOrders((prev) => ({
      ...prev,
      [orderId]: !prev[orderId],
    }));
  };

  const filteredOrders = orders.filter((order) => {
    if (!searchQuery.trim()) return true;
    const q = searchQuery.toLowerCase();
    const matchesId = order.id.toString().includes(q);
    const matchesPayment = order.paymentMethod?.toLowerCase().includes(q);
    const matchesItems = order.items?.some((item) =>
      item.productName.toLowerCase().includes(q)
    );
    return matchesId || matchesPayment || matchesItems;
  });

  const totalSpent = orders.reduce((acc, curr) => acc + (curr.totalAmount || 0), 0);

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card orders-modal" onClick={(e) => e.stopPropagation()}>
        {/* Modal Header */}
        <div className="modal-header">
          <div className="orders-header-title-box">
            <div className="orders-header-icon-badge">
              <PackageCheck size={20} className="modal-header-icon" />
            </div>
            <div>
              <div className="orders-title-row">
                <h2 className="modal-title">My Purchase History</h2>
                <span className="orders-count-pill">{orders.length} Orders</span>
              </div>
              <p className="modal-subtitle">
                Track your executive stationery orders, receipts, and order statuses
              </p>
            </div>
          </div>
          <div className="orders-header-actions">
            <button
              type="button"
              className="orders-refresh-btn"
              onClick={fetchOrders}
              disabled={loading}
              title="Refresh order history"
            >
              <RefreshCw size={15} className={loading ? 'spin-icon' : ''} />
            </button>
            <button className="modal-close-btn" onClick={onClose} aria-label="Close purchase history">
              <X size={20} />
            </button>
          </div>
        </div>

        {/* Quick Stats Bar */}
        {orders.length > 0 && (
          <div className="orders-quick-stats-bar">
            <div className="orders-stat-item">
              <span className="stat-label">Total Orders</span>
              <strong className="stat-val">{orders.length}</strong>
            </div>
            <div className="orders-stat-divider" />
            <div className="orders-stat-item">
              <span className="stat-label">Total Lifetime Spend</span>
              <strong className="stat-val stat-spend">Rs. {totalSpent.toFixed(2)}</strong>
            </div>
            <div className="orders-stat-divider" />
            <div className="orders-stat-item">
              <span className="stat-label">Account Tier</span>
              <span className="stat-badge">
                <Sparkles size={12} /> Executive Client
              </span>
            </div>
          </div>
        )}

        {/* Search Bar */}
        {orders.length > 3 && (
          <div className="orders-search-wrapper">
            <Search size={16} className="orders-search-icon" />
            <input
              type="text"
              className="orders-search-input"
              placeholder="Search by order #, product name, payment..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            {searchQuery && (
              <button
                type="button"
                className="orders-search-clear"
                onClick={() => setSearchQuery('')}
              >
                <X size={14} />
              </button>
            )}
          </div>
        )}

        {error && <div className="alert-box alert-error">{error}</div>}

        {/* Orders Body */}
        <div className="orders-modal-body">
          {loading && orders.length === 0 ? (
            <div className="orders-loading-state">
              <RefreshCw size={36} className="spin-icon text-accent" />
              <p>Retrieving your order portfolio...</p>
            </div>
          ) : orders.length === 0 ? (
            <div className="orders-empty-state">
              <div className="empty-state-icon-box">
                <ShoppingBag size={48} className="empty-icon" />
              </div>
              <h3>No Purchase History Yet</h3>
              <p>You haven't placed any stationery orders yet. Browse our executive storefront collection to get started.</p>
              {onBrowseStore && (
                <button
                  type="button"
                  className="btn btn-primary orders-browse-btn"
                  onClick={() => {
                    onClose();
                    onBrowseStore();
                  }}
                >
                  <span>Explore Stationery Storefront</span>
                  <ArrowUpRight size={16} />
                </button>
              )}
            </div>
          ) : filteredOrders.length === 0 ? (
            <div className="orders-empty-state">
              <Search size={40} className="empty-icon" />
              <h3>No matching orders found</h3>
              <p>No orders matched your search query "{searchQuery}".</p>
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                onClick={() => setSearchQuery('')}
              >
                Clear Filter
              </button>
            </div>
          ) : (
            <div className="orders-cards-list">
              {filteredOrders.map((order) => {
                const isExpanded = expandedOrders[order.id] !== false; // expanded by default
                const itemsCount = order.items?.reduce((acc, i) => acc + i.quantity, 0) || 0;
                const formattedDate = new Date(order.date).toLocaleDateString('en-US', {
                  year: 'numeric',
                  month: 'short',
                  day: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit',
                });

                return (
                  <div key={order.id} className="order-history-card">
                    {/* Card Top Header */}
                    <div className="order-card-header">
                      <div className="order-main-identifiers">
                        <span className="order-ref-pill">#{order.id}</span>
                        <div className="order-meta-group">
                          <span className="order-date-text">
                            <Calendar size={13} />
                            {formattedDate}
                          </span>
                          <span className="order-items-count-text">
                            <Tag size={12} />
                            {itemsCount} {itemsCount === 1 ? 'item' : 'items'}
                          </span>
                        </div>
                      </div>

                      <div className="order-header-right">
                        <div className="order-payment-badge">
                          <CreditCard size={13} />
                          <span>{order.paymentMethod?.toUpperCase() || 'CASH'}</span>
                        </div>
                        <span className="order-status-badge">
                          <CheckCircle2 size={13} /> Completed
                        </span>
                      </div>
                    </div>

                    {/* Collapsible Items List */}
                    {isExpanded && (
                      <div className="order-items-detail-box">
                        <div className="order-items-detail-header">
                          <span>Purchased Products</span>
                          <span>Line Total</span>
                        </div>
                        <div className="order-items-scroll-list">
                          {order.items && order.items.length > 0 ? (
                            order.items.map((item, idx) => (
                              <div key={idx} className="order-detail-row">
                                <div className="order-detail-info">
                                  <span className="order-detail-name">{item.productName}</span>
                                  <span className="order-detail-qty">
                                    Qty: <strong>{item.quantity}</strong> × Rs. {Number(item.price).toFixed(2)}
                                  </span>
                                </div>
                                <span className="order-detail-price">
                                  Rs. {(item.price * item.quantity).toFixed(2)}
                                </span>
                              </div>
                            ))
                          ) : (
                            <div className="order-detail-row">
                              <span className="text-muted">General Stationery Item</span>
                              <span className="order-detail-price">Rs. {order.totalAmount.toFixed(2)}</span>
                            </div>
                          )}
                        </div>
                      </div>
                    )}

                    {/* Card Footer */}
                    <div className="order-card-footer">
                      <button
                        type="button"
                        className="order-expand-toggle-btn"
                        onClick={() => toggleExpand(order.id)}
                      >
                        {isExpanded ? (
                          <>
                            <span>Hide Details</span>
                            <ChevronUp size={14} />
                          </>
                        ) : (
                          <>
                            <span>Show Details ({order.items?.length || 1} items)</span>
                            <ChevronDown size={14} />
                          </>
                        )}
                      </button>

                      <div className="order-total-price-group">
                        <span className="order-total-label">Total Amount Paid:</span>
                        <span className="order-total-amount">
                          Rs. {Number(order.totalAmount).toFixed(2)}
                        </span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
