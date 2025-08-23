import apiClient from './api';
import { CustomerOrder, PagedResult, OrderFilter } from '../types';

export interface CreateOrderRequest {
  customerId: string;
  customerName: string;
  productType: 'LMR' | 'FFV';
  requestedDeliveryDate: string;
  items: CreateOrderItemRequest[];
}

export interface CreateOrderItemRequest {
  productCode: string;
  description: string;
  quantity: number;
  unit: string;
  specifications?: string;
}

export interface OrderTrackingResponse {
  order: CustomerOrder;
  timeline: OrderTimelineEvent[];
  estimatedDeliveryDate?: string;
}

export interface OrderTimelineEvent {
  id: string;
  orderId: string;
  status: string;
  description: string;
  timestamp: string;
  details?: string;
}

class CustomerApiService {
  // Submit a new order
  async submitOrder(orderData: CreateOrderRequest): Promise<CustomerOrder> {
    const response = await apiClient.post('/orders', orderData);
    return response.data;
  }

  // Get customer's orders with filtering and pagination
  async getMyOrders(
    filter?: OrderFilter,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResult<CustomerOrder>> {
    const params = new URLSearchParams();
    
    if (filter?.status?.length) {
      filter.status.forEach(status => params.append('status', status));
    }
    if (filter?.productType?.length) {
      filter.productType.forEach(type => params.append('productType', type));
    }
    if (filter?.startDate) params.append('startDate', filter.startDate);
    if (filter?.endDate) params.append('endDate', filter.endDate);
    if (filter?.searchTerm) params.append('searchTerm', filter.searchTerm);
    
    params.append('pageNumber', pageNumber.toString());
    params.append('pageSize', pageSize.toString());

    const response = await apiClient.get(`/orders/my-orders?${params.toString()}`);
    return response.data;
  }

  // Get detailed order tracking information
  async getOrderTracking(orderId: string): Promise<OrderTrackingResponse> {
    const response = await apiClient.get(`/orders/${orderId}/tracking`);
    return response.data;
  }

  // Get order by ID
  async getOrderById(orderId: string): Promise<CustomerOrder> {
    const response = await apiClient.get(`/orders/${orderId}`);
    return response.data;
  }

  // Cancel an order (if allowed)
  async cancelOrder(orderId: string, reason?: string): Promise<void> {
    await apiClient.put(`/orders/${orderId}/cancel`, { reason });
  }
}

export const customerApiService = new CustomerApiService();