# CONTRIBUTING GUIDE — AutoWash Pro
## SWP391 | FPT University

---

## 1. REPO STRUCTURE

```
AutoWashPro/
├── src/
│   ├── API/
│   ├── Application/
│   ├── Domain/
│   └── Infrastructure/
├── issues.md
├── CONTRIBUTING.md
└── README.md
```

> **Rule:** Chỉ Leader setup 4 folder chính ban đầu.  
> Subfolder và file .cs tạo khi nào nhận issue mới tạo.

---

## 2. BRANCH NAMING

```
feature/ISSUE-{number}-{short-description}
fix/ISSUE-{number}-{short-description}
```

**Ví dụ:**
```
feature/ISSUE-01-auth
feature/ISSUE-06-create-booking
fix/ISSUE-06-booking-validation
```

> **Rule:** Mỗi issue = 1 branch riêng. Không code nhiều issue trên cùng 1 branch.

---

## 3. COMMIT MESSAGE

```
[ISSUE-{number}] {Động từ} {mô tả ngắn}
```

**Động từ chuẩn:**
| Động từ | Dùng khi |
|---------|----------|
| `Add` | Tạo file/feature mới |
| `Update` | Sửa logic đã có |
| `Fix` | Sửa bug |
| `Remove` | Xóa file/code |
| `Refactor` | Cải thiện code, không đổi behavior |

**Ví dụ:**
```
[ISSUE-01] Add register endpoint
[ISSUE-01] Add JWT middleware
[ISSUE-01] Fix duplicate phone validation
[ISSUE-06] Add booking validation service
[ISSUE-06] Update pending quota check logic
```

> **Rule:** Commit nhỏ, rõ ràng. Không commit "fix bug" hay "update code".

---

## 4. WORKFLOW

```
1. Nhận issue từ Leader
2. Kéo code mới nhất từ main
        git checkout main
        git pull origin main

3. Tạo branch mới
        git checkout -b feature/ISSUE-XX-ten-issue

4. Code theo File Structure trong issue
        - Tạo đúng subfolder như issue mô tả
        - Không đụng file của issue khác

5. Commit thường xuyên (mỗi khi xong 1 task nhỏ)
        git add .
        git commit -m "[ISSUE-XX] Add xxx"

6. Push branch lên remote
        git push origin feature/ISSUE-XX-ten-issue

7. Tạo Pull Request lên main
        - Title: [ISSUE-XX] Tên issue
        - Description: checklist những gì đã làm
        - Assign Leader review

8. Chờ Leader approve → merge
```

---

## 5. PULL REQUEST RULES

- **Không tự merge** PR của mình — phải có Leader approve
- **Không merge** nếu còn conflict chưa giải quyết
- PR title format: `[ISSUE-XX] Tên issue`
- Trong PR description dán checklist từ issue vào, tick những task đã xong

**Ví dụ PR description:**
```
## ISSUE-01: [AUTH] Member Registration & Login

- [x] Tạo POST /api/auth/register
- [x] Tạo POST /api/auth/login
- [x] Tạo JWT middleware
- [ ] Viết unit test  ← chưa xong ghi rõ
```

---

## 6. FOLDER OWNERSHIP

Mỗi người chỉ tạo/sửa file trong scope issue của mình.  
Nếu cần sửa file của issue khác → báo Leader hoặc người sở hữu issue đó trước.

| Issue | Folder chính liên quan |
|-------|----------------------|
| ISSUE-01 | `API/Controllers/AuthController.cs`, `API/Middleware/`, `Application/Services/AuthService.cs` |
| ISSUE-02 | `API/Controllers/ProfileController.cs`, `API/Controllers/VehicleController.cs` |
| ISSUE-03 | `API/Controllers/Admin/AdminCustomerController.cs` |
| ISSUE-04 | `API/Controllers/ServiceController.cs`, `API/Controllers/Admin/AdminServiceController.cs` |
| ISSUE-05 | `API/Controllers/PromotionController.cs`, `API/Controllers/Admin/AdminPromotionController.cs` |
| ISSUE-06 | `API/Controllers/BookingController.cs`, `Application/Services/BookingService.cs`, `Application/Services/InvoiceService.cs` |
| ISSUE-07 | `API/Controllers/BookingController.cs` ← extend ISSUE-06 |
| ISSUE-08 | `API/Controllers/Admin/AdminBookingController.cs`, `Infrastructure/Jobs/AutoNoShowJob.cs` |
| ISSUE-09 | `API/Controllers/Admin/AdminPaymentController.cs`, `Application/Services/PaymentService.cs` |
| ISSUE-10 | `API/Controllers/LoyaltyController.cs`, `Infrastructure/Jobs/PointExpiryJob.cs` |
| ISSUE-11 | `API/Controllers/Admin/AdminTierController.cs`, `Infrastructure/Jobs/TierDowngradeJob.cs` |
| ISSUE-12 | `API/Controllers/Admin/AdminReportController.cs` |

---

## 7. DO & DON'T

| ✅ DO | ❌ DON'T |
|-------|---------|
| Commit mỗi khi xong 1 task nhỏ | Commit cả đống 1 lần cuối ngày |
| Đặt tên branch đúng convention | Đặt tên branch kiểu `test`, `abc`, `mycode` |
| Tạo file đúng folder theo issue | Tạo file lung tung rồi move sau |
| Báo Leader nếu bị block | Im lặng khi không biết làm |
| Pull main trước khi tạo branch | Tạo branch từ branch cũ của mình |
| Giải quyết conflict trước khi tạo PR | Push code conflict lên |

---

## 8. DAILY STANDUP (gợi ý)

Mỗi ngày team sync ngắn:
- **Hôm qua làm gì?** → commit nào, task nào xong
- **Hôm nay làm gì?** → task tiếp theo trong issue
- **Bị block ở đâu?** → cần ai support

