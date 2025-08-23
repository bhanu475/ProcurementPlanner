import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import DashboardSummary from '../DashboardSummary';
import { DashboardSummary as DashboardSummaryType } from '../../../types';

const mockSummary: DashboardSummaryType = {
  totalOrders: 100,
  pendingOrders: 25,
  ordersInProgress: 40,
  completedOrders: 30,
  activeSuppliers: 15,
  ordersThisMonth: 45,
  ordersByStatus: {
    'Submitted': 10,
    'UnderReview': 15,
    'PlanningInProgress': 20,
    'PurchaseOrdersCreated': 15,
    'AwaitingSupplierConfirmation': 10,
    'InProduction': 15,
    'ReadyForDelivery': 10,
    'Delivered': 30,
    'Cancelled': 5
  },
  ordersByProductType: {
    'LMR': 60,
    'FFV': 40
  }
};

describe('DashboardSummary', () => {
  it('renders loading state correctly', () => {
    render(<DashboardSummary summary={null} loading={true} />);
    
    // Should show loading skeletons
    const skeletons = document.querySelectorAll('.animate-pulse');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('renders no data state correctly', () => {
    render(<DashboardSummary summary={null} loading={false} />);
    
    expect(screen.getByText('No dashboard data available')).toBeInTheDocument();
  });

  it('renders summary data correctly', () => {
    render(<DashboardSummary summary={mockSummary} loading={false} />);
    
    // Check main summary cards
    expect(screen.getByText('Total Orders')).toBeInTheDocument();
    expect(screen.getAllByText('100')).toHaveLength(1);
    expect(screen.getByText('Pending Orders')).toBeInTheDocument();
    expect(screen.getAllByText('25')).toHaveLength(1);
    expect(screen.getByText('In Progress')).toBeInTheDocument();
    expect(screen.getAllByText('40')).toHaveLength(2); // Appears in main card and product type breakdown
    expect(screen.getByText('Completed')).toBeInTheDocument();
    expect(screen.getAllByText('30')).toHaveLength(2); // Appears in main card and status breakdown
    expect(screen.getByText('Active Suppliers')).toBeInTheDocument();
    expect(screen.getAllByText('15')).toHaveLength(4); // Appears in main card and multiple status breakdown entries
    expect(screen.getByText('Orders This Month')).toBeInTheDocument();
    expect(screen.getAllByText('45')).toHaveLength(1);
  });

  it('renders status breakdown correctly', () => {
    render(<DashboardSummary summary={mockSummary} loading={false} />);
    
    expect(screen.getByText('Orders by Status')).toBeInTheDocument();
    expect(screen.getByText('Submitted')).toBeInTheDocument();
    expect(screen.getByText('Under Review')).toBeInTheDocument();
    expect(screen.getByText('Planning In Progress')).toBeInTheDocument();
  });

  it('renders product type breakdown correctly', () => {
    render(<DashboardSummary summary={mockSummary} loading={false} />);
    
    expect(screen.getByText('Orders by Product Type')).toBeInTheDocument();
    expect(screen.getByText('LMR')).toBeInTheDocument();
    expect(screen.getByText('FFV')).toBeInTheDocument();
    expect(screen.getAllByText('60')).toHaveLength(1);
    expect(screen.getAllByText('40')).toHaveLength(2); // Appears in main card and product type breakdown
  });

  it('displays correct icons for each summary card', () => {
    render(<DashboardSummary summary={mockSummary} loading={false} />);
    
    // Check that icons are rendered (emojis in this case)
    expect(screen.getByText('📋')).toBeInTheDocument(); // Total Orders
    expect(screen.getByText('⏳')).toBeInTheDocument(); // Pending Orders
    expect(screen.getByText('🔄')).toBeInTheDocument(); // In Progress
    expect(screen.getByText('✅')).toBeInTheDocument(); // Completed
    expect(screen.getByText('🏢')).toBeInTheDocument(); // Active Suppliers
    expect(screen.getByText('📈')).toBeInTheDocument(); // Orders This Month
  });
});