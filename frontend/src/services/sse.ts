export function subscribeToStockEvents(onStockUpdate: (data: any) => void): () => void {
  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://stationary-new-1.onrender.com/api';
  const streamUrl = `${apiBase}/events/stream`;

  let eventSource: EventSource | null = null;
  let isSubscribed = true;

  const connect = () => {
    try {
      eventSource = new EventSource(streamUrl);

      eventSource.addEventListener('stock_update', (event: MessageEvent) => {
        try {
          const data = JSON.parse(event.data);
          onStockUpdate(data);
        } catch (e) {
          console.error('Error parsing SSE stock_update event:', e);
        }
      });

      eventSource.onerror = () => {
        if (eventSource) {
          eventSource.close();
        }
        // Attempt reconnection after 5 seconds if still subscribed
        if (isSubscribed) {
          setTimeout(connect, 5000);
        }
      };
    } catch (err) {
      console.warn('EventSource not supported or connection error:', err);
    }
  };

  connect();

  return () => {
    isSubscribed = false;
    if (eventSource) {
      eventSource.close();
    }
  };
}
