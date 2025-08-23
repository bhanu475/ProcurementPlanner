// Common types for the procurement planner application

export interface User {
  id: string;
  email: string;
  role: 'lmr_planner' | 'supplier' | 'customer';
  name: string;
}

export type ProductType = 'LMR' | 'FFV';

export type OrderStatus = 
  | 'Submitted'
  | 'UnderReview'
  | 'PlanningInProgress'
  | 'PurchaseOrdersCreated'
  | 'AwaitingSupplierConfirmation'
  | 'InProduction'
  | 'ReadyForDelivery'
  | 'Delivered'
  | 'Cancelled';

export type PurchaseOrderStatus = 
  | 'Created'
  | 'SentToSupplier'
  | 'Confirmed'
  | 'Rejected'
  | 'InProduction'
  | 'ReadyForShipment'
  | 'Shipped'
  | 'Delivered';

export interface CustomerOrder {
  id: string;
  orderNumber: string;
  customerId: string; // DODAAC
  customerName: string;
  productType: ProductType;
  requestedDeliveryDate: string;
  status: OrderStatus;
  items: OrderItem[];
  createdAt: string;
  createdBy: string;
}

export interface OrderItem {
  id: string;
  orderId: string;
  productCode: string;
  description: string;
  quantity: number;
  unit: string;
  specifications: string;
}

export interface Supplier {
  id: string;
  name: string;
  contactEmail: string;
  contactPhone: string;
  address: string;
  isActive: boolean;
  capabilities: SupplierCapability[];
  performance: SupplierPerformanceMetrics;
}

export interface SupplierCapability {
  id: string;
  supplierId: string;
  productType: ProductType;
  maxMonthlyCapacity: number;
  currentCommitments: number;
  qualityRating: number;
}

export interface SupplierPerformanceMetrics {
  supplierId: string;
  onTimeDeliveryRate: number;
  qualityScore: number;
  totalOrdersCompleted: number;
  lastUpdated: string;
}

export interface PurchaseOrder {
  id: string;
  purchaseOrderNumber: string;
  customerOrderId: string;
  supplierId: string;
  status: PurchaseOrderStatus;
  requiredDeliveryDate: string;
  items: PurchaseOrderItem[];
  createdAt: string;
  createdBy: string;
  confirmedAt?: string;
  supplierNotes?: string;
}

export interface PurchaseOrderItem {
  id: string;
  purchaseOrderId: string;
  orderItemId: string;
  allocatedQuantity: number;
  packagingDetails?: string;
  deliveryMethod?: string;
  estimatedDeliveryDate?: string;
}

export interface DistributionSuggestion {
  customerOrderId: string;
  suggestions: SupplierAllocation[];
  totalCapacityUtilization: number;
}

export interface SupplierAllocation {
  supplierId: string;
  supplierName: string;
  allocatedQuantity: number;
  capacityUtilization: number;
  performanceScore: number;
  items: OrderItemAllocation[];
}

export interface OrderItemAllocation {
  orderItemId: string;
  allocatedQuantity: number;
}

export interface DashboardSummary {
  totalOrders: number;
  pendingOrders: number;
  ordersInProgress: number;
  completedOrders: number;
  activeSuppliers: number;
  ordersThisMonth: number;
  ordersByStatus: { [key in OrderStatus]: number };
  ordersByProductType: { [key in ProductType]: number };
}

export interface OrderFilter {
  status?: OrderStatus[];
  productType?: ProductType[];
  customerId?: string;
  startDate?: string;
  endDate?: string;
  searchTerm?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// Legacy types for backward compatibility
export interface Order extends CustomerOrder {}
export interface OrderItem extends OrderItem {}