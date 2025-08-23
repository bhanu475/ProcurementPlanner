import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RootState, AppDispatch } from '../store';
import {
  fetchOrders,
  fetchDashboardSummary,
  fetchDistributionSuggestion,
  createPurchaseOrders,
  setFilter,
  clearFilter,
  setSelectedOrder,
  clearDistributionSuggestion,
  setPagination,
  updateOrderStatusRealtime
} from '../store/slices/ordersSlice';
import { CustomerOrder, OrderFilter, ProductType, OrderStatus } from '../types';
import { signalRService } from '../services/signalr';
import DashboardSummary from '../components/Dashboard/DashboardSummary';
import OrderFilters from '../components/Dashboard/OrderFilters';
import OrdersTable from '../components/Dashboard/OrdersTable';
import OrderReviewModal from '../components/Dashboard/OrderReviewModal';
import DistributionPlanningModal from '../components/Dashboard/DistributionPlanningModal';

const Dashboard: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const {
    orders,
    dashboardSummary,
    selectedOrder,
    distributionSuggestion,
    loading,
    dashboardLoading,
    distributionLoading,
    error,
    filter,
    pagination
  } = useSelector((state: RootState) => state.orders);

  const [showOrderReview, setShowOrderReview] = useState(false);
  const [showDistributionPlanning, setShowDistributionPlanning] = useState(false);
  const [groupBy, setGroupBy] = useState<'productType' | 'deliveryDate' | 'status'>('productType');

  useEffect(() => {
    // Initialize dashboard data
    dispatch(fetchDashboardSummary());
    dispatch(fetchOrders({ filter, pageNumber: pagination.pageNumber, pageSize: pagination.pageSize }));

    // Setup SignalR connection for real-time updates
    const setupSignalR = async () => {
      await signalRService.startConnection();
      signalRService.onOrderStatusUpdate((orderId: string, status: string) => {
        dispatch(updateOrderStatusRealtime({ orderId, status: status as OrderStatus }));
        // Refresh dashboard summary when status changes
        dispatch(fetchDashboardSummary());
      });
    };

    setupSignalR();

    return () => {
      signalRService.stopConnection();
    };
  }, [dispatch]);

  useEffect(() => {
    // Refetch orders when filter changes
    dispatch(fetchOrders({ filter, pageNumber: pagination.pageNumber, pageSize: pagination.pageSize }));
  }, [dispatch, filter, pagination.pageNumber, pagination.pageSize]);

  const handleFilterChange = (newFilter: Partial<OrderFilter>) => {
    dispatch(setFilter(newFilter));
    dispatch(setPagination({ pageNumber: 1, pageSize: pagination.pageSize }));
  };

  const handleClearFilters = () => {
    dispatch(clearFilter());
    dispatch(setPagination({ pageNumber: 1, pageSize: pagination.pageSize }));
  };

  const handleOrderSelect = (order: CustomerOrder) => {
    dispatch(setSelectedOrder(order));
    setShowOrderReview(true);
  };

  const handleStartDistribution = (order: CustomerOrder) => {
    dispatch(setSelectedOrder(order));
    dispatch(fetchDistributionSuggestion(order.id));
    setShowDistributionPlanning(true);
  };

  const handleCreatePurchaseOrders = async (distributionPlan: any) => {
    if (selectedOrder) {
      await dispatch(createPurchaseOrders({
        customerOrderId: selectedOrder.id,
        distributionPlan
      }));
      setShowDistributionPlanning(false);
      dispatch(clearDistributionSuggestion());
      dispatch(setSelectedOrder(null));
      // Refresh orders to show updated status
      dispatch(fetchOrders({ filter, pageNumber: pagination.pageNumber, pageSize: pagination.pageSize }));
    }
  };

  const handlePageChange = (pageNumber: number) => {
    dispatch(setPagination({ pageNumber, pageSize: pagination.pageSize }));
  };

  const handlePageSizeChange = (pageSize: number) => {
    dispatch(setPagination({ pageNumber: 1, pageSize }));
  };

  const getGroupedOrders = () => {
    const grouped: { [key: string]: CustomerOrder[] } = {};
    
    orders.forEach(order => {
      let key: string;
      switch (groupBy) {
        case 'productType':
          key = order.productType;
          break;
        case 'deliveryDate':
          key = new Date(order.requestedDeliveryDate).toDateString();
          break;
        case 'status':
          key = order.status;
          break;
        default:
          key = 'All';
      }
      
      if (!grouped[key]) {
        grouped[key] = [];
      }
      grouped[key].push(order);
    });
    
    return grouped;
  };

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
          <strong className="font-bold">Error: </strong>
          <span className="block sm:inline">{error}</span>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold text-gray-900">LMR Planner Dashboard</h1>
        <div className="flex items-center space-x-4">
          <label className="text-sm font-medium text-gray-700">Group by:</label>
          <select
            value={groupBy}
            onChange={(e) => setGroupBy(e.target.value as any)}
            className="border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="productType">Product Type</option>
            <option value="deliveryDate">Delivery Date</option>
            <option value="status">Status</option>
          </select>
        </div>
      </div>

      {/* Dashboard Summary Cards */}
      <DashboardSummary 
        summary={dashboardSummary} 
        loading={dashboardLoading} 
      />

      {/* Order Filters */}
      <OrderFilters
        filter={filter}
        onFilterChange={handleFilterChange}
        onClearFilters={handleClearFilters}
      />

      {/* Orders Table */}
      <OrdersTable
        orders={orders}
        groupedOrders={getGroupedOrders()}
        groupBy={groupBy}
        loading={loading}
        pagination={pagination}
        onOrderSelect={handleOrderSelect}
        onStartDistribution={handleStartDistribution}
        onPageChange={handlePageChange}
        onPageSizeChange={handlePageSizeChange}
      />

      {/* Order Review Modal */}
      {showOrderReview && selectedOrder && (
        <OrderReviewModal
          order={selectedOrder}
          onClose={() => {
            setShowOrderReview(false);
            dispatch(setSelectedOrder(null));
          }}
          onStartDistribution={() => {
            setShowOrderReview(false);
            handleStartDistribution(selectedOrder);
          }}
        />
      )}

      {/* Distribution Planning Modal */}
      {showDistributionPlanning && selectedOrder && (
        <DistributionPlanningModal
          order={selectedOrder}
          distributionSuggestion={distributionSuggestion}
          loading={distributionLoading}
          onClose={() => {
            setShowDistributionPlanning(false);
            dispatch(clearDistributionSuggestion());
            dispatch(setSelectedOrder(null));
          }}
          onCreatePurchaseOrders={handleCreatePurchaseOrders}
        />
      )}
    </div>
  );
};

export default Dashboard;