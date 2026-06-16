# SAML認証強制環境でのサイボウズ製品 API アクセス

## 背景・課題

cybozu.com では「ログイン時に SAML 認証のみを使うように制限する」設定が可能。  
この設定を有効にすると、通常ユーザーはパスワード認証によるログインが禁止されるため、  
**パスワード認証を使う REST API の実行も通常ユーザーには不可能**になる。

## SAML強制時の各認証方式への影響

### kintone

| 認証方式 | SAML強制時の影響 |
|----------|----------------|
| パスワード認証（X-Cybozu-Authorization） | cybozu.com 共通管理者のみ使用可能。一般ユーザーは不可 |
| APIトークン認証 | 影響なし（引き続き使用可能） |
| セッション認証 | 影響なし（kintone 内 JS カスタマイズから引き続き使用可能） |
| OAuth 2.0 認証 | 影響なし（引き続き使用可能） |

### ガルーン（クラウド版）

| 認証方式 | SAML強制時の影響 |
|----------|----------------|
| パスワード認証（X-Cybozu-Authorization） | cybozu.com 共通管理者のみ使用可能。一般ユーザーは不可 |
| セッション認証 | 影響なし（Garoon 内 JS カスタマイズから引き続き使用可能） |
| OAuth 2.0 認証 | 影響なし（クラウド版のみ対応） |

### ガルーン（パッケージ版）

パッケージ版は cybozu.com の SAML 設定に依存しない独自の認証システムを持つ。  
OAuth 2.0 は非対応のため、パスワード認証またはセッション認証のみとなる。

## cybozu.com 共通管理者によるパスワード認証

SAML強制環境でも **cybozu.com 共通管理者は例外的にパスワード認証が可能**。

- 専用 URL `https://{サブドメイン}.cybozu.com/login?saml=off` からパスワードでログイン可能
- この例外は無効にできない（SAML 設定失敗時の管理者アクセス確保のため）
- REST API でも同様に、パスワード認証を使う API コールが共通管理者アカウントで実行可能

**セキュリティ上の課題:**  
共通管理者アカウントはシステム全体に対する強力な権限を持つ。  
このアカウントの認証情報を API 実行スクリプトに含めることは権限過剰となりやすく、  
情報漏洩リスクも高い。本番環境での使用は最小限にとどめることが推奨される。

## SAML強制環境での推奨認証方式

### kintone の場合

**APIトークン認証が最も推奨**。

- SAML 制限を受けない
- パスワードを管理する必要がない
- アプリ単位で権限を絞れるため最小権限の原則に準拠しやすい
- 複数アプリをまたぐ場合は複数トークンをカンマ区切りで指定可能

OAuth 2.0 もユーザー委任型の連携（ユーザーが同意してアクセス許可する形式）に適している。

### ガルーン（クラウド版）の場合

**OAuth 2.0 認証が推奨**。

- SAML 制限を受けない
- Authorization Code Grant フローでユーザーの同意を経てアクセストークンを取得
- スコープを細かく設定することで最小権限を実現できる
- アクセストークンには有効期限があり、定期的な再取得が必要（リフレッシュトークンで自動更新可能）

OAuth クライアントは cybozu.com 管理画面（共通管理 > OAuth）で登録する。

### ガルーン（パッケージ版）の場合

OAuth 2.0 が使用不可のため、以下のいずれかを選択する。

1. **共通管理者アカウントのパスワード認証**（権限過剰に注意）
2. **専用の API 実行ユーザーを作成**してパスワード認証（SAML強制の対象外にする必要あり）
3. **SAML 強制の適用ユーザーを絞る**（cybozu.com 管理画面で設定可能）

## OAuth クライアントの設定手順（kintone / Garoon クラウド版共通）

1. cybozu.com 管理画面 > 共通管理 > セキュリティ > OAuth を開く
2. 「OAuthクライアントの追加」をクリック
3. クライアント名・ロゴ・リダイレクトエンドポイントを設定して保存
4. 利用を許可するユーザーを設定
5. 発行されたクライアント ID・クライアントシークレットをアプリ側に設定

**OAuth フロー（Authorization Code Grant）の概要:**

```
1. アプリが認可エンドポイントへリダイレクト
   https://{サブドメイン}.cybozu.com/oauth2/authorization?...

2. ユーザーが cybozu.com でログインして同意

3. 認可コードがリダイレクト URI に返る

4. アプリがトークンエンドポイントで認可コードをアクセストークンに交換
   POST https://{サブドメイン}.cybozu.com/oauth2/token

5. アクセストークンを使って API を呼び出す
   Authorization: Bearer {アクセストークン}
```

## セキュリティ上の考慮点まとめ

| 考慮点 | 対策 |
|--------|------|
| 共通管理者アカウントの権限過剰 | 可能な限り APIトークン・OAuth を使用し、共通管理者認証情報は API スクリプトに組み込まない |
| APIトークンの漏洩 | 環境変数・シークレット管理サービスで管理し、コードに直書きしない |
| OAuth クライアントシークレットの管理 | 同上。不要になったクライアントは速やかに削除する |
| 不要な権限の付与 | kintone APIトークン・OAuth スコープは必要最小限に絞る |
| 2要素認証ユーザーの制約 | パスワード認証が使えないため、APIトークンまたは OAuth を使用する |

## 公式ドキュメント

- [ログイン時にSAML認証だけを使うように制限する | cybozu.com ヘルプ](https://jp.kintone.help/general/ja/admin/list_saml/saml_restriction.html)
- [認証（kintone）](https://cybozu.dev/ja/kintone/docs/rest-api/overview/authentication/)
- [認証（Garoon）](https://cybozu.dev/ja/garoon/docs/rest-api/overview/authentication/)
- [OAuthクライアントを追加する](https://cybozu.dev/ja/common/docs/oauth-client/add-client/)
- [OAuth 2.0 を使って cybozu.com の REST API を Postman で叩く方法](https://cybozu.dev/ja/common/tips/authentication/oauth2-restapi-postman/)
- [Google Apps Script から OAuth 2.0 で kintone API を利用する](https://cybozu.dev/ja/common/tips/authentication/utilizing-kintone-api-from-google-apps-script-with-oauth2.0/)
