# オンプレAD環境での認証情報管理

## 結論

オンプレミスActive Directory環境で「プログラムから外部サービスのID・パスワード・APIキーを安全に扱う」場合、gMSAだけでは解決しない。gMSAはWindowsサービスやタスクをドメインアカウントとして実行するためのWindowsアカウント管理機能であり、外部SaaSやWeb APIのシークレットを保管・取得するシークレット管理基盤ではない。

Azure Managed Identityも、基本的にはAzureリソースに割り当てるIDである。オンプレミスサーバーでManaged Identityを使ってAzure Key Vaultにアクセスしたい場合は、対象サーバーをAzure Arc対応サーバーとして登録し、Azure ArcのManaged Identityを使う必要がある。

完全オンプレミスで完結させる場合の現実的な選択肢は、次の3つ。

- DPAPI LocalMachine
- HashiCorp Vault
- 証明書認証

Azure Arcを利用できる場合は、C#では `ManagedIdentityCredential` と `SecretClient` を使ってAzure Key Vaultからシークレットを取得できる。

## gMSAでできること・できないこと

gMSA（Group Managed Service Account）は、Windows Serverのサービスやタスクスケジューラをドメイン管理のサービスアカウントで動かすための仕組みである。パスワードはActive Directory側で管理され、Windowsが自動的にローテーションする。

向いている用途:

- Windowsサービスをドメインアカウントとして実行する
- SQL Serverやファイル共有など、Windows統合認証を使う社内リソースへアクセスする
- サービスアカウントのパスワードを人が直接管理しないようにする

向いていない用途:

- 外部SaaSのAPIキーを保管する
- Web APIのBearer Tokenを保管する
- Basic認証のID・パスワードをアプリケーションへ安全に配布する
- アプリケーションから任意のシークレットを取得する

つまり、gMSAは「Windows上でどのアカウントとして実行するか」の管理には有効だが、「外部サービス用のシークレットを保存・取得・監査・ローテーションする」用途には別の仕組みが必要になる。

## Managed Identityとオンプレミス

Managed Identityは、アプリケーションコードに資格情報を持たせず、Azure AD（Microsoft Entra ID）で管理されるIDを使ってAzureリソースへアクセスする仕組みである。通常はAzure VM、App Service、FunctionsなどのAzureリソースに割り当てて使う。

オンプレミスサーバーはそのままではAzureリソースではないため、Managed Identityを直接利用できない。オンプレミスサーバーからManaged IdentityでAzure Key Vaultへアクセスするには、Azure Arc対応サーバーとして登録し、Azure ArcのManaged Identityを使う。

Azure Arc利用時のC#実装は、Azure SDKの標準的な組み合わせでよい。

- 認証: `Azure.Identity.ManagedIdentityCredential`
- Key Vaultアクセス: `Azure.Security.KeyVault.Secrets.SecretClient`

サンプル: [5_AzureArc_KeyVault_CSHARP.cs](5_AzureArc_KeyVault_CSHARP.cs)

## 完全オンプレミスでの選択肢

### 1. DPAPI LocalMachine

Windows DPAPIの `DataProtectionScope.LocalMachine` を使い、同一マシンでのみ復号できる形でシークレットを暗号化して保存する方法。

メリット:

- Windows標準機能で実装できる
- 外部サービスや追加サーバーが不要
- 小規模な単一サーバー用途では導入が軽い

注意点:

- 同一マシン上の権限を持つプロセスから復号され得る
- サーバーを再構築すると復号できなくなる
- 複数台展開、コンテナ、頻繁なリプレースには向かない
- 監査、集中管理、ローテーションの仕組みは別途必要

既存サンプル: [4_暗号化設定ファイル_サーバー_CSHARP.cs](4_暗号化設定ファイル_サーバー_CSHARP.cs)

### 2. HashiCorp Vault

HashiCorp Vaultを社内に構築し、アプリケーションがVaultへ認証してKV v2などからシークレットを取得する方法。

オンプレAD環境では、次の認証方式が候補になる。

- AppRole認証: サーバーやアプリケーション単位で `role_id` と `secret_id` を使って認証する
- LDAP認証: Active Directoryと連携し、ユーザーまたはサービス用のAD資格情報で認証する

メリット:

- シークレットを中央管理できる
- 監査ログを取りやすい
- ローテーションやアクセス制御を設計しやすい
- Azureに依存せずオンプレミスで完結できる

注意点:

- Vaultサーバーの構築、初期化、unseal、バックアップ、監視が必要
- AppRoleの `secret_id` 自体をどう配布・保護するかを設計する必要がある
- LDAP認証を使う場合も、アプリケーションにADパスワードを持たせる設計は慎重に扱う

サンプル: [6_HashiCorpVault_CSHARP.cs](6_HashiCorpVault_CSHARP.cs)

### 3. 証明書認証

ID・パスワードやAPIキーではなく、クライアント証明書で認証する方式。社内PKI、Windows証明書ストア、AD CSなどと組み合わせる。

メリット:

- パスワードをアプリケーションに持たせない設計にしやすい
- 証明書ストアと秘密鍵ACLでアクセス制御できる
- mTLSやSAML/OIDCのクライアント認証に使える

注意点:

- 接続先サービスが証明書認証に対応している必要がある
- 証明書発行、更新、失効、配布の運用が必要
- 秘密鍵のエクスポート可否とACL設定を誤ると漏洩リスクが残る

## 選定目安

| 条件 | 推奨 |
|---|---|
| 単一Windowsサーバーで小規模に完結したい | DPAPI LocalMachine |
| 複数サーバーでシークレットを集中管理したい | HashiCorp Vault |
| AD/LDAPと連携したオンプレ基盤を作りたい | HashiCorp Vault + LDAP認証 |
| アプリケーション単位で機械的にVaultへ認証したい | HashiCorp Vault + AppRole認証 |
| 接続先が証明書認証に対応している | 証明書認証 |
| Azure Key Vaultを使いたいが実行環境はオンプレ | Azure Arc + Managed Identity |
| Azure VMやApp Service上で実行する | Managed Identity + Azure Key Vault |
| Windows統合認証で社内リソースへアクセスする | gMSA |

## 実装上の注意

- gMSAを使っても、外部サービスのAPIキーやパスワードは別途管理が必要。
- Managed Identityをオンプレミスで使う場合は、Azure Arc対応サーバーとして登録する。
- DPAPI LocalMachineは、暗号化ファイルのACLを必ず絞る。
- VaultのAppRoleを使う場合、`secret_id` の配布経路と有効期限を設計する。
- VaultのLDAP認証を使う場合、ADパスワードをアプリケーションに固定保存する設計は避け、可能なら実行基盤側で注入する。
- ログ、例外、デバッグ出力にシークレット値を出さない。

## 出典

- Microsoft Learn: Managed identities for Azure resources  
  https://learn.microsoft.com/ja-jp/entra/identity/managed-identities-azure-resources/overview
- Microsoft Learn: Azure Arc対応サーバーでのマネージドID認証  
  https://learn.microsoft.com/ja-jp/azure/azure-arc/servers/managed-identity-authentication
- Microsoft Learn: Group Managed Service Accounts overview  
  https://learn.microsoft.com/ja-jp/windows-server/security/group-managed-service-accounts/group-managed-service-accounts-overview
- HashiCorp Vault Documentation  
  https://developer.hashicorp.com/vault/docs

