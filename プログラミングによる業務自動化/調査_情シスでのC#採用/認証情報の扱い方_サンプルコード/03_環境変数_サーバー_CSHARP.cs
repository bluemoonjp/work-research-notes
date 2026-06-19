// =============================================================================
// 方法3: 環境変数による認証情報の管理
//
// 対象環境  : サーバーで一括実行する C# プログラム
// 認証情報  : IT 管理者が管理する共有ID・アクセストークン
//
// 仕組み:
//   - 認証情報をOSの環境変数として設定し、プログラムは実行時に読み取る
//   - プログラムのコードや設定ファイルに認証情報を持たない
//
// 推奨する環境変数の注入方法（優先度順）:
//   1. CI/CD のシークレット機能（GitHub Actions Secrets、Azure Pipeline 変数等）
//      実行ごとにプロセス環境に注入される。OS の環境変数に永続しない
//   2. タスクスケジューラからラッパースクリプト（.cmd/.ps1）を起動してプロセス環境に注入する（バッチ実行向け）
//   3. サービスマネージャー（sc.exe や NSSM）の環境変数設定
//   4. OS の Machine 環境変数（最終手段。平文でレジストリに保存される）
//
// 注意:
//   - 環境変数は平文保存。コード内に書くよりは安全だが、
//     OS の Machine 環境変数に直接書き込むのは最終手段として考える
//   - 環境変数の変更後はサービスやタスクスケジューラの再起動が必要
//   - Windows・Linux・Docker いずれでも同じコードが動く
// =============================================================================

using System;

// ─────────────────────────────────────────────────────────────
// 環境変数の読み取りと検証
// ─────────────────────────────────────────────────────────────

static class AppConfig
{
    // 環境変数名の定数（IT 管理者と命名を合わせる）
    public const string EnvUserId    = "MYAPP_USER_ID";
    public const string EnvApiToken  = "MYAPP_API_TOKEN";

    /// <summary>
    /// 必須の環境変数をすべて読み取る。
    /// 未設定の変数がある場合は変数名だけを表示してエラー終了する（値は表示しない）。
    /// </summary>
    public static (string UserId, string ApiToken) LoadRequired()
    {
        var userId   = Environment.GetEnvironmentVariable(EnvUserId);
        var apiToken = Environment.GetEnvironmentVariable(EnvApiToken);

        // 未設定の変数名を収集する（値はログに出さない）
        var missing = new System.Collections.Generic.List<string>();
        if (string.IsNullOrEmpty(userId))   missing.Add(EnvUserId);
        if (string.IsNullOrEmpty(apiToken)) missing.Add(EnvApiToken);

        if (missing.Count > 0)
        {
            Console.Error.WriteLine("以下の環境変数が設定されていません:");
            foreach (var name in missing)
                Console.Error.WriteLine($"  {name}");

            Console.Error.WriteLine();
            Console.Error.WriteLine("推奨: タスクスケジューラや NSSM のラッパースクリプトから実行時にプロセス環境変数として注入してください（OS に永続しない）。");
            Console.Error.WriteLine("最終手段 — Machine 環境変数への保存（管理者 PowerShell）:");
            Console.Error.WriteLine($"  [System.Environment]::SetEnvironmentVariable(\"{EnvUserId}\", \"値\", \"Machine\")");
            Console.Error.WriteLine($"  [System.Environment]::SetEnvironmentVariable(\"{EnvApiToken}\", \"値\", \"Machine\")");
            Console.Error.WriteLine("設定後はサービスまたはタスクスケジューラを再起動してください。");

            Environment.Exit(1);
        }

        return (userId!, apiToken!);
    }
}

// ─────────────────────────────────────────────────────────────
// メイン処理
// ─────────────────────────────────────────────────────────────

var (userId, apiToken) = AppConfig.LoadRequired();

// ─────────────────────────────────────────────────────────────
// ここから先で userId / apiToken を使って業務処理を実行する
// 値をコンソールやログに出力しないこと
// ─────────────────────────────────────────────────────────────

Console.WriteLine($"ユーザーID [{userId}] で処理を開始します。");
// 例: await apiClient.CallAsync(userId, apiToken);
