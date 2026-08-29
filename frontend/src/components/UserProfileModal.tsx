import React, { useState, useEffect } from 'react';
import {
  X,
  User as UserIcon,
  ShoppingBag,
  PackageCheck,
  CreditCard,
  MapPin,
  Phone,
  Mail,
  Shield,
  Sparkles,
  Calendar,
  LogOut,
  Check,
  Edit3,
  Save,
  Bell,
  ExternalLink,
  Lock,
  Building,
  CheckCircle2,
  Clock
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { api } from '../services/api';
import type { Order } from '../types';

interface UserProfileModalProps {
  isOpen: boolean;
  onClose: () => void;
  onOpenMyOrders: () => void;
  onOpenCart: () => void;
}

export const UserProfileModal: React.FC<UserProfileModalProps> = ({
  isOpen,
  onClose,
  onOpenMyOrders,
  onOpenCart,
}) => {
  const { user, logout } = useAuth();
  const { itemCount } = useCart();
  const [activeTab, setActiveTab] = useState<'overview' | 'delivery' | 'settings'>('overview');
  const [orders, setOrders] = useState<Order[]>([]);
  const [loadingOrders, setLoadingOrders] = useState(false);

  // Delivery Profile State (saved in localStorage for persistence)
  const [fullName, setFullName] = useState(() => localStorage.getItem('profile_fullName') || user?.username || 'Valued Client');
  const [phone, setPhone] = useState(() => localStorage.getItem('profile_phone') || '+1 (555) 234-5678');
  const [email, setEmail] = useState(() => localStorage.getItem('profile_email') || `${user?.username || 'user'}@lumina-atelier.com`);
  const [deskLocation, setDeskLocation] = useState(() => localStorage.getItem('profile_deskLocation') || 'Executive Suite 4B, Tower A');
  const [deliveryNotes, setDeliveryNotes] = useState(() => localStorage.getItem('profile_deliveryNotes') || 'Leave on reception desk if away');
  const [isEditingAddress, setIsEditingAddress] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);

  // Notifications State
  const [notifyOrderUpdates, setNotifyOrderUpdates] = useState(true);
  const [notifyRestock, setNotifyRestock] = useState(true);

  useEffect(() => {
    if (isOpen && user) {
      const fetchOrders = async () => {
        setLoadingOrders(true);
        try {
          const data = await api.getMyOrders();
          setOrders(Array.isArray(data) ? data : []);
        } catch {
          setOrders([]);
        } finally {
          setLoadingOrders(false);
        }
      };
      fetchOrders();
    }
  }, [isOpen, user]);

  if (!isOpen || !user) return null;

  const totalSpent = orders.reduce((acc, curr) => acc + (curr.totalAmount || 0), 0);
  const userInitial = (user.username || 'U').charAt(0).toUpperCase();

  const handleSaveDeliveryInfo = (e: React.FormEvent) => {
    e.preventDefault();
    localStorage.setItem('profile_fullName', fullName);
    localStorage.setItem('profile_phone', phone);
    localStorage.setItem('profile_email', email);
    localStorage.setItem('profile_deskLocation', deskLocation);
    localStorage.setItem('profile_deliveryNotes', deliveryNotes);
    setIsEditingAddress(false);
    setSaveSuccess(true);
    setTimeout(() => setSaveSuccess(false), 2500);
  };

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card user-profile-modal" onClick={(e) => e.stopPropagation()}>
        {/* Cover Header */}
        <div className="profile-cover-banner">
          <div className="profile-cover-gradient" />
          <button className="profile-modal-close-btn" onClick={onClose} aria-label="Close Profile">
            <X size={18} />
          </button>
        </div>

        {/* Avatar & Main Identity Row */}
        <div className="profile-identity-section">
          <div className="profile-avatar-wrapper">
            <div className="profile-avatar-circle">
              <span>{userInitial}</span>
            </div>
            <div className="profile-online-badge" title="Account Active" />
          </div>

          <div className="profile-identity-info">
            <div className="profile-name-row">
              <h2 className="profile-display-name">{user.username}</h2>
              <span className="profile-role-pill">
                <Shield size={12} /> {user.role || 'Member'}
              </span>
            </div>
            <p className="profile-tier-text">
              <Sparkles size={13} className="sparkle-icon" /> Executive Stationery Client • ID #{user.id}
            </p>
          </div>

          <div className="profile-header-actions">
            <button
              type="button"
              className="btn btn-secondary btn-sm profile-signout-btn"
              onClick={() => {
                onClose();
                logout();
              }}
              title="Sign out of your account"
            >
              <LogOut size={14} />
              <span>Sign Out</span>
            </button>
          </div>
        </div>

        {/* Bento Stats Row */}
        <div className="profile-bento-grid">
          <div className="bento-card" onClick={() => { onClose(); onOpenMyOrders(); }} title="View your orders">
            <div className="bento-icon-wrapper orders">
              <PackageCheck size={18} />
            </div>
            <div className="bento-data">
              <span className="bento-label">Orders Completed</span>
              <strong className="bento-val">{loadingOrders ? '...' : orders.length}</strong>
            </div>
            <ExternalLink size={13} className="bento-hover-link" />
          </div>

          <div className="bento-card">
            <div className="bento-icon-wrapper spend">
              <CreditCard size={18} />
            </div>
            <div className="bento-data">
              <span className="bento-label">Lifetime Spend</span>
              <strong className="bento-val spend-val">Rs. {totalSpent.toFixed(2)}</strong>
            </div>
          </div>

          <div className="bento-card" onClick={() => { onClose(); onOpenCart(); }} title="View your shopping cart">
            <div className="bento-icon-wrapper cart">
              <ShoppingBag size={18} />
            </div>
            <div className="bento-data">
              <span className="bento-label">In Your Cart</span>
              <strong className="bento-val">{itemCount} {itemCount === 1 ? 'item' : 'items'}</strong>
            </div>
            <ExternalLink size={13} className="bento-hover-link" />
          </div>

          <div className="bento-card">
            <div className="bento-icon-wrapper status">
              <CheckCircle2 size={18} />
            </div>
            <div className="bento-data">
              <span className="bento-label">Account Status</span>
              <strong className="bento-val status-val">Verified & Active</strong>
            </div>
          </div>
        </div>

        {/* Tab Navigation */}
        <div className="profile-tabs-nav">
          <button
            type="button"
            className={`profile-tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
            onClick={() => setActiveTab('overview')}
          >
            <span>Overview & Activity</span>
          </button>

          <button
            type="button"
            className={`profile-tab-btn ${activeTab === 'delivery' ? 'active' : ''}`}
            onClick={() => setActiveTab('delivery')}
          >
            <span>Delivery & Desk Location</span>
          </button>

          <button
            type="button"
            className={`profile-tab-btn ${activeTab === 'settings' ? 'active' : ''}`}
            onClick={() => setActiveTab('settings')}
          >
            <span>Preferences & Security</span>
          </button>
        </div>

        {/* Tab Content Body */}
        <div className="profile-tab-content-body">
          {/* TAB 1: OVERVIEW */}
          {activeTab === 'overview' && (
            <div className="profile-overview-panel">
              {/* Recent Orders Section */}
              <div className="profile-section-card">
                <div className="profile-section-header">
                  <div className="header-left">
                    <PackageCheck size={16} className="text-accent" />
                    <h4>Recent Purchases</h4>
                  </div>
                  {orders.length > 0 && (
                    <button
                      type="button"
                      className="btn-link-action"
                      onClick={() => {
                        onClose();
                        onOpenMyOrders();
                      }}
                    >
                      <span>View All ({orders.length})</span>
                      <ExternalLink size={12} />
                    </button>
                  )}
                </div>

                {orders.length === 0 ? (
                  <div className="profile-empty-orders">
                    <ShoppingBag size={32} className="text-muted" />
                    <p>No past orders recorded yet.</p>
                  </div>
                ) : (
                  <div className="profile-recent-orders-list">
                    {orders.slice(0, 3).map((order) => (
                      <div key={order.id} className="profile-recent-order-item">
                        <div className="order-item-left">
                          <span className="order-pill">#{order.id}</span>
                          <span className="order-date-mini">
                            <Clock size={12} /> {new Date(order.date).toLocaleDateString()}
                          </span>
                        </div>
                        <div className="order-item-middle">
                          <span className="order-items-snippet">
                            {order.items?.length || 1} {order.items?.length === 1 ? 'item' : 'items'}
                          </span>
                        </div>
                        <div className="order-item-right">
                          <span className="order-payment-pill">{order.paymentMethod?.toUpperCase()}</span>
                          <strong className="order-amount-mini">Rs. {order.totalAmount.toFixed(2)}</strong>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Quick Actions Card */}
              <div className="profile-quick-actions-card">
                <h4>Quick Portfolio Shortcuts</h4>
                <div className="quick-actions-grid">
                  <button
                    type="button"
                    className="quick-action-btn"
                    onClick={() => {
                      onClose();
                      onOpenMyOrders();
                    }}
                  >
                    <PackageCheck size={18} />
                    <div>
                      <strong>Order History</strong>
                      <p>View all invoices & receipts</p>
                    </div>
                  </button>

                  <button
                    type="button"
                    className="quick-action-btn"
                    onClick={() => {
                      onClose();
                      onOpenCart();
                    }}
                  >
                    <ShoppingBag size={18} />
                    <div>
                      <strong>Shopping Cart</strong>
                      <p>{itemCount} items ready for checkout</p>
                    </div>
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* TAB 2: DELIVERY & DESK LOCATION */}
          {activeTab === 'delivery' && (
            <div className="profile-delivery-panel">
              {saveSuccess && (
                <div className="alert-box alert-success">
                  <Check size={16} />
                  <span>Delivery details saved successfully!</span>
                </div>
              )}

              <form onSubmit={handleSaveDeliveryInfo} className="delivery-form">
                <div className="form-header-bar">
                  <h4>Default Office & Delivery Destination</h4>
                  <button
                    type="button"
                    className="btn btn-secondary btn-sm"
                    onClick={() => setIsEditingAddress(!isEditingAddress)}
                  >
                    <Edit3 size={13} />
                    <span>{isEditingAddress ? 'Cancel' : 'Edit Details'}</span>
                  </button>
                </div>

                <div className="form-grid-2">
                  <div className="form-group">
                    <label className="form-label">Full Name / Attention To</label>
                    <div className="input-with-icon">
                      <UserIcon size={16} className="input-icon" />
                      <input
                        type="text"
                        className="form-input"
                        value={fullName}
                        onChange={(e) => setFullName(e.target.value)}
                        disabled={!isEditingAddress}
                        required
                      />
                    </div>
                  </div>

                  <div className="form-group">
                    <label className="form-label">Contact Phone</label>
                    <div className="input-with-icon">
                      <Phone size={16} className="input-icon" />
                      <input
                        type="text"
                        className="form-input"
                        value={phone}
                        onChange={(e) => setPhone(e.target.value)}
                        disabled={!isEditingAddress}
                      />
                    </div>
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Corporate Email</label>
                  <div className="input-with-icon">
                    <Mail size={16} className="input-icon" />
                    <input
                      type="email"
                      className="form-input"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      disabled={!isEditingAddress}
                      required
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Desk / Office Location / Floor</label>
                  <div className="input-with-icon">
                    <Building size={16} className="input-icon" />
                    <input
                      type="text"
                      className="form-input"
                      value={deskLocation}
                      onChange={(e) => setDeskLocation(e.target.value)}
                      disabled={!isEditingAddress}
                      placeholder="e.g. Building 3, Floor 4, Suite 412"
                      required
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label className="form-label">Special Delivery Instructions</label>
                  <textarea
                    className="form-input form-textarea"
                    rows={2}
                    value={deliveryNotes}
                    onChange={(e) => setDeliveryNotes(e.target.value)}
                    disabled={!isEditingAddress}
                    placeholder="e.g. Leave with floor manager if unavailable"
                  />
                </div>

                {isEditingAddress && (
                  <button type="submit" className="btn btn-primary btn-full save-delivery-btn">
                    <Save size={16} />
                    <span>Save Delivery Preferences</span>
                  </button>
                )}
              </form>
            </div>
          )}

          {/* TAB 3: SETTINGS & SECURITY */}
          {activeTab === 'settings' && (
            <div className="profile-settings-panel">
              {/* Notification Preferences */}
              <div className="settings-group-card">
                <div className="settings-header">
                  <Bell size={16} className="text-accent" />
                  <h4>Notification Preferences</h4>
                </div>

                <div className="settings-toggle-row">
                  <div className="toggle-text">
                    <strong>Order Status Updates</strong>
                    <p>Receive notifications when stationery orders are packed and out for delivery</p>
                  </div>
                  <input
                    type="checkbox"
                    className="custom-toggle"
                    checked={notifyOrderUpdates}
                    onChange={(e) => setNotifyOrderUpdates(e.target.checked)}
                  />
                </div>

                <div className="settings-toggle-row">
                  <div className="toggle-text">
                    <strong>Inventory Restock Alerts</strong>
                    <p>Get notified when out-of-stock executive pens & notebooks are replenished</p>
                  </div>
                  <input
                    type="checkbox"
                    className="custom-toggle"
                    checked={notifyRestock}
                    onChange={(e) => setNotifyRestock(e.target.checked)}
                  />
                </div>
              </div>

              {/* Security Details */}
              <div className="settings-group-card">
                <div className="settings-header">
                  <Lock size={16} className="text-accent" />
                  <h4>Account Security & Session</h4>
                </div>

                <div className="security-item-row">
                  <div>
                    <strong>JWT Bearer Authentication</strong>
                    <p>Encrypted session with automatic token refresh</p>
                  </div>
                  <span className="badge badge-success">
                    <Check size={12} /> Active
                  </span>
                </div>

                <div className="security-item-row">
                  <div>
                    <strong>Assigned Account Role</strong>
                    <p>Standard Customer & Storefront Privileges</p>
                  </div>
                  <span className="badge badge-info">
                    <Shield size={12} /> {user.role || 'User'}
                  </span>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
