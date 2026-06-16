# サイボウズ kintone REST API アクセス

## 概要

kintone REST API は、kintone アプリのレコード操作・ファイル管理・アプリ設定変更などを外部から行える HTTP ベースの API。  
クラウド版 kintone 専用。

## エンドポイント形式

```
https://{サブドメイン}.cybozu.com/k/v1/{リソース}.json
```

**例:**
```
https://example.cybozu.com/k/v1/record.json
https://example.cybozu.com/k/v1/records.json
https://example.cybozu.com/k/v1/file.json
```

## 認証方法

### 1. APIトークン認証（推奨）

アプリごとに発行した API トークンをヘッダーに指定する方式。  
パスワードを扱わないためセキュアで、外部システム連携に適している。

```
X-Cybozu-API-Token: {APIトークン}
```

**複数トークンの指定（最大9個）:**

```
X-Cybozu-API-Token: {トークン1},{トークン2},{トークン3}
```

または複数のヘッダーを使用することも可能。

**注意:** APIトークンを使用した操作は、Administrator による操作として扱われる。

### 2. パスワード認証（X-Cybozu-Authorization）

ログイン名とパスワードを `ログイン名:パスワード` の形式で Base64 エンコードし、リクエストヘッダーに指定する。

```
X-Cybozu-Authorization: {Base64エンコードした「ログイン名:パスワード」}
```

**注意事項:**
- 2要素認証を有効にしているユーザーは使用不可
- SAML認証のみ使用に制限している環境では、cybozu.com 共通管理者のみ利用可能（後述）

### 3. セッション認証

kintone 内の JavaScript カスタマイズから `kintone.api()` メソッドを使用するか、`X-Requested-With: XMLHttpRequest` ヘッダーを付けて Fetch API を呼び出す方式。  
POST/PUT/DELETE メソッドでは CSRF トークンも付与が必要。

### 4. OAuth 2.0 認証

cybozu.com 管理画面で登録した OAuth クライアントを使用する方式。  
Authorization Code Grant フローでアクセストークンを取得してから API を呼び出す。  
ユーザーの ID/パスワードをアプリ側で保管する必要がなく、外部サービス連携に適している。

## curl でのテスト方法

### レコードを1件取得する（GET・パスワード認証）

```bash
curl -X GET "https://{サブドメイン}.cybozu.com/k/v1/record.json?app={アプリID}&id={レコードID}" \
  -H "X-Cybozu-Authorization: {Base64エンコード済み認証情報}"
```

### レコードを1件取得する（GET・APIトークン認証）

```bash
curl -X GET "https://{サブドメイン}.cybozu.com/k/v1/record.json?app={アプリID}&id={レコードID}" \
  -H "X-Cybozu-API-Token: {APIトークン}"
```

### レコードを登録する（POST・APIトークン認証）

```bash
curl -X POST "https://{サブドメイン}.cybozu.com/k/v1/record.json" \
  -H "X-Cybozu-API-Token: {APIトークン}" \
  -H "Content-Type: application/json" \
  -d "{\"app\": \"{アプリID}\", \"record\": {\"フィールドコード\": {\"value\": \"値\"}}}"
```

### IPアドレス制限環境での Basic 認証併用

```bash
curl -X GET "https://{サブドメイン}.cybozu.com/k/v1/record.json?app={アプリID}&id={レコードID}" \
  -H "X-Cybozu-Authorization: {Base64エンコード済み認証情報}" \
  -H "Authorization: Basic {Basic認証のBase64エンコード済み認証情報}"
```

**Base64 エンコードの例（PowerShell）:**

```powershell
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("ログイン名:パスワード"))
```

## 利用できる主な API

| カテゴリ | エンドポイント | 主な操作 |
|----------|---------------|----------|
| レコード | `/k/v1/record.json` | 1件取得・登録・更新・削除 |
| レコード（複数） | `/k/v1/records.json` | 複数取得・一括登録・一括更新・一括削除 |
| カーソル | `/k/v1/records/cursor.json` | 大量レコードのカーソル取得 |
| ファイル | `/k/v1/file.json` | ファイルアップロード・ダウンロード |
| アプリ情報 | `/k/v1/app.json` | アプリ情報取得 |
| フォーム | `/k/v1/app/form/fields.json` | フィールド情報取得 |
| ビュー | `/k/v1/app/views.json` | ビュー情報取得 |
| コメント | `/k/v1/record/comment.json` | コメント取得・投稿・削除 |
| アクセス権 | `/k/v1/app/acl.json` | アクセス権取得・更新 |

## 注意事項

- レスポンスは JSON 形式
- 1回のリクエストで取得できるレコード数は最大 500 件（大量取得はカーソル API を使用）
- APIトークンはアプリ単位で発行し、アクセス権（レコード閲覧・追加・編集・削除など）を個別に設定できる
- ゲストスペース内のアプリへのアクセスはエンドポイントが異なる: `/k/guest/{スペースID}/v1/...`

## 公式ドキュメント

- [kintone REST API ドキュメント トップ](https://cybozu.dev/ja/kintone/docs/rest-api/)
- [認証](https://cybozu.dev/ja/kintone/docs/rest-api/overview/authentication/)
- [curlコマンドでkintone REST APIを実行してみよう](https://cybozu.dev/ja/kintone/tips/development/development-productivity/http-client/kintone-rest-api-using-curl-command/)
- [APIトークンを使ってみよう](https://cybozu.dev/ja/kintone/tips/development/customize/development-know-how/api-tokens/)
- [kintone REST API の共通仕様](https://developer.cybozu.io/hc/ja/articles/201941754-kintone-REST-API%E3%81%AE%E5%85%B1%E9%80%9A%E4%BB%95%E6%A7%98)
