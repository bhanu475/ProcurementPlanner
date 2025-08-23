import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import SupplierDashboard from '../SupplierDashboard';
import { supplierApi } from '../../../services/supplierApi';
import { PurchaseOrder, SupplierPerformanceMetrics } from '../../../types';

// Mock the supplier API
vi.mock('../../../services/supplierApi', () => ({
  supplierApi: {
    getPurchaseOrders: vi.fn(),
    getPerformanceMetrics: vi.fn(),
  },
}));

const mockPurchaseOrders: PurchaseOrder[] = [
  {
    id: '1',
    purchaseOrderNumber: 'PO-001',
    customerOrderId: 'co-1',
    supplierId: 'supplier-1',
    status: 'SentToSupplier',
    requiredDeliveryDate: '2024-01-15',
    items: [
      {
        id: 'item-1',
        purchaseOrderId: '1',
        orderItemId: 'oi-1',
        allocatedQuantity: 10,
      },
    ],
    createdAt: '2024-01-01',
    createdBy: 'planner-1',
  },
  {
    id: '2',
    purchaseOrderNumber: 'PO-002',
    customerOrderId: 'co-2',
    supplierId: 'supplier-1',
    status: 'Confirmed',
    requiredDeliveryDate: '2024-01-20',
    items: [
      {
        id: 'item-2',
        purchaseOrderId: '2',
        orderItemId: 'oi-2',
        allocatedQuantity: 5,
      },
    ],
    createdAt: '2024-01-02',
    createdBy: 'planner-1',
  },
];

const mockPerformanceMetrics: SupplierPerformanceMetrics = {
  supplierId: 'supplier-1',
  onTimeDeliveryRate: 0.95,
  qualityScore: 8.5,
  totalOrdersCompleted: 150,
  lastUpdated: '2024-01-01',
};

describe('SupplierDashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially', () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockImplementation(() => new Promise(() => {}));
    vi.mocked(supplierApi.getPerformanceMetrics).mockImplementation(() => new Promise(() => {}));

    render(<SupplierDashboard supplierId="supplier-1" />);

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('renders dashboard data successfully', async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue({
      data: mockPurchaseOrders,
    } as any);
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPerformanceMetrics,
    } as any);

    render(<SupplierDashboard supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('Pending Orders')).toBeInTheDocument();
      expect(screen.getByText('Confirmed Orders')).toBeInTheDocument();
      expect(screen.getByText('Total Orders')).toBeInTheDocument();
    });

    // Check performance metrics
    expect(screen.getByText('95.0%')).toBeInTheDocument(); // On-time delivery rate
    expect(screen.getByText('8.5/10')).toBeInTheDocument(); // Quality score
    expect(screen.getByText('150')).toBeInTheDocument(); // Total orders completed
  });

  it('displays purchase orders table', async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue({
      data: mockPurchaseOrders,
    } as any);
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPerformanceMetrics,
    } as any);

    render(<SupplierDashboard supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('PO-001')).toBeInTheDocument();
      expect(screen.getByText('PO-002')).toBeInTheDocument();
      expect(screen.getByText('SentToSupplier')).toBeInTheDocument();
      expect(screen.getByText('Confirmed')).toBeInTheDocument();
    });
  });

  it('handles API errors gracefully', async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockRejectedValue(new Error('API Error'));
    vi.mocked(supplierApi.getPerformanceMetrics).mockRejectedValue(new Error('API Error'));

    render(<SupplierDashboard supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load dashboard data')).toBeInTheDocument();
    });
  });

  it('displays empty state when no orders exist', async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue({
      data: [],
    } as any);
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPerformanceMetrics,
    } as any);

    render(<SupplierDashboard supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('No purchase orders available.')).toBeInTheDocument();
    });
  });
});