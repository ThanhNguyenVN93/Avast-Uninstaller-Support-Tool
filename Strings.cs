using System.Globalization;

namespace frm_avast_uninstaller
{
    internal static class Strings
    {
        private static readonly bool Vi =
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi";

        // ── Form1 ────────────────────────────────────────────────────────
        public static string FormTitle =>
            Vi ? "Công cụ gỡ cài đặt Avast" : "Avast Uninstaller Support Tool";

        public static string NormalModeStatus =>
            Vi ? "Công cụ sẽ khởi động vào Safe Mode để gỡ Avast."
               : "The tool will restart into Safe Mode to remove Avast.";

        public static string RestartButton =>
            Vi ? "Khởi động lại vào Safe Mode" : "Restart to Safe Mode";

        public static string Restarting =>
            Vi ? "Đang khởi động lại vào Safe Mode..." : "Restarting into Safe Mode...";

        public static string ErrorPrefix => Vi ? "Lỗi: " : "Error: ";

        public static string Countdown(int s) =>
            Vi ? $"Tự động khởi động lại sau: {s} giây" : $"Auto-restarting in: {s} s";

        // ── Program ──────────────────────────────────────────────────────
        public static string UninstallerMissing =>
            Vi ? "Không tìm thấy avastclear.exe trong resources."
               : "avastclear.exe not found in embedded resources.";

        public static string UninstallerStartFailed =>
            Vi ? "Không thể khởi chạy avastclear.exe."
               : "Failed to start avastclear.exe.";

        public static string HashMismatch =>
            Vi ? "File avastclear.exe nhúng không khớp hash SHA-256. Có thể file đã bị thay đổi."
               : "Embedded avastclear.exe SHA-256 hash mismatch. The file may have been tampered with.";

        public static string UninstallBanner =>
            Vi ? "✓  Đã gỡ cài đặt Avast hoàn tất!"
               : "✓  Avast has been successfully removed!";

        public static string UninstallComplete =>
            Vi ? "Đã gỡ cài đặt Avast hoàn tất!\nMáy tính sẽ tự động khởi động về chế độ bình thường."
               : "Avast has been successfully removed!\nThe computer will restart normally.";

        public static string DoneTitle  => Vi ? "Hoàn Tất" : "Done";
        public static string ErrorTitle => Vi ? "Lỗi"      : "Error";

        public static string AlreadyRunning =>
            Vi ? "Ứng dụng đang chạy. Không thể mở thêm instance."
               : "Application is already running. Cannot open another instance.";

        public static string RunOnceWriteFailed =>
            Vi ? "Không thể ghi registry RunOnce. Máy tính sẽ không tự chạy tool sau khi khởi động lại."
               : "Failed to write RunOnce registry entry. The tool will not auto-run after restart.";

        public static string WindowsDirNotFound =>
            Vi ? "Không tìm thấy thư mục Windows."
               : "Windows directory not found.";

        public static string SystemExeNotFound(string exe) =>
            Vi ? $"Không tìm thấy {exe} trong thư mục hệ thống."
               : $"{exe} not found in system directory.";

        public static string ProcessStartFailed(string exe) =>
            Vi ? $"Không thể khởi chạy {exe}."
               : $"Failed to start {exe}.";

        public static string BcdeditFailed(int exitCode) =>
            Vi ? $"bcdedit thất bại (exit code {exitCode}). Cấu hình boot không được thay đổi."
               : $"bcdedit failed (exit code {exitCode}). Boot configuration was not changed.";

        // ── DonateForm ───────────────────────────────────────────────────
        public static string DonateTitle =>
            Vi ? "Ủng hộ tác giả" : "Donate";

        public static string DonateThanks =>
            Vi ? "Cảm ơn bạn đã sử dụng tool! Nếu thấy hữu ích, hãy ủng hộ tác giả ♥"
               : "Thanks for using this tool! If it helped, consider supporting the author ♥";

        public static string DonateTCB   => Vi ? "Techcombank" : "Techcombank";
        public static string DonateMoMo  => Vi ? "Ví MoMo"     : "MoMo Wallet";
        public static string DonateClose => Vi ? "Đóng"        : "Close";

        // ── Errors ───────────────────────────────────────────────────────
        public static string NormalCleanupError(string msg) =>
            Vi ? "Lỗi dọn dẹp sau Safe Mode:\n"  + msg
               : "Post-Safe-Mode cleanup error:\n" + msg;

        public static string SafeModeError(string msg) =>
            Vi ? "Lỗi xử lý Safe Mode:\n" + msg
               : "Safe Mode error:\n"      + msg;

        public static string OsNotSupported =>
            Vi ? "Hệ điều hành này không được hỗ trợ.\nYêu cầu Windows 7 SP1 (build 7601) trở lên."
               : "This operating system is not supported.\nRequires Windows 7 SP1 (build 7601) or later.";
    }
}
