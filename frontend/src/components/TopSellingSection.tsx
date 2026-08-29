import React from 'react';
import { Flame, Sparkles, TrendingUp } from 'lucide-react';
import type { Product } from '../types';
import { ProductCard } from './ProductCard';

interface TopSellingSectionProps {
  products: Product[];
  onOpenAuth: (tab?: 'login' | 'register') => void;
}

export const TopSellingSection: React.FC<TopSellingSectionProps> = ({ products, onOpenAuth }) => {
  // Pick top 4 products for the static Best Sellers showcase
  const topProducts = products.slice(0, 4);

  if (topProducts.length === 0) return null;

  return (
    <section className="top-selling-section">
      <div className="section-header-row">
        <div className="section-header-left">
          <div className="section-badge-pill">
            <Flame size={14} className="flame-icon" />
            <span>Studio Bestsellers</span>
          </div>
          <h2 className="section-heading">Top Selling Stationery</h2>
          <p className="section-subheading">
            Our most popular precision instruments, archival notebooks, and customer favorites.
          </p>
        </div>

        <div className="section-header-right">
          <span className="live-popularity-tag">
            <TrendingUp size={14} /> High Demand
          </span>
        </div>
      </div>

      {/* Static 4-Column Grid (NO SLIDING, NO CAROUSEL) */}
      <div className="top-selling-grid">
        {topProducts.map((product, idx) => (
          <div key={product.id} className="top-selling-card-wrapper">
            <div className="top-selling-rank-badge">
              <Sparkles size={12} /> #{idx + 1} Best Seller
            </div>
            <ProductCard product={product} onOpenAuth={onOpenAuth} />
          </div>
        ))}
      </div>
    </section>
  );
};
