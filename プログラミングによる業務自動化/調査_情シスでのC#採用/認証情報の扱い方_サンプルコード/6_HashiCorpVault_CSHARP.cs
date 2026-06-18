// =============================================================================
// 方法6: HashiCorp Vault + AppRole認証 / LDAP認証（AD統合）
//
// 対象環境  : オンプレミスサーバー、社内ネットワーク上の業務アプリケーション
// 認証情報  : HashiCorp Vault KV v2 に保存したシークレット
//
// 仕組み:
//   - AppRole認証: アプリケーション単位で role_id + secret_id を使って Vault に認証する
//   - LDAP認証: Active Directory と連携した LDAP 認証で Vault に認証する
//   - 認証後、KV v2 から指定パスのシークレットを取得する
//
// 必要な NuGet パッケージ（動作確認用の固定例）:
//   - VaultSharp 1.17.5.1
//
// 参考URL:
//   https://developer.hashicorp.com/vault/docs/auth/approle
//   https://developer.hashicorp.com/vault/docs/auth/ldap
//
// 実行前に共通の環境変数を設定する:
//   setx VAULT_ADDR "https://vault.example.local:8200"
//   setx VAULT_KV_MOUNT "secret"
//   setx VAULT_SECRET_PATH "apps/myapp"
//   setx VAULT_SECRET_KEY "ApiToken"
//
// AppRole認証で実行する場合:
//   setx VAULT_AUTH_METHOD "approle"
//   setx VAULT_ROLE_ID "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
//   setx VAULT_SECRET_ID "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy"
//
// LDAP認証で実行する場合:
//   setx VAULT_AUTH_METHOD "ldap"
//   setx VAULT_LDAP_USERNAME "domain-user-or-service-account"
//   setx VAULT_LDAP_PASSWORD "password"
//
// 注意:
//   - このサンプルはシークレット値を表示しない
//   - role_id / secret_id / LDAPパスワード自体もログに出力しない
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.LDAP;

var vaultAddress = RequiredEnvironmentVariable("VAULT_ADDR");
var authMethodName = RequiredEnvironmentVariable("VAULT_AUTH_METHOD");
var kvMountPoint = OptionalEnvironmentVariable("VAULT_KV_MOUNT", "secret");
var secretPath = RequiredEnvironmentVariable("VAULT_SECRET_PATH");
var secretKey = RequiredEnvironmentVariable("VAULT_SECRET_KEY");

IAuthMethodInfo authMethod = CreateAuthMethod(authMethodName);
var settings = new VaultClientSettings(vaultAddress, authMethod);
IVaultClient vaultClient = new VaultClient(settings);

try
{
    var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync<Dictionary<string, object>>(
        path: secretPath,
        mountPoint: kvMountPoint
    );

    if (!secret.Data.TryGetValue(secretKey, out var secretValue) || secretValue is null)
        throw new InvalidOperationException($"Vault の KV v2 パス [{kvMountPoint}/{secretPath}] にキー [{secretKey}] がありません。");

    var secretText = Convert.ToString(secretValue) ?? string.Empty;
    if (string.IsNullOrWhiteSpace(secretText))
        throw new InvalidOperationException($"Vault のキー [{secretKey}] の値が空です。");

    Console.WriteLine($"Vault の KV v2 パス [{kvMountPoint}/{secretPath}] からキー [{secretKey}] を取得しました。");

    // ここから先で secretText を使って業務処理を実行する。
    // 値をコンソールやログに出力しないこと。
    await UseSecretAsync(secretText);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Vault からの取得に失敗しました: {ex.Message}");
    Environment.ExitCode = 1;
}

static IAuthMethodInfo CreateAuthMethod(string authMethodName)
{
    if (authMethodName.Equals("approle", StringComparison.OrdinalIgnoreCase))
    {
        var roleId = RequiredEnvironmentVariable("VAULT_ROLE_ID");
        var secretId = RequiredEnvironmentVariable("VAULT_SECRET_ID");
        return new AppRoleAuthMethodInfo(roleId, secretId);
    }

    if (authMethodName.Equals("ldap", StringComparison.OrdinalIgnoreCase))
    {
        var username = RequiredEnvironmentVariable("VAULT_LDAP_USERNAME");
        var password = RequiredEnvironmentVariable("VAULT_LDAP_PASSWORD");
        return new LDAPAuthMethodInfo(username, password);
    }

    throw new InvalidOperationException("VAULT_AUTH_METHOD は approle または ldap を指定してください。");
}

static string RequiredEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);

    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"環境変数 {name} を設定してください。");

    return value;
}

static string OptionalEnvironmentVariable(string name, string defaultValue)
{
    var value = Environment.GetEnvironmentVariable(name);
    return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}

static Task UseSecretAsync(string secretValue)
{
    if (string.IsNullOrWhiteSpace(secretValue))
        throw new InvalidOperationException("取得したシークレットが空です。");

    // 例:
    //   await apiClient.CallAsync(secretValue);
    return Task.CompletedTask;
}

