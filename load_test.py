import asyncio
import time
import sys
import httpx

if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8")

# Target API endpoint (can be local or your live Render backend)
API_URL = "https://stationary-new-1.onrender.com/api/productsapi"
CONCURRENT_USERS = 1000

async def simulate_user(client: httpx.AsyncClient, user_id: int, results: list):
    start = time.perf_counter()
    try:
        response = await client.get(API_URL, timeout=30.0)
        elapsed_ms = (time.perf_counter() - start) * 1000.0
        results.append({
            "user_id": user_id,
            "status": response.status_code,
            "elapsed_ms": elapsed_ms,
            "success": response.is_success
        })
    except Exception as e:
        elapsed_ms = (time.perf_counter() - start) * 1000.0
        results.append({
            "user_id": user_id,
            "status": 0,
            "error": str(e),
            "elapsed_ms": elapsed_ms,
            "success": False
        })

async def run_load_test(total_users=1000, max_concurrency=200):
    print("=" * 65)
    print(f"🚀 STARTING LOAD TEST: {total_users} CONCURRENT USERS")
    print(f"🎯 Target URL: {API_URL}")
    print("=" * 65)

    results = []
    limits = httpx.Limits(max_keepalive_connections=max_concurrency, max_connections=max_concurrency)
    
    start_total = time.perf_counter()
    
    async with httpx.AsyncClient(limits=limits) as client:
        # Create tasks in batches or all concurrently
        semaphore = asyncio.Semaphore(max_concurrency)

        async def sem_task(uid):
            async with semaphore:
                await simulate_user(client, uid, results)

        tasks = [sem_task(i + 1) for i in range(total_users)]
        print(f"⚡ Dispatching {total_users} concurrent user requests...")
        await asyncio.gather(*tasks)

    total_time = time.perf_counter() - start_total

    # Metrics calculation
    successful = [r for r in results if r["success"]]
    failed = [r for r in results if not r["success"]]
    latencies = sorted([r["elapsed_ms"] for r in successful]) if successful else []

    avg_latency = sum(latencies) / len(latencies) if latencies else 0
    p50_latency = latencies[int(len(latencies) * 0.50)] if latencies else 0
    p95_latency = latencies[int(len(latencies) * 0.95)] if latencies else 0
    p99_latency = latencies[int(len(latencies) * 0.99)] if latencies else 0
    min_latency = latencies[0] if latencies else 0
    max_latency = latencies[-1] if latencies else 0
    rps = total_users / total_time if total_time > 0 else 0

    print("\n" + "=" * 65)
    print("📊 LOAD TEST RESULTS SUMMARY")
    print("=" * 65)
    print(f"✅ Total Requests:       {total_users}")
    print(f"🎉 Successful (200 OK):  {len(successful)} ({(len(successful)/total_users)*100:.1f}%)")
    print(f"❌ Failed / Errors:      {len(failed)}")
    print(f"⏱️  Total Test Duration:  {total_time:.2f} seconds")
    print(f"🚀 Throughput (RPS):     {rps:.2f} req/sec")
    print("-" * 65)
    print("⚡ LATENCY BREAKDOWN (Redis + Server Response):")
    print(f"   • Min Latency:        {min_latency:.2f} ms")
    print(f"   • Avg Latency:        {avg_latency:.2f} ms")
    print(f"   • Median (p50):       {p50_latency:.2f} ms")
    print(f"   • p95 Latency:        {p95_latency:.2f} ms")
    print(f"   • p99 Latency:        {p99_latency:.2f} ms")
    print(f"   • Max Latency:        {max_latency:.2f} ms")
    print("=" * 65)

if __name__ == "__main__":
    asyncio.run(run_load_test(total_users=CONCURRENT_USERS, max_concurrency=100))
