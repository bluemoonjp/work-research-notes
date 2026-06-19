// =============================================================================
// 方法1: Windows DPAPI (CurrentUser) による認証情報の管理
//
// 対象環境  : 社員PC に配布する C# プログラム（Windows 専用）
// 認証情報  : 社員が自分で入力する個別の ID・パスワード
//
// 仕組み:
//   - 初回起動時にコンソールで ID・パスワードを入力する
//   - Windows DPAPI (ProtectedData.Protect) で暗号化して PC に保存する
//   - 2回目以降は保存済みデータを自動で復号して使う
//   - 認証情報を変更したい場合は credentials.dat を削除して再起動する
//
// 重要: DataProtectionScope.CurrentUser で暗号化したデータは
//       「そのWindowsユーザー」×「そのPC」でしか復号できない
//
// 制限: Windows 専用。Linux・macOS では ProtectedData は動作しない
//       (.NET Core / .NET 5+ は Windows のみ対応)
//
// 必要な NuGet パッケージ:
//   - System.Security.Cryptography.ProtectedData
// =============================================================================

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// ─────────────────────────────────────────────────────────────
// 認証情報の保存・読み込み
// ─────────────────────────────────────────────────────────────

static class CredentialStore
{
    // %LOCALAPPDATA% を使う。%APPDATA% はローミングプロファイルで
    // 他のPCにコピーされる恐れがあるため避ける
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyCompanyApp",
        "credentials.dat"
    );

    // アプリ固有のエントロピー（アプリを区別するための補助値。秘密鍵ではない。保護の主体は DataProtectionScope.CurrentUser）
    static readonly byte[] Entropy = { 0x4D, 0x79, 0x41, 0x70, 0x70, 0x4B, 0x65, 0x79 };

    public static bool Exists() => File.Exists(FilePath);

    public static void Save(string userId, string password)
    {
        var plain = Encoding.UTF8.GetBytes($"{userId}\n{password}");
        var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllBytes(FilePath, encrypted);

        // 保存後は平文データをメモリから消去する
        Array.Clear(plain, 0, plain.Length);
    }

    public static (string UserId, string Password) Load()
    {
        var encrypted = File.ReadAllBytes(FilePath);
        byte[] plain;

        try
        {
            plain = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException)
        {
            // Windowsユーザーのパスワード変更時などに発生する
            throw new InvalidOperationException(
                "認証情報の復号に失敗しました。credentials.dat を削除して再起動してください。"
            );
        }

        var text = Encoding.UTF8.GetString(plain);
        Array.Clear(plain, 0, plain.Length);

        var parts = text.Split('\n', 2);
        if (parts.Length < 2)
            throw new InvalidOperationException("credentials.dat の形式が不正です。削除して再起動してください。");

        return (parts[0], parts[1]);
    }

    public static void Delete()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}

// ─────────────────────────────────────────────────────────────
// コンソールからマスク入力（画面に表示しない）
// ─────────────────────────────────────────────────────────────

static class ConsoleInput
{
    public static string ReadMasked(string prompt)
    {
        Console.Write(prompt);
        var sb = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);  // 画面に表示しない

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
                Console.Write("\b \b");
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                sb.Append(key.KeyChar);
                Console.Write('*');
            }
        }

        return sb.ToString();
    }
}

// ─────────────────────────────────────────────────────────────
// メイン処理
// ─────────────────────────────────────────────────────────────

string userId;
string password;

if (CredentialStore.Exists())
{
    // 2回目以降: 保存済みの認証情報を読み込む
    Console.WriteLine("保存済みの認証情報を読み込みます...");

    try
    {
        (userId, password) = CredentialStore.Load();
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return;
    }
}
else
{
    // 初回: コンソールで入力して保存する
    Console.WriteLine("認証情報が見つかりません。初回入力を行います。");
    Console.Write("ユーザーID: ");
    userId = Console.ReadLine() ?? string.Empty;
    password = ConsoleInput.ReadMasked("パスワード: ");

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
    {
        Console.Error.WriteLine("ユーザーIDまたはパスワードが入力されていません。");
        return;
    }

    CredentialStore.Save(userId, password);
    Console.WriteLine("認証情報を暗号化して保存しました。");
}

// ─────────────────────────────────────────────────────────────
// ここから先で userId / password を使って業務処理を実行する
// 値をコンソールやログに出力しないこと
// ─────────────────────────────────────────────────────────────

Console.WriteLine($"ユーザーID [{userId}] で処理を開始します。");
// 例: await browser.LoginAsync(userId, password);
