import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

class SignalRService {
  private connection: HubConnection | null = null;

  async startConnection(): Promise<void> {
    const token = localStorage.getItem('token');
    
    this.connection = new HubConnectionBuilder()
      .withUrl(`${import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'}/hubs/orders`, {
        accessTokenFactory: () => token || ''
      })
      .build();

    try {
      await this.connection.start();
      console.log('SignalR connection started');
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
    }
  }

  onOrderStatusUpdate(callback: (orderId: string, status: string) => void): void {
    if (this.connection) {
      this.connection.on('OrderStatusUpdated', callback);
    }
  }

  onOrderCreated(callback: (order: any) => void): void {
    if (this.connection) {
      this.connection.on('OrderCreated', callback);
    }
  }

  onPurchaseOrderCreated(callback: (purchaseOrder: any) => void): void {
    if (this.connection) {
      this.connection.on('PurchaseOrderCreated', callback);
    }
  }

  onSupplierConfirmation(callback: (purchaseOrderId: string, status: string) => void): void {
    if (this.connection) {
      this.connection.on('SupplierConfirmation', callback);
    }
  }

  onDashboardUpdate(callback: (summary: any) => void): void {
    if (this.connection) {
      this.connection.on('DashboardUpdated', callback);
    }
  }

  // Join specific groups for targeted updates
  async joinPlannerGroup(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.invoke('JoinPlannerGroup');
      } catch (error) {
        console.error('Error joining planner group:', error);
      }
    }
  }

  async leavePlannerGroup(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.invoke('LeavePlannerGroup');
      } catch (error) {
        console.error('Error leaving planner group:', error);
      }
    }
  }

  async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRService = new SignalRService();