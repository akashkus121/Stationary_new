import React, { useState, useEffect } from 'react';
import {
  X,
  Lock,
  User as UserIcon,
  Shield,
  Eye,
  EyeOff,
  KeyRound,
  Sparkles,
  CheckCircle2,
  ArrowRight,
  UserPlus,
  PenTool,
  ShieldCheck
} from 'lucide-react';
import { api } from '../services/api';
import { useAuth } from '../context/AuthContext';

interface AuthModalProps {
  isOpen: boolean;
  initialTab?: 'login' | 'register';
  onClose: () => void;
}

export const AuthModal: React.FC<AuthModalProps> = ({ isOpen, initialTab = 'login', onClose }) => {
  const { login: setAuthContext } = useAuth();
  const [isLoginTab, setIsLoginTab] = useState(initialTab === 'login');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [role, setRole] = useState<'User' | 'Admin'>('User');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setIsLoginTab(initialTab === 'login');
      setError('');
      setShowPassword(false);
    }
  }, [isOpen, initialTab]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      if (isLoginTab) {
        const res = await api.login(username, password);
        setAuthContext(res.accessToken || res.token, res.user, res.refreshToken);
        onClose();
      } else {
        const res = await api.register(username, password, role);
        setAuthContext(res.accessToken || res.token, res.user, res.refreshToken);
        onClose();
      }
    } catch (err: any) {
      setError(err.message || 'Authentication failed. Please verify your credentials.');
    } finally {
      setLoading(false);
    }
  };

  const handleQuickFill = (u: string, p: string) => {
    setUsername(u);
    setPassword(p);
    setIsLoginTab(true);
    setError('');
  };

  return (
    <div className="modal-backdrop luxury-auth-backdrop" onClick={onClose}>
      <div
        className="modal-card luxury-auth-card"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Glow Ambient Decoration */}
        <div className="auth-ambient-glow" />

        {/* Close Button */}
        <button
          className="luxury-modal-close-btn"
          onClick={onClose}
          aria-label="Close modal"
        >
          <X size={18} />
        </button>

        {/* Brand Header */}
        <div className="luxury-auth-header">
          <div className="luxury-brand-badge">
            <PenTool size={22} className="brand-badge-icon" />
          </div>
          <div className="luxury-auth-title-box">
            <span className="luxury-auth-kicker">
              <Sparkles size={13} className="kicker-sparkle" />
              {isLoginTab ? 'Executive Client Portal' : 'New Member Registration'}
            </span>
            <h2 className="luxury-auth-title">
              {isLoginTab ? 'Welcome Back' : 'Create Your Account'}
            </h2>
            <p className="luxury-auth-desc">
              {isLoginTab
                ? 'Sign in to access your curated catalog, orders & bespoke cart'
                : 'Join Lumina Atelier for premium executive stationery & workspace solutions'}
            </p>
          </div>
        </div>

        {/* Segmented Tab Controls */}
        <div className="luxury-auth-tab-segment">
          <button
            type="button"
            className={`luxury-tab-btn ${isLoginTab ? 'active' : ''}`}
            onClick={() => {
              setIsLoginTab(true);
              setError('');
            }}
          >
            <KeyRound size={15} />
            <span>Sign In</span>
          </button>
          <button
            type="button"
            className={`luxury-tab-btn ${!isLoginTab ? 'active' : ''}`}
            onClick={() => {
              setIsLoginTab(false);
              setError('');
            }}
          >
            <UserPlus size={15} />
            <span>Create Account</span>
          </button>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="luxury-auth-alert">
            <span>{error}</span>
          </div>
        )}

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="luxury-auth-form">
          <div className="luxury-form-group">
            <label className="luxury-form-label">Username</label>
            <div className="luxury-input-wrapper">
              <UserIcon size={17} className="luxury-input-icon" />
              <input
                type="text"
                className="luxury-form-input"
                placeholder="Enter your username (e.g. test)"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                autoComplete="username"
                required
              />
            </div>
          </div>

          <div className="luxury-form-group">
            <div className="luxury-label-split">
              <label className="luxury-form-label">Password</label>
              {isLoginTab && <span className="luxury-label-hint">Min. 4 characters</span>}
            </div>
            <div className="luxury-input-wrapper">
              <Lock size={17} className="luxury-input-icon" />
              <input
                type={showPassword ? 'text' : 'password'}
                className="luxury-form-input password-field"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete={isLoginTab ? 'current-password' : 'new-password'}
                required
              />
              <button
                type="button"
                className="luxury-eye-toggle-btn"
                onClick={() => setShowPassword(!showPassword)}
                title={showPassword ? 'Hide password' : 'Show password'}
                aria-label="Toggle password visibility"
              >
                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          {!isLoginTab && (
            <div className="luxury-form-group">
              <label className="luxury-form-label">Select Account Role</label>
              <div className="luxury-role-grid">
                <button
                  type="button"
                  className={`luxury-role-card ${role === 'User' ? 'active' : ''}`}
                  onClick={() => setRole('User')}
                >
                  <div className="role-card-header">
                    <UserIcon size={18} className="role-icon" />
                    {role === 'User' && <CheckCircle2 size={16} className="role-check-active" />}
                  </div>
                  <div className="role-card-body">
                    <strong className="role-name">Executive Shopper</strong>
                    <span className="role-desc">Storefront catalog, purchasing & order tracking</span>
                  </div>
                </button>

                <button
                  type="button"
                  className={`luxury-role-card ${role === 'Admin' ? 'active' : ''}`}
                  onClick={() => setRole('Admin')}
                >
                  <div className="role-card-header">
                    <Shield size={18} className="role-icon" />
                    {role === 'Admin' && <CheckCircle2 size={16} className="role-check-active" />}
                  </div>
                  <div className="role-card-body">
                    <strong className="role-name">Administrator</strong>
                    <span className="role-desc">Inventory management, CSV import & sales reports</span>
                  </div>
                </button>
              </div>
            </div>
          )}

          <button
            type="submit"
            className="btn-luxury-auth-submit"
            disabled={loading}
          >
            {loading ? (
              <span className="btn-loading-text">Authenticating...</span>
            ) : isLoginTab ? (
              <>
                <span>Sign In to Lumina Atelier</span>
                <ArrowRight size={17} />
              </>
            ) : (
              <>
                <span>Complete Registration</span>
                <Sparkles size={17} />
              </>
            )}
          </button>
        </form>

        {/* Demo Credentials Footer */}
        <div className="luxury-auth-footer">
          <div className="auth-security-badge">
            <ShieldCheck size={14} className="security-icon" />
            <span>256-Bit Encrypted Secure Authentication</span>
          </div>

          <div className="luxury-demo-chip-bar">
            <span className="demo-chip-label">Quick Demo Access:</span>
            <button
              type="button"
              className="luxury-demo-chip"
              onClick={() => handleQuickFill('test', '12345')}
              title="Click to fill Test User credentials"
            >
              <span>Test User</span>
              <span className="demo-chip-divider">•</span>
              <strong>12345</strong>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

