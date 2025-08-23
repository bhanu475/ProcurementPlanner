import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import SupplierPerformanceMetricsComponent from '../SupplierPerformanceMetrics';
import { supplierApi } from '../../../services/supplierApi';
import { SupplierPerformanceMetrics } from '../../../types';

// Mock the supplier API
vi.mock('../../../services/supplierApi', () => ({
  supplierApi: {
    getPerformanceMetrics: vi.fn(),
  },
}));

const mockPerformanceMetrics: SupplierPerformanceMetrics = {
  supplierId: 'supplier-1',
  onTimeDeliveryRate: 0.95,
  qualityScore: 8.5,
  totalOrdersCompleted: 150,
  lastUpdated: '2024-01-01T00:00:00Z',
};

const mockPoorPerformanceMetrics: SupplierPerformanceMetrics = {
  supplierId: 'supplier-2',
  onTimeDeliveryRate: 0.65,
  qualityScore: 5.2,
  totalOrdersCompleted: 25,
  lastUpdated: '2024-01-01T00:00:00Z',
};

describe('SupplierPerformanceMetricsComponent', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially', () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockImplementation(() => new Promise(() => {}));

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('renders performance metrics successfully', async () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPerformanceMetrics,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('Performance Metrics')).toBeInTheDocument();
      expect(screen.getByText('95.0%')).toBeInTheDocument(); // On-time delivery rate
      expect(screen.getByText('8.5')).toBeInTheDocument(); // Quality score
      expect(screen.getByText('150')).toBeInTheDocument(); // Total orders completed
    });

    expect(screen.getByText('Last updated: 1/1/2024')).toBeInTheDocument();
  });

  it('displays excellent performance badges for high scores', async () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPerformanceMetrics,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    await waitFor(() => {
      const excellentBadges = screen.getAllByText('Excellent');
      expect(excellentBadges).toHaveLength(2); // For delivery rate and quality score
      
      const experiencedBadge = screen.getByText('Experienced');
      expect(experiencedBadge).toBeInTheDocument();
    });
  });

  it('displays poor performance badges for low scores', async () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPoorPerformanceMetrics,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-2" />);

    await waitFor(() => {
      const needsImprovementBadges = screen.getAllByText('Needs Improvement');
      expect(needsImprovementBadges).toHaveLength(2); // For delivery rate and quality score
      
      const growingBadge = screen.getByText('Growing');
      expect(growingBadge).toBeInTheDocument();
    });
  });

  it('displays correct color coding for performance values', async () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPerformanceMetrics,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    await waitFor(() => {
      const deliveryRate = screen.getByText('95.0%');
      const qualityScore = screen.getByText('8.5');
      
      expect(deliveryRate).toHaveClass('text-green-600');
      expect(qualityScore).toHaveClass('text-green-600');
    });
  });

  it('displays performance insights for excellent performance', async () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPerformanceMetrics,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('Performance Insights')).toBeInTheDocument();
      expect(screen.getByText('Excellent on-time delivery performance')).toBeInTheDocument();
      expect(screen.getByText('High quality standards maintained')).toBeInTheDocument();
      expect(screen.getByText('Experienced supplier with proven track record')).toBeInTheDocument();
    });
  });

  it('displays performance insights for poor performance', async () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPoorPerformanceMetrics,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-2" />);

    await waitFor(() => {
      expect(screen.getByText('Focus needed on improving delivery timeliness')).toBeInTheDocument();
      expect(screen.getByText('Quality improvements needed to meet standards')).toBeInTheDocument();
    });
  });

  it('displays progress bars with correct widths', async () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: mockPerformanceMetrics,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    await waitFor(() => {
      const progressBars = screen.getAllByTestId('progress-bar');
      
      // Check that progress bars have correct width styles
      const deliveryProgressBar = progressBars[0];
      const qualityProgressBar = progressBars[1];
      
      expect(deliveryProgressBar).toHaveStyle('width: 95%');
      expect(qualityProgressBar).toHaveStyle('width: 85%'); // 8.5/10 * 100
    });
  });

  it('handles API errors gracefully', async () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockRejectedValue(new Error('API Error'));

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load performance metrics')).toBeInTheDocument();
    });
  });

  it('displays no data message when performance data is null', async () => {
    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: null,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('No performance data available.')).toBeInTheDocument();
    });
  });

  it('displays correct experience level based on total orders', async () => {
    const establishedSupplier = {
      ...mockPerformanceMetrics,
      totalOrdersCompleted: 75,
    };

    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: establishedSupplier,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('Established')).toBeInTheDocument();
    });
  });

  it('formats large numbers with locale formatting', async () => {
    const highVolumeSupplier = {
      ...mockPerformanceMetrics,
      totalOrdersCompleted: 1500,
    };

    vi.mocked(supplierApi.getPerformanceMetrics).mockResolvedValue({
      data: highVolumeSupplier,
    } as any);

    render(<SupplierPerformanceMetricsComponent supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText('1,500')).toBeInTheDocument();
    });
  });
});