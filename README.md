# Avast Uninstaller Support Tool

> Removes Avast Antivirus completely using Avast's own official cleanup tools, running in Safe Mode where Avast's self-protection cannot interfere.

---

## English

### Why this tool?

Avast embeds self-protection mechanisms that block standard uninstallation. This tool restarts Windows into **Safe Mode**, where Avast services are inactive, and runs Avast's own removal tools to ensure a clean removal.

### How it works

```
Normal Mode
  └─ OS check (Win 7 SP1+)
  └─ Form: 10-second countdown → Restart to Safe Mode
     └─ Sets RunOnce *FrmAvastUnins  (fires even in Safe Mode)
     └─ bcdedit safeboot minimal → shutdown /r

Safe Mode  (automatic via RunOnce)
  └─ avastclear.exe  ← Avast's official cleanup tool (GUI)
  └─ WriteCompletionFlag + sets RunOnce FrmAvastUninsNormal
  └─ bcdedit deletevalue safeboot → shutdown /r

Normal Mode  (automatic via RunOnce)
  └─ icarus.exe /manual_update /uninstall:avast-suite
  └─ Cleans leftover registry keys
  └─ DonateForm: shows completion banner + support QR codes
```

### Requirements

| | |
|---|---|
| OS | Windows 7 SP1 (build 7601) or later |
| Arch | x86 / x64 |
| Rights | Administrator |

### Usage

1. Run **`Avast Uninstaller Support Tool.exe`** as Administrator
2. Wait for the 10-second countdown (or click the button immediately)
3. The PC restarts into Safe Mode — `avastclear.exe` opens automatically
4. Complete the removal in `avastclear.exe`, then wait
5. The PC restarts back to Normal Mode and finishes cleanup automatically

### Build

Open `frm_avast_uninstaller.slnx` in Visual Studio 2022, set configuration to **Release**, build.  
The output is a single portable `.exe` — no installer needed.

---

## Tiếng Việt

### Tại sao cần tool này?

Avast có cơ chế tự bảo vệ ngăn việc gỡ cài đặt thông thường. Tool này khởi động lại Windows vào **Safe Mode** — nơi Avast không thể tự bảo vệ — và chạy chính tool gỡ cài đặt của Avast để đảm bảo gỡ sạch hoàn toàn.

### Cách hoạt động

```
Normal Mode (lần 1)
  └─ Kiểm tra OS (Win 7 SP1 trở lên)
  └─ Form: đếm ngược 10 giây → Khởi động vào Safe Mode
     └─ Đặt RunOnce *FrmAvastUnins  (chạy được trong Safe Mode nhờ prefix *)
     └─ bcdedit safeboot minimal → shutdown /r

Safe Mode  (tự động qua RunOnce)
  └─ avastclear.exe  ← tool chính chủ của Avast (có giao diện)
  └─ Ghi CompletionFlag + đặt RunOnce FrmAvastUninsNormal
  └─ bcdedit deletevalue safeboot → shutdown /r

Normal Mode (lần 2 — tự động qua RunOnce)
  └─ icarus.exe /manual_update /uninstall:avast-suite
  └─ Xóa các registry key còn sót lại
  └─ Hiện DonateForm: banner "Hoàn tất" + mã QR ủng hộ
```

### Yêu cầu

| | |
|---|---|
| OS | Windows 7 SP1 (build 7601) trở lên |
| Kiến trúc | x86 / x64 |
| Quyền | Administrator |

### Cách dùng

1. Chạy **`Avast Uninstaller Support Tool.exe`** bằng quyền Administrator
2. Chờ đếm ngược 10 giây (hoặc bấm nút ngay)
3. Máy tính khởi động vào Safe Mode — `avastclear.exe` tự mở
4. Hoàn tất gỡ cài đặt trong `avastclear.exe`, chờ xong
5. Máy tính tự khởi động về Normal Mode và hoàn tất dọn dẹp

### Build

Mở `frm_avast_uninstaller.slnx` trong Visual Studio 2022, chọn configuration **Release**, build.  
Output là một file `.exe` duy nhất, portable — không cần cài đặt.

---

## License

[MIT](LICENSE) © 2026 thanhnguyenit9x93
