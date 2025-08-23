import React, { useState, useEffect } from 'react';
import { CustomerOrder, DistributionSuggestion, SupplierAllocation } from '../../types';

interface DistributionPlanningModalProps {
  order: CustomerOrder;
  distributionSuggestion: DistributionSuggestion | null;
  loading: boolean;
  onClose: () => void;
  onCreatePurchaseOrders: (distributionPlan: any) => void;
}

const DistributionPlanningModal: React.FC<DistributionPlanningModalProps> = ({
  order,
  distributionSuggestion,
  loading,
  onClose,
  onCreatePurchaseOrders
}) => {
  const [selectedAllocations, setSelectedAllocations] = useState<SupplierAllocation[]>([]);
  const [customAllocations, setCustomAllocations] = useState<{ [supplierId: string]: number }>({});

  useEffect(() => {
    if (distributionSuggestion) {
      setSelectedAllocations(distributionSuggestion.suggestions);
      // Initialize custom allocations with suggested values
      const initialCustom: { [supplierId: string]: number } = {};
      distributionSuggestion.suggestions.forEach(suggestion => {
        initialCustom[suggestion.supplierId] = suggestion.allocatedQuantity;
      });
      setCustomAllocations(initialCustom);
    }
  }, [distributionSuggestion]);

  const handleAllocationChange = (supplierId: string, quantity: number) => {
    setCustomAllocations(prev => ({
      ...prev,
      [supplierId]: quantity
    }));

    // Update selected allocations
    setSelectedAllocations(prev => 
      prev.map(allocation => 
        allocation.supplierId === supplierId 
          ? { ...allocation, allocatedQuantity: quantity }
          : allocation
      )
    );
  };

  const handleSupplierToggle = (allocation: SupplierAllocation, selected: boolean) => {
    if (selected) {
      setSelectedAllocations(prev => [...prev, allocation]);
      setCustomAllocations(prev => ({
        ...prev,
        [allocation.supplierId]: allocation.allocatedQuantity
      }));
    } else {
      setSelectedAllocations(prev => prev.filter(a => a.supplierId !== allocation.supplierId));
      setCustomAllocations(prev => {
        const newAllocations = { ...prev };
        delete newAllocations[allocation.supplierId];
        return newAllocations;
      });
    }
  };

  const getTotalAllocated = () => {
    return Object.values(customAllocations).reduce((sum, qty) => sum + qty, 0);
  };

  const getTotalRequired = () => {
    return order.items.reduce((sum, item) => sum + item.quantity, 0);
  };

  const isValidDistribution = () => {
    const totalAllocated = getTotalAllocated();
    const totalRequired = getTotalRequired();
    return totalAllocated === totalRequired && selectedAllocations.length > 0;
  };

  const handleCreatePurchaseOrders = () => {
    const distributionPlan = {
      customerOrderId: order.id,
      allocations: selectedAllocations.map(allocation => ({
        supplierId: allocation.supplierId,
        allocatedQuantity: customAllocations[allocation.supplierId],
        items: allocation.items.map(item => ({
          orderItemId: item.orderItemId,
          allocatedQuantity: item.allocatedQuantity
        }))
      }))
    };

    onCreatePurchaseOrders(distributionPlan);
  };

  const getPerformanceColor = (score: number) => {
    if (score >= 90) return 'text-green-600';
    if (score >= 80) return 'text-yellow-600';
    if (score >= 70) return 'text-orange-600';
    return 'text-red-600';
  };

  const getCapacityColor = (utilization: number) => {
    if (utilization <= 70) return 'bg-green-500';
    if (utilization <= 85) return 'bg-yellow-500';
    if (utilization <= 95) return 'bg-orange-500';
    return 'bg-red-500';
  };

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-10 mx-auto p-5 border w-11/12 max-w-6xl shadow-lg rounded-md bg-white">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h3 className="text-2xl font-bold text-gray-900">Distribution Planning</h3>
            <p className="text-sm text-gray-600 mt-1">
              Order: {order.orderNumber} • Product Type: {order.productType}
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-2xl font-bold"
          >
            ×
          </button>
        </div>

        {loading ? (
          <div className="flex justify-center items-center h-64">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            <span className="ml-3 text-gray-600">Analyzing supplier capacity and generating suggestions...</span>
          </div>
        ) : !distributionSuggestion ? (
          <div className="text-center py-12">
            <div className="text-gray-500">
              <div className="text-4xl mb-4">⚠️</div>
              <h3 className="text-lg font-medium text-gray-900 mb-2">No distribution suggestions available</h3>
              <p className="text-sm text-gray-500">Unable to generate supplier distribution for this order.</p>
            </div>
          </div>
        ) : (
          <div className="space-y-6">
            {/* Distribution Summary */}
            <div className="bg-blue-50 p-4 rounded-lg border border-blue-200">
              <div className="flex justify-between items-center">
                <div>
                  <h4 className="text-lg font-semibold text-blue-900">Distribution Summary</h4>
                  <p className="text-sm text-blue-700 mt-1">
                    Total Required: {getTotalRequired()} units • 
                    Total Allocated: {getTotalAllocated()} units • 
                    Capacity Utilization: {distributionSuggestion.totalCapacityUtilization.toFixed(1)}%
                  </p>
                </div>
                <div className={`px-3 py-1 rounded-full text-sm font-medium ${
                  isValidDistribution() 
                    ? 'bg-green-100 text-green-800' 
                    : 'bg-red-100 text-red-800'
                }`}>
                  {isValidDistribution() ? 'Valid Distribution' : 'Invalid Distribution'}
                </div>
              </div>
            </div>

            {/* Supplier Allocations */}
            <div>
              <h4 className="text-lg font-semibold text-gray-900 mb-4">Supplier Allocations</h4>
              <div className="space-y-4">
                {distributionSuggestion.suggestions.map((suggestion) => {
                  const isSelected = selectedAllocations.some(a => a.supplierId === suggestion.supplierId);
                  const currentAllocation = customAllocations[suggestion.supplierId] || 0;
                  
                  return (
                    <div 
                      key={suggestion.supplierId} 
                      className={`border rounded-lg p-4 ${
                        isSelected ? 'border-blue-300 bg-blue-50' : 'border-gray-200 bg-white'
                      }`}
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex items-start space-x-3">
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={(e) => handleSupplierToggle(suggestion, e.target.checked)}
                            className="mt-1 h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                          />
                          <div className="flex-1">
                            <h5 className="font-medium text-gray-900">{suggestion.supplierName}</h5>
                            <div className="flex items-center space-x-4 mt-2 text-sm text-gray-600">
                              <div className="flex items-center">
                                <span className="font-medium">Performance:</span>
                                <span className={`ml-1 font-semibold ${getPerformanceColor(suggestion.performanceScore)}`}>
                                  {suggestion.performanceScore.toFixed(1)}%
                                </span>
                              </div>
                              <div className="flex items-center">
                                <span className="font-medium">Capacity:</span>
                                <div className="ml-2 w-16 bg-gray-200 rounded-full h-2">
                                  <div 
                                    className={`h-2 rounded-full ${getCapacityColor(suggestion.capacityUtilization)}`}
                                    style={{ width: `${Math.min(suggestion.capacityUtilization, 100)}%` }}
                                  ></div>
                                </div>
                                <span className="ml-1 text-xs">{suggestion.capacityUtilization.toFixed(1)}%</span>
                              </div>
                            </div>
                          </div>
                        </div>
                        
                        {isSelected && (
                          <div className="flex items-center space-x-2">
                            <label className="text-sm font-medium text-gray-700">Quantity:</label>
                            <input
                              type="number"
                              min="0"
                              max={getTotalRequired()}
                              value={currentAllocation}
                              onChange={(e) => handleAllocationChange(suggestion.supplierId, parseInt(e.target.value) || 0)}
                              className="w-20 border border-gray-300 rounded-md px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                            />
                          </div>
                        )}
                      </div>

                      {isSelected && suggestion.items.length > 0 && (
                        <div className="mt-4 pl-7">
                          <h6 className="text-sm font-medium text-gray-700 mb-2">Item Breakdown:</h6>
                          <div className="space-y-1">
                            {suggestion.items.map((item) => {
                              const orderItem = order.items.find(oi => oi.id === item.orderItemId);
                              return (
                                <div key={item.orderItemId} className="flex justify-between text-xs text-gray-600">
                                  <span>{orderItem?.productCode || 'Unknown Item'}</span>
                                  <span>{item.allocatedQuantity} units</span>
                                </div>
                              );
                            })}
                          </div>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Validation Messages */}
            {!isValidDistribution() && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <div className="flex">
                  <div className="text-red-400 mr-3">⚠️</div>
                  <div>
                    <h4 className="text-sm font-medium text-red-800">Distribution Issues</h4>
                    <ul className="mt-2 text-sm text-red-700 list-disc list-inside">
                      {getTotalAllocated() !== getTotalRequired() && (
                        <li>
                          Total allocated quantity ({getTotalAllocated()}) must equal required quantity ({getTotalRequired()})
                        </li>
                      )}
                      {selectedAllocations.length === 0 && (
                        <li>At least one supplier must be selected</li>
                      )}
                    </ul>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex justify-end space-x-3 mt-6 pt-6 border-t border-gray-200">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Cancel
          </button>
          
          {distributionSuggestion && (
            <button
              onClick={handleCreatePurchaseOrders}
              disabled={!isValidDistribution()}
              className={`px-4 py-2 text-sm font-medium text-white rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 ${
                isValidDistribution()
                  ? 'bg-blue-600 hover:bg-blue-700'
                  : 'bg-gray-400 cursor-not-allowed'
              }`}
            >
              Create Purchase Orders
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default DistributionPlanningModal;