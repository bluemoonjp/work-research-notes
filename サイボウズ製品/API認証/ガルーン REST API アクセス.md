# サイボウズ ガルーン REST API アクセス

## 概要

Garoon REST API は、スケジュール・ワークフロー・メッセージなど Garoon のデータを外部から操作できる HTTP ベースの API。  
クラウド版・パッケージ版（Windows/Linux）の両方に対応している。

## エンドポイント形式

### クラウド版

```
https://{サブドメイン}.cybozu.com/g/api/v1/{APPLICATION_NAME}/{RESOURCE}
```

### パッケージ版（Windows）

```
http://{ホスト名またはIPアドレス}/scripts/{インストール識別子}/grn.exe/api/v1/{APPLICATION_NAME}/{RESOURCE}
```

### パッケージ版（Linux）

```
http://{ホスト名またはIPアドレス}/cgi-bin/{インストール識別子}/grn.cgi/api/v1/{APPLICATION_NAME}/{RESOURCE}
```

## 認証方法

### 1. パスワード認証（X-Cybozu-Authorization）

ログイン名とパスワードを `ログイン名:パスワード` の形式で Base64 エンコードし、リクエストヘッダーに指定する。

```
X-Cybozu-Authorization: {Base64エンコードした「ログイン名:パスワード」}
```

**注意事項:**
- 2要素認証を有効にしているユーザーは使用不可
- SAML認証のみ使用に制限している環境では、cybozu.com 共通管理者のみ利用可能（後述）

### 2. セッション認証

Garoon に適用した JavaScript カスタマイズ内から `garoon.api()` メソッドを使用するか、`X-Requested-With: XMLHttpRequest` ヘッダーを付けて Fetch API を呼び出す方式。  
外部サーバーから呼び出す場合には使用できない。

### 3. OAuth クライアント認証

cybozu.com 管理画面で登録した OAuth クライアントを使用する方式。  
Authorization Code Grant フローでアクセストークンを取得してから API を呼び出す。

**注意:** OAuth 対応はクラウド版のみ。パッケージ版 Garoon では使用不可。

### 4. Basic 認証（IPアドレス制限併用時）

IPアドレス制限を設定している場合に追加で必要になることがある。  
`Authorization: Basic {Base64エンコードした「ユーザー名:パスワード」}` ヘッダーを付与する。

## 認証の優先順位

1. パスワード認証（X-Cybozu-Authorization）
2. OAuth 認証
3. セッション認証

## curl でのテスト方法

### 予定を1件取得する（GET）

```bash
curl -X GET "https://{サブドメイン}.cybozu.com/g/api/v1/schedule/events/{予定ID}" \
  -H "X-Cybozu-Authorization: {Base64エンコード済み認証情報}"
```

**Base64 エンコードの例（PowerShell）:**

```powershell
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("ログイン名:パスワード"))
```

### 予定を登録する（POST）

```bash
curl -X POST "https://{サブドメイン}.cybozu.com/g/api/v1/schedule/events" \
  -H "X-Cybozu-Authorization: {Base64エンコード済み認証情報}" \
  -H "Content-Type: application/json" \
  -d "{\"eventType\":\"REGULAR\",\"subject\":\"打ち合わせ\",\"start\":{\"dateTime\":\"2026-07-01T10:00:00+09:00\",\"timeZone\":\"Asia/Tokyo\"},\"isStartOnly\":false,\"attendees\":[{\"code\":\"{ユーザーコード}\",\"type\":\"USER\"}]}"
```

### パッケージ版（Linux）での例

```bash
curl -X GET "http://{ホスト名}/cgi-bin/{インストール識別子}/grn.cgi/api/v1/schedule/events/{予定ID}" \
  -H "X-Cybozu-Authorization: {Base64エンコード済み認証情報}"
```

## 利用できる主な API

| カテゴリ | 主な操作 |
|----------|----------|
| スケジュール | 予定の取得・登録・更新・削除、施設・施設グループ管理 |
| ワークフロー | 申請データ取得、申請フォーム情報取得 |
| メッセージ | メッセージ取得・作成、添付ファイル管理 |
| スペース | ディスカッション・コメント作成、ファイル管理 |
| ユーザー／組織 | ユーザー一覧・組織一覧の取得 |
| 在席確認 | 在席情報の取得・更新 |
| ToDo | ToDoの取得・管理 |
| 通知 | 通知情報の取得 |
| 掲示板 | 掲示板情報の取得 |
| プロキシ API | 外部サービスへのプロキシリクエスト |

## 注意事項

- REST API のバージョンは現在 `v1`
- レスポンスは JSON 形式
- 日時は ISO 8601 形式（タイムゾーン付き）で指定する
- パッケージ版と クラウド版でエンドポイントのパス構造が異なる点に注意

## 公式ドキュメント

- [Garoon REST API ドキュメント トップ](https://cybozu.dev/ja/garoon/docs/rest-api/)
- [Garoon REST API の概要](https://cybozu.dev/ja/garoon/docs/rest-api/overview/garoon-rest-api-overview/)
- [認証](https://cybozu.dev/ja/garoon/docs/rest-api/overview/authentication/)
- [1件の予定を取得する](https://cybozu.dev/ja/garoon/docs/rest-api/schedule/get-schedule-event/)
