export interface User {
  id: number;
  username: string;
  role: 'Admin' | 'User';
}

export interface Product {
  id: number;
  name: string;
  category: string;
  price: number;
  stockQuantity: number;
  lowStockThreshold: number;
  isVisible: boolean;
  imagePath?: string;
  description?: string;
  isOutOfStock?: boolean;
  isLowStock?: boolean;
}

export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  category: string;
  price: number;
  imagePath: string;
  stockQuantity: number;
  isOutOfStock: boolean;
  quantity: number;
  subtotal: number;
}

export interface CartResponse {
  items: CartItem[];
  itemCount: number;
  subtotal: number;
  tax: number;
  total: number;
}

export interface OrderItem {
  id?: number;
  productId: number;
  productName: string;
  quantity: number;
  price: number;
  itemTotal?: number;
}

export interface Order {
  id: number;
  userId?: number;
  username?: string;
  date: string;
  totalAmount: number;
  paymentMethod: string;
  itemCount?: number;
  items: OrderItem[];
}

export interface StockSummary {
  totalProducts: number;
  outOfStockCount: number;
  lowStockCount: number;
  inStockCount: number;
}

export interface SalesReportRow {
  orderId: number;
  orderDate: string;
  username: string;
  items: number;
  amount: number;
}

export interface SalesReport {
  selectedDate: string;
  totalOrders: number;
  totalSalesAmount: number;
  totalItemsSold: number;
  rows: SalesReportRow[];
}

export interface OcrInventoryItem {
  productName: string;
  quantity: number;
}
