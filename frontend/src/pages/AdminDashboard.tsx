import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Package,
  Plus,
  Edit2,
  Trash2,
  AlertTriangle,
  FileSpreadsheet,
  FileText,
  Upload,
  Download,
  Calendar,
  RefreshCw,
  Search,
  ScanLine,
  ShoppingBag,
  CreditCard,
  User as UserIcon,
  Radio,
  Database,
  CheckCircle2,
  FileCheck,
  Layers,
  Sparkles,
  X,
  DollarSign,
  Check,
  Minus,
  Sliders,
  AlertCircle,
  RefreshCcw,
  ShieldCheck,
  Box,
  TrendingUp,
  HelpCircle,
  Clock,
  PieChart,
  Eye,
  EyeOff
} from 'lucide-react';
import type { Product, SalesReport, Order } from '../types';
import { api } from '../services/api';
import { ProductModal } from '../components/ProductModal';
import { subscribeToStockEvents } from '../services/sse';

export const AdminDashboard: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'products' | 'stock-alerts' | 'bulk-create' | 'all-orders' | 'reports' | 'ocr-upload'>('products');

  // Products Tab State
  const [products, setProducts] = useState<Product[]>([]);
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [alertMsg, setAlertMsg] = useState<{ type: 'success' | 'error' | 'warning'; text: string } | null>(null);
  const [isFallbackMode, setIsFallbackMode] = useState(false);

  // All Orders State
  const [allOrders, setAllOrders] = useState<Order[]>([]);
  const [allOrdersLoading, setAllOrdersLoading] = useState(false);
  const [orderSearch, setOrderSearch] = useState('');

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);

  // Stock Alerts & Inventory Management State
  const [stockUpdates, setStockUpdates] = useState<Array<{
    productId: number;
    name: string;
    category: string;
    price: number;
    imagePath?: string;
    isVisible: boolean;
    newStockQuantity: number;
    newLowStockThreshold: number;
    origStockQuantity: number;
    origLowStockThreshold: number;
  }>>([]);
  const [stockFilterTab, setStockFilterTab] = useState<'all' | 'in-stock' | 'low-stock' | 'out-of-stock'>('all');
  const [stockSearch, setStockSearch] = useState('');
  const [stockCategoryFilter, setStockCategoryFilter] = useState('');
  const [savingStock, setSavingStock] = useState(false);

  // Bulk Creation State
  const [bulkRows, setBulkRows] = useState([
    { name: 'Executive A5 Leather Journal', category: 'Notebooks', price: 18.99, stockQuantity: 50, lowStockThreshold: 10, isVisible: true, imageUrl: 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600' },
    { name: 'Vintage Brass Fountain Pen', category: 'Writing', price: 24.50, stockQuantity: 35, lowStockThreshold: 5, isVisible: true, imageUrl: 'https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?w=600' },
    { name: 'Pastel Morandi Gel Pens (10-Pack)', category: 'Writing', price: 12.99, stockQuantity: 80, lowStockThreshold: 15, isVisible: true, imageUrl: 'https://images.unsplash.com/photo-1585336261026-7f4153b6d773?w=600' },
  ]);
  const [csvFile, setCsvFile] = useState<File | null>(null);
  const [csvPreview, setCsvPreview] = useState<Array<{ name: string; category: string; price: number; stock: number; threshold: number; imageUrl: string; isValid: boolean; errorMsg?: string }>>([]);
  const [isDraggingCsv, setIsDraggingCsv] = useState(false);
  const [bulkLoading, setBulkLoading] = useState(false);
  const [uploadStep, setUploadStep] = useState<string>('');
  const [uploadProgress, setUploadProgress] = useState(0);
  const [showFormatGuide, setShowFormatGuide] = useState(false);

  // Sales Reports State
  const [reportDate, setReportDate] = useState<string>(new Date().toISOString().split('T')[0]);
  const [reportData, setReportData] = useState<SalesReport | null>(null);
  const [reportLoading, setReportLoading] = useState(false);
  const [reportSearch, setReportSearch] = useState('');

  // OCR Upload State
  const [ocrFile, setOcrFile] = useState<File | null>(null);
  const [ocrThreshold, setOcrThreshold] = useState(5);
  const [ocrResult, setOcrResult] = useState<any | null>(null);
  const [ocrLoading, setOcrLoading] = useState(false);

  // Fetch Products
  const fetchProducts = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.getProducts({ search, category, stockFilter: 'all', pageSize: 100 });
      setProducts(res.products || []);
    } catch (err: any) {
      setAlertMsg({ type: 'error', text: err.message || 'Failed to load products.' });
    } finally {
      setLoading(false);
    }
  }, [search, category]);

  // Fetch All Customer Orders
  const fetchAllOrders = useCallback(async () => {
    setAllOrdersLoading(true);
    try {
      const data = await api.getAllOrders();
      setAllOrders(data || []);
    } catch (err: any) {
      setAlertMsg({ type: 'error', text: err.message || 'Failed to load customer orders.' });
    } finally {
      setAllOrdersLoading(false);
    }
  }, []);

  // Fetch Stock Management Data
  const fetchStockData = useCallback(async () => {
    try {
      const data = await api.getStockManagement();
      setIsFallbackMode(!!data.isFallback);

      if (data.allProducts) {
        setStockUpdates(
          data.allProducts.map((p: Product) => ({
            productId: p.id,
            name: p.name,
            category: p.category || 'Stationery',
            price: p.price || 0,
            imagePath: p.imagePath,
            isVisible: p.isVisible !== false,
            newStockQuantity: p.stockQuantity,
            newLowStockThreshold: p.lowStockThreshold || 5,
            origStockQuantity: p.stockQuantity,
            origLowStockThreshold: p.lowStockThreshold || 5,
          }))
        );
      }
    } catch {
      setIsFallbackMode(true);
    }
  }, []);

  // Fetch Sales Report Data
  const fetchSalesReport = useCallback(async () => {
    setReportLoading(true);
    try {
      const data = await api.getDailySalesReport(reportDate);
      setReportData(data);
    } catch (err: any) {
      setAlertMsg({ type: 'error', text: err.message || 'Failed to load sales report.' });
    } finally {
      setReportLoading(false);
    }
  }, [reportDate]);

  const normalizeCat = (c: string) => {
    if (!c) return 'Stationery';
    const lower = c.trim().toLowerCase();
    if (lower.includes('writ') || lower.includes('pen') || lower.includes('ink')) return 'Writing';
    if (lower.includes('note') || lower.includes('journal') || lower.includes('paper')) return 'Notebooks';
    if (lower.includes('desk') || lower.includes('mat') || lower.includes('organizer') || lower.includes('sticky')) return 'Desk Accessories';
    if (lower.includes('art') || lower.includes('paint') || lower.includes('sketch')) return 'Art Supplies';
    if (lower.includes('office') || lower.includes('tape') || lower.includes('stapler')) return 'Office Supplies';
    if (lower.includes('school') || lower.includes('draft') || lower.includes('ruler')) return 'School & Drafting';
    return c.trim().charAt(0).toUpperCase() + c.trim().slice(1);
  };

  useEffect(() => {
    api.getCategories().then((cats) => {
      const cleanCats = Array.from(new Set((cats || []).map(normalizeCat))).sort();
      setCategories(cleanCats.length > 0 ? cleanCats : ['Art Supplies', 'Desk Accessories', 'Notebooks', 'Office Supplies', 'School & Drafting', 'Writing']);
    }).catch(() => {
      setCategories(['Art Supplies', 'Desk Accessories', 'Notebooks', 'Office Supplies', 'School & Drafting', 'Writing']);
    });
  }, []);

  useEffect(() => {
    if (activeTab === 'products') fetchProducts();
    if (activeTab === 'all-orders') fetchAllOrders();
    if (activeTab === 'stock-alerts') fetchStockData();
    if (activeTab === 'reports') fetchSalesReport();
  }, [activeTab, fetchProducts, fetchAllOrders, fetchStockData, fetchSalesReport]);

  // Real-time Server-Sent Events (SSE) listener
  useEffect(() => {
    const unsubscribe = subscribeToStockEvents(() => {
      fetchProducts();
      fetchAllOrders();
      fetchStockData();
      fetchSalesReport();
    });
    return () => unsubscribe();
  }, [fetchProducts, fetchAllOrders, fetchStockData, fetchSalesReport]);

  const handleToggleVisibility = async (id: number) => {
    try {
      const res = await api.toggleProductVisibility(id);
      setAlertMsg({ type: 'success', text: res.message });
      fetchProducts();
      fetchStockData();
    } catch (err: any) {
      setAlertMsg({ type: 'error', text: err.message });
    }
  };

  const handleDeleteProduct = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this product?')) return;
    try {
      const res = await api.deleteProduct(id);
      setAlertMsg({ type: 'success', text: res.message });
      fetchProducts();
      fetchStockData();
    } catch (err: any) {
      setAlertMsg({ type: 'error', text: err.message });
    }
  };

  // Stock Steppers and Quantity Editor
  const handleStockDelta = (productId: number, delta: number) => {
    setStockUpdates((prev) =>
      prev.map((item) =>
        item.productId === productId
          ? { ...item, newStockQuantity: Math.max(0, item.newStockQuantity + delta) }
          : item
      )
    );
  };

  const handleStockQuantityChange = (productId: number, val: number) => {
    setStockUpdates((prev) =>
      prev.map((item) =>
        item.productId === productId ? { ...item, newStockQuantity: Math.max(0, val) } : item
      )
    );
  };

  const handleThresholdChange = (productId: number, val: number) => {
    setStockUpdates((prev) =>
      prev.map((item) =>
        item.productId === productId ? { ...item, newLowStockThreshold: Math.max(0, val) } : item
      )
    );
  };

  const modifiedStockCount = useMemo(() => {
    return stockUpdates.filter(
      (s) =>
        s.newStockQuantity !== s.origStockQuantity ||
        s.newLowStockThreshold !== s.origLowStockThreshold
    ).length;
  }, [stockUpdates]);

  const handleResetStockChanges = () => {
    setStockUpdates((prev) =>
      prev.map((item) => ({
        ...item,
        newStockQuantity: item.origStockQuantity,
        newLowStockThreshold: item.origLowStockThreshold,
      }))
    );
  };

  const handleSaveBulkStock = async () => {
    setSavingStock(true);
    try {
      const updatesToSend = stockUpdates.map((s) => ({
        productId: s.productId,
        newStockQuantity: s.newStockQuantity,
        newLowStockThreshold: s.newLowStockThreshold,
      }));
      const res = await api.bulkUpdateStock(updatesToSend);
      setAlertMsg({ type: 'success', text: res.message || 'Inventory updated successfully!' });
      await fetchStockData();
    } catch (err: any) {
      setAlertMsg({ type: 'error', text: err.message || 'Failed to update stock.' });
    } finally {
      setSavingStock(false);
    }
  };

  // Filtered Stock Inventory List
  const filteredStockList = useMemo(() => {
    return stockUpdates.filter((item) => {
      if (stockFilterTab === 'in-stock' && item.newStockQuantity <= item.newLowStockThreshold) return false;
      if (stockFilterTab === 'low-stock' && (item.newStockQuantity <= 0 || item.newStockQuantity > item.newLowStockThreshold)) return false;
      if (stockFilterTab === 'out-of-stock' && item.newStockQuantity > 0) return false;
      if (stockCategoryFilter && item.category.toLowerCase() !== stockCategoryFilter.toLowerCase()) return false;
      if (stockSearch && !item.name.toLowerCase().includes(stockSearch.toLowerCase()) && !item.productId.toString().includes(stockSearch)) return false;
      return true;
    });
  }, [stockUpdates, stockFilterTab, stockCategoryFilter, stockSearch]);

  // Inventory KPI calculations
  const inventoryKpis = useMemo(() => {
    const totalSkus = stockUpdates.length;
    const outOfStock = stockUpdates.filter((s) => s.newStockQuantity <= 0).length;
    const lowStock = stockUpdates.filter((s) => s.newStockQuantity > 0 && s.newStockQuantity <= s.newLowStockThreshold).length;
    const healthyStock = totalSkus - outOfStock - lowStock;
    const totalUnits = stockUpdates.reduce((sum, s) => sum + s.newStockQuantity, 0);
    const totalValuation = stockUpdates.reduce((sum, s) => sum + (s.price * s.newStockQuantity), 0);
    return { totalSkus, outOfStock, lowStock, healthyStock, totalUnits, totalValuation };
  }, [stockUpdates]);

  // Fast Client-Side CSV Parser
  const parseCsvText = (text: string) => {
    const lines = text.split(/\r?\n/).filter((l) => l.trim().length > 0);
    if (lines.length <= 1) return [];

    const parseLine = (line: string): string[] => {
      const row: string[] = [];
      let inQuotes = false;
      let current = '';
      for (let i = 0; i < line.length; i++) {
        const c = line[i];
        if (c === '"') {
          if (inQuotes && i + 1 < line.length && line[i + 1] === '"') {
            current += '"';
            i++;
          } else {
            inQuotes = !inQuotes;
          }
        } else if (c === ',' && !inQuotes) {
          row.push(current.trim());
          current = '';
        } else {
          current += c;
        }
      }
      row.push(current.trim());
      return row;
    };

    const header = parseLine(lines[0]).map((h) => h.toLowerCase().replace(/[^a-z]/g, ''));
    const nameIdx = header.findIndex((h) => h.includes('name') || h.includes('title') || h.includes('product')) >= 0 ? header.findIndex((h) => h.includes('name') || h.includes('title') || h.includes('product')) : 0;
    const catIdx = header.findIndex((h) => h.includes('cat')) >= 0 ? header.findIndex((h) => h.includes('cat')) : 1;
    const priceIdx = header.findIndex((h) => h.includes('price') || h.includes('cost') || h.includes('rate')) >= 0 ? header.findIndex((h) => h.includes('price') || h.includes('cost') || h.includes('rate')) : 2;
    const stockIdx = header.findIndex((h) => h.includes('stock') || h.includes('qty') || h.includes('quantity')) >= 0 ? header.findIndex((h) => h.includes('stock') || h.includes('qty') || h.includes('quantity')) : 3;
    const threshIdx = header.findIndex((h) => h.includes('threshold') || h.includes('low')) >= 0 ? header.findIndex((h) => h.includes('threshold') || h.includes('low')) : 4;
    const imgIdx = header.findIndex((h) => h.includes('image') || h.includes('img') || h.includes('url') || h.includes('photo')) >= 0 ? header.findIndex((h) => h.includes('image') || h.includes('img') || h.includes('url') || h.includes('photo')) : 7;

    const parsedRows: Array<{ name: string; category: string; price: number; stock: number; threshold: number; imageUrl: string; isValid: boolean; errorMsg?: string }> = [];

    for (let i = 1; i < lines.length; i++) {
      const vals = parseLine(lines[i]);
      if (vals.length < 2) continue;

      const name = (vals[nameIdx] || '').replace(/^["']|["']$/g, '').trim();
      const category = (vals[catIdx] || 'Stationery').replace(/^["']|["']$/g, '').trim();
      const rawPrice = (vals[priceIdx] || '0').replace(/[^0-9.]/g, '');
      const price = parseFloat(rawPrice) || 0;
      const stock = parseInt(vals[stockIdx] || '10', 10) || 10;
      const threshold = parseInt(vals[threshIdx] || '5', 10) || 5;
      const imageUrl = (imgIdx >= 0 && imgIdx < vals.length ? vals[imgIdx] : '').replace(/^["']|["']$/g, '').trim();

      const isValid = Boolean(name && price > 0);
      parsedRows.push({
        name,
        category: category || 'Stationery',
        price,
        stock,
        threshold,
        imageUrl,
        isValid,
        errorMsg: !name ? 'Missing product name' : price <= 0 ? 'Price must be > $0.00' : undefined,
      });
    }

    return parsedRows;
  };

  const handleSelectCsvFile = (file: File) => {
    setCsvFile(file);
    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target?.result as string;
      if (content) {
        const parsed = parseCsvText(content);
        setCsvPreview(parsed);
      }
    };
    reader.readAsText(file);
  };

  const handleLoadSampleCsv = async () => {
    try {
      setBulkLoading(true);
      const fallbackSample = `Name,Category,Price,StockQuantity,LowStockThreshold,IsVisible,Description,ImageUrl
"Executive Leather Hardcover Notebook","Notebooks",19.99,60,10,true,"Premium 120gsm ivory ruled paper journal with dual ribbon markers","https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600"
"Vintage Fountain Pen - Matte Black","Writing",28.50,40,8,true,"Fine nib brass body fountain pen with smooth ink flow","https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?w=600"
"Pastel Morandi Gel Pens (10-Pack)","Writing",11.99,120,15,true,"0.5mm quick-drying smudge-proof pastel color ink pens","https://images.unsplash.com/photo-1585336261026-7f4153b6d773?w=600"
"Dual-Sided Leather Desk Mat","Desk Accessories",24.99,35,5,true,"Waterproof anti-slip extended mouse pad and writing blotter","https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=600"
"Pastel Sticky Notes Cube (600 Sheets)","Desk Accessories",7.49,150,20,true,"Color-coded removable adhesive notes for agile task boards","https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=600"
"Artist Watercolor Sketchbook A4","Art Supplies",22.50,45,5,true,"300gsm cold-pressed 100% cotton acid-free paper for wet media","https://images.unsplash.com/photo-1513364776144-60967b0f800f?w=600"
"Heavy-Duty Desktop Tape Dispenser","Office Supplies",13.25,50,8,true,"Weighted non-skid base tape dispenser with sharp cutting blade","https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=600"
"Precision Acrylic Ruler Set (30cm)","School & Drafting",6.99,90,12,true,"Transparent metric and imperial measuring scales with beveled edges","https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=600"`;
      const blob = new Blob([fallbackSample], { type: 'text/csv' });
      const file = new File([blob], 'sample_products.csv', { type: 'text/csv' });
      handleSelectCsvFile(file);
      setAlertMsg({ type: 'success', text: 'Loaded 8 real stationery products ready for 1-click import!' });
    } finally {
      setBulkLoading(false);
    }
  };

  const handleCsvUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!csvFile) {
      setAlertMsg({ type: 'error', text: 'Please select or drop a CSV file first.' });
      return;
    }
    setBulkLoading(true);
    setUploadProgress(20);
    setUploadStep('Validating file & preparing batch payload...');

    try {
      setUploadProgress(50);
      setUploadStep('Synchronizing with Database & Invalidating Redis cache...');

      const res = await api.bulkCreateFromCsv(csvFile);

      setUploadProgress(100);
      setUploadStep('Import completed successfully!');
      setAlertMsg({ type: 'success', text: res.message || 'CSV imported successfully!' });
      setCsvFile(null);
      setCsvPreview([]);
      fetchProducts();
      fetchStockData();
    } catch (err: any) {
      setAlertMsg({ type: 'error', text: err.message || 'Failed to upload CSV.' });
    } finally {
      setTimeout(() => {
        setBulkLoading(false);
        setUploadProgress(0);
        setUploadStep('');
      }, 500);
    }
  };

  const handleBulkFormSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const validRows = bulkRows.filter((r) => r.name.trim() !== '' && r.price > 0);
    if (validRows.length === 0) {
      setAlertMsg({ type: 'error', text: 'Please enter at least one valid product name and price.' });
      return;
    }
    setBulkLoading(true);
    try {
      const res = await api.bulkCreateProducts(validRows);
      setAlertMsg({ type: 'success', text: res.message || `Successfully created ${validRows.length} products!` });
      setBulkRows([
        { name: '', category: 'Stationery', price: 9.99, stockQuantity: 50, lowStockThreshold: 5, isVisible: true, imageUrl: '' },
      ]);
      fetchProducts();
      fetchStockData();
    } catch (err: any) {
      setAlertMsg({ type: 'error', text: err.message });
    } finally {
      setBulkLoading(false);
    }
  };

  const handleOcrSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!ocrFile) {
      setAlertMsg({ type: 'error', text: 'Please select a receipt/inventory image file.' });
      return;
    }
    setOcrLoading(true);
    try {
      const res = await api.uploadOcrInventory(ocrFile, ocrThreshold);
      setOcrResult(res);
      setAlertMsg({ type: 'success', text: res.message });
      fetchStockData();
    } catch (err: any) {
      setAlertMsg({ type: 'error', text: err.message });
    } finally {
      setOcrLoading(false);
    }
  };

  const handleExportClientCsv = () => {
    if (!reportData || !reportData.rows || reportData.rows.length === 0) {
      setAlertMsg({ type: 'warning', text: 'No order data available to export for this date.' });
      return;
    }
    const headers = 'Order ID,Order Date,Customer Username,Items Count,Amount Paid ($)\n';
    const rowsCsv = reportData.rows
      .map(r => `"${r.orderId}","${new Date(r.orderDate).toLocaleString()}","${r.username}",${r.items},${r.amount.toFixed(2)}`)
      .join('\n');
    const blob = new Blob([headers + rowsCsv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `SalesReport_${reportDate}.csv`;
    link.click();
    setAlertMsg({ type: 'success', text: 'Downloaded Sales CSV report!' });
  };

  const filteredOrders = allOrders.filter(
    (o) =>
      (o.username || '').toLowerCase().includes(orderSearch.toLowerCase()) ||
      o.id.toString().includes(orderSearch)
  );

  const filteredReportRows = useMemo(() => {
    if (!reportData || !reportData.rows) return [];
    if (!reportSearch.trim()) return reportData.rows;
    return reportData.rows.filter(
      (r) =>
        r.username.toLowerCase().includes(reportSearch.toLowerCase()) ||
        r.orderId.toString().includes(reportSearch)
    );
  }, [reportData, reportSearch]);

  const reportAov = useMemo(() => {
    if (!reportData || reportData.totalOrders === 0) return 0;
    return reportData.totalSalesAmount / reportData.totalOrders;
  }, [reportData]);

  return (
    <div className="admin-dashboard-page">
      {/* Executive Header */}
      <div className="dashboard-header">
        <div>
          <div className="dashboard-title-flex">
            <h1 className="dashboard-title">Admin Management Suite</h1>
            <span className="sse-live-tag" title="Real-Time Server-Sent Events Active">
              <Radio size={14} className="sse-icon" /> Live SSE Stream
            </span>
          </div>
          <p className="dashboard-subtitle">
            Executive inventory command center, high-speed bulk operations, customer ledger, and sales intelligence.
          </p>
        </div>

        <div className="admin-header-actions">
          <button
            className="btn btn-secondary"
            onClick={() => setActiveTab('bulk-create')}
          >
            <FileSpreadsheet size={16} />
            <span>Bulk CSV / Matrix</span>
          </button>

          <button
            className="btn btn-primary"
            onClick={() => {
              setSelectedProduct(null);
              setIsModalOpen(true);
            }}
          >
            <Plus size={18} />
            <span>Add Single Product</span>
          </button>
        </div>
      </div>

      {alertMsg && (
        <div className={`alert-box alert-${alertMsg.type}`}>
          <div className="flex items-center gap-2">
            {alertMsg.type === 'success' && <CheckCircle2 size={18} />}
            {alertMsg.type === 'error' && <AlertCircle size={18} />}
            {alertMsg.type === 'warning' && <AlertTriangle size={18} />}
            <span>{alertMsg.text}</span>
          </div>
          <button className="alert-dismiss" onClick={() => setAlertMsg(null)}>×</button>
        </div>
      )}

      {isFallbackMode && (
        <div className="fallback-sync-banner">
          <div className="flex items-center gap-2">
            <Database size={18} />
            <span>Database Fallback Active: Offline queue enabled. All operations will auto-sync when SQL comes online.</span>
          </div>
        </div>
      )}

      {/* Navigation Tabs (Admin Only - No Storefront Tab) */}
      <div className="admin-tabs-bar">
        <button className={`admin-tab ${activeTab === 'products' ? 'active' : ''}`} onClick={() => setActiveTab('products')}>
          <Package size={18} />
          <span>Products List ({products.length})</span>
        </button>

        <button className={`admin-tab ${activeTab === 'stock-alerts' ? 'active' : ''}`} onClick={() => setActiveTab('stock-alerts')}>
          <Sliders size={18} />
          <span>Inventory & Stock {inventoryKpis.lowStock + inventoryKpis.outOfStock > 0 && <span className="tab-pill-warning">{inventoryKpis.lowStock + inventoryKpis.outOfStock}</span>}</span>
        </button>

        <button className={`admin-tab ${activeTab === 'bulk-create' ? 'active' : ''}`} onClick={() => setActiveTab('bulk-create')}>
          <FileSpreadsheet size={18} />
          <span>Bulk Upload & CSV Matrix</span>
        </button>

        <button className={`admin-tab ${activeTab === 'all-orders' ? 'active' : ''}`} onClick={() => setActiveTab('all-orders')}>
          <ShoppingBag size={18} />
          <span>Customer Orders ({allOrders.length})</span>
        </button>

        <button className={`admin-tab ${activeTab === 'reports' ? 'active' : ''}`} onClick={() => setActiveTab('reports')}>
          <FileText size={18} />
          <span>Sales Reports & Intelligence</span>
        </button>

        <button className={`admin-tab ${activeTab === 'ocr-upload' ? 'active' : ''}`} onClick={() => setActiveTab('ocr-upload')}>
          <ScanLine size={18} />
          <span>OCR Scanner</span>
        </button>
      </div>

      {/* TAB 1: PRODUCTS TABLE */}
      {activeTab === 'products' && (
        <div className="admin-tab-content">
          <div className="admin-filter-bar">
            <div className="search-input-wrapper">
              <Search size={18} className="search-icon" />
              <input
                type="text"
                className="search-input"
                placeholder="Search products by name or SKU ID..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>

            <select className="filter-select" value={category} onChange={(e) => setCategory(e.target.value)}>
              <option value="">All Categories</option>
              {categories.map((c) => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>

            <button className="btn btn-secondary btn-icon-only" onClick={fetchProducts} title="Refresh Products">
              <RefreshCw size={16} />
            </button>
          </div>

          {loading ? (
            <div className="loading-state"><RefreshCw size={32} className="spin-icon" /><p>Loading products...</p></div>
          ) : products.length === 0 ? (
            <div className="empty-catalog-state">
              <Package size={56} className="empty-icon" />
              <h3>No Products Found</h3>
              <p>Add a new product or use Bulk CSV to seed your inventory.</p>
            </div>
          ) : (
            <div className="table-responsive">
              <table className="admin-data-table">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Image</th>
                    <th>Product Name</th>
                    <th>Category</th>
                    <th>Price</th>
                    <th>Stock</th>
                    <th>Health</th>
                    <th>Show to Users (Storefront)</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {products.map((p) => {
                    const imgUrl = (p.imagePath && p.imagePath.trim() !== '')
                      ? (p.imagePath.startsWith('http://') || p.imagePath.startsWith('https://')
                          ? p.imagePath
                          : `${(import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api').replace('/api', '')}${p.imagePath.startsWith('/') ? '' : '/'}${p.imagePath}`)
                      : 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=100';

                    return (
                      <tr key={p.id}>
                        <td><span className="sku-tag">#{p.id}</span></td>
                        <td>
                          <div className="table-product-thumb-box">
                            <img
                              src={imgUrl}
                              alt={p.name}
                              className="table-product-thumb"
                              onError={(e) => {
                                (e.target as HTMLImageElement).src = 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=100';
                              }}
                            />
                          </div>
                        </td>
                        <td className="product-title-cell"><strong>{p.name}</strong></td>
                        <td><span className="cat-pill">{p.category}</span></td>
                        <td><strong className="text-primary">Rs. {Number(p.price).toFixed(2)}</strong></td>
                        <td>
                          <strong className={p.stockQuantity <= 0 ? 'text-danger font-bold' : p.stockQuantity <= p.lowStockThreshold ? 'text-warning font-bold' : 'text-success font-bold'}>
                            {p.stockQuantity} units
                          </strong>
                        </td>
                        <td>
                          {p.stockQuantity <= 0 ? (
                            <span className="badge badge-danger">Out of Stock</span>
                          ) : p.stockQuantity <= p.lowStockThreshold ? (
                            <span className="badge badge-warning">Low Stock ({p.stockQuantity})</span>
                          ) : (
                            <span className="badge badge-success">In Stock</span>
                          )}
                        </td>
                        <td>
                          {/* Toggle to show/hide item to users */}
                          <button
                            type="button"
                            className={`status-toggle-pill ${p.isVisible ? 'is-active' : 'is-hidden'}`}
                            onClick={() => handleToggleVisibility(p.id)}
                            title={`Click to ${p.isVisible ? 'Hide from' : 'Show to'} users in storefront`}
                          >
                            <span className="toggle-dot">
                              {p.isVisible ? <Eye size={11} /> : <EyeOff size={11} />}
                            </span>
                            <span className="toggle-label-text">{p.isVisible ? 'Visible (Shown)' : 'Hidden (Private)'}</span>
                          </button>
                        </td>
                        <td>
                          <div className="table-action-btns">
                            <button
                              className="action-btn edit-btn"
                              onClick={() => {
                                setSelectedProduct(p);
                                setIsModalOpen(true);
                              }}
                              title="Edit Product"
                            >
                              <Edit2 size={16} />
                            </button>
                            <button
                              className="action-btn delete-btn"
                              onClick={() => handleDeleteProduct(p.id)}
                              title="Delete Product"
                            >
                              <Trash2 size={16} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* TAB 2: INVENTORY & STOCK MANAGEMENT OVERHAUL */}
      {activeTab === 'stock-alerts' && (
        <div className="admin-tab-content inventory-command-center">
          {/* Executive KPI Summary Cards */}
          <div className="inventory-kpi-grid">
            <div className="inv-kpi-card kpi-total">
              <div className="kpi-icon-wrap">
                <Box size={24} />
              </div>
              <div className="kpi-info">
                <span className="kpi-title">Total Products</span>
                <span className="kpi-number">{inventoryKpis.totalSkus}</span>
                <span className="kpi-subtext">{inventoryKpis.totalUnits.toLocaleString()} units stored</span>
              </div>
            </div>

            <div className="inv-kpi-card kpi-healthy">
              <div className="kpi-icon-wrap">
                <ShieldCheck size={24} />
              </div>
              <div className="kpi-info">
                <span className="kpi-title">Healthy Stock</span>
                <span className="kpi-number">{inventoryKpis.healthyStock}</span>
                <span className="kpi-subtext">Adequate inventory levels</span>
              </div>
            </div>

            <div className="inv-kpi-card kpi-warning">
              <div className="kpi-icon-wrap">
                <AlertTriangle size={24} />
              </div>
              <div className="kpi-info">
                <span className="kpi-title">Low Stock Alert</span>
                <span className="kpi-number">{inventoryKpis.lowStock}</span>
                <span className="kpi-subtext">Needs reordering soon</span>
              </div>
            </div>

            <div className="inv-kpi-card kpi-danger">
              <div className="kpi-icon-wrap">
                <AlertCircle size={24} />
              </div>
              <div className="kpi-info">
                <span className="kpi-title">Out of Stock</span>
                <span className="kpi-number">{inventoryKpis.outOfStock}</span>
                <span className="kpi-subtext">Critical replenishment</span>
              </div>
            </div>

            <div className="inv-kpi-card kpi-valuation">
              <div className="kpi-icon-wrap">
                <DollarSign size={24} />
              </div>
              <div className="kpi-info">
                <span className="kpi-title">Inventory Value</span>
                <span className="kpi-number">Rs. {inventoryKpis.totalValuation.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                <span className="kpi-subtext">Asset retail valuation</span>
              </div>
            </div>
          </div>

          {/* Unsaved Changes Banner */}
          {modifiedStockCount > 0 && (
            <div className="unsaved-changes-floating-bar">
              <div className="flex items-center gap-3">
                <span className="unsaved-badge">{modifiedStockCount} Unsaved Changes</span>
                <span className="text-sm">You have adjusted stock levels. Save now to apply to database and sync storefront.</span>
              </div>
              <div className="flex items-center gap-2">
                <button className="btn btn-secondary btn-sm" onClick={handleResetStockChanges}>
                  <RefreshCcw size={14} />
                  <span>Reset</span>
                </button>
                <button className="btn btn-primary btn-sm" onClick={handleSaveBulkStock} disabled={savingStock}>
                  {savingStock ? (
                    <>
                      <RefreshCw size={14} className="spin-icon" />
                      <span>Saving...</span>
                    </>
                  ) : (
                    <>
                      <Check size={14} />
                      <span>Save All Changes</span>
                    </>
                  )}
                </button>
              </div>
            </div>
          )}

          {/* Inventory Controls & Filter Bar */}
          <div className="inventory-controls-header mt-24">
            <div className="inv-tab-pills">
              <button
                className={`inv-tab-pill ${stockFilterTab === 'all' ? 'active' : ''}`}
                onClick={() => setStockFilterTab('all')}
              >
                All Products ({stockUpdates.length})
              </button>
              <button
                className={`inv-tab-pill ${stockFilterTab === 'in-stock' ? 'active' : ''}`}
                onClick={() => setStockFilterTab('in-stock')}
              >
                Healthy ({inventoryKpis.healthyStock})
              </button>
              <button
                className={`inv-tab-pill warning ${stockFilterTab === 'low-stock' ? 'active' : ''}`}
                onClick={() => setStockFilterTab('low-stock')}
              >
                ⚠️ Low Stock ({inventoryKpis.lowStock})
              </button>
              <button
                className={`inv-tab-pill danger ${stockFilterTab === 'out-of-stock' ? 'active' : ''}`}
                onClick={() => setStockFilterTab('out-of-stock')}
              >
                🚨 Out of Stock ({inventoryKpis.outOfStock})
              </button>
            </div>

            <div className="inv-filter-inputs">
              <div className="search-input-wrapper">
                <Search size={16} className="search-icon" />
                <input
                  type="text"
                  className="search-input text-sm"
                  placeholder="Filter by name or ID..."
                  value={stockSearch}
                  onChange={(e) => setStockSearch(e.target.value)}
                />
              </div>

              <select
                className="filter-select text-sm"
                value={stockCategoryFilter}
                onChange={(e) => setStockCategoryFilter(e.target.value)}
              >
                <option value="">All Categories</option>
                {categories.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>

              <button
                className="btn btn-primary"
                onClick={handleSaveBulkStock}
                disabled={savingStock}
              >
                {savingStock ? 'Saving...' : `Save Stock Updates ${modifiedStockCount > 0 ? `(${modifiedStockCount})` : ''}`}
              </button>
            </div>
          </div>

          {/* Interactive Inventory Table with Steppers & Visibility Toggle */}
          <div className="table-responsive mt-16">
            <table className="admin-data-table inventory-table">
              <thead>
                <tr>
                  <th style={{ width: '7%' }}>ID</th>
                  <th style={{ width: '28%' }}>Product</th>
                  <th style={{ width: '13%' }}>Category</th>
                  <th style={{ width: '22%' }}>Stock Adjustment</th>
                  <th style={{ width: '8%' }}>Min Alert</th>
                  <th style={{ width: '11%' }}>Status</th>
                  <th style={{ width: '11%' }}>User Visibility</th>
                </tr>
              </thead>
              <tbody>
                {filteredStockList.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="text-center p-32 text-muted">
                      No products matching the selected inventory filter.
                    </td>
                  </tr>
                ) : (
                  filteredStockList.map((item) => {
                    const isModified =
                      item.newStockQuantity !== item.origStockQuantity ||
                      item.newLowStockThreshold !== item.origLowStockThreshold;

                    const imgUrl = (item.imagePath && item.imagePath.trim() !== '')
                      ? (item.imagePath.startsWith('http://') || item.imagePath.startsWith('https://')
                          ? item.imagePath
                          : `${(import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api').replace('/api', '')}${item.imagePath.startsWith('/') ? '' : '/'}${item.imagePath}`)
                      : 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=100';

                    return (
                      <tr key={item.productId} className={isModified ? 'row-modified' : ''}>
                        <td>
                          <span className="sku-tag">#{item.productId}</span>
                        </td>
                        <td>
                          <div className="flex items-center gap-3">
                            <div className="table-product-thumb-box mini">
                              <img
                                src={imgUrl}
                                alt={item.name}
                                className="table-product-thumb"
                                onError={(e) => {
                                  (e.target as HTMLImageElement).src = 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=100';
                                }}
                              />
                            </div>
                            <div>
                              <strong className="product-row-name">{item.name}</strong>
                              <div className="product-row-meta">
                                <span className="text-muted">Rs. {Number(item.price).toFixed(2)}</span>
                                {isModified && <span className="modified-indicator-dot">• Modified</span>}
                              </div>
                            </div>
                          </div>
                        </td>
                        <td>
                          <span className="cat-pill">{item.category}</span>
                        </td>
                        <td>
                          <div className="stock-stepper-control">
                            <button
                              type="button"
                              className="stepper-btn"
                              onClick={() => handleStockDelta(item.productId, -5)}
                              title="Decrease by 5"
                            >
                              -5
                            </button>
                            <button
                              type="button"
                              className="stepper-btn"
                              onClick={() => handleStockDelta(item.productId, -1)}
                              title="Decrease by 1"
                            >
                              <Minus size={14} />
                            </button>
                            <input
                              type="number"
                              min="0"
                              className={`form-input stepper-input ${item.newStockQuantity <= 0 ? 'is-zero' : item.newStockQuantity <= item.newLowStockThreshold ? 'is-low' : ''}`}
                              value={item.newStockQuantity}
                              onChange={(e) => handleStockQuantityChange(item.productId, parseInt(e.target.value) || 0)}
                            />
                            <button
                              type="button"
                              className="stepper-btn"
                              onClick={() => handleStockDelta(item.productId, 1)}
                              title="Increase by 1"
                            >
                              <Plus size={14} />
                            </button>
                            <button
                              type="button"
                              className="stepper-btn"
                              onClick={() => handleStockDelta(item.productId, 5)}
                              title="Increase by 5"
                            >
                              +5
                            </button>
                            <button
                              type="button"
                              className="stepper-btn"
                              onClick={() => handleStockDelta(item.productId, 10)}
                              title="Increase by 10"
                            >
                              +10
                            </button>
                          </div>
                        </td>
                        <td>
                          <input
                            type="number"
                            min="0"
                            className="form-input table-input threshold-input"
                            value={item.newLowStockThreshold}
                            onChange={(e) => handleThresholdChange(item.productId, parseInt(e.target.value) || 0)}
                            title="Low Stock Alert Threshold"
                          />
                        </td>
                        <td>
                          {item.newStockQuantity <= 0 ? (
                            <span className="badge badge-danger">Out of Stock</span>
                          ) : item.newStockQuantity <= item.newLowStockThreshold ? (
                            <span className="badge badge-warning">Low Stock</span>
                          ) : (
                            <span className="badge badge-success">In Stock</span>
                          )}
                        </td>
                        <td>
                          {/* Toggle to show/hide item to users */}
                          <button
                            type="button"
                            className={`status-toggle-pill mini ${item.isVisible ? 'is-active' : 'is-hidden'}`}
                            onClick={() => handleToggleVisibility(item.productId)}
                            title={`Click to ${item.isVisible ? 'Hide from' : 'Show to'} storefront users`}
                          >
                            <span className="toggle-dot">
                              {item.isVisible ? <Eye size={10} /> : <EyeOff size={10} />}
                            </span>
                            <span className="toggle-label-text">{item.isVisible ? 'Visible' : 'Hidden'}</span>
                          </button>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 3: HIGH-SPEED BULK CREATE & CSV IMPORT */}
      {activeTab === 'bulk-create' && (
        <div className="admin-tab-content">
          <div className="bulk-hero-banner">
            <div className="bulk-hero-content">
              <div className="bulk-hero-tag">
                <Sparkles size={14} /> High-Speed Supabase & Redis Synchronizer
              </div>
              <h2>Bulk Product Creation & CSV Import</h2>
              <p>
                Import dozens or hundreds of stationery products with sub-second response times. Validates rows client-side before asynchronous persistence.
              </p>
            </div>
            <div className="bulk-hero-actions">
              <button
                type="button"
                className="btn btn-secondary-glass"
                onClick={handleLoadSampleCsv}
                disabled={bulkLoading}
              >
                <Sparkles size={16} />
                <span>⚡ Load 8 Sample Products</span>
              </button>
              <button
                type="button"
                className="btn btn-secondary-glass"
                onClick={() => setShowFormatGuide(!showFormatGuide)}
              >
                <HelpCircle size={16} />
                <span>CSV Column Guide</span>
              </button>
              <a
                href={api.downloadCsvTemplateUrl()}
                target="_blank"
                rel="noreferrer"
                className="btn btn-primary-glass"
              >
                <Download size={16} />
                <span>Download Template CSV</span>
              </a>
            </div>
          </div>

          {/* Format Guide Box */}
          {showFormatGuide && (
            <div className="csv-format-guide-card">
              <div className="guide-header">
                <h4>Required & Optional CSV Column Structure</h4>
                <button className="guide-close-btn" onClick={() => setShowFormatGuide(false)}>×</button>
              </div>
              <div className="guide-content">
                <p className="text-sm text-muted">Ensure your CSV header contains the following column names (case-insensitive):</p>
                <div className="guide-columns-tags">
                  <span className="guide-tag required">Name * (string)</span>
                  <span className="guide-tag required">Price * (number)</span>
                  <span className="guide-tag optional">Category (string)</span>
                  <span className="guide-tag optional">StockQuantity (number)</span>
                  <span className="guide-tag optional">LowStockThreshold (number)</span>
                  <span className="guide-tag optional">IsVisible (true/false)</span>
                  <span className="guide-tag optional">ImageUrl (URL)</span>
                  <span className="guide-tag optional">Description (string)</span>
                </div>
              </div>
            </div>
          )}

          <div className="bulk-grid-layout">
            {/* CSV IMPORT CARD */}
            <div className="card-section bulk-csv-card">
              <div className="card-header-flex">
                <div>
                  <div className="card-badge-title">
                    <FileSpreadsheet size={20} className="text-primary" />
                    <h3>Import via CSV File</h3>
                  </div>
                  <p className="card-desc">Upload or drag and drop your inventory CSV for instant bulk ingestion.</p>
                </div>
              </div>

              <form onSubmit={handleCsvUpload} className="mt-16">
                <div
                  className={`file-dropzone-modern ${isDraggingCsv ? 'dropzone-active' : ''} ${csvFile ? 'dropzone-has-file' : ''}`}
                  onDragOver={(e) => {
                    e.preventDefault();
                    setIsDraggingCsv(true);
                  }}
                  onDragLeave={() => setIsDraggingCsv(false)}
                  onDrop={(e) => {
                    e.preventDefault();
                    setIsDraggingCsv(false);
                    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
                      handleSelectCsvFile(e.dataTransfer.files[0]);
                    }
                  }}
                >
                  <input
                    type="file"
                    id="csv-file-input"
                    accept=".csv"
                    className="file-hidden-input"
                    onChange={(e) => e.target.files && e.target.files[0] && handleSelectCsvFile(e.target.files[0])}
                  />
                  <label htmlFor="csv-file-input" className="dropzone-inner-label">
                    {csvFile ? (
                      <div className="dropzone-file-selected">
                        <div className="file-icon-badge">
                          <FileCheck size={32} />
                        </div>
                        <div className="file-selected-details">
                          <span className="file-name">{csvFile.name}</span>
                          <span className="file-meta">
                            {(csvFile.size / 1024).toFixed(1)} KB &bull; {csvPreview.length} items parsed &bull; {csvPreview.filter(p => p.isValid).length} ready
                          </span>
                        </div>
                        <button
                          type="button"
                          className="file-remove-btn"
                          onClick={(e) => {
                            e.preventDefault();
                            setCsvFile(null);
                            setCsvPreview([]);
                          }}
                          title="Remove file"
                        >
                          <X size={16} />
                        </button>
                      </div>
                    ) : (
                      <div className="dropzone-empty-prompt">
                        <div className="dropzone-icon-circle">
                          <Upload size={28} />
                        </div>
                        <h4>Drop your CSV file here, or <span className="text-primary-link">browse</span></h4>
                        <p className="dropzone-sub">Accepts UTF-8 .csv files with Name, Price, Category, Stock & Images</p>
                      </div>
                    )}
                  </label>
                </div>

                {/* Progress Animation Bar */}
                {bulkLoading && (
                  <div className="bulk-upload-progress-container mt-16">
                    <div className="progress-status-header">
                      <span className="progress-step-text">{uploadStep}</span>
                      <span className="progress-percentage-text">{uploadProgress}%</span>
                    </div>
                    <div className="progress-track">
                      <div className="progress-fill" style={{ width: `${uploadProgress}%` }} />
                    </div>
                  </div>
                )}

                {/* CSV PREVIEW TABLE */}
                {csvPreview.length > 0 && (
                  <div className="csv-preview-container mt-16">
                    <div className="csv-preview-header">
                      <div className="flex items-center gap-2">
                        <Layers size={16} className="text-primary" />
                        <span className="font-semibold text-sm">
                          Parsed Preview ({csvPreview.filter(p => p.isValid).length} Valid Products Ready)
                        </span>
                      </div>
                      <span className="badge badge-success text-xs">Verified Format</span>
                    </div>

                    <div className="table-responsive csv-preview-table-wrap">
                      <table className="admin-data-table csv-mini-table">
                        <thead>
                          <tr>
                            <th>Product Name</th>
                            <th>Category</th>
                            <th>Price</th>
                            <th>Stock</th>
                            <th>Status</th>
                          </tr>
                        </thead>
                        <tbody>
                          {csvPreview.slice(0, 8).map((item, idx) => (
                            <tr key={idx} className={item.isValid ? '' : 'row-invalid'}>
                              <td className="font-medium text-sm">
                                <div className="flex items-center gap-2">
                                  {item.imageUrl && (
                                    <img
                                      src={item.imageUrl}
                                      alt=""
                                      className="csv-mini-thumb"
                                      onError={(e) => {
                                        (e.target as HTMLElement).style.display = 'none';
                                      }}
                                    />
                                  )}
                                  <span>{item.name || '<Unnamed>'}</span>
                                </div>
                              </td>
                              <td><span className="cat-pill">{item.category}</span></td>
                              <td className="font-bold text-primary">Rs. {item.price.toFixed(2)}</td>
                              <td><span className="stock-count-tag">{item.stock} in stock</span></td>
                              <td>
                                {item.isValid ? (
                                  <span className="badge badge-success text-xs">Ready</span>
                                ) : (
                                  <span className="badge badge-danger text-xs">{item.errorMsg || 'Invalid'}</span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    {csvPreview.length > 8 && (
                      <div className="csv-preview-footer text-xs text-muted">
                        + {csvPreview.length - 8} more products ready to be imported into database.
                      </div>
                    )}
                  </div>
                )}

                <button
                  type="submit"
                  className="btn btn-primary btn-full mt-16"
                  disabled={bulkLoading || !csvFile || csvPreview.filter(p => p.isValid).length === 0}
                >
                  {bulkLoading ? (
                    <>
                      <RefreshCw size={18} className="spin-icon" />
                      <span>{uploadStep || 'Importing Products...'}</span>
                    </>
                  ) : (
                    <>
                      <Upload size={18} />
                      <span>Upload & Import {csvPreview.length > 0 ? `${csvPreview.filter(p => p.isValid).length} Products` : 'CSV'}</span>
                    </>
                  )}
                </button>
              </form>
            </div>

            {/* MULTI-ROW MATRIX CREATOR */}
            <div className="card-section bulk-matrix-card">
              <div className="card-header-flex">
                <div>
                  <div className="card-badge-title">
                    <Package size={20} className="text-primary" />
                    <h3>Multi-Row Matrix Creator</h3>
                  </div>
                  <p className="card-desc">Type products in an interactive spreadsheet grid for instant batch insert.</p>
                </div>
              </div>

              <form onSubmit={handleBulkFormSubmit} className="mt-16">
                <div className="table-responsive matrix-table-wrap">
                  <table className="admin-data-table matrix-table">
                    <thead>
                      <tr>
                        <th style={{ width: '28%' }}>Product Name *</th>
                        <th style={{ width: '20%' }}>Category</th>
                        <th style={{ width: '14%' }}>Price ($)</th>
                        <th style={{ width: '12%' }}>Stock</th>
                        <th style={{ width: '12%' }}>Threshold</th>
                        <th style={{ width: '14%' }}>Action</th>
                      </tr>
                    </thead>
                    <tbody>
                      {bulkRows.map((row, idx) => (
                        <tr key={idx}>
                          <td>
                            <input
                              type="text"
                              className="form-input"
                              placeholder="e.g. Hardcover Journal"
                              value={row.name}
                              onChange={(e) => {
                                const copy = [...bulkRows];
                                copy[idx].name = e.target.value;
                                setBulkRows(copy);
                              }}
                              required={idx === 0}
                            />
                          </td>
                          <td>
                            <input
                              type="text"
                              className="form-input"
                              placeholder="Category"
                              value={row.category}
                              onChange={(e) => {
                                const copy = [...bulkRows];
                                copy[idx].category = e.target.value;
                                setBulkRows(copy);
                              }}
                            />
                          </td>
                          <td>
                            <input
                              type="number"
                              step="0.01"
                              min="0.01"
                              className="form-input"
                              value={row.price}
                              onChange={(e) => {
                                const copy = [...bulkRows];
                                copy[idx].price = parseFloat(e.target.value) || 0;
                                setBulkRows(copy);
                              }}
                            />
                          </td>
                          <td>
                            <input
                              type="number"
                              min="0"
                              className="form-input"
                              value={row.stockQuantity}
                              onChange={(e) => {
                                const copy = [...bulkRows];
                                copy[idx].stockQuantity = parseInt(e.target.value) || 0;
                                setBulkRows(copy);
                              }}
                            />
                          </td>
                          <td>
                            <input
                              type="number"
                              min="0"
                              className="form-input"
                              value={row.lowStockThreshold}
                              onChange={(e) => {
                                const copy = [...bulkRows];
                                copy[idx].lowStockThreshold = parseInt(e.target.value) || 0;
                                setBulkRows(copy);
                              }}
                            />
                          </td>
                          <td>
                            <div className="flex items-center gap-1">
                              <button
                                type="button"
                                className="action-btn delete-btn mini"
                                onClick={() => {
                                  if (bulkRows.length > 1) {
                                    setBulkRows(bulkRows.filter((_, i) => i !== idx));
                                  }
                                }}
                                title="Remove row"
                              >
                                <Trash2 size={14} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="form-actions-flex mt-16">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() =>
                      setBulkRows([
                        ...bulkRows,
                        { name: '', category: 'Stationery', price: 9.99, stockQuantity: 25, lowStockThreshold: 5, isVisible: true, imageUrl: '' },
                      ])
                    }
                  >
                    + Add Another Row
                  </button>

                  <button type="submit" className="btn btn-primary" disabled={bulkLoading}>
                    {bulkLoading ? 'Creating Products...' : `Create All ${bulkRows.filter(r => r.name.trim()).length} Products`}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* TAB 4: ALL CUSTOMER ORDERS */}
      {activeTab === 'all-orders' && (
        <div className="admin-tab-content">
          <div className="admin-filter-bar">
            <div className="search-input-wrapper">
              <Search size={18} className="search-icon" />
              <input
                type="text"
                className="search-input"
                placeholder="Search orders by customer username or order ID..."
                value={orderSearch}
                onChange={(e) => setOrderSearch(e.target.value)}
              />
            </div>
            <button className="btn btn-secondary" onClick={fetchAllOrders}>
              <RefreshCw size={16} />
              <span>Refresh Orders</span>
            </button>
          </div>

          {allOrdersLoading ? (
            <div className="loading-state"><RefreshCw size={32} className="spin-icon" /><p>Fetching customer orders...</p></div>
          ) : filteredOrders.length === 0 ? (
            <div className="empty-catalog-state">
              <ShoppingBag size={56} className="empty-icon" />
              <h3>No Customer Orders Found</h3>
              <p>When customers complete purchases, their orders will appear here in real-time.</p>
            </div>
          ) : (
            <div className="all-orders-admin-list">
              {filteredOrders.map((order) => (
                <div key={order.id} className="admin-order-card">
                  <div className="admin-order-header">
                    <div className="admin-order-meta">
                      <span className="order-badge">Order #{order.id}</span>
                      <span className="customer-tag">
                        <UserIcon size={14} />
                        Customer: <strong>{order.username || `User #${order.userId}`}</strong>
                      </span>
                      <span className="order-date-tag">
                        <Calendar size={14} />
                        {new Date(order.date).toLocaleString()}
                      </span>
                    </div>

                    <div className="order-payment-tag">
                      <CreditCard size={14} />
                      <span>{order.paymentMethod?.toUpperCase()}</span>
                    </div>
                  </div>

                  <div className="admin-order-items-grid">
                    {order.items?.map((item, idx) => (
                      <div key={idx} className="admin-order-item-badge">
                        <span>{item.productName}</span>
                        <strong>× {item.quantity} (Rs. {(item.price * item.quantity).toFixed(2)})</strong>
                      </div>
                    ))}
                  </div>

                  <div className="admin-order-footer">
                    <span>Total Amount Charged</span>
                    <strong className="order-total-price">Rs. {Number(order.totalAmount).toFixed(2)}</strong>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* TAB 5: SALES REPORTS & INTELLIGENCE */}
      {activeTab === 'reports' && (
        <div className="admin-tab-content sales-reports-dashboard">
          {/* Top Date & Export Bar */}
          <div className="reports-executive-bar">
            <div className="reports-date-controls">
              <div className="date-picker-group">
                <Calendar size={18} className="text-primary" />
                <input
                  type="date"
                  className="form-input date-input"
                  value={reportDate}
                  onChange={(e) => setReportDate(e.target.value)}
                />
              </div>

              {/* Quick Date Presets */}
              <div className="quick-date-pills">
                <button
                  type="button"
                  className={`date-pill ${reportDate === new Date().toISOString().split('T')[0] ? 'active' : ''}`}
                  onClick={() => setReportDate(new Date().toISOString().split('T')[0])}
                >
                  Today
                </button>
                <button
                  type="button"
                  className="date-pill"
                  onClick={() => {
                    const d = new Date();
                    d.setDate(d.getDate() - 1);
                    setReportDate(d.toISOString().split('T')[0]);
                  }}
                >
                  Yesterday
                </button>
                <button
                  type="button"
                  className="date-pill"
                  onClick={() => {
                    const d = new Date();
                    d.setDate(d.getDate() - 7);
                    setReportDate(d.toISOString().split('T')[0]);
                  }}
                >
                  7 Days Ago
                </button>
              </div>
            </div>

            <div className="report-export-btns-group">
              <button
                type="button"
                className="btn btn-secondary-glass btn-sm"
                onClick={handleExportClientCsv}
                title="Export displayed orders as CSV"
              >
                <FileSpreadsheet size={16} />
                <span>Export CSV</span>
              </button>

              <a
                href={api.downloadExcelReportUrl(reportDate)}
                target="_blank"
                rel="noreferrer"
                className="btn btn-success-glass btn-sm"
              >
                <FileSpreadsheet size={16} />
                <span>Excel (.xlsx)</span>
              </a>

              <a
                href={api.downloadPdfReportUrl(reportDate)}
                target="_blank"
                rel="noreferrer"
                className="btn btn-danger-glass btn-sm"
              >
                <FileText size={16} />
                <span>PDF Statement</span>
              </a>
            </div>
          </div>

          {reportLoading ? (
            <div className="loading-state">
              <RefreshCw size={36} className="spin-icon" />
              <p>Aggregating daily sales intelligence...</p>
            </div>
          ) : reportData ? (
            <>
              {/* Sales KPI Cards */}
              <div className="reports-kpi-grid mt-20">
                <div className="rep-kpi-card kpi-revenue">
                  <div className="rep-kpi-header">
                    <span className="rep-kpi-label">Gross Revenue</span>
                    <div className="rep-kpi-icon"><DollarSign size={20} /></div>
                  </div>
                  <div className="rep-kpi-value">Rs. {reportData.totalSalesAmount.toFixed(2)}</div>
                  <div className="rep-kpi-footer">
                    <TrendingUp size={14} className="text-success" />
                    <span>Total billed for {reportDate}</span>
                  </div>
                </div>

                <div className="rep-kpi-card kpi-orders">
                  <div className="rep-kpi-header">
                    <span className="rep-kpi-label">Orders Placed</span>
                    <div className="rep-kpi-icon"><ShoppingBag size={20} /></div>
                  </div>
                  <div className="rep-kpi-value">{reportData.totalOrders}</div>
                  <div className="rep-kpi-footer">
                    <CheckCircle2 size={14} className="text-primary" />
                    <span>Completed checkouts</span>
                  </div>
                </div>

                <div className="rep-kpi-card kpi-items">
                  <div className="rep-kpi-header">
                    <span className="rep-kpi-label">Units Sold</span>
                    <div className="rep-kpi-icon"><Box size={20} /></div>
                  </div>
                  <div className="rep-kpi-value">{reportData.totalItemsSold}</div>
                  <div className="rep-kpi-footer">
                    <Layers size={14} className="text-info" />
                    <span>Physical stationery products</span>
                  </div>
                </div>

                <div className="rep-kpi-card kpi-aov">
                  <div className="rep-kpi-header">
                    <span className="rep-kpi-label">Avg Order Value (AOV)</span>
                    <div className="rep-kpi-icon"><PieChart size={20} /></div>
                  </div>
                  <div className="rep-kpi-value">Rs. {reportAov.toFixed(2)}</div>
                  <div className="rep-kpi-footer">
                    <span>Revenue per customer transaction</span>
                  </div>
                </div>
              </div>

              {/* Transactions Ledger Table */}
              <div className="card-section mt-24">
                <div className="section-header-flex">
                  <div>
                    <h3>Order Ledger Transactions &bull; {reportDate}</h3>
                    <p className="card-desc">Detailed transactional breakdown of customer orders processed on this date.</p>
                  </div>

                  <div className="search-input-wrapper ledger-search">
                    <Search size={16} className="search-icon" />
                    <input
                      type="text"
                      className="search-input text-sm"
                      placeholder="Search transactions..."
                      value={reportSearch}
                      onChange={(e) => setReportSearch(e.target.value)}
                    />
                  </div>
                </div>

                <div className="table-responsive mt-16">
                  <table className="admin-data-table">
                    <thead>
                      <tr>
                        <th style={{ width: '12%' }}>Order ID</th>
                        <th style={{ width: '25%' }}>Timestamp</th>
                        <th style={{ width: '28%' }}>Customer Account</th>
                        <th style={{ width: '15%' }}>Items Purchased</th>
                        <th style={{ width: '20%' }}>Amount Paid</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredReportRows.length === 0 ? (
                        <tr>
                          <td colSpan={5} className="text-center p-32 text-muted">
                            {reportData.rows.length === 0 ? 'No customer orders were placed on this date.' : 'No transactions matching search term.'}
                          </td>
                        </tr>
                      ) : (
                        filteredReportRows.map((row) => (
                          <tr key={row.orderId}>
                            <td>
                              <span className="sku-tag">#{row.orderId}</span>
                            </td>
                            <td>
                              <div className="flex items-center gap-2 text-sm">
                                <Clock size={14} className="text-muted" />
                                <span>{new Date(row.orderDate).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                              </div>
                            </td>
                            <td>
                              <div className="flex items-center gap-2">
                                <div className="user-avatar-dot" />
                                <strong>{row.username}</strong>
                              </div>
                            </td>
                            <td>
                              <span className="badge badge-info">{row.items} items</span>
                            </td>
                            <td>
                              <strong className="text-primary font-bold text-base">Rs. {row.amount.toFixed(2)}</strong>
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </>
          ) : null}
        </div>
      )}

      {/* TAB 6: OCR INVENTORY UPLOAD */}
      {activeTab === 'ocr-upload' && (
        <div className="admin-tab-content">
          <div className="card-section max-w-600">
            <h3>OCR Inventory & Receipt Reader</h3>
            <p className="card-desc">Upload a picture of an inventory receipt or document to scan and automatically increment stock levels.</p>

            <form onSubmit={handleOcrSubmit}>
              <div className="form-group mt-16">
                <label className="form-label">Default Low-Stock Threshold</label>
                <input
                  type="number"
                  min="1"
                  className="form-input"
                  value={ocrThreshold}
                  onChange={(e) => setOcrThreshold(parseInt(e.target.value) || 5)}
                />
              </div>

              <div className="file-dropzone mt-16">
                <ScanLine size={48} className="dropzone-icon" />
                <input
                  type="file"
                  accept="image/*,.pdf"
                  onChange={(e) => e.target.files && setOcrFile(e.target.files[0])}
                />
                <span>{ocrFile ? ocrFile.name : 'Select receipt image file'}</span>
              </div>

              <button type="submit" className="btn btn-primary btn-full mt-16" disabled={ocrLoading || !ocrFile}>
                {ocrLoading ? 'Scanning & Parsing...' : 'Process Inventory Image'}
              </button>
            </form>

            {ocrResult && (
              <div className="ocr-results-box mt-24">
                <h4>Processing Output Summary</h4>
                <div className="alert-box alert-success">{ocrResult.message}</div>
                {ocrResult.extractedItems && ocrResult.extractedItems.length > 0 && (
                  <div className="extracted-items-list mt-12">
                    <h5>Extracted Products ({ocrResult.extractedItems.length}):</h5>
                    <ul>
                      {ocrResult.extractedItems.map((item: any, idx: number) => (
                        <li key={idx}><strong>{item.productName}</strong>: {item.quantity} units</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Edit / Create Product Modal */}
      <ProductModal
        isOpen={isModalOpen}
        product={selectedProduct}
        onClose={() => setIsModalOpen(false)}
        onSuccess={() => {
          fetchProducts();
          fetchStockData();
        }}
      />
    </div>
  );
};
