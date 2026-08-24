# Hướng dẫn chạy & test CloneEbay Seller (Role 1 + Role 2 + Role 3)

Tài liệu này giúp bạn chạy project trên máy, sửa lỗi thường gặp, và có kịch bản test cho từng tính năng — đặc biệt là module **Coupon (Role 3)** vừa được thêm.

---

## 0. Chẩn đoán lỗi bạn đang gặp: "mở lên chỉ thấy Home, không login được"

Đã kiểm tra trực tiếp trên máy bạn và tìm ra 2 nguyên nhân:

1. **Chỉ có `CloneEbay.Web` được chạy, `CloneEbay.API` chưa chạy.** Trong ảnh Visual Studio bạn gửi, toolbar chỉ chọn `CloneEbay.Web` làm Startup Project. Trang Web gọi API ở `https://localhost:7000` để đăng nhập — nếu API không chạy, request sẽ lỗi kết nối và trang xử lý im lặng (rơi về Home / không phản hồi).
2. **`CloneEbay.API` chưa có connection string tới SQL Server.** Khi thử chạy API trên máy bạn, lỗi chính xác là:
   ```
   Unhandled exception. System.InvalidOperationException: Connection string 'DefaultConnection' is not configured.
      at CloneEbay.Infrastructure.DependencyInjection.AddInfrastructure(...)
   ```
   Nghĩa là bạn chưa chạy script cấu hình secrets lần nào trên máy này — kể cả khi bạn bật API lên, nó sẽ crash ngay khi khởi động.

Tin tốt: máy bạn **đã có SQL Server chạy sẵn** (service `MSSQLSERVER` đang `Running`, tên máy là `FTMH`, instance mặc định — nghĩa là không cần hậu tố `\SQLEXPRESS` gì cả, chỉ cần `localhost`).

### Cách sửa — làm theo đúng thứ tự

**Bước 1 — Cấu hình secrets (chạy 1 lần duy nhất).** Mở PowerShell tại thư mục gốc project:

```powershell
cd "C:\Users\ASUS\Downloads\EBay_Seller-main\EBay_Seller-main"
.\scripts\configure-local-secrets.ps1 -SqlServer "localhost"
```

Script sẽ hỏi 3 mật khẩu (nhập gì cũng được, chỉ cần **nhớ lại**):
- SQL Server password → mật khẩu đăng nhập SQL bằng tài khoản `sa` (nếu bạn không nhớ mật khẩu `sa`, xem mục Troubleshooting bên dưới).
- Local admin password → mật khẩu bạn sẽ dùng để đăng nhập tài khoản `admin@cloneebay.local`.
- Demo seller password → mật khẩu dùng chung cho 3 tài khoản seller demo có sẵn.

**Bước 2 — Áp migration** (tạo toàn bộ bảng, kể cả bảng `Coupon` mới):

```powershell
dotnet ef database update --project CloneEbay.Infrastructure --startup-project CloneEbay.API
```

**Bước 3 — Cấu hình Visual Studio chạy CẢ HAI project cùng lúc** (bắt buộc, nếu không Web sẽ không gọi được API):

1. Chuột phải vào **Solution 'CloneEbay_Seller'** trong Solution Explorer → **Configure Startup Projects...**
2. Chọn **Multiple startup projects**.
3. Đặt `CloneEbay.API` = **Start**, `CloneEbay.Web` = **Start**, các project còn lại = **None**.
4. Kéo `CloneEbay.API` lên trên `CloneEbay.Web` trong danh sách (để API khởi động trước).
5. Bấm **OK**, sau đó bấm **F5** (hoặc nút ▷ màu xanh) để chạy — Visual Studio sẽ mở 2 trình duyệt/console cùng lúc.

Nếu không dùng Visual Studio, chạy CLI với 2 terminal riêng:

```powershell
dotnet run --project CloneEbay.API --launch-profile https
```
```powershell
dotnet run --project CloneEbay.Web --launch-profile https
```

Kiểm tra API sống chưa: mở `https://localhost:7000/swagger` — thấy trang Swagger UI là API đã chạy đúng.

---

## 1. Tài khoản để test

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@cloneebay.local` | mật khẩu bạn đặt ở Bước 1 (Local admin password) |
| Seller đã duyệt (Approved) | `seller.approved@cloneebay.local` | mật khẩu Demo seller bạn đặt ở Bước 1 |
| Seller đang chờ duyệt (Pending) | `seller.pending@cloneebay.local` | mật khẩu Demo seller |
| Seller bị từ chối (Rejected) | `seller.rejected@cloneebay.local` | mật khẩu Demo seller |

> Muốn test đầy đủ module Coupon, đăng nhập bằng `seller.approved@cloneebay.local` — tài khoản này có sẵn 4 sản phẩm demo (Role 2 seed data) để gắn coupon vào.

---

## 2. Kịch bản test theo từng module

### 2.1. Role 1 — Auth & Seller Verification
1. Vào `https://localhost:7248` → **Đăng ký** một tài khoản mới → phải chuyển sang trạng thái đăng nhập được ngay.
2. Đăng xuất, đăng nhập lại bằng tài khoản vừa tạo.
3. Vào **"Xác minh Seller"** → nộp hồ sơ (Apply) → trạng thái phải là **Pending**.
4. Đăng nhập bằng `admin@cloneebay.local` → vào **"Duyệt hồ sơ"** → duyệt (Approve) hồ sơ vừa nộp.
5. Đăng nhập lại tài khoản seller vừa được duyệt → menu phải hiện thêm **Sản phẩm**, **Mã giảm giá**, **Hồ sơ cửa hàng** (vì đã `verified = true`).

### 2.2. Role 2 — Sản phẩm & Đấu giá
1. Đăng nhập `seller.approved@cloneebay.local` → **Sản phẩm** → **Đăng sản phẩm mới** → điền form, upload 1-8 ảnh → submit.
2. Thử để trống ảnh / nhập giá âm → phải báo lỗi validate, không cho submit.
3. Sửa tồn kho (Inventory) về `0` → trạng thái sản phẩm tự chuyển **OutOfStock**; sửa lại > 0 → tự chuyển lại **Active**.
4. Tạo 1 sản phẩm đấu giá (`IsAuction = true`, đặt `AuctionEndTime` vài phút sau) → vào trang chi tiết, đăng nhập tài khoản khác đặt giá (bid) → giá đặt sau phải cao hơn giá trước, nếu không sẽ báo lỗi `BID_TOO_LOW`.

### 2.3. Role 3 — Mã giảm giá (Coupon) — mới thêm
1. Đăng nhập `seller.approved@cloneebay.local` → menu **Mã giảm giá** → **Tạo mã giảm giá**.
2. Chọn 1 sản phẩm, nhập mã (vd `SUMMER10`), phần trăm giảm (vd `10`), ngày bắt đầu/kết thúc, số lượt tối đa (vd `5`) → Submit.
   - Nếu đã bật reCAPTCHA thật (`Recaptcha:Enabled = true`), mở DevTools → tab **Network** trước khi submit để thấy request `POST /api/coupons` có kèm field `recaptchaToken`.
3. Thử tạo mã trùng code cho **cùng 1 sản phẩm** → phải báo lỗi "Mã này đã tồn tại cho sản phẩm đã chọn" (`COUPON_CODE_ALREADY_EXISTS`).
4. Thử nhập `EndDate` trước `StartDate` → phải bị chặn ngay ở form (validate client-side) và cả ở API nếu bypass.
5. Thử đăng nhập bằng seller khác (không sở hữu sản phẩm đó) và gọi sửa/xóa coupon qua Swagger → phải bị từ chối `403 FORBIDDEN_NOT_OWNER`.
6. Test API `validate` (không cần đăng nhập) — mở trực tiếp trên trình duyệt hoặc Swagger:
   ```
   GET https://localhost:7000/api/coupons/validate?code=SUMMER10&productId=<id-sản-phẩm>
   ```
   Kết quả mong đợi khi hợp lệ:
   ```json
   { "success": true, "data": { "isValid": true, "reason": null, "couponId": 1, "discountPercent": 10, "remainingUsage": 5 } }
   ```
   Đổi `code` sai hoặc `productId` sai → `isValid: false`, `reason: "COUPON_NOT_FOUND"`.
7. Sửa `EndDate` của 1 coupon về quá khứ (qua nút **Chỉnh sửa**) → coupon phải hiện trạng thái **Hết hạn (Expired)** trong danh sách, và gọi `validate` phải trả `reason: "EXPIRED"`.
8. Lọc danh sách coupon theo sản phẩm / trạng thái ở trang **Mã giảm giá** → danh sách phải lọc đúng.

---

### 2.4. Role 4 — Quản lý Đơn hàng & Phiếu vận chuyển (Module 4) — mới thêm

1. **Xem danh sách đơn hàng của Seller (F4.1)**:
   - Đăng nhập `seller.approved@cloneebay.local` → Gọi API `GET /api/orders?status=Pending`.
   - Trong kết quả trả về `data.items[]`, trường `totalPrice` chỉ tính tổng tiền hàng của seller hiện tại (nếu đơn có sản phẩm của seller khác, tiền của seller khác không được cộng vào).
2. **Xem chi tiết đơn hàng (F4.1 & F4.5 Detail)**:
   - Gọi `GET /api/orders/{id}` với đơn hàng ở trên.
   - Danh sách `orderItems` chỉ hiển thị sản phẩm thuộc về seller này. Có thông tin `address`, `payment`, `shipping`.
3. **Xác nhận đơn hàng & Trừ tồn kho (F4.2)**:
   - Gọi `PUT /api/orders/{id}/confirm` trên đơn ở trạng thái `Pending` và `Payment.status = Completed`.
   - Đơn chuyển sang `status: "Confirmed"`, đồng thời `Inventory.quantity` của sản phẩm bị trừ đúng số lượng.
   - Test ngoại lệ 1 (Payment chưa completed): Thử confirm đơn có Payment pending → Báo lỗi `409 PAYMENT_NOT_COMPLETED`.
   - Test ngoại lệ 2 (Kho không đủ): Thử confirm đơn có số lượng lớn hơn kho → Báo lỗi `409 INSUFFICIENT_INVENTORY`, không trừ kho 1 phần.
4. **Tạo nhãn vận chuyển (F4.4)**:
   - Gọi `POST /api/orders/{id}/shipping-label` với body `{"carrier": "GHN"}` trên đơn đã `Confirmed`.
   - Trả về mã `trackingNumber` tự sinh (vd `GHN-20260824...`) và `status: "LabelCreated"`.
   - Trạng thái `OrderTable.status` **VẪN LÀ `"Confirmed"`** (chưa tự chuyển `Shipped`).
   - Test ngoại lệ: Gọi tạo nhãn trên đơn `Pending` → Báo lỗi `409 ORDER_NOT_CONFIRMED`.
5. **Cập nhật trạng thái vận chuyển & State Machine (F4.3)**:
   - Gọi `PUT /api/orders/{id}/status` với body `{"status": "HandedToCarrier"}`.
   - Trạng thái đơn tự động đồng bộ sang `status: "Shipped"`.
   - Tiếp tục cập nhật sang `InTransit` và `Delivered`.
   - Test ngoại lệ: Đơn đang ở `Delivered`, cố chuyển ngược về `Preparing` → Báo lỗi `422 INVALID_STATUS_TRANSITION`.
6. **Xem thông tin Vận chuyển & Thanh toán Read-Only (F4.5)**:
   - Gọi `GET /api/shipping/{orderId}` → Trả về carrier, trackingNumber, status, estimatedArrival.
   - Gọi `GET /api/orders/{id}/payment` → Trả về amount, method, status, paidAt (Read-only).

---

## 3. Test nhanh bằng Swagger (không cần giao diện Web)

1. Mở `https://localhost:7000/swagger`.
2. Gọi `POST /api/auth/login` với email/mật khẩu seller đã duyệt → copy `accessToken` từ response.
3. Bấm nút **Authorize** (góc trên phải Swagger) → dán `Bearer <accessToken>`.
4. Giờ có thể gọi trực tiếp các API `Coupons`: `POST /api/coupons`, `GET /api/coupons`, `PUT /api/coupons/{id}`, `DELETE /api/coupons/{id}`.
   - Lưu ý: nếu `Recaptcha:Enabled = true` trên API, `POST /api/coupons` bắt buộc có `recaptchaToken` hợp lệ trong body — test qua Swagger sẽ không có token thật (vì không chạy qua trình duyệt), nên sẽ bị từ chối `403 RECAPTCHA_VERIFICATION_FAILED`. Muốn test nhanh qua Swagger, tạm tắt bằng cách:
     ```powershell
     dotnet user-secrets set "Recaptcha:Enabled" "false" --project CloneEbay.API
     ```
     Nhớ bật lại `"true"` khi test qua giao diện Web thật.

---

## 4. Kiểm tra kết nối Database khi gặp lỗi

Nếu API vẫn báo lỗi liên quan `DefaultConnection` / không kết nối được SQL Server sau khi đã chạy `configure-local-secrets.ps1`:

```powershell
# Xem secret hiện tại đang trỏ tới đâu (không hiện mật khẩu dạng plaintext an toàn, nhưng bạn thấy được server/database)
dotnet user-secrets list --project CloneEbay.API

# Test kết nối SQL Server trực tiếp bằng sqlcmd (nếu có cài SQL Server Command Line Tools)
sqlcmd -S localhost -U sa -P "<mật-khẩu-sa-của-bạn>" -Q "SELECT @@VERSION"
```

Lỗi thường gặp:
- **Login failed for user 'sa'`** → tài khoản `sa` có thể đang bị disable hoặc SQL Server đang ở chế độ **Windows Authentication only**. Mở **SQL Server Management Studio (SSMS)** → chuột phải server → **Properties → Security** → chọn **SQL Server and Windows Authentication mode** → khởi động lại service `MSSQLSERVER` → vào **Security → Logins → sa** → Enable + đặt lại mật khẩu.
- **Cannot open database "CloneEbayDB"** → database chưa tồn tại, chạy lại Bước 2 (`dotnet ef database update`) để nó tự tạo.
- **A network-related or instance-specific error** → sai tên server. Vì máy bạn dùng instance mặc định, dùng `localhost` (không phải `GRIFFITHTM` — đó là tên máy của người tạo project gốc).

---

## 5. Checklist trước khi báo "đã test xong"

- [ ] `dotnet build CloneEbay_Seller.sln` chạy sạch, 0 lỗi.
- [ ] `dotnet test CloneEbay_Seller.sln` toàn bộ pass (bao gồm 19 test mới của `Role3CouponTests`).
- [ ] Đăng nhập được cả 4 loại tài khoản ở mục 1.
- [ ] Tạo/sửa/xóa được 1 coupon qua giao diện Web.
- [ ] `GET /api/coupons/validate` trả đúng kết quả cho các trường hợp: hợp lệ, sai mã, hết hạn, hết lượt.
- [ ] reCAPTCHA thật hoạt động (thấy `recaptchaToken` trong request khi tạo coupon qua Web, và bị từ chối nếu tắt token giả qua Swagger lúc `Recaptcha:Enabled = true`).
