# CloneEbay Seller - Role 1 + Role 2 + Role 3

Role 1 provides the shared authentication and seller foundation:

- Register, login, refresh-token rotation, and logout.
- JWT role authorization and the `VerifiedSeller` policy.
- Seller apply, verification status, admin approval/rejection.
- Public and editable store profiles with local banner upload.
- ASP.NET Core MVC seller UI with cookie session and API JWT forwarding.
- SQL Server EF Core migration, three demo seller states, health checks, Docker Compose, and Kubernetes rolling updates.

Role 2 provides the product catalog and auction workflow assigned to Thành:

- Product, Category, Inventory, and Bid domain models with SQL Server mapping and migration.
- Verified-Seller create/update/hide/delete APIs with 1–8 validated product images.
- Public product list/detail with filter, search, sort, and pagination.
- Inventory synchronization and automatic `Active`/`OutOfStock` transitions.
- Fixed-price and auction listings, bidding, Seller bid ledger, manual close, and background expiry closing.
- Responsive Product Command Center, Listing Studio with live preview, and public listing detail UI.
- Two-layer product-create protection: Nginx burst limiting plus a Redis-backed 20-products/hour/seller quota.
- Serializable bid/auction transactions, idempotent auction closing, and automatic winning Order creation.
- Automated xUnit coverage for critical Role 1 and Role 2 acceptance rules.

Role 3 provides marketing coupons for the product catalog:

- Coupon CRUD scoped to one product per code, owned exclusively by the listing's verified seller.
- `GET /api/coupons/validate` for order/checkout flows to check a code before applying a discount.
- Computed lifecycle status (`Scheduled` / `Active` / `Expired` / `UsedUp`) plus usage-count tracking.
- Google reCAPTCHA v3 verification on coupon creation, with score/action checks and failure logging.
- Seller Web UI: coupon list with product/status filters and a create/edit form with the reCAPTCHA widget.

## Architecture

```text
CloneEbay.API -> CloneEbay.Application -> CloneEbay.Domain
        |                 ^
        +-> CloneEbay.Infrastructure

CloneEbay.Web -> HTTP/JWT -> CloneEbay.API
```

`Domain` has no outward dependency. `Application` owns use cases and interfaces. `Infrastructure` implements persistence and security. `API` is the composition and HTTP layer.

## Local setup

Prerequisites: .NET 8 SDK/runtime and a reachable SQL Server instance.

1. Restore packages:

   ```powershell
   dotnet restore CloneEbay_Seller.sln
   ```

2. Configure secrets interactively. The default server is `GRIFFITHTM`, database `CloneEbayDB`, and SQL user `sa`:

   ```powershell
   .\scripts\configure-local-secrets.ps1
   ```

3. Apply all Role 1 and Role 2 migrations:

   ```powershell
   dotnet ef database update --project CloneEbay.Infrastructure --startup-project CloneEbay.API
   ```

4. Start the API and Web in separate terminals:

   ```powershell
   dotnet run --project CloneEbay.API --launch-profile https
   dotnet run --project CloneEbay.Web --launch-profile https
   ```

API Swagger: `https://localhost:7000/swagger`  
Seller Web: `https://localhost:7248`

The development admin email is `admin@cloneebay.local`; its password is the value entered by the setup script. The next development startup creates the admin or safely resets that local admin password.

The setup script also asks for one local demo password and seeds these idempotent accounts:

```text
seller.approved@cloneebay.local  -> Seller / Approved
seller.pending@cloneebay.local   -> Buyer / Pending
seller.rejected@cloneebay.local  -> Buyer / Rejected
```

## Docker Compose

Copy the environment template, replace every placeholder, and start the isolated API, Web, SQL Server, and Redis development stack:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Canonical Compose entry point (Web + API + uploads): `http://localhost:8080`  
Direct Web debug port: `http://localhost:7248`  
Direct API/Swagger debug port: `http://localhost:7000/swagger`

The ignored `.env` file is for local development only. Do not commit it.

## Main API routes

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh-token
POST /api/auth/logout

POST /api/seller/apply
GET  /api/seller/verification-status

GET  /api/admin/seller
PUT  /api/admin/seller/{id}/approve
PUT  /api/admin/seller/{id}/reject

GET  /api/store/{sellerId}
GET  /api/store/me
PUT  /api/store

GET    /api/categories
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
PATCH  /api/products/{id}/hide
DELETE /api/products/{id}

GET /api/inventory/{productId}
PUT /api/inventory/{productId}

POST /api/products/{id}/bids
GET  /api/products/{id}/bids
POST /api/products/{id}/close-auction

GET    /api/coupons?productId=&status=
GET    /api/coupons/{id}
POST   /api/coupons
PUT    /api/coupons/{id}
DELETE /api/coupons/{id}
GET    /api/coupons/validate?code=&productId=
```

`PUT /api/store` accepts either `application/json` for partial updates or `multipart/form-data` when uploading a banner. JSON field names follow the shared contract: `storeName`, `description`, and `bannerImageURL`.

All JSON responses use the shared envelope:

```json
{
  "success": true,
  "data": {},
  "error": null,
  "meta": { "traceId": "..." }
}
```

JWT access tokens expose `sub`, `role`, optional `storeId`, and `verified`. Product and Inventory write endpoints protect ownership and use the API policy:

```csharp
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
```

Run the full Role 1 acceptance flow against a running API. The script prompts for the admin password and creates one uniquely named smoke-test user:

```powershell
.\scripts\smoke-role1.ps1 -ApiBaseUrl https://localhost:7000
```

## Role 2 behavior

`POST /api/products` and `PUT /api/products/{id}` accept `multipart/form-data`. A create request requires `Title`, `Description`, `Price`, `CategoryId`, `IsAuction`, `Quantity`, and one to eight `Images`; an auction additionally requires `AuctionEndTime` at least one minute in the future.

Only the listing owner can manage a product or view its full bid ledger. A hidden listing is not public. Quantity `0` moves an active listing to `OutOfStock`; replenishing it makes it active again. Product and inventory edits commit atomically. Products referenced by `OrderItem` cannot be deleted. Once an auction has a bid, its starting price/type are locked and the listing cannot be deleted.

Bid placement and auction closing use SQL Server serializable transactions with update locks. The closing worker checks expired auctions every 30 seconds, resolves equal bids by earliest timestamp, and atomically creates one `OrderTable` plus one `OrderItem` with status `WaitingPayment`. A unique auction-order key makes repeated/manual/multi-pod close attempts idempotent.

Run repeatable unit and acceptance checks:

```powershell
dotnet test CloneEbay_Seller.sln
.\scripts\smoke-role1.ps1 -ApiBaseUrl https://localhost:7000
.\scripts\smoke-role2.ps1 -ApiBaseUrl https://localhost:7000
.\scripts\smoke-role2-auction.ps1 -ApiBaseUrl https://localhost:7000
```

The auction smoke test intentionally waits for the configured auction end and leaves its closed auction/order as traceable acceptance data.

Development startup seeds four idempotent Role 2 listings for `seller.approved@cloneebay.local`. The seeder restores any individually missing demo listing without duplicating the others.

The Nginx edge configuration and verification commands are documented in `deploy/nginx/README.md`.

## Role 3 behavior

A coupon always belongs to exactly one product (`productId`), and only that product's owning seller can create, read, update, or delete it — `POST/PUT/DELETE /api/coupons*` all require the `VerifiedSeller` policy plus ownership of the target product. Coupon codes are case-insensitive and unique per product (not globally), and `discountPercent` must be within `(0, 100]`. Status is computed on read, never stored: `Scheduled` before `startDate`, `Active` between `startDate` and `endDate` while `usageCount < maxUsage`, `Expired` after `endDate`, and `UsedUp` once `usageCount` reaches `maxUsage`. Coupons that have already been redeemed at least once (`usageCount > 0`) cannot be deleted.

`GET /api/coupons/validate?code=&productId=` is anonymous by design so a buyer-facing checkout flow (Role 4 / Order module) can check a code before applying a discount; it returns `isValid` plus a machine-readable `reason` (`COUPON_NOT_FOUND`, `NOT_STARTED`, `EXPIRED`, `USAGE_LIMIT_REACHED`) instead of an error status.

Coupon creation is protected by Google reCAPTCHA v3: the Seller Web create form requests a token scoped to the `create_coupon` action and forwards it as `recaptchaToken`; the API verifies it against Google's `siteverify` endpoint, checks the action name and the `Recaptcha:MinimumScore` threshold (default `0.5`), and logs every rejected attempt (missing/invalid token, action mismatch, low score, upstream failure). Verification is fully opt-in via `Recaptcha:Enabled` — set to `false` (the default) for local development so every token is accepted, and set the real `Recaptcha:SecretKey` (API) / `Recaptcha:SiteKey` (Web) only where a Google reCAPTCHA v3 site is registered.

## Kubernetes

Manifests are under `deploy/k8s`. Copy `secret.example.yaml` to the ignored `secret.yaml`, replace placeholders locally, and do not commit the real secret. Build/publish the `cloneebay-api:1.0.0` and `cloneebay-web:1.0.0` images, then apply the complete stack with `kubectl apply -k deploy/k8s`. A one-shot `--migrate-only` Job performs additive database migrations without running demo seeds; wait for it with `kubectl wait --for=condition=complete job/cloneebay-database-migration -n cloneebay --timeout=180s` before checking the API rollout. The stack includes Redis persistence and readiness checks. The API uses two replicas, `maxUnavailable: 0`, startup/readiness/liveness probes, an HPA, and a PodDisruptionBudget.

Both deployments use a pre-stop drain delay, a termination grace period, and shared persistent Data Protection keys. API banner uploads use a shared `ReadWriteMany` volume so a public banner remains available regardless of which replica handles the request.

The SQL Server hostname used inside Kubernetes must be reachable from the cluster; a Windows machine name that works locally may not resolve from a container cluster.
