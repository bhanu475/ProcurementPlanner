// Common types for the procurement planner application

export interface User {
  id: string;
  email: string;
  role: 'lmr_planner' | 'supplier' | 'customer';
  name: string;
}

export interface Order {
  id: string;
  customerName: string;
  items: OrderItem[];
  status: 'pending' | 'confirmed' | 'in_progress' | 'delivered';
  createdAt: string;
  updatedAt: string;
}

export interface OrderItem {
  id: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  supplierId?: string;
}

export interface Supplier {
  id: string;
  name: string;
  email: string;
  capabilities: string[];
  performanceRating: number;
}

export interface PurchaseOrder {
  id: string;
  supplierId: string;
  items: OrderItem[];
  status: 'pending' | 'confirmed' | 'in_progress' | 'completed';
  deliveryDate: string;
  createdAt: string;
}