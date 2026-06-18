// =============================================================================
// 方法5: Azure Arc + Managed Identity + Azure Key Vault
//
// 対象環境  : Azure Arc 対応サーバーとして登録したオンプレミスサーバー
// 認証情報  : Azure Key Vault に保存したシークレット
//
// 仕組み:
//   - オンプレミスサーバーを Azure Arc 対応サーバーとして登録する
//   - Azure Arc の Managed Identity に Key Vault のシークレット読み取り権限を付与する
//   - C# から ManagedIdentityCredential + SecretClient でシークレットを取得する
//
// 必要な NuGet パッケージ（動作確認用の固定例）:
//   - Azure.Identity 1.13.2
//   - Azure.Security.KeyVault.Secrets 4.7.0
//
// 参考URL:
//   https://learn.microsoft.com/ja-jp/azure/azure-arc/servers/managed-identity-authentication
//
// 実行前に環境変数を設定する:
//   setx KEY_VAULT_URI "https://your-vault-name.vault.azure.net/"
//   setx KEY_VAULT_SECRET_NAME "YourSecretName"
//
// 注意:
//   - このサンプルはシークレット値を表示しない
//   - 取得した値は業務処理へ渡し、ログには出力しない
// =============================================================================

using System;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var keyVaultUri = RequiredEnvironmentVariable("KEY_VAULT_URI");
var secretName = RequiredEnvironmentVariable("KEY_VAULT_SECRET_NAME");

var credential = new ManagedIdentityCredential();
var client = new SecretClient(new Uri(keyVaultUri), credential);

try
{
    KeyVaultSecret secret = await client.GetSecretAsync(secretName);

    Console.WriteLine($"Key Vault からシークレット [{secret.Name}] を取得しました。");

    // ここから先で secret.Value を使って業務処理を実行する。
    // 値をコンソールやログに出力しないこと。
    await UseSecretAsync(secret.Value);
}
catch (AuthenticationFailedException ex)
{
    Console.Error.WriteLine($"Managed Identity 認証に失敗しました: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (RequestFailedException ex)
{
    Console.Error.WriteLine($"Key Vault からの取得に失敗しました: {ex.Status} {ex.ErrorCode} {ex.Message}");
    Environment.ExitCode = 1;
}

static string RequiredEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);

    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"環境変数 {name} を設定してください。");

    return value;
}

static Task UseSecretAsync(string secretValue)
{
    if (string.IsNullOrWhiteSpace(secretValue))
        throw new InvalidOperationException("取得したシークレットが空です。");

    // 例:
    //   await apiClient.CallAsync(secretValue);
    return Task.CompletedTask;
}

