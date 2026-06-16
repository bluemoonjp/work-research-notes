# kintone OAuth 2.0 認証

## 概要

kintone は OAuth 2.0 の認可コードフロー（Authorization Code Grant）に対応している。
外部アプリケーションがユーザーの kintone 資格情報を直接保持せずに API アクセスを得る標準的な方法。

出典:
- [認証 - cybozu developer network](https://cybozu.dev/ja/kintone/docs/rest-api/overview/authentication/)
- [OAuthクライアント - cybozu developer network](https://cybozu.dev/ja/common/docs/oauth-client/)

---

## 対応状況

- kintone クラウド版: OAuth 2.0 対応済み
- 認証フロー: Authorization Code Grant のみ（Client Credentials Grant は非対応）
- SAML 強制環境でも OAuth は利用可能（詳細は後述）

---

## cybozu.com での OAuth クライアント登録手順

cybozu.com 共通管理画面で操作する。cybozu.com 共通管理者権限が必要。

出典: [OAuth クライアントを追加する - cybozu developer network](https://cybozu.dev/ja/common/docs/oauth-client/add-client/)

### 手順

1. cybozu.com 共通管理 → 「システム管理」→「外部連携」を開く
2. 「高度な連携を設定する」→「OAuth クライアントの追加」をクリック
3. 以下の情報を入力して保存する

| 項目 | 説明 |
|------|------|
| クライアント名 | 必須。アプリケーションの表示名 |
| クライアントロゴ | 任意。認可画面に表示されるロゴ画像 |
| リダイレクトエンドポイント | 必須。認可コードを受け取る URL。認可後にここへリダイレクトされる |

4. 保存後、以下が自動生成される

| 項目 | 説明 |
|------|------|
| クライアント ID | アプリケーション識別子 |
| クライアントシークレット | 機密情報。安全に保管すること |

5. 「連携利用ユーザーの設定」で OAuth を許可するユーザーにチェックを入れて保存する
   - チェックされたユーザーのみ OAuth クライアントを利用可能

---

## 認証フロー（認可コードフロー）

### エンドポイント

| 種別 | URL |
|------|-----|
| 認可エンドポイント | `https://{サブドメイン}.cybozu.com/oauth2/authorization` |
| トークンエンドポイント | `https://{サブドメイン}.cybozu.com/oauth2/token` |

### ステップ 1: 認可コードの取得

ブラウザで以下の URL にアクセスし、ユーザーに認可を求める。

```
https://{サブドメイン}.cybozu.com/oauth2/authorization
  ?client_id={クライアント ID}
  &redirect_uri={リダイレクト URI}
  &state={CSRF対策用ランダム文字列}
  &response_type=code
  &scope=k:app_record:read
```

ユーザーが許可すると、リダイレクト URI に以下のパラメータが付いてリダイレクトされる。

```
{リダイレクト URI}?code={認可コード}&state={stateの値}
```

- 認可コードの有効期限: **10 分**

### ステップ 2: アクセストークンの取得

認可コードを使ってトークンエンドポイントにリクエストを送る。

**curl の例:**

```bash
curl -X POST "https://{サブドメイン}.cybozu.com/oauth2/token" \
  -H "Authorization: Basic {Base64エンコードした「クライアントID:クライアントシークレット」}" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "redirect_uri={リダイレクト URI}" \
  -d "code={認可コード}"
```

**Base64 エンコードの例（PowerShell）:**

```powershell
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("{クライアントID}:{クライアントシークレット}"))
```

**レスポンス例:**

```json
{
  "access_token": "eyJ...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "dGhp..."
}
```

- `expires_in`: アクセストークンの有効期限（秒）。通常 3600 秒（1 時間）。

### ステップ 3: API リクエスト

取得したアクセストークンを `Authorization: Bearer` ヘッダーに指定して API を呼び出す。

```bash
curl -X GET "https://{サブドメイン}.cybozu.com/k/v1/records.json?app=1" \
  -H "Authorization: Bearer {アクセストークン}"
```

### ステップ 4: アクセストークンの更新（リフレッシュ）

リフレッシュトークンを使って新しいアクセストークンを取得する。

```bash
curl -X POST "https://{サブドメイン}.cybozu.com/oauth2/token" \
  -H "Authorization: Basic {Base64エンコードした「クライアントID:クライアントシークレット」}" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token" \
  -d "refresh_token={リフレッシュトークン}"
```

---

## トークンの有効期限

| トークン | 有効期限 |
|----------|----------|
| 認可コード | 10 分（期限切れ後は再取得が必要） |
| アクセストークン | `expires_in` 秒（通常 3600 秒 = 1 時間） |
| リフレッシュトークン | **有効期限なし** |

- 1 つの OAuth クライアントに対し 1 ユーザーあたり最大 10 個のリフレッシュトークンを生成可能。
- 上限を超えると古いリフレッシュトークンから無効化される。

出典: [OAuthクライアント：リフレッシュトークンによるアクセストークン取得のエラーについて](https://community.cybozu.dev/t/topic/4389)

---

## スコープ一覧

スコープは OAuth トークンのアクセス範囲を限定する仕組み。ユーザー自身の権限を超えたアクセスは付与されない。

出典: [kintoneのOAuthスコープ - cybozu developer network](https://cybozu.dev/ja/common/docs/oauth-client/scope-kintone/)

| スコープ値 | 説明 |
|-----------|------|
| `k:app_record:read` | レコード取得・コメント取得・レコードアクセス権取得 |
| `k:app_record:write` | レコード追加・更新・削除・コメント追加・ステータス更新 |
| `k:app_settings:read` | アプリ設定・フォーム・ビュー・通知の取得 |
| `k:app_settings:write` | アプリ設定・フォーム・ビュー・通知の更新 |
| `k:file:read` | ファイルのダウンロード |
| `k:file:write` | ファイルのアップロード |

複数のスコープはスペース区切りで指定する（URL エンコードは `%20` または `+`）。

---

## C# での実装例

Authorization Code フローはブラウザ操作（ユーザーの認可）が必要なため、サーバーサイドアプリ向けの例。

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class KintoneOAuthClient
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly string _subdomain;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public KintoneOAuthClient(string subdomain, string clientId, string clientSecret)
    {
        _subdomain = subdomain;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    // ステップ2: 認可コードからアクセストークンを取得する
    public async Task<TokenResponse> GetAccessTokenAsync(string authorizationCode, string redirectUri)
    {
        var tokenEndpoint = $"https://{_subdomain}.cybozu.com/oauth2/token";

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

        var requestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
        });

        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = requestBody
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TokenResponse>(json)!;
    }

    // ステップ4: リフレッシュトークンでアクセストークンを更新する
    public async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken)
    {
        var tokenEndpoint = $"https://{_subdomain}.cybozu.com/oauth2/token";

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

        var requestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
        });

        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = requestBody
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TokenResponse>(json)!;
    }

    // ステップ3: アクセストークンを使って kintone API を呼び出す
    public async Task<string> GetRecordsAsync(string accessToken, int appId)
    {
        var url = $"https://{_subdomain}.cybozu.com/k/v1/records.json?app={appId}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}

public record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken
);
```

---

## SAML 強制環境との共存

- **OAuth 認証は SAML 強制環境でも利用可能**。制限を受けない。
- SAML 強制が影響するのはパスワード認証のみ（cybozu.com 共通管理者を除く）。
- kintone 公式ドキュメントより: 「SAML 認証を設定している環境でも、OAuth クライアントを使用した認証で kintone REST API を実行できる」

| 認証方式 | SAML 強制時の影響 |
|---------|----------------|
| パスワード認証 | 共通管理者のみ利用可能（一般ユーザーは不可） |
| API トークン認証 | 制限なし |
| OAuth 認証 | **制限なし** |
| セッション認証 | 制限なし |

出典: [認証 - cybozu developer network（kintone）](https://cybozu.dev/ja/kintone/docs/rest-api/overview/authentication/)
