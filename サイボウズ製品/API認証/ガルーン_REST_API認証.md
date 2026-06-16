# ガルーン REST API 認証

## 概要

ガルーン（クラウド版）の REST API では複数の認証方式が利用できる。
kintone と異なり、ガルーン固有の API トークン機能は存在しない（2025 年時点）。

出典:
- [認証 - cybozu developer network（ガルーン）](https://cybozu.dev/ja/garoon/docs/rest-api/overview/authentication/)
- [Garoon REST APIの共通仕様 - cybozu developer network](https://developer.cybozu.io/hc/ja/articles/360000503306-Garoon-REST-API%E3%81%AE%E5%85%B1%E9%80%9A%E4%BB%95%E6%A7%98)

---

## 認証方式一覧

| 認証方式 | ヘッダー / 方法 | SAML 強制時 |
|---------|--------------|------------|
| パスワード認証 | `X-Cybozu-Authorization: Base64(ログイン名:パスワード)` | 共通管理者のみ利用可 |
| セッション認証 | ブラウザのセッション Cookie を利用 | **制限なし** |
| OAuth クライアント認証 | `Authorization: Bearer {アクセストークン}` | **制限なし** |

### 補足: 2 要素認証ユーザーの制限

2 要素認証を有効にしているユーザーは、**パスワード認証で REST API を実行できない**。

---

## 各認証方式の詳細

### パスワード認証

ログイン名とパスワードを Base64 エンコードしてヘッダーに指定する。

```bash
curl -X GET "https://{サブドメイン}.cybozu.com/g/api/v1/schedule/events" \
  -H "X-Cybozu-Authorization: $(echo -n 'ログイン名:パスワード' | base64)"
```

**制限事項:**
- SAML 認証のみを使うように制限した環境では、cybozu.com 共通管理者のみ実行可能
- 2 要素認証を有効にしたユーザーは実行不可

### セッション認証

Web ブラウザで cybozu.com にログインしたときのセッションを利用する。
kintone JavaScript カスタマイズや、ブラウザ上の操作連動処理で使われる方式。

プログラムからの定期実行・バッチ処理には向かない。

### OAuth クライアント認証

kintone の OAuth 2.0 認証と同一の仕組みを使う。
cybozu.com 共通管理で登録した OAuth クライアントのアクセストークンをヘッダーに指定する。

```bash
curl -X GET "https://{サブドメイン}.cybozu.com/g/api/v1/schedule/events" \
  -H "Authorization: Bearer {アクセストークン}"
```

OAuth クライアントの登録・フローは kintone と共通。
→ 参照: `kintone_OAuth2.0認証.md`

**スコープ（ガルーン用）:**

ガルーン用のスコープは kintone のスコープとは別に設定されている。

出典: [ガルーンのOAuthスコープ - cybozu developer network](https://cybozu.dev/ja/common/docs/oauth-client/scope-garoon/)

---

## SAML 強制環境での認証オプション

SAML 認証のみを強制する設定（「SAML 認証の使用を必須にする」有効）では、次の通り。

| 認証方式 | 一般ユーザー | cybozu.com 共通管理者 |
|---------|------------|----------------------|
| パスワード認証 | **不可** | 可 |
| セッション認証 | 可（SAML でログイン後のセッション） | 可 |
| OAuth 認証 | **可** | 可 |

**推奨:** SAML 強制環境でのプログラムからのバッチアクセスには OAuth 認証を使用する。

---

## セキュリティAPIキー・サービスアカウントの有無

- **セキュリティ API キー**: ガルーン独自のセキュリティ API キー機能は**存在しない**（2025 年時点）
- **サービスアカウント**: ガルーンにはサービスアカウント概念は**存在しない**
- **API トークン**: kintone にはアプリ単位の API トークンがあるが、**ガルーンには API トークン機能はない**

プログラム連携には OAuth か、パスワード認証（SAML 強制でない環境）を使う。

---

## ガルーン SOAP API との比較

ガルーンには REST API の他に SOAP API も存在する。

| | REST API | SOAP API |
|-|----------|----------|
| 対応バージョン | クラウド版・パッケージ版 | クラウド版・パッケージ版 |
| 認証 | パスワード・セッション・OAuth | リクエストトークン方式 |
| 推奨 | 新規開発で推奨 | 既存連携の維持 |

出典: [Garoon SOAP APIの共通仕様 - cybozu developer network](https://cybozu.dev/ja/garoon/docs/soap-api/overview/soap/)
