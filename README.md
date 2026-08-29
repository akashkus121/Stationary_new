# 🖋️ Lumina Atelier - Executive Stationery & Workspace Management System

A full-stack, enterprise-grade Stationery & Inventory Management platform built with **ASP.NET Core 8 Web API** and **React 19 + TypeScript (Vite)**. Featuring high-availability order queuing via **Upstash Redis Message Queue**, dual-layer caching, real-time stock sync via Server-Sent Events (SSE), PostgreSQL (Supabase) database persistence, PDF/Excel sales intelligence reporting, and a luxury executive storefront.

[![.NET 8](https://img.shields.io/badge/.NET-8.0_Web_API-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19.0_TypeScript-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![Vite](https://img.shields.io/badge/Vite-6.0-646CFF?logo=vite&logoColor=white)](https://vitejs.dev/)
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL_Supabase-336791?logo=postgresql&logoColor=white)](https://supabase.com/)
[![Redis](https://img.shields.io/badge/Message_Queue-Upstash_Redis-DC382D?logo=redis&logoColor=white)](https://upstash.com/)
[![Docker](https://img.shields.io/badge/Container-Docker_Multi--Stage-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Render](https://img.shields.io/badge/Backend_Deploy-Render-46E3B7?logo=render&logoColor=black)](https://render.com/)
[![Vercel](https://img.shields.io/badge/Frontend_Deploy-Vercel-000000?logo=vercel&logoColor=white)](https://vercel.com/)

---

## ⚡ High-Availability Architecture & Resilient Message Queue

```
                       [ Customer Places Order ]
                                  │
                                  ▼
               [ PostgreSQL Primary Database Accessible? ]
                       ├─────────────────────┬─────────────────────┐
                     YES                     │                    NO / TIMEOUT
                      ▼                      │                     ▼
             [ Commit to DB ]                │    [ Enqueue to Upstash Redis Message Queue ]
                                             │      • LPUSH orders:pending
                                             │      • Store snapshot in orders:user:{id}
                                             │      • 0ms Lost Orders Guarantee!
                                             │
                                             ▼
                     [ PendingQueueProcessorService (Background Worker) ]
                                             │
                                             ├─► Monitors database connectivity every 15s
                                             └─► RPOP orders:pending ──► Writes to DB ──► Decrements Stock ──► SSE Confirm
```

- **Zero-Downtime Checkout**: If the primary PostgreSQL database suffers network partitions, connection limits, or cloud maintenance, orders are automatically routed into the **Upstash Redis Message Queue (`orders:pending`)**.
- **Instant Customer Visibility**: Queued orders are simultaneously written to the customer's Redis cache key (`orders:user:{userId}`), allowing users to immediately review their order in **My Orders** without interruption.
- **Automated Background Sync**: `PendingQueueProcessorService` monitors database health in the background and drains the Redis queue to the master database once reconnected.

---

## 🌟 Key Capabilities & Features

### 🛍️ Luxury Customer Storefront
- **Static Best Sellers Showcase**: Clean 4-product curated showcase with rank badges (no intrusive auto-sliding carousels).
- **Debounced Instant Search**: 350ms debounced live search for seamless item and category filtering without server churn.
- **Dual-Layer Product Caching**:
  - **Backend Distributed Cache**: Redis stores individual product details (`products:id:{id}`), categories, and filtered product queries.
  - **Frontend Memory Cache**: Client caches responses for instant 0ms category switching.
- **Targeted SSE In-Memory Updates**: Real-time stock decrements update specific items directly in client state without re-fetching entire product pages over the network.
- **Floating Action Navigation (FABs)**:
  - **Cart FAB (Bottom-Right)**: Pulsing item counter badge, real-time total quantity indicator.
  - **My Orders FAB (Bottom-Left)**: Instant access to purchase ledger and delivery receipts.
- **Stock-Aware Cart Steppers**: Automatically transforms the single `Add to Cart` button into an interactive quantity stepper `[-] Qty [+]` when added to cart.
- **Seamless Multi-Option Checkout**: Supports UPI (Copy ID / QR Code), Credit/Debit Card (auto-formatting & CVV), and Cash on Delivery.
- **Localized Pricing**: Formatted with standard Indian Rupee (`Rs.`) throughout.

### 🛡️ Admin Management Suite
- **Stock Management & Alerts**: Quick-stock stepper controls, low-stock threshold badges, and bulk save workflows.
- **Batch CSV Product Ingestion**: Download pre-formatted CSV template, live CSV preview table with validation badges, and 1-click batch database insertion.
- **Sales Intelligence & Reports**:
  - Real-time KPIs: Gross Revenue, Orders Placed, Units Sold, and Average Order Value (AOV).
  - Daily Transaction Ledger with instant search and date presets (Today, Yesterday, 7 Days Ago).
  - 1-Click PDF Statement generation via **QuestPDF** and Excel spreadsheets via **ClosedXML**.
- **Real-Time SSE Sync**: Automatically broadcasts inventory and purchase events across connected browser sessions via `/api/events/stream`.

---

## 📁 Repository Structure

```
Stationary_new/
├── Stationary/                     # ASP.NET Core 8 Web API Backend
│   ├── Controllers/                # REST API Controllers
│   │   ├── AuthController.cs       # JWT Authentication & Auto-seed
│   │   ├── ProductsController.cs   # Product CRUD, Caching & Pagination
│   │   ├── CartController.cs       # Stock-validated Cart Operations
│   │   ├── OrdersController.cs     # Checkout, Upstash Redis Queue & Receipts
│   │   ├── AdminController.cs      # Stock Steppers, Visibility & CSV Ingestion
│   │   ├── ReportsController.cs    # Daily Sales, PDF & Excel Exports
│   │   └── EventsController.cs     # Server-Sent Events (SSE) Stock Stream
│   ├── Data/                       # Entity Framework Core & Supabase Context
│   │   ├── ApplicationDbContext.cs # Npgsql EF Core DBContext
│   │   └── DatabaseSetup/          # PostgreSQL installation & stored procedures
│   ├── Models/                     # Data Models & DTOs
│   │   ├── Product.cs              # Product Entity
│   │   ├── User.cs                 # User Entity & Role Enum
│   │   ├── Cart.cs                 # Cart Item DTOs
│   │   └── Order.cs                # Order & OrderItem Entities
│   ├── Services/                   # Business Services & Cloud Integrations
│   │   ├── ProductService.cs       # Product Repository Logic & Redis Caching
│   │   ├── CartService.cs          # Redis + DB Cart Synchronization
│   │   ├── RedisCacheService.cs    # Upstash Redis Queue + Memory Fallback
│   │   ├── PendingQueueProcessorService.cs # Background Redis Queue Worker
│   │   ├── EventStreamService.cs   # Real-time SSE Stock Broadcast Service
│   │   └── CloudinaryService.cs    # Cloudinary Image Hosting Integration
│   ├── appsettings.json            # Configuration & Connection Strings
│   ├── Dockerfile                  # Backend Multi-Stage Dockerfile (QuestPDF + OCR)
│   └── Stationary.csproj           # .NET 8 Project Dependencies
│
├── frontend/                       # React 19 + TypeScript Frontend (Vite)
│   ├── public/                     # Static Assets & Icons
│   ├── src/
│   │   ├── components/             # Reusable UI Components
│   │   │   ├── Navbar.tsx          # Top Bar & User Avatar
│   │   │   ├── TopSellingSection.tsx # Static 4-Product Best Sellers Grid
│   │   │   ├── ProductCard.tsx     # Streamlined Product Card with Stepper
│   │   │   ├── CartDrawer.tsx      # Slide-out Shopping Cart & Breakdown
│   │   │   ├── CheckoutModal.tsx   # Multi-step Payment & Receipt Modal
│   │   │   ├── MyOrdersModal.tsx   # Purchase Ledger & Tracking Modal
│   │   │   ├── AuthModal.tsx       # Luxury Segmented Sign In / Register Modal
│   │   │   └── UserProfileModal.tsx # Account Settings & Lifetime Stats
│   │   ├── context/                # React Context Providers
│   │   │   ├── AuthContext.tsx     # Authentication State & Auto-refresh
│   │   │   └── CartContext.tsx     # Cart State, Counts & Steppers
│   │   ├── pages/
│   │   │   ├── CatalogPage.tsx     # Executive Storefront with Debounced Search
│   │   │   └── AdminDashboard.tsx  # 5-Tab Admin Management Suite
│   │   ├── services/
│   │   │   ├── api.ts              # Fetch API Client & In-Memory Cache
│   │   │   └── sse.ts              # SSE EventSource Connection Manager
│   │   ├── index.css               # Luxury Executive Design System (Vanilla CSS)
│   │   ├── App.tsx                 # Root Layout & Dynamic Tab Router
│   │   └── main.tsx                # React Root Entrypoint
│   ├── .env.production             # Render API Base URL
│   ├── package.json                # Dependencies & Scripts
│   ├── tsconfig.json               # TypeScript Compiler Configuration
│   └── vite.config.ts              # Vite Bundler Setup
│
├── Dockerfile                      # Root Dockerfile for Render Deployments
├── render.yaml                     # Render Infrastructure as Code (Blueprint)
└── README.md                       # Documentation & Project Guide
```

---

## 🛠️ Technology Stack

| Layer | Technologies |
|---|---|
| **Frontend** | React 19, TypeScript, Vite, Lucide Icons, Vanilla CSS Design System |
| **Backend** | ASP.NET Core 8 Web API, C#, Entity Framework Core 9 |
| **Database** | PostgreSQL on Supabase (Npgsql) |
| **Message Queue & Cache** | Upstash Redis Distributed Queue (`LPUSH`/`RPOP`) + Distributed Cache |
| **Document Generation** | QuestPDF (PDF Statements), ClosedXML (Excel Reports) |
| **Media Hosting** | Cloudinary DotNet API |
| **Real-time Engine** | Server-Sent Events (SSE) `/api/events/stream` |
| **Container & Cloud** | Docker Multi-Stage Build, Render (Backend API), Vercel (Frontend) |

---

## 🚀 Getting Started Locally

### 1. Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) & npm

### 2. Backend Setup (`/Stationary`)
```bash
# Navigate to backend directory
cd Stationary

# Restore dependencies
dotnet restore

# Run API (defaults to http://localhost:5000)
dotnet run
```

### 3. Frontend Setup (`/frontend`)
```bash
# Navigate to frontend directory
cd ../frontend

# Install dependencies
npm install

# Start development server (defaults to http://localhost:5173)
npm run dev
```

### 4. Default Demo Credentials
- **Standard User**: `test` / `12345` (or 1-click fill in the Login Modal)
- **Admin**: `akash` / `12345`

---

## ☁️ Deployment

### Backend on Render (Docker)
1. Link your repository in [Render.com](https://dashboard.render.com/).
2. Select **Docker** environment.
   - **Docker Context**: `./Stationary`
   - **Dockerfile Path**: `./Stationary/Dockerfile`
3. Set environment variables:
   - `ConnectionStrings__DefaultConnection`: Your Supabase PostgreSQL connection string.
   - `ConnectionStrings__Redis`: Your Upstash Redis connection string.
   - `Jwt__Secret`: Your JWT signing secret.

### Frontend on Vercel
1. Import repository in [Vercel](https://vercel.com/).
2. Set **Root Directory** to `frontend`.
3. Set environment variable:
   ```env
   VITE_API_BASE_URL=https://<your-render-backend-url>/api
   ```
4. Deploy!

---

## 📝 License
This project is licensed under the [MIT License](LICENSE).
