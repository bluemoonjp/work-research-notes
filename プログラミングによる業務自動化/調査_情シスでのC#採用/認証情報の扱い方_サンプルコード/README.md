# 認証情報の扱い方 — サンプルコード

[04_認証情報の扱い方.md](../04_認証情報の扱い方.md) に対応する C# サンプルコード集。

## ファイル一覧

| ファイル | 対応方法 | 対象環境 |
|---|---|---|
| [01_DPAPI_個人PC_CSHARP.cs](01_DPAPI_個人PC_CSHARP.cs) | 方法1: Windows DPAPI (CurrentUser) | 社員PC — 個別ID・パスワード |
| [02_Windowsシークレット管理_CSHARP.cs](02_Windowsシークレット管理_CSHARP.cs) | 方法2: Windows資格情報マネージャー | 社員PC — IT管理の共有認証情報 |
| [03_環境変数_サーバー_CSHARP.cs](03_環境変数_サーバー_CSHARP.cs) | 方法3: 環境変数 | サーバー — シンプル単一台構成 |
| [04_暗号化設定ファイル_サーバー_CSHARP.cs](04_暗号化設定ファイル_サーバー_CSHARP.cs) | 方法4: DPAPI (LocalMachine) + 暗号化ファイル | サーバー — 環境変数が使えない場合 |
| [05_AzureArc_KeyVault_CSHARP.cs](05_AzureArc_KeyVault_CSHARP.cs) | 方法5: Managed Identity + Azure Key Vault | Azure Arc 登録済みサーバー / Azure VM |
| [06_HashiCorpVault_CSHARP.cs](06_HashiCorpVault_CSHARP.cs) | 方法6: HashiCorp Vault | オンプレ — 複数台・集中管理 |

## 使い方

各ファイルのヘッダーコメントに以下が記載されています:

- 前提条件と対象環境
- 必要な NuGet パッケージ（あれば）
- 環境変数の設定方法（あれば）
- 実行手順

方法の選択基準は [04_認証情報の扱い方.md](../04_認証情報の扱い方.md) の「方法の選び方」表を参照してください。
