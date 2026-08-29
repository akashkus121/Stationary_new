import React from 'react';
import { User as UserIcon, ShieldAlert, LogOut, PenTool } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

interface NavbarProps {
  onOpenAuth: (tab?: 'login' | 'register') => void;
  onOpenMyOrders?: () => void;
  onOpenProfile?: () => void;
  activeTab?: 'catalog' | 'admin';
  setActiveTab: (tab: 'catalog' | 'admin') => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onOpenAuth, onOpenProfile, setActiveTab }) => {
  const { user, isAdmin, logout } = useAuth();

  return (
    <header className="navbar-container">
      <div className="navbar-inner">
        {/* Brand Logo & Name */}
        <div className="navbar-brand" onClick={() => setActiveTab(isAdmin ? 'admin' : 'catalog')}>
          <div className="brand-icon-box">
            <PenTool size={22} className="brand-icon" />
          </div>
          <div className="brand-text-container">
            <h1 className="brand-title">Lumina<span className="brand-accent">Atelier</span></h1>
            <p className="brand-subtitle">Executive Stationery & Workspace</p>
          </div>
        </div>

        {/* Center Nav Links (Admin Indicator Only) */}
        {isAdmin && (
          <nav className="navbar-nav">
            <div className="admin-active-suite-tag">
              <ShieldAlert size={17} />
              <span>Admin Management Suite</span>
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

