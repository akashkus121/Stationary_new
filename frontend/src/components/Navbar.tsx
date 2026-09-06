import React from 'react';
import { User as UserIcon, ShieldAlert, LogOut, PenTool, Store } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

interface NavbarProps {
  onOpenAuth: (tab?: 'login' | 'register') => void;
  onOpenMyOrders?: () => void;
  onOpenProfile?: () => void;
  activeTab?: 'catalog' | 'admin';
  setActiveTab: (tab: 'catalog' | 'admin') => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onOpenAuth, onOpenProfile, activeTab = 'catalog', setActiveTab }) => {
  const { user, isAdmin, logout } = useAuth();

  return (
    <header className="navbar-container">
      <div className="navbar-inner">
        {/* Brand Logo & Name */}
        <div className="navbar-brand clickable" onClick={() => setActiveTab('catalog')}>
          <div className="brand-icon-box">
            <PenTool size={22} className="brand-icon" />
          </div>
          <div className="brand-text-container">
            <h1 className="brand-title">Lumina<span className="brand-accent">Atelier</span></h1>
            <p className="brand-subtitle">Executive Stationery & Workspace</p>
          </div>
        </div>

        {/* Center Nav Links (Admin vs Storefront Switcher) */}
        {isAdmin && (
          <nav className="navbar-nav">
            <div className="admin-nav-switcher">
              <button
                type="button"
                className={`nav-switch-btn ${activeTab === 'catalog' ? 'active' : ''}`}
                onClick={() => setActiveTab('catalog')}
                title="View customer storefront"
              >
                <Store size={16} />
                <span>Storefront</span>
              </button>
              <button
                type="button"
                className={`nav-switch-btn ${activeTab === 'admin' ? 'active' : ''}`}
                onClick={() => setActiveTab('admin')}
                title="Open Admin Management Suite"
              >
                <ShieldAlert size={16} />
                <span>Admin Suite</span>
              </button>
            </div>
          </nav>
        )}

        {/* Right Actions */}
        <div className="navbar-actions">
          {/* User Profile & Logout */}
          {user ? (
            <div className="user-profile-menu">
              <button
                type="button"
                className="user-avatar-badge clickable"
                onClick={() => (!isAdmin && onOpenProfile ? onOpenProfile() : null)}
                title={!isAdmin ? "View Profile & Settings" : "Admin Profile"}
              >
                <div className="nav-avatar-circle">
                  {(user.username || 'U').charAt(0).toUpperCase()}
                </div>
                <span className="username-text">{user.username || 'User'}</span>
                <span className={`role-tag ${(user.role || 'User').toLowerCase()}`}>{user.role || 'User'}</span>
              </button>

              <button className="logout-icon-btn" onClick={logout} title="Sign Out">
                <LogOut size={16} />
              </button>
            </div>
          ) : (
            <button className="auth-login-btn" onClick={() => onOpenAuth('login')}>
              <UserIcon size={16} />
              <span>Sign In / Register</span>
            </button>
          )}
        </div>
      </div>
    </header>
  );
};

