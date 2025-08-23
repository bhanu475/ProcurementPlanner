# Procurement Planner Frontend

A React TypeScript application for the Procurement Planner system, built with Vite and modern web technologies.

## Features

- **React 18** with TypeScript
- **React Router** for client-side routing
- **Redux Toolkit** for state management
- **Tailwind CSS** for styling
- **SignalR** for real-time updates
- **Axios** for API communication
- **Vitest** for testing
- **Role-based authentication** and protected routes

## Project Structure

```
src/
├── components/
│   ├── Auth/
│   │   └── ProtectedRoute.tsx
│   └── Layout/
│       ├── Header.tsx
│       ├── Layout.tsx
│       └── __tests__/
├── pages/
│   ├── Login.tsx
│   ├── Dashboard.tsx
│   ├── SupplierPortal.tsx
│   ├── CustomerOrders.tsx
│   └── Unauthorized.tsx
├── services/
│   ├── api.ts
│   └── signalr.ts
├── store/
│   ├── index.ts
│   └── slices/
│       ├── authSlice.ts
│       ├── ordersSlice.ts
│       └── suppliersSlice.ts
├── types/
│   └── index.ts
└── test/
    └── setup.ts
```

## Getting Started

### Prerequisites

- Node.js (v18 or higher)
- npm or yarn

### Installation

1. Install dependencies:
```bash
npm install
```

2. Copy environment variables:
```bash
cp .env.example .env
```

3. Update the `.env` file with your API base URL:
```
VITE_API_BASE_URL=http://localhost:5000/api
```

### Development

Start the development server:
```bash
npm run dev
```

The application will be available at `http://localhost:5173`

### Building

Build for production:
```bash
npm run build
```

### Testing

Run tests:
```bash
npm run test
```

Run tests once:
```bash
npm run test:run
```

## User Roles

The application supports three user roles:

1. **LMR Planner** - Access to dashboard for order management and supplier coordination
2. **Supplier** - Access to supplier portal for purchase order management
3. **Customer** - Access to order tracking and submission

## Authentication

The application uses JWT-based authentication with automatic token refresh and role-based route protection.

## Real-time Updates

SignalR is integrated for real-time order status updates across all connected clients.

## API Integration

The application communicates with the backend API using Axios with automatic authentication token handling and error management.