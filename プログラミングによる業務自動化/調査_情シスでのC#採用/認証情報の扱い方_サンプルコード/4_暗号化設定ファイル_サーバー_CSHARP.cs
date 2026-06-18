// =============================================================================
// 方法4: Windows DPAPI (LocalMachine) + 暗号化設定ファイル
//
// 対象環境  : サーバーで一括実行する C# プログラム（Windows 専用）
// 認証情報  : IT 管理者が管理する共有ID・アクセストークン
//
// 仕組み:
//   - IT 管理者が初回のみ --setup モードで認証情報を暗号化してファイルに保存する
//   - 以降の実行時は暗号化ファイルを自動で復号して使う
//   - DPAPI LocalMachine スコープで暗号化するため、同一サーバー上では
//     サービスアカウントを含むどのプロセスも復号できる
//
// ！重要な制限（必ず確認）！
//   - 暗号化ファイルを別のサーバーにコピーしても復号できない（マシン間で移植不可）
//   - 同一マシン上の任意のプロセスが復号可能（管理者権限の別プロセス含む）
//   - 複数台展開・コンテナ・サーバー再構築が多い環境には向かない
//   → これらの制約がある場合は方法3（環境変数）または Azure Key Vault を検討する
//
// 使い方:
//   初回セットアップ（管理者が1回だけ実行）:
//     dotnet run -- --setup
//   通常実行:
//     dotnet run
//
// セットアップ後に必ず実施するACL設定（管理者コマンドプロンプト）:
//   icacls "C:\ProgramData\MyCompanyApp\credentials.dat" /inheritance:r
//     /grant "NT AUTHORITY\SYSTEM:(R)" /grant "BUILTIN\Administrators:(R)"
//   専用サービスアカウントで実行する場合はそのアカウントの読み取り権限も追加する:
//     icacls "C:\ProgramData\MyCompanyApp\credentials.dat" /grant "DOMAIN\svc-myapp:(R)"
//
// 制限: Windows 専用（ProtectedData は Windows のみ対応）
//
// 必要な NuGet パッケージ:
//   - System.Security.Cryptography.ProtectedData
// =============================================================================

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// ─────────────────────────────────────────────────────────────
// 認証情報の保存・読み込み (LocalMachine スコープ)
// ─────────────────────────────────────────────────────────────

static class ServerCredentialStore
{
    // ProgramData はマシン全体で共有されるアプリデータの推奨保存先
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "MyCompanyApp",
        "credentials.dat"
    );

    // アプリ固有のエントロピー（アプリを区別するための補助値。秘密鍵ではない。保護の主体は DPAPI と ACL）
    static readonly byte[] Entropy = { 0x53, 0x72, 0x76, 0x4B, 0x65, 0x79, 0x30, 0x31 };

    public static bool Exists() => File.Exists(FilePath);

    /// <summary>
    /// 認証情報を LocalMachine スコープで暗号化して保存する。
    /// 管理者による初回セットアップ時のみ呼び出す。
    /// </summary>
    public static void Save(string userId, string token)
    {
        var plain = Encoding.UTF8.GetBytes($"{userId}\n{token}");
        var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.LocalMachine);

        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(FilePath, encrypted);
        Array.Clear(plain, 0, plain.Length);

        Console.WriteLine($"認証情報を暗号化して保存しました: {FilePath}");
        Console.WriteLine();
        Console.WriteLine("次のコマンドでファイルのアクセス権を制限してください（管理者コマンドプロンプト）:");
        Console.WriteLine($@"  icacls ""{FilePath}"" /inheritance:r /grant ""NT AUTHORITY\SYSTEM:(R)"" /grant ""BUILTIN\Administrators:(R)""");
        Console.WriteLine("専用サービスアカウントで実行する場合は、そのアカウントの読み取り権限も追加してください:");
        Console.WriteLine($@"  icacls ""{FilePath}"" /grant ""DOMAIN\\サービスアカウント名:(R)""");
    }

    /// <summary>
    /// 保存済みの認証情報を復号して返す。
    /// </summary>
    public static (string UserId, string Token) Load()
    {
        if (!File.Exists(FilePath))
            throw new FileNotFoundException(
                $"認証情報ファイルが見つかりません: {FilePath}\n" +
                "管理者が --setup モードで初期設定を行う必要があります。"
            );

        var encrypted = File.ReadAllBytes(FilePath);
        byte[] plain;

        try
        {
            plain = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.LocalMachine);
        }
        catch (CryptographicException)
        {
            // サーバーの再構築や OS 再インストール後に発生する
            throw new InvalidOperationException(
                $"認証情報の復号に失敗しました。{FilePath} を削除し、" +
                "管理者が --setup モードで再設定してください。"
            );
        }

        var text = Encoding.UTF8.GetString(plain);
        Array.Clear(plain, 0, plain.Length);

        var parts = text.Split('\n', 2);
        if (parts.Length < 2)
            throw new InvalidOperationException(
                "認証情報ファイルの形式が不正です。削除して再設定してください。"
            );

        return (parts[0], parts[1]);
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
            var key = Console.ReadKey(intercept: true);

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

bool isSetup = args.Length > 0 && args[0] == "--setup";

if (isSetup)
{
    // ── セットアップモード（管理者が初回のみ実行）──
    Console.WriteLine("=== セットアップモード ===");
    Console.WriteLine("認証情報を入力してください（入力内容は画面に表示されません）。");
    Console.WriteLine();

    Console.Write("ユーザーID: ");
    var userId = Console.ReadLine() ?? string.Empty;
    var token = ConsoleInput.ReadMasked("アクセストークン: ");

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
    {
        Console.Error.WriteLine("ユーザーIDまたはアクセストークンが入力されていません。");
        return;
    }

    try
    {
        ServerCredentialStore.Save(userId, token);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"保存に失敗しました: {ex.Message}");
    }

    return;
}

// ── 通常実行モード ──
string runUserId;
string apiToken;

try
{
    (runUserId, apiToken) = ServerCredentialStore.Load();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
    return;
}

// ─────────────────────────────────────────────────────────────
// ここから先で runUserId / apiToken を使って業務処理を実行する
// 値をコンソールやログに出力しないこと
// ─────────────────────────────────────────────────────────────

Console.WriteLine($"ユーザーID [{runUserId}] で処理を開始します。");
// 例: await apiClient.CallAsync(runUserId, apiToken);
