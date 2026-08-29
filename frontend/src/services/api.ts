const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

const getHeaders = (isMultipart = false) => {
  const token = localStorage.getItem('accessToken') || localStorage.getItem('token');
  const headers: Record<string, string> = {};
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  if (!isMultipart) {
    headers['Content-Type'] = 'application/json';
  }
  return headers;
};

export const api = {
  // Auth
  async login(username: string, password: string) {
    const res = await fetch(`${API_BASE_URL}/authapi/login`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ username, password }),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Login failed.');

    if (data.accessToken) {
      localStorage.setItem('accessToken', data.accessToken);
      localStorage.setItem('token', data.accessToken);
    }
    if (data.refreshToken) {
      localStorage.setItem('refreshToken', data.refreshToken);
    }
    if (data.user) {
      localStorage.setItem('user', JSON.stringify(data.user));
    }
    return data;
  },

  async register(username: string, password: string, role = 'User') {
    const res = await fetch(`${API_BASE_URL}/authapi/register`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ username, password, role }),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Registration failed.');

    if (data.accessToken) {
      localStorage.setItem('accessToken', data.accessToken);
      localStorage.setItem('token', data.accessToken);
    }
    if (data.refreshToken) {
      localStorage.setItem('refreshToken', data.refreshToken);
    }
    if (data.user) {
      localStorage.setItem('user', JSON.stringify(data.user));
    }
    return data;
  },

  async refreshToken() {
    const currentRefreshToken = localStorage.getItem('refreshToken');
    if (!currentRefreshToken) return null;

    try {
      const res = await fetch(`${API_BASE_URL}/authapi/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: currentRefreshToken }),
      });

      if (!res.ok) {
        localStorage.removeItem('user');
        localStorage.removeItem('accessToken');
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        return null;
      }

      const data = await res.json();
      if (data.accessToken && data.refreshToken) {
        localStorage.setItem('accessToken', data.accessToken);
        localStorage.setItem('token', data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
        if (data.user) {
          localStorage.setItem('user', JSON.stringify(data.user));
        }
      }
      return data;
    } catch {
      return null;
    }
  },

  async logout() {
    const refreshToken = localStorage.getItem('refreshToken');
    const userStr = localStorage.getItem('user');
    let userId: number | undefined;
    if (userStr) {
      try {
        const user = JSON.parse(userStr);
        userId = user.id;
      } catch {}
    }

    try {
      await fetch(`${API_BASE_URL}/authapi/logout`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ refreshToken, userId }),
      });
    } catch {}

    localStorage.removeItem('user');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
  },

  async getCurrentUser() {
    let res = await fetch(`${API_BASE_URL}/authapi/me`, {
      headers: getHeaders(),
    });

    if (res.status === 401) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        res = await fetch(`${API_BASE_URL}/authapi/me`, {
          headers: getHeaders(),
        });
      }
    }

    if (!res.ok) return null;
    return await res.json();
  },

  // Products
  async getProducts(params?: {
    search?: string;
    category?: string;
    stockFilter?: string;
    page?: number;
    pageSize?: number;
  }) {
    const query = new URLSearchParams();
    if (params?.search) query.append('search', params.search);
    if (params?.category) query.append('category', params.category);
    if (params?.stockFilter) query.append('stockFilter', params.stockFilter);
    if (params?.page) query.append('page', params.page.toString());
    if (params?.pageSize) query.append('pageSize', params.pageSize.toString());

    const res = await fetch(`${API_BASE_URL}/productsapi?${query.toString()}`);
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to fetch products.');
    return data;
  },

  async getTopProducts(count = 5): Promise<any[]> {
    try {
      const res = await fetch(`${API_BASE_URL}/productsapi/top?count=${count}`);
      if (!res.ok) return [];
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    } catch {
      return [];
    }
  },

  async getCategories(): Promise<string[]> {
    const res = await fetch(`${API_BASE_URL}/productsapi/categories`);
    if (!res.ok) return [];
    return await res.json();
  },

  async createProduct(formData: FormData) {
    const res = await fetch(`${API_BASE_URL}/productsapi`, {
      method: 'POST',
      headers: getHeaders(true),
      body: formData,
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to create product.');
    return data;
  },

  async updateProduct(id: number, formData: FormData) {
    const res = await fetch(`${API_BASE_URL}/productsapi/${id}`, {
      method: 'PUT',
      headers: getHeaders(true),
      body: formData,
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to update product.');
    return data;
  },

  async toggleProductVisibility(id: number) {
    const res = await fetch(`${API_BASE_URL}/productsapi/${id}/visibility`, {
      method: 'PATCH',
      headers: getHeaders(),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to toggle visibility.');
    return data;
  },

  async deleteProduct(id: number) {
    const res = await fetch(`${API_BASE_URL}/productsapi/${id}`, {
      method: 'DELETE',
      headers: getHeaders(),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to delete product.');
    return data;
  },

  // Cart
  async getCart() {
    let res = await fetch(`${API_BASE_URL}/cartapi`, {
      headers: getHeaders(),
    });

    if (res.status === 401) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        res = await fetch(`${API_BASE_URL}/cartapi`, {
          headers: getHeaders(),
        });
      }
    }

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to fetch cart.');
    return data;
  },

  async getCartCount() {
    const res = await fetch(`${API_BASE_URL}/cartapi/count`, {
      headers: getHeaders(),
    });
    const data = await res.json();
    return data.count || 0;
  },

  async addToCart(productId: number, quantity = 1) {
    let res = await fetch(`${API_BASE_URL}/cartapi/add`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ productId, quantity }),
    });

    if (res.status === 401) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        res = await fetch(`${API_BASE_URL}/cartapi/add`, {
          method: 'POST',
          headers: getHeaders(),
          body: JSON.stringify({ productId, quantity }),
        });
      }
    }

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to add item to cart.');
    return data;
  },

  async updateCartQuantity(productId: number, quantity: number) {
    let res = await fetch(`${API_BASE_URL}/cartapi/update`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify({ productId, quantity }),
    });

    if (res.status === 401) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        res = await fetch(`${API_BASE_URL}/cartapi/update`, {
          method: 'PUT',
          headers: getHeaders(),
          body: JSON.stringify({ productId, quantity }),
        });
      }
    }

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to update cart quantity.');
    return data;
  },

  async removeFromCart(productId: number) {
    let res = await fetch(`${API_BASE_URL}/cartapi/remove/${productId}`, {
      method: 'DELETE',
      headers: getHeaders(),
    });

    if (res.status === 401) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        res = await fetch(`${API_BASE_URL}/cartapi/remove/${productId}`, {
          method: 'DELETE',
          headers: getHeaders(),
        });
      }
    }

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to remove cart item.');
    return data;
  },

  // Orders
  async checkout(paymentMethod: 'cash' | 'upi') {
    let res = await fetch(`${API_BASE_URL}/ordersapi/checkout`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ paymentMethod }),
    });

    if (res.status === 401) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        res = await fetch(`${API_BASE_URL}/ordersapi/checkout`, {
          method: 'POST',
          headers: getHeaders(),
          body: JSON.stringify({ paymentMethod }),
        });
      }
    }

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Checkout failed.');
    return data;
  },

  async getMyOrders() {
    let res = await fetch(`${API_BASE_URL}/ordersapi/my-orders`, {
      headers: getHeaders(),
    });

    if (res.status === 401) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        res = await fetch(`${API_BASE_URL}/ordersapi/my-orders`, {
          headers: getHeaders(),
        });
      }
    }

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to fetch order history.');
    return data;
  },

  async getAllOrders() {
    let res = await fetch(`${API_BASE_URL}/ordersapi/all-orders`, {
      headers: getHeaders(),
    });

    if (res.status === 401) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        res = await fetch(`${API_BASE_URL}/ordersapi/all-orders`, {
          headers: getHeaders(),
        });
      }
    }

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to fetch all orders.');
    return data;
  },

  // Admin Features
  async getStockManagement() {
    const res = await fetch(`${API_BASE_URL}/adminapi/stock-management`, {
      headers: getHeaders(),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to fetch stock management data.');
    return data;
  },

  async getStockAlerts() {
    const res = await fetch(`${API_BASE_URL}/adminapi/stock-alerts`, {
      headers: getHeaders(),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to fetch stock alerts.');
    return data;
  },

  async bulkUpdateStock(stockUpdates: Array<{ productId: number; newStockQuantity: number; newLowStockThreshold: number }>) {
    const res = await fetch(`${API_BASE_URL}/adminapi/bulk-update-stock`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(stockUpdates),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to update stock in bulk.');
    return data;
  },

  async bulkCreateProducts(products: Array<{ name: string; category: string; price: number; stockQuantity: number; lowStockThreshold: number; isVisible: boolean; imageUrl?: string }>) {
    const res = await fetch(`${API_BASE_URL}/adminapi/bulk-create`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(products),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to create products in bulk.');
    return data;
  },

  async bulkCreateFromCsv(csvFile: File) {
    const formData = new FormData();
    formData.append('csvFile', csvFile);

    const res = await fetch(`${API_BASE_URL}/adminapi/bulk-create-csv`, {
      method: 'POST',
      headers: getHeaders(true),
      body: formData,
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to upload CSV.');
    return data;
  },

  downloadCsvTemplateUrl() {
    return `${API_BASE_URL}/adminapi/download-csv-template`;
  },

  async uploadOcrInventory(file: File, defaultThreshold = 5) {
    const formData = new FormData();
    formData.append('file', file);

    const res = await fetch(`${API_BASE_URL}/adminapi/inventory-upload?defaultThreshold=${defaultThreshold}`, {
      method: 'POST',
      headers: getHeaders(true),
      body: formData,
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to process OCR inventory upload.');
    return data;
  },

  // Reports
  async getDailySalesReport(date?: string) {
    const query = date ? `?date=${date}` : '';
    const res = await fetch(`${API_BASE_URL}/reportsapi/daily${query}`, {
      headers: getHeaders(),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to load sales report.');
    return data;
  },

  downloadPdfReportUrl(date?: string) {
    const query = date ? `?date=${date}` : '';
    return `${API_BASE_URL}/reportsapi/pdf${query}`;
  },

  downloadExcelReportUrl(date?: string) {
    const query = date ? `?date=${date}` : '';
    return `${API_BASE_URL}/reportsapi/excel${query}`;
  }
};
