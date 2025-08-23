import React from 'react';
import { DashboardSummary as DashboardSummaryType } from '../../types';

interface DashboardSummaryProps {
  summary: DashboardSummaryType | null;
  loading: boolean;
}

const DashboardSummary: React.FC<DashboardSummaryProps> = ({ summary, loading }) => {
  if (loading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {[...Array(4)].map((_, index) => (
          <div key={index} className="bg-white p-6 rounded-lg shadow animate-pulse">
            <div className="h-4 bg-gray-200 rounded mb-2"></div>
            <div className="h-8 bg-gray-200 rounded"></div>
          </div>
        ))}
      </div>
    );
  }

  if (!summary) {
    return (
      <div className="bg-yellow-100 border border-yellow-400 text-yellow-700 px-4 py-3 rounded">
        No dashboard data available
      </div>
    );
  }

  const summaryCards = [
    {
      title: 'Total Orders',
      value: summary.totalOrders,
      color: 'text-blue-600',
      bgColor: 'bg-blue-50',
      icon: '📋'
    },
    {
      title: 'Pending Orders',
      value: summary.pendingOrders,
      color: 'text-yellow-600',
      bgColor: 'bg-yellow-50',
      icon: '⏳'
    },
    {
      title: 'In Progress',
      value: summary.ordersInProgress,
      color: 'text-orange-600',
      bgColor: 'bg-orange-50',
      icon: '🔄'
    },
    {
      title: 'Completed',
      value: summary.completedOrders,
      color: 'text-green-600',
      bgColor: 'bg-green-50',
      icon: '✅'
    },
    {
      title: 'Active Suppliers',
      value: summary.activeSuppliers,
      color: 'text-purple-600',
      bgColor: 'bg-purple-50',
      icon: '🏢'
    },
    {
      title: 'Orders This Month',
      value: summary.ordersThisMonth,
      color: 'text-indigo-600',
      bgColor: 'bg-indigo-50',
      icon: '📈'
    }
  ];

  return (
    <div className="space-y-6">
      {/* Main Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-4">
        {summaryCards.map((card, index) => (
          <div key={index} className={`${card.bgColor} p-6 rounded-lg shadow-sm border`}>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600 mb-1">{card.title}</p>
                <p className={`text-2xl font-bold ${card.color}`}>{card.value}</p>
              </div>
              <div className="text-2xl">{card.icon}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Status Breakdown */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white p-6 rounded-lg shadow-sm border">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Orders by Status</h3>
          <div className="space-y-3">
            {Object.entries(summary.ordersByStatus).map(([status, count]) => (
              <div key={status} className="flex justify-between items-center">
                <span className="text-sm text-gray-600">{status.replace(/([A-Z])/g, ' $1').trim()}</span>
                <span className="font-semibold text-gray-900">{count}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow-sm border">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Orders by Product Type</h3>
          <div className="space-y-3">
            {Object.entries(summary.ordersByProductType).map(([productType, count]) => (
              <div key={productType} className="flex justify-between items-center">
                <span className="text-sm text-gray-600">{productType}</span>
                <div className="flex items-center space-x-2">
                  <span className="font-semibold text-gray-900">{count}</span>
                  <div className="w-16 bg-gray-200 rounded-full h-2">
                    <div 
                      className="bg-blue-600 h-2 rounded-full" 
                      style={{ 
                        width: `${(count / summary.totalOrders) * 100}%` 
                      }}
                    ></div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default DashboardSummary;