import React, { useEffect, useState } from 'react';
import { PurchaseOrder, SupplierPerformanceMetrics } from '../../types';
import { supplierApi } from '../../services/supplierApi';

interface SupplierDashboardProps {
  supplierId: string;
}

const SupplierDashboard: React.FC<SupplierDashboardProps> = ({ supplierId }) => {
  const [purchaseOrders, setPurchaseOrders] = useState<PurchaseOrder[]>([]);
  const [performance, setPerformance] = useState<SupplierPerformanceMetrics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        const [ordersResponse, performanceResponse] = await Promise.all([
          supplierApi.getPurchaseOrders(supplierId),
          supplierApi.getPerformanceMetrics(supplierId)
        ]);
        
        setPurchaseOrders(ordersResponse.data);
        setPerformance(performanceResponse.data);
      } catch (err) {
        setError('Failed to load dashboard data');
        console.error('Dashboard error:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, [supplierId]);

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
        <span className="ml-2">Loading...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-md p-4">
        <p className="text-red-800">{error}</p>
      </div>
    );
  }

  const pendingOrders = purchaseOrders.filter(po => po.status === 'SentToSupplier');
  const confirmedOrders = purchaseOrders.filter(po => po.status === 'Confirmed');
  const inProgressOrders = purchaseOrders.filter(po => ['InProduction', 'ReadyForShipment'].includes(po.status));

  return (
    <div className="space-y-6">
      {/* Dashboard Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-sm font-medium text-gray-500">Pending Orders</h3>
          <p className="text-2xl font-bold text-orange-600">{pendingOrders.length}</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-sm font-medium text-gray-500">Confirmed Orders</h3>
          <p className="text-2xl font-bold text-green-600">{confirmedOrders.length}</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-sm font-medium text-gray-500">In Progress</h3>
          <p className="text-2xl font-bold text-blue-600">{inProgressOrders.length}</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-sm font-medium text-gray-500">Total Orders</h3>
          <p className="text-2xl font-bold text-gray-900">{purchaseOrders.length}</p>
        </div>
      </div>

      {/* Performance Metrics */}
      {performance && (
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-semibold mb-4">Performance Metrics</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <p className="text-sm text-gray-500">On-Time Delivery Rate</p>
              <p className="text-xl font-bold text-green-600">
                {(performance.onTimeDeliveryRate * 100).toFixed(1)}%
              </p>
            </div>
            <div>
              <p className="text-sm text-gray-500">Quality Score</p>
              <p className="text-xl font-bold text-blue-600">
                {performance.qualityScore.toFixed(1)}/10
              </p>
            </div>
            <div>
              <p className="text-sm text-gray-500">Total Orders Completed</p>
              <p className="text-xl font-bold text-gray-900">
                {performance.totalOrdersCompleted}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Recent Purchase Orders */}
      <div className="bg-white p-6 rounded-lg shadow">
        <h3 className="text-lg font-semibold mb-4">Recent Purchase Orders</h3>
        {purchaseOrders.length === 0 ? (
          <p className="text-gray-500">No purchase orders available.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    PO Number
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Required Delivery
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Items
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {purchaseOrders.slice(0, 5).map((order) => (
                  <tr key={order.id}>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {order.purchaseOrderNumber}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                        order.status === 'SentToSupplier' ? 'bg-orange-100 text-orange-800' :
                        order.status === 'Confirmed' ? 'bg-green-100 text-green-800' :
                        order.status === 'InProduction' ? 'bg-blue-100 text-blue-800' :
                        'bg-gray-100 text-gray-800'
                      }`}>
                        {order.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {new Date(order.requiredDeliveryDate).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {order.items.length} items
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button className="text-blue-600 hover:text-blue-900">
                        View Details
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default SupplierDashboard;