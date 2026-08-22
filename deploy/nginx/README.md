# Product edge rate limiting

Nginx is the canonical Compose entry point at `http://localhost:8080`.

- `POST /api/products`: edge burst guard at 5 requests/minute with burst 3.
- The API additionally enforces the business quota configured by `RateLimiting:ProductCreatesPerHour` (20/hour by default) with a Redis counter keyed by Seller and UTC hour, so the quota remains correct across API replicas.
- Product uploads are capped at 45 MB per request at the edge; the API still validates at most 8 images and 5 MB per image.
- API, uploaded media, health checks, and MVC pages share one origin through the reverse proxy.

Inspect the effective Nginx configuration after starting Compose:

```powershell
docker compose exec nginx nginx -T
```

Reload after a configuration-only change:

```powershell
docker compose exec nginx nginx -s reload
```
