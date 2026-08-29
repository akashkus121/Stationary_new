import { useState, useEffect } from 'react';
import { ShoppingCart, PackageCheck } from 'lucide-react';
import { AuthProvider, useAuth } from './context/AuthContext';
import { CartProvider, useCart } from './context/CartContext';
import { Navbar } from './components/Navbar';
import { CatalogPage } from './pages/CatalogPage';
import { AdminDashboard } from './pages/AdminDashboard';
import { AuthModal } from './components/AuthModal';
import { CartDrawer } from './components/CartDrawer';
import { CheckoutModal } from './components/CheckoutModal';
import { MyOrdersModal } from './components/MyOrdersModal';
import { UserProfileModal } from './components/UserProfileModal';
import './index.css';

function MainApp() {
  const { user, isAdmin } = useAuth();
  const { itemCount, setIsCartOpen } = useCart();
  const [activeTab, setActiveTab] = useState<'catalog' | 'admin'>('catalog');
  const [isAuthOpen, setIsAuthOpen] = useState(false);
  const [authInitialTab, setAuthInitialTab] = useState<'login' | 'register'>('login');
  const [isCheckoutOpen, setIsCheckoutOpen] = useState(false);
  const [isMyOrdersOpen, setIsMyOrdersOpen] = useState(false);
  const [isProfileOpen, setIsProfileOpen] = useState(false);

  const handleOpenAuth = (tab: 'login' | 'register' = 'login') => {
    setAuthInitialTab(tab);
    setIsAuthOpen(true);
  };

  // Auto-switch to admin suite when logging in as Admin
  useEffect(() => {
    if (isAdmin) {
      setActiveTab('admin');
    }
  }, [isAdmin]);

  return (
    <div className="app-layout">
      {/* Navigation Header */}
      <Navbar
        onOpenAuth={handleOpenAuth}
        onOpenMyOrders={() => {
          if (!user) handleOpenAuth('login');
          else setIsMyOrdersOpen(true);
        }}
        onOpenProfile={() => {
          if (!user) handleOpenAuth('login');
          else setIsProfileOpen(true);
        }}
        activeTab={activeTab}
        setActiveTab={(tab) => {
          if (tab === 'admin' && !isAdmin) {
            handleOpenAuth('login');
            return;
          }
          setActiveTab(tab);
        }}
      />

      {/* Page Content */}
      <main className="main-content">
        {activeTab === 'catalog' ? (
          <CatalogPage
            onOpenAuth={handleOpenAuth}
            onOpenMyOrders={() => {
              if (!user) handleOpenAuth('login');
              else setIsMyOrdersOpen(true);
            }}
          />
        ) : isAdmin ? (
          <AdminDashboard />
        ) : (
          <div className="alert-box alert-error">
            <span>Admin authorization required to access this area.</span>
          </div>
        )}
      </main>

      {/* Global Modals & Drawers */}
      <AuthModal
        isOpen={isAuthOpen}
        initialTab={authInitialTab}
        onClose={() => setIsAuthOpen(false)}
      />
      <CartDrawer onOpenCheckout={() => setIsCheckoutOpen(true)} />
      <CheckoutModal
        isOpen={isCheckoutOpen}
        onClose={() => setIsCheckoutOpen(false)}
        onOrderComplete={() => {
          setIsCheckoutOpen(false);
          setActiveTab('catalog');
        }}
        onViewMyOrders={() => {
          setIsCheckoutOpen(false);
          setIsMyOrdersOpen(true);
        }}
      />
      <MyOrdersModal
        isOpen={isMyOrdersOpen}
        onClose={() => setIsMyOrdersOpen(false)}
        onBrowseStore={() => {
          setIsMyOrdersOpen(false);
          setActiveTab('catalog');
        }}
      />
      <UserProfileModal
        isOpen={isProfileOpen}
        onClose={() => setIsProfileOpen(false)}
        onOpenMyOrders={() => setIsMyOrdersOpen(true)}
        onOpenCart={() => setIsCartOpen(true)}
      />

      {/* Floating Action Buttons: Cart on Bottom-Right, My Orders on Bottom-Left */}
      {!isAdmin && (
        <div className="floating-bottom-actions-container">
          {/* Bottom Left: Floating My Orders Button */}
          <button
            type="button"
            className="floating-fab floating-my-orders-btn"
            onClick={() => {
              if (!user) handleOpenAuth('login');
              else setIsMyOrdersOpen(true);
            }}
            title="View Purchase History & Orders"
            aria-label="My Orders"
          >
            <PackageCheck size={20} className="fab-icon" />
            <span className="fab-label">My Orders</span>
          </button>

          {/* Bottom Right: Floating Cart Button */}
          <button
            type="button"
            className="floating-fab floating-cart-btn"
            onClick={() => setIsCartOpen(true)}
            title="View Shopping Cart"
            aria-label="Shopping Cart"
          >
            <div className="fab-icon-wrapper">
              <ShoppingCart size={22} className="fab-icon" />
              {itemCount > 0 && <span className="floating-cart-badge">{itemCount}</span>}
            </div>
            <span className="fab-label">Cart</span>
            {itemCount > 0 && <span className="fab-count-pill">{itemCount} items</span>}
          </button>
        </div>
      )}
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <CartProvider>
        <MainApp />
      </CartProvider>
    </AuthProvider>
  );
}
