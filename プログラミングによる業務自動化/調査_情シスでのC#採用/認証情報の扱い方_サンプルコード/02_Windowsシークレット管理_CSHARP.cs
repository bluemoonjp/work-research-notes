// =============================================================================
// 方法2: Windows 資格情報マネージャーによる認証情報の管理
//
// 対象環境  : 社員PC に配布する C# プログラム（Windows 専用）
// 認証情報  : IT 管理者が事前に配布する共有ID・アクセストークン
//
// 仕組み:
//   - IT 管理者が cmdkey コマンド（ログオンスクリプト等）で各社員 PC の
//     Windows 資格情報マネージャーに認証情報を登録する
//   - プログラムは CredRead (Win32 API) で認証情報を読み取る
//   - NuGet パッケージ不要（Win32 API を P/Invoke で直接呼び出す）
//
// 重要: 資格情報マネージャーは「ユーザー単位のストア」
//       全ユーザーで共有するストアではない。各 PC の各ユーザーに個別に登録が必要
//
// IT 管理者による登録コマンド（ログオンスクリプトや GPO で実行）:
//   cmdkey /add:MyCompanyApp:ApiToken /user:service-account /pass:アクセストークン
//
// 注意:
//   - コントロールパネル「資格情報マネージャー」から中身が見えるため、
//     高権限の認証情報（管理者パスワード等）には使わない
//   - 1件あたりの保存サイズ上限は 2560 バイト
//     長いアクセストークンや JSON は超過する場合がある
//
// 制限: Windows 専用（advapi32.dll を使用）
// =============================================================================

using System;
using System.Runtime.InteropServices;
using System.Text;

// ─────────────────────────────────────────────────────────────
// Windows 資格情報マネージャー P/Invoke 定義
// ─────────────────────────────────────────────────────────────

static class WindowsCredentialManager
{
    const int CRED_TYPE_GENERIC = 1;
    const int CRED_PERSIST_LOCAL_MACHINE = 2;
    const int ERROR_NOT_FOUND = 1168;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        [MarshalAs(UnmanagedType.LPWStr)] public string TargetName;
        [MarshalAs(UnmanagedType.LPWStr)] public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)] public string TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)] public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode,
               CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CredReadW(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode,
               CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CredWriteW(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true,
               CallingConvention = CallingConvention.Winapi)]
    static extern void CredFree(IntPtr buffer);

    /// <summary>
    /// 資格情報マネージャーから認証情報を読み取る。
    /// 見つからない場合は null を返す。
    /// </summary>
    public static (string UserName, string Password)? Read(string targetName)
    {
        if (!CredReadW(targetName, CRED_TYPE_GENERIC, 0, out var credPtr))
        {
            int error = Marshal.GetLastWin32Error();
            if (error == ERROR_NOT_FOUND)
                return null;

            throw new InvalidOperationException(
                $"CredRead が失敗しました (Win32 エラー: {error})。" +
                "認証情報マネージャーへのアクセス権限を確認してください。"
            );
        }

        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
            var password = cred.CredentialBlobSize > 0
                ? Encoding.Unicode.GetString(
                    ReadBytes(cred.CredentialBlob, (int)cred.CredentialBlobSize))
                : string.Empty;

            return (cred.UserName ?? string.Empty, password);
        }
        finally
        {
            CredFree(credPtr);  // アンマネージドメモリを必ず解放する
        }
    }

    static byte[] ReadBytes(IntPtr ptr, int length)
    {
        var bytes = new byte[length];
        Marshal.Copy(ptr, bytes, 0, length);
        return bytes;
    }

    /// <summary>
    /// 資格情報マネージャーに認証情報を保存する。
    /// プログラムから登録する場合に使用（IT 管理者の配布には cmdkey コマンドを推奨）。
    /// </summary>
    public static void Write(string targetName, string userName, string password)
    {
        var blob = Encoding.Unicode.GetBytes(password);

        // 2560 バイト制限を確認する
        if (blob.Length > 2560)
            throw new ArgumentException(
                $"パスワード/トークンが資格情報マネージャーの上限 (2560 バイト) を超えています。" +
                $"現在のサイズ: {blob.Length} バイト"
            );

        var handle = GCHandle.Alloc(blob, GCHandleType.Pinned);
        try
        {
            var cred = new CREDENTIAL
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = targetName,
                UserName = userName,
                CredentialBlob = handle.AddrOfPinnedObject(),
                CredentialBlobSize = (uint)blob.Length,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
            };

            if (!CredWriteW(ref cred, 0))
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    $"CredWrite が失敗しました (Win32 エラー: {error})。"
                );
            }
        }
        finally
        {
            handle.Free();
            Array.Clear(blob, 0, blob.Length);
        }
    }
}

// ─────────────────────────────────────────────────────────────
// メイン処理
// ─────────────────────────────────────────────────────────────

// IT 管理者が登録した際のターゲット名と一致させる
// 命名例: "会社名またはアプリ名:用途"
const string TargetName = "MyCompanyApp:ApiToken";

var credential = WindowsCredentialManager.Read(TargetName);

if (credential is null)
{
    Console.Error.WriteLine(
        $"認証情報が見つかりません (ターゲット: {TargetName})。"
    );
    Console.Error.WriteLine(
        "IT 管理者に次のコマンドの実行を依頼してください:"
    );
    Console.Error.WriteLine(
        $"  cmdkey /add:{TargetName} /user:ユーザーID /pass:アクセストークン"
    );
    return;
}

var (userId, apiToken) = credential.Value;

// ─────────────────────────────────────────────────────────────
// ここから先で userId / apiToken を使って業務処理を実行する
// 値をコンソールやログに出力しないこと
// ─────────────────────────────────────────────────────────────

Console.WriteLine($"ユーザーID [{userId}] で処理を開始します。");
// 例: await apiClient.CallAsync(userId, apiToken);
