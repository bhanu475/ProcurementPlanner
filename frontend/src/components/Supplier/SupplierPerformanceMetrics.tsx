import React, { useEffect, useState } from 'react';
import { SupplierPerformanceMetrics } from '../../types';
import { supplierApi } from '../../services/supplierApi';

interface SupplierPerformanceMetricsProps {
  supplierId: string;
}

const SupplierPerformanceMetricsComponent: React.FC<SupplierPerformanceMetricsProps> = ({ supplierId }) => {
  const [performance, setPerformance] = useState<SupplierPerformanceMetrics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchPerformanceMetrics = async () => {
      try {
        setLoading(true);
        const response = await supplierApi.getPerformanceMetrics(supplierId);
        setPerformance(response.data);
      } catch (err) {
        setError('Failed to load performance metrics');
        console.error('Performance metrics error:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchPerformanceMetrics();
  }, [supplierId]);

  const getPerformanceColor = (value: number, type: 'percentage' | 'score') => {
    if (type === 'percentage') {
      if (value >= 0.9) return 'text-green-600';
      if (value >= 0.7) return 'text-yellow-600';
      return 'text-red-600';
    } else {
      if (value >= 8) return 'text-green-600';
      if (value >= 6) return 'text-yellow-600';
      return 'text-red-600';
    }
  };

  const getPerformanceBadge = (value: number, type: 'percentage' | 'score') => {
    if (type === 'percentage') {
      if (value >= 0.9) return { text: 'Excellent', color: 'bg-green-100 text-green-800' };
      if (value >= 0.7) return { text: 'Good', color: 'bg-yellow-100 text-yellow-800' };
      return { text: 'Needs Improvement', color: 'bg-red-100 text-red-800' };
    } else {
      if (value >= 8) return { text: 'Excellent', color: 'bg-green-100 text-green-800' };
      if (value >= 6) return { text: 'Good', color: 'bg-yellow-100 text-yellow-800' };
      return { text: 'Needs Improvement', color: 'bg-red-100 text-red-800' };
    }
  };

  if (loading) {
    return (
      <div className="bg-white p-6 rounded-lg shadow">
        <div className="animate-pulse">
          <div className="h-4 bg-gray-200 rounded w-1/4 mb-4"></div>
          <div className="space-y-3">
            <div className="h-3 bg-gray-200 rounded"></div>
            <div className="h-3 bg-gray-200 rounded w-5/6"></div>
            <div className="h-3 bg-gray-200 rounded w-4/6"></div>
          </div>
        </div>
        <span className="sr-only">Loading...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white p-6 rounded-lg shadow">
        <div className="bg-red-50 border border-red-200 rounded-md p-4">
          <p className="text-red-800">{error}</p>
        </div>
      </div>
    );
  }

  if (!performance) {
    return (
      <div className="bg-white p-6 rounded-lg shadow">
        <p className="text-gray-500">No performance data available.</p>
      </div>
    );
  }

  const onTimeDeliveryBadge = getPerformanceBadge(performance.onTimeDeliveryRate, 'percentage');
  const qualityScoreBadge = getPerformanceBadge(performance.qualityScore, 'score');

  return (
    <div className="bg-white p-6 rounded-lg shadow">
      <div className="flex justify-between items-center mb-6">
        <h3 className="text-lg font-semibold text-gray-900">Performance Metrics</h3>
        <span className="text-sm text-gray-500">
          Last updated: {new Date(performance.lastUpdated).toLocaleDateString()}
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* On-Time Delivery Rate */}
        <div className="text-center">
          <div className="mb-2">
            <span className={`text-3xl font-bold ${getPerformanceColor(performance.onTimeDeliveryRate, 'percentage')}`}>
              {(performance.onTimeDeliveryRate * 100).toFixed(1)}%
            </span>
          </div>
          <p className="text-sm text-gray-600 mb-2">On-Time Delivery Rate</p>
          <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${onTimeDeliveryBadge.color}`}>
            {onTimeDeliveryBadge.text}
          </span>
          
          {/* Progress Bar */}
          <div className="mt-3">
            <div className="bg-gray-200 rounded-full h-2">
              <div 
                className={`h-2 rounded-full ${
                  performance.onTimeDeliveryRate >= 0.9 ? 'bg-green-500' :
                  performance.onTimeDeliveryRate >= 0.7 ? 'bg-yellow-500' : 'bg-red-500'
                }`}
                style={{ width: `${performance.onTimeDeliveryRate * 100}%` }}
                data-testid="progress-bar"
              ></div>
            </div>
          </div>
        </div>

        {/* Quality Score */}
        <div className="text-center">
          <div className="mb-2">
            <span className={`text-3xl font-bold ${getPerformanceColor(performance.qualityScore, 'score')}`}>
              {performance.qualityScore.toFixed(1)}
            </span>
            <span className="text-lg text-gray-500">/10</span>
          </div>
          <p className="text-sm text-gray-600 mb-2">Quality Score</p>
          <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${qualityScoreBadge.color}`}>
            {qualityScoreBadge.text}
          </span>
          
          {/* Progress Bar */}
          <div className="mt-3">
            <div className="bg-gray-200 rounded-full h-2">
              <div 
                className={`h-2 rounded-full ${
                  performance.qualityScore >= 8 ? 'bg-green-500' :
                  performance.qualityScore >= 6 ? 'bg-yellow-500' : 'bg-red-500'
                }`}
                style={{ width: `${(performance.qualityScore / 10) * 100}%` }}
                data-testid="progress-bar"
              ></div>
            </div>
          </div>
        </div>

        {/* Total Orders Completed */}
        <div className="text-center">
          <div className="mb-2">
            <span className="text-3xl font-bold text-blue-600">
              {performance.totalOrdersCompleted.toLocaleString()}
            </span>
          </div>
          <p className="text-sm text-gray-600 mb-2">Total Orders Completed</p>
          <span className="inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-blue-100 text-blue-800">
            {performance.totalOrdersCompleted > 100 ? 'Experienced' : 
             performance.totalOrdersCompleted > 50 ? 'Established' : 'Growing'}
          </span>
        </div>
      </div>

      {/* Performance Insights */}
      <div className="mt-6 pt-6 border-t border-gray-200">
        <h4 className="font-medium text-gray-900 mb-3">Performance Insights</h4>
        <div className="space-y-2 text-sm text-gray-600">
          {performance.onTimeDeliveryRate >= 0.9 && (
            <div className="flex items-center">
              <svg className="w-4 h-4 text-green-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
              Excellent on-time delivery performance
            </div>
          )}
          {performance.qualityScore >= 8 && (
            <div className="flex items-center">
              <svg className="w-4 h-4 text-green-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
              High quality standards maintained
            </div>
          )}
          {performance.totalOrdersCompleted > 100 && (
            <div className="flex items-center">
              <svg className="w-4 h-4 text-blue-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
              Experienced supplier with proven track record
            </div>
          )}
          {performance.onTimeDeliveryRate < 0.7 && (
            <div className="flex items-center">
              <svg className="w-4 h-4 text-red-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              Focus needed on improving delivery timeliness
            </div>
          )}
          {performance.qualityScore < 6 && (
            <div className="flex items-center">
              <svg className="w-4 h-4 text-red-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              Quality improvements needed to meet standards
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default SupplierPerformanceMetricsComponent;