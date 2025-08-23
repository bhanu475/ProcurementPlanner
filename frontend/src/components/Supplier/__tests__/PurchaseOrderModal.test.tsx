import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import PurchaseOrderModal from '../PurchaseOrderModal';
import { supplierApi } from '../../../services/supplierApi';
import { PurchaseOrder } from '../../../types';

// Mock the supplier API
vi.mock('../../../services/supplierApi', () => ({
  supplierApi: {
    confirmPurchaseOrder: vi.fn(),
  },
}));

// Mock the PackagingDeliveryForm component
vi.mock('../PackagingDeliveryForm', () => ({
  default: ({ onSubmit, onCancel, loading }: any) => (
    <div data-testid="packaging-form">
      <button onClick={() => onSubmit({ 'item-1': { packagingDetails: 'Box', deliveryMethod: 'Standard', estimatedDeliveryDate: '2024-01-20' } })}>
        Submit Packaging
      </button>
      <button onClick={onCancel}>Cancel Packaging</button>
      <span>{loading ? 'Loading...' : 'Ready'}</span>
    </div>
  ),
}));

const mockPurchaseOrder: PurchaseOrder = {
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
};

const mockConfirmedOrder: PurchaseOrder = {
  ...mockPurchaseOrder,
  status: 'Confirmed',
  confirmedAt: '2024-01-02',
  supplierNotes: 'Order confirmed',
};

describe('PurchaseOrderModal', () => {
  const mockOnClose = vi.fn();
  const mockOnUpdate = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders purchase order details correctly', () => {
    render(
      <PurchaseOrderModal
        purchaseOrder={mockPurchaseOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    expect(screen.getByText('Purchase Order Details - PO-001')).toBeInTheDocument();
    expect(screen.getByText('SentToSupplier')).toBeInTheDocument();
    expect(screen.getByText('1/15/2024')).toBeInTheDocument(); // Required delivery date
    expect(screen.getAllByText('10')).toHaveLength(2); // Total items and allocated quantity
  });

  it('shows confirm and reject buttons for pending orders', () => {
    render(
      <PurchaseOrderModal
        purchaseOrder={mockPurchaseOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    expect(screen.getByText('Confirm Order')).toBeInTheDocument();
    expect(screen.getByText('Reject Order')).toBeInTheDocument();
  });

  it('does not show action buttons for confirmed orders', () => {
    render(
      <PurchaseOrderModal
        purchaseOrder={mockConfirmedOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    expect(screen.queryByText('Confirm Order')).not.toBeInTheDocument();
    expect(screen.queryByText('Reject Order')).not.toBeInTheDocument();
  });

  it('opens packaging form when confirm order is clicked', () => {
    render(
      <PurchaseOrderModal
        purchaseOrder={mockPurchaseOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    const confirmButton = screen.getByText('Confirm Order');
    fireEvent.click(confirmButton);

    expect(screen.getByTestId('packaging-form')).toBeInTheDocument();
  });

  it('opens rejection form when reject order is clicked', () => {
    render(
      <PurchaseOrderModal
        purchaseOrder={mockPurchaseOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    const rejectButton = screen.getByText('Reject Order');
    fireEvent.click(rejectButton);

    expect(screen.getByText('Reason for Rejection')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Please provide a reason for rejecting this order...')).toBeInTheDocument();
  });

  it('handles order confirmation successfully', async () => {
    const updatedOrder = { ...mockPurchaseOrder, status: 'Confirmed' as const };
    vi.mocked(supplierApi.confirmPurchaseOrder).mockResolvedValue({
      data: updatedOrder,
    } as any);

    render(
      <PurchaseOrderModal
        purchaseOrder={mockPurchaseOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    // Open packaging form
    const confirmButton = screen.getByText('Confirm Order');
    fireEvent.click(confirmButton);

    // Submit packaging form
    const submitButton = screen.getByText('Submit Packaging');
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(supplierApi.confirmPurchaseOrder).toHaveBeenCalledWith('1', {
        status: 'Confirmed',
        supplierNotes: 'Order confirmed by supplier',
        items: expect.any(Array),
      });
      expect(mockOnUpdate).toHaveBeenCalledWith(updatedOrder);
    });
  });

  it('handles order rejection successfully', async () => {
    const rejectedOrder = { ...mockPurchaseOrder, status: 'Rejected' as const };
    vi.mocked(supplierApi.confirmPurchaseOrder).mockResolvedValue({
      data: rejectedOrder,
    } as unknown);

    render(
      <PurchaseOrderModal
        purchaseOrder={mockPurchaseOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    // Open rejection form
    const rejectButton = screen.getByText('Reject Order');
    fireEvent.click(rejectButton);

    // Enter rejection reason
    const reasonTextarea = screen.getByPlaceholderText('Please provide a reason for rejecting this order...');
    fireEvent.change(reasonTextarea, { target: { value: 'Cannot fulfill due to capacity constraints' } });

    // Submit rejection
    const confirmRejectionButton = screen.getByText('Confirm Rejection');
    fireEvent.click(confirmRejectionButton);

    await waitFor(() => {
      expect(supplierApi.confirmPurchaseOrder).toHaveBeenCalledWith('1', {
        status: 'Rejected',
        supplierNotes: 'Cannot fulfill due to capacity constraints',
      });
      expect(mockOnUpdate).toHaveBeenCalledWith(rejectedOrder);
    });
  });

  it('validates rejection reason is required', async () => {
    render(
      <PurchaseOrderModal
        purchaseOrder={mockPurchaseOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    // Open rejection form
    const rejectButton = screen.getByText('Reject Order');
    fireEvent.click(rejectButton);

    // Try to submit without reason
    const confirmRejectionButton = screen.getByText('Confirm Rejection');
    fireEvent.click(confirmRejectionButton);

    await waitFor(() => {
      expect(screen.getByText('Please provide a reason for rejection')).toBeInTheDocument();
      expect(supplierApi.confirmPurchaseOrder).not.toHaveBeenCalled();
    });
  });

  it('handles API errors gracefully', async () => {
    vi.mocked(supplierApi.confirmPurchaseOrder).mockRejectedValue(new Error('API Error'));

    render(
      <PurchaseOrderModal
        purchaseOrder={mockPurchaseOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    // Open packaging form and submit
    const confirmButton = screen.getByText('Confirm Order');
    fireEvent.click(confirmButton);

    const submitButton = screen.getByText('Submit Packaging');
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Failed to confirm purchase order')).toBeInTheDocument();
    });
  });

  it('closes modal when close button is clicked', () => {
    render(
      <PurchaseOrderModal
        purchaseOrder={mockPurchaseOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    const closeButton = screen.getByRole('button', { name: 'Close modal' });
    fireEvent.click(closeButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  it('displays confirmation details for confirmed orders', () => {
    render(
      <PurchaseOrderModal
        purchaseOrder={mockConfirmedOrder}
        onClose={mockOnClose}
        onUpdate={mockOnUpdate}
      />
    );

    expect(screen.getByText('Confirmation Details')).toBeInTheDocument();
    expect(screen.getByText('1/2/2024')).toBeInTheDocument(); // Confirmed date
    expect(screen.getByText('Order confirmed')).toBeInTheDocument(); // Supplier notes
  });
});