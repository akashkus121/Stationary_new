import React, { useState, useEffect } from 'react';
import { X, Lock, User as UserIcon, Shield, Eye, EyeOff, KeyRound, Sparkles, CheckCircle2, ArrowRight } from 'lucide-react';
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
      setError(err.message || 'An error occurred. Please try again.');
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
    <div className="modal-backdrop" onClick={onClose}>
      <div
        className="modal-card auth-modal responsive-auth-card"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="modal-header auth-modal-header">
          <div className="modal-title-group">
            <div className="auth-header-pill">
              <Sparkles size={13} className="sparkle-icon" />
              <span>{isLoginTab ? 'Secure Portal' : 'New Member Registration'}</span>
            </div>
            <h2 className="modal-title">{isLoginTab ? 'Welcome Back' : 'Create Account'}</h2>
            <p className="modal-subtitle">
              {isLoginTab
                ? 'Sign in to access your cart, order history, and executive catalog'
                : 'Join Lumina Atelier as an Executive Client or Administrator'}
            </p>
          </div>
          <button className="modal-close-btn" onClick={onClose} aria-label="Close authentication modal">
            <X size={18} />
          </button>
        </div>

        {/* Tab Toggle */}
        <div className="auth-tabs">
          <button
            type="button"
            className={`auth-tab ${isLoginTab ? 'active' : ''}`}
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
            className={`auth-tab ${!isLoginTab ? 'active' : ''}`}
            onClick={() => {
              setIsLoginTab(false);
              setError('');
            }}
          >
            <UserIcon size={15} />
            <span>Register</span>
          </button>
        </div>

        {error && <div className="alert-box alert-error">{error}</div>}

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label className="form-label">Username</label>
            <div className="input-with-icon">
              <UserIcon size={17} className="input-icon" />
              <input
                type="text"
                className="form-input"
                placeholder="Enter your username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                autoComplete="username"
                required
              />
            </div>
          </div>

          <div className="form-group">
            <div className="label-row-flex">
              <label className="form-label">Password</label>
              {isLoginTab && (
                <span className="form-hint-text">Minimum 4 characters</span>
              )}
            </div>
            <div className="input-with-icon">
              <Lock size={17} className="input-icon" />
              <input
                type={showPassword ? 'text' : 'password'}
                className="form-input"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete={isLoginTab ? "current-password" : "new-password"}
                required
              />
              <button
                type="button"
                className="password-toggle-btn"
                onClick={() => setShowPassword(!showPassword)}
                title={showPassword ? 'Hide password' : 'Show password'}
                aria-label="Toggle password visibility"
              >
                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          {!isLoginTab && (
            <div className="form-group role-selection-group">
              <label className="form-label">Select Account Role</label>
              <div className="role-selector">
                <button
                  type="button"
                  className={`role-option ${role === 'User' ? 'active' : ''}`}
                  onClick={() => setRole('User')}
                >
                  <UserIcon size={17} />
                  <div className="role-option-text">
                    <strong>User</strong>
                    <small>Standard Storefront Access</small>
                  </div>
                  {role === 'User' && <CheckCircle2 size={15} className="role-check" />}
                </button>

                <button
                  type="button"
                  className={`role-option ${role === 'Admin' ? 'active' : ''}`}
                  onClick={() => setRole('Admin')}
                >
                  <Shield size={17} />
                  <div className="role-option-text">
                    <strong>Admin</strong>
                    <small>Inventory & Management</small>
                  </div>
                  {role === 'Admin' && <CheckCircle2 size={15} className="role-check" />}
                </button>
              </div>
            </div>
          )}

          <button
            type="submit"
            className="btn btn-primary btn-full auth-submit-btn"
            disabled={loading}
          >
            {loading ? (
              <span>Authenticating...</span>
            ) : isLoginTab ? (
              <>
                <span>Sign In to Account</span>
                <ArrowRight size={16} />
              </>
            ) : (
              <>
                <span>Create Account</span>
                <Sparkles size={16} />
              </>
            )}
          </button>
        </form>

        {/* Quick Demo Logins Helper */}
        <div className="auth-demo-hint">
          <span className="demo-label">Quick Demo Access:</span>
          <div className="demo-chips">
            <button
              type="button"
              className="auth-demo-chip"
              onClick={() => handleQuickFill('test', '12345')}
              title="Click to fill Test User credentials"
            >
              Test User • <strong>12345</strong>
            </button>
            <button
              type="button"
              className="auth-demo-chip"
              onClick={() => handleQuickFill('akash', '12345')}
              title="Click to fill Akash Admin credentials"
            >
              Admin (Akash) • <strong>12345</strong>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
