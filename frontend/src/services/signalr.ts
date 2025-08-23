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

  async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRService = new SignalRService();