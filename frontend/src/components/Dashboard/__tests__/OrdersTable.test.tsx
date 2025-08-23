import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import OrdersTable from '../OrdersTable';
import { CustomerOrder } from '../../../types';
import { beforeEach } from 'node:test';

const mockOrders: CustomerOrder[] = [
  {
    id: '1',
    orderNumber: 'ORD-001',
    customerId: 'DODAAC123',
    customerName: 'Test Customer 1',
    productType: 'LMR',
    requestedDeliveryDate: '2024-12-31T00:00:00Z',
    status: 'Submitted',
    items: [
      {
        id: '1',
        orderId: '1',
        productCode: 'PROD-001',
        description: 'Test Product 1',
        quantity: 10,
        unit: 'EA',
        specifications: 'Test specs'
      }
    ],
    createdAt: '2024-01-01T00:00:00Z',
    createdBy: 'test-user'
  },
  {
    id: '2',
    orderNumber: 'ORD-002',
    customerId: 'DODAAC456',
    customerName: 'Test Customer 2',
    productType: 'FFV',
    requestedDeliveryDate: '2024-12-31T00:00:00Z',
    status: 'UnderReview',
    items: [
      {
        id: '2',
        orderId: '2',
        productCode: 'PROD-002',
        description: 'Test Product 2',
        quantity: 5,
        unit: 'LB',
        specifications: 'Test specs 2'
      }
    ],
    createdAt: '2024-01-02T00:00:00Z',
    createdBy: 'test-user-2'
  }
];

const mockGroupedOrders = {
  'LMR': [mockOrders[0]],
  'FFV': [mockOrders[1]]
};

const mockPagination = {
  pageNumber: 1,
  pageSize: 10,
  totalCount: 2,
  totalPages: 1
};

const mockOnOrderSelect = vi.fn();
const mockOnStartDistribution = vi.fn();
const mockOnPageChange = vi.fn();
const mockOnPageSizeChange = vi.fn();

const defaultProps = {
  orders: mockOrders,
  groupedOrders: mockGroupedOrders,
  groupBy: 'productType' as const,
  loading: false,
  pagination: mockPagination,
  onOrderSelect: mockOnOrderSelect,
  onStartDistribution: mockOnStartDistribution,
  onPageChange: mockOnPageChange,
  onPageSizeChange: mockOnPageSizeChange
};

describe('OrdersTable', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state correctly', () => {
    render(<OrdersTable {...defaultProps} loading={true} />);
    
    expect(document.querySelector('.animate-pulse')).toBeInTheDocument();
  });

  it('renders empty state correctly', () => {
    render(<OrdersTable {...defaultProps} orders={[]} groupedOrders={{}} />);
    
    expect(screen.getByText('No orders found')).toBeInTheDocument();
    expect(screen.getByText('Try adjusting your filters to see more results.')).toBeInTheDocument();
  });

  it('renders orders table correctly', () => {
    render(<OrdersTable {...defaultProps} />);
    
    expect(screen.getAllByText((content, element) => {
      return element?.textContent === 'Customer Orders (2)';
    })).toHaveLength(2); // Header appears in both div and h3
    expect(screen.getAllByText('LMR')).toHaveLength(2); // Appears in group header and product type badge
    expect(screen.getAllByText('FFV')).toHaveLength(2); // Appears in group header and product type badge
    expect(screen.getAllByText('(1)')).toHaveLength(2); // Both groups have 1 item
    expect(screen.getByText('ORD-001')).toBeInTheDocument();
    expect(screen.getByText('ORD-002')).toBeInTheDocument();
  });

  it('displays order information correctly', () => {
    render(<OrdersTable {...defaultProps} />);
    
    // Check first order details
    expect(screen.getByText('Test Customer 1')).toBeInTheDocument();
    expect(screen.getByText('DODAAC123')).toBeInTheDocument();
    expect(screen.getAllByText('1 item')).toHaveLength(2); // Both orders have 1 item
    
    // Check second order details
    expect(screen.getByText('Test Customer 2')).toBeInTheDocument();
    expect(screen.getByText('DODAAC456')).toBeInTheDocument();
  });

  it('displays correct status badges', () => {
    render(<OrdersTable {...defaultProps} />);
    
    expect(screen.getByText('Submitted')).toBeInTheDocument();
    expect(screen.getByText('Under Review')).toBeInTheDocument();
  });

  it('displays correct product type badges', () => {
    render(<OrdersTable {...defaultProps} />);
    
    const lmrBadges = screen.getAllByText('LMR');
    const ffvBadges = screen.getAllByText('FFV');
    
    expect(lmrBadges.length).toBeGreaterThan(0);
    expect(ffvBadges.length).toBeGreaterThan(0);
  });

  it('handles review button click correctly', () => {
    render(<OrdersTable {...defaultProps} />);
    
    const reviewButtons = screen.getAllByText('Review');
    fireEvent.click(reviewButtons[0]);
    
    expect(mockOnOrderSelect).toHaveBeenCalledWith(mockOrders[0]);
  });

  it('handles distribute button click correctly', () => {
    render(<OrdersTable {...defaultProps} />);
    
    const distributeButtons = screen.getAllByText('Distribute');
    fireEvent.click(distributeButtons[0]);
    
    expect(mockOnStartDistribution).toHaveBeenCalledWith(mockOrders[0]);
  });

  it('shows distribute button only for eligible orders', () => {
    const ordersWithDifferentStatuses = [
      { ...mockOrders[0], status: 'Submitted' as const },
      { ...mockOrders[1], status: 'Delivered' as const }
    ];
    
    const groupedWithDifferentStatuses = {
      'LMR': [ordersWithDifferentStatuses[0]],
      'FFV': [ordersWithDifferentStatuses[1]]
    };
    
    render(<OrdersTable {...defaultProps} orders={ordersWithDifferentStatuses} groupedOrders={groupedWithDifferentStatuses} />);
    
    const distributeButtons = screen.queryAllByText('Distribute');
    expect(distributeButtons).toHaveLength(1); // Only for 'Submitted' status
  });

  it('handles pagination correctly', () => {
    const paginationProps = {
      ...defaultProps,
      pagination: {
        pageNumber: 2,
        pageSize: 10,
        totalCount: 25,
        totalPages: 3
      }
    };
    
    render(<OrdersTable {...paginationProps} />);
    
    // Check pagination controls
    expect(screen.getByText('of 25 results')).toBeInTheDocument();
    
    const nextButton = screen.getByText('Next');
    fireEvent.click(nextButton);
    
    expect(mockOnPageChange).toHaveBeenCalledWith(3);
  });

  it('handles page size change correctly', () => {
    render(<OrdersTable {...defaultProps} />);
    
    const pageSizeSelect = screen.getByDisplayValue('10');
    fireEvent.change(pageSizeSelect, { target: { value: '25' } });
    
    expect(mockOnPageSizeChange).toHaveBeenCalledWith(25);
  });

  it('formats dates correctly', () => {
    render(<OrdersTable {...defaultProps} />);
    
    expect(screen.getByText((content, element) => {
      return element?.textContent === 'Created: Jan 1, 2024';
    })).toBeInTheDocument();
    expect(screen.getByText((content, element) => {
      return element?.textContent === 'Created: Jan 2, 2024';
    })).toBeInTheDocument();
    expect(screen.getAllByText('Dec 31, 2024')).toHaveLength(2); // Both orders have same delivery date
  });

  it('handles multiple items correctly', () => {
    const orderWithMultipleItems = {
      ...mockOrders[0],
      items: [
        mockOrders[0].items[0],
        { ...mockOrders[0].items[0], id: '3', productCode: 'PROD-003' }
      ]
    };
    
    const ordersWithMultiple = [orderWithMultipleItems, mockOrders[1]];
    const groupedWithMultiple = { 'LMR': [orderWithMultipleItems], 'FFV': [mockOrders[1]] };
    
    render(<OrdersTable {...defaultProps} orders={ordersWithMultiple} groupedOrders={groupedWithMultiple} />);
    
    expect(screen.getByText('2 items')).toBeInTheDocument();
  });
});