export interface QueuedOperation {
  id: string;
  type: 'bulk_stock_update' | 'bulk_create' | 'product_update';
  payload: any;
  timestamp: number;
}

const STORAGE_KEY = 'lumina_resilient_sync_queue';

export const resilientSyncService = {
  getQueue(): QueuedOperation[] {
    try {
      const data = localStorage.getItem(STORAGE_KEY);
      return data ? JSON.parse(data) : [];
    } catch {
      return [];
    }
  },

  enqueue(type: QueuedOperation['type'], payload: any): QueuedOperation {
    const queue = this.getQueue();
    const item: QueuedOperation = {
      id: `queue_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`,
      type,
      payload,
      timestamp: Date.now(),
    };
    queue.push(item);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(queue));
    return item;
  },

  removeFromQueue(id: string): void {
    const queue = this.getQueue().filter((item) => item.id !== id);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(queue));
  },

  clearQueue(): void {
    localStorage.removeItem(STORAGE_KEY);
  },

  hasPending(): boolean {
    return this.getQueue().length > 0;
  },
};
