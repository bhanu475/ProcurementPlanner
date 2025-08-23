import apiClient from './api';
import { PurchaseOrder, SupplierPerformanceMetrics } from '../types';

export interface SupplierConfirmationData {
  status: 'Confirmed' | 'Rejected';
  supplierNotes: string;
  items?: Array<{
    id: string;
    allocatedQuantity: number;
    packagingDetails?: string;
    deliveryMethod?: string;
    estimatedDeliveryDate?: string;
  }>;
}

export const supplierApi = {
  // Get purchase orders for a specific supplier
  getPurchaseOrders: (supplierId: string) => {
    return apiClient.get<PurchaseOrder[]>(`/procurement/supplier/${supplierId}`);
  },

  // Get supplier performance metrics
  getPerformanceMetrics: (supplierId: string) => {
    return apiClient.get<SupplierPerformanceMetrics>(`/suppliers/${supplierId}/performance`);
  },

  // Confirm or reject a purchase order
  confirmPurchaseOrder: (purchaseOrderId: string, confirmationData: SupplierConfirmationData) => {
    return apiClient.put<PurchaseOrder>(`/procurement/${purchaseOrderId}/confirm`, confirmationData);
  },

  // Get purchase order details
  getPurchaseOrderDetails: (purchaseOrderId: string) => {
    return apiClient.get<PurchaseOrder>(`/procurement/${purchaseOrderId}`);
  },

  // Update purchase order status
  updatePurchaseOrderStatus: (purchaseOrderId: string, status: string, notes?: string) => {
    return apiClient.put<PurchaseOrder>(`/procurement/${purchaseOrderId}/status`, {
      status,
      supplierNotes: notes
    });
  }
};