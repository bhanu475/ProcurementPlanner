import React, { useState, useEffect } from 'react';
import { useSelector } from 'react-redux';
import { RootState } from '../store';
import SupplierDashboard from '../components/Supplier/SupplierDashboard';
import PurchaseOrderList from '../components/Supplier/PurchaseOrderList';
import SupplierPerformanceMetrics from '../components/Supplier/SupplierPerformanceMetrics';

type TabType = 'dashboard' | 'orders' | 'performance';

const SupplierPortal: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabType>('dashboard');
  const { user } = useSelector((state: RootState) => state.auth);
  
  // For demo purposes, using user.id as supplierId
  // In a real app, this would come from the user's supplier profile
  const supplierId = user?.id || '';

  useEffect(() => {
    // Redirect to login if not authenticated or not a supplier
    if (!user || user.role !== 'supplier') {
      window.location.href = '/login';
    }
  }, [user]);

  if (!user || user.role !== 'supplier') {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="text-center">
          <p className="text-gray-600">Access denied. Supplier authentication required.</p>
          <button 
            onClick={() => window.location.href = '/login'}
            className="mt-4 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
          >
            Go to Login
          </button>
        </div>
      </div>
    );
  }

  const tabs = [
    { id: 'dashboard' as TabType, label: 'Dashboard', icon: '📊' },
    { id: 'orders' as TabType, label: 'Purchase Orders', icon: '📋' },
    { id: 'performance' as TabType, label: 'Performance', icon: '📈' }
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white p-6 rounded-lg shadow">
        <div className="flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Supplier Portal</h1>
            <p className="text-gray-600">Welcome back, {user.name}</p>
          </div>
          <div className="text-right">
            <p className="text-sm text-gray-500">Supplier ID</p>
            <p className="font-medium">{supplierId}</p>
          </div>
        </div>
      </div>

      {/* Navigation Tabs */}
      <div className="bg-white rounded-lg shadow">
        <div className="border-b border-gray-200">
          <nav className="-mb-px flex space-x-8 px-6">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`py-4 px-1 border-b-2 font-medium text-sm whitespace-nowrap ${
                  activeTab === tab.id
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                <span className="mr-2">{tab.icon}</span>
                {tab.label}
              </button>
            ))}
          </nav>
        </div>

        {/* Tab Content */}
        <div className="p-6">
          {activeTab === 'dashboard' && (
            <SupplierDashboard supplierId={supplierId} />
          )}
          
          {activeTab === 'orders' && (
            <PurchaseOrderList supplierId={supplierId} />
          )}
          
          {activeTab === 'performance' && (
            <SupplierPerformanceMetrics supplierId={supplierId} />
          )}
        </div>
      </div>
    </div>
  );
};

export default SupplierPortal;