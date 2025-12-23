# 🔍 Hướng dẫn sử dụng chức năng giải mã file EZP

## ✅ Đã thêm

### 1. Methods trong DeThiExportService:

#### a) `DecryptEzpFile(string encryptedContent)`
- **Input**: Nội dung file EZP (string)
- **Output**: `(bool Success, string Message, string? DecryptedJson)`
- **Dùng để**: Giải mã nội dung đã có sẵn

#### b) `DecryptEzpFileFromBytes(byte[] fileBytes)`
- **Input**: Byte array của file EZP
- **Output**: `(bool Success, string Message, string? DecryptedJson)`
- **Dùng để**: Giải mã file upload

---

## 📝 Cách sử dụng

### Option 1: Sử dụng trong code

```csharp
// Inject service
private readonly DeThiExportService _exportService;

// Giải mã từ string
var (success, message, jsonContent) = _exportService.DecryptEzpFile(encryptedString);
if (success)
{
    Console.WriteLine($"JSON: {jsonContent}");
}

// Giải mã từ byte array
byte[] fileBytes = await File.ReadAllBytesAsync("test.ezp");
var (success, message, jsonContent) = _exportService.DecryptEzpFileFromBytes(fileBytes);
```

### Option 2: Sử dụng API endpoint (KHUYẾN NGHỊ)

**Thêm endpoint sau vào `DeThiController.cs` (trước dấu `}` cuối):**

Xem file: `DecryptEzpEndpoint.cs.txt` và copy code vào cuối `DeThiController.cs`

**Vị trí:** Sau method `ExportTuLuanWord`, trước dấu `}` đóng class.

---

## 🧪 Test với API

### 1. Start app:
```bash
dotnet run
```

### 2. Mở Swagger:
```
https://localhost:xxxx/swagger
```

### 3. Tìm endpoint:
```
POST /api/DeThi/decrypt-ezp
```

### 4. Upload file .ezp:
- Click "Try it out"
- Choose file .ezp
- Click "Execute"

### 5. Kết quả:
```json
{
  "success": true,
  "message": "Xử lý file thành công!",
  "data": {
    "message": "Giải mã thành công!",
    "jsonContent": "{ \"ExportVersion\": \"1.0\", ... }",
    "fileSize": 12345,
    "isEncrypted": true
  }
}
```

---

## 🎯 Use Cases

### 1. Kiểm tra file có mã hóa không:
```
Upload file → Nếu:
- isEncrypted: true → File được mã hóa
- isEncrypted: false → File plain JSON
```

### 2. Debug password sai:
```
Upload file → Nếu lỗi:
"Mật khẩu không đúng! Kiểm tra lại password trong appsettings.json"
→ Password trong config sai
```

### 3. Xem nội dung đề thi:
```
Upload file → Copy "jsonContent"
→ Paste vào JSON viewer online
→ Xem cấu trúc đề thi
```

---

## 💡 Ví dụ với Postman

### Request:
```
POST https://localhost:7001/api/DeThi/decrypt-ezp
Content-Type: multipart/form-data

Body:
- file: [Select .ezp file]
```

### Response Success (Encrypted):
```json
{
  "success": true,
  "message": "Xử lý file thành công!",
  "data": {
    "message": "Giải mã thành công!",
    "jsonContent": "{\"ExportVersion\":\"1.0\",\"DeThiInfo\":{...}}",
    "fileSize": 5678,
    "isEncrypted": true
  }
}
```

### Response Success (Plain JSON):
```json
{
  "success": true,
  "message": "Xử lý file thành công!",
  "data": {
    "message": "File không được mã hóa (plain JSON).",
    "jsonContent": "{\"ExportVersion\":\"1.0\",...}",
    "fileSize": 5678,
    "isEncrypted": false
  }
}
```

### Response Error (Wrong Password):
```json
{
  "success": false,
  "message": "Mật khẩu không đúng! Kiểm tra lại password trong appsettings.json",
  "data": null
}
```

---

## 🔧 Troubleshooting

| Lỗi | Nguyên nhân | Giải pháp |
|-----|-------------|-----------|
| "File rỗng" | Upload file 0 bytes | Kiểm tra lại file |
| "Mật khẩu không đúng" | Password config sai | Kiểm tra appsettings.json |
| "Không tìm thấy password trong cấu hình" | Thiếu EzpSettings | Thêm config vào appsettings.json |
| "Lỗi đọc file" | File corrupt | Tải lại file |

---

## ✨ Lưu ý

- ✅ Method này **CHỈ** dùng để test/debug
- ✅ Không nên expose endpoint này ra production
- ✅ Password được lấy từ appsettings.json tự động
- ✅ Hỗ trợ cả file mã hóa và plain JSON

---

**Happy Testing!** 🎉
