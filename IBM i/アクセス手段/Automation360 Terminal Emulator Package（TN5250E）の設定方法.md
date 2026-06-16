# Terminal Emulator Package で TN5250E（IBM i / AS/400）に接続・操作する方法

Automation 360 の Terminal Emulator Package を使って IBM i（AS/400）の 5250 グリーン画面（TN5250E）に接続・操作する手順をまとめる。

## 前提

- Automation 360 の Terminal Emulator Package は TN5250E を含む複数の端末タイプに対応している
- 接続プロトコルは Telnet・SSH1・SSH2 をサポート
- **Get field / Get all fields** は TN3270E・TN5250E 専用（ANSI・VT 系では不可）
- **Set field** は TN3270E・TN5250E に加え ANSI・VT100 でも使用可能
- Bot Runner は Windows 上で動作する

---

## 1. 接続手順（Connect アクション）

### ステップ 1: Connect アクションを配置

Bot Editor で「Terminal Emulator」パッケージ → 「Connect」アクションを選択する。

### ステップ 2: 接続パラメーターを設定

| パラメーター | 設定内容 |
|---|---|
| **Session name（セッション名）** | 任意の名前（例: `IBM_i_Session`）。後続アクションで使用する識別子 |
| **Host name** | IBM i の IP アドレスまたはホスト名（例: `192.168.1.10`） |
| **Port** | TN5250 標準ポート: `23`（Telnet）、SSH の場合: `22` |
| **Terminal type** | `TN5250E` を選択 |
| **Connection type** | `Telnet`（暗号化不要の場合）または `SSH2`（SSH 経由の場合） |
| **Show terminal window** | チェックありで Bot 実行時に端末画面を表示（デバッグ時に有効） |
| **Set cursor to beginning** | チェックありで接続後に最初の入力フィールドにフォーカスを設定 |
| **Host name security** | `NONE`（暗号化なし）/ `SSL` / `TLS`。IBM i 側の Telnet 設定と合わせる |
| **Enable TN5250E support** | チェックありでデバイス名・リソース（LU）名の詳細を指定可能 |
| **Device name** | `Enable TN5250E support` 有効時に指定。固定デバイス名運用環境で使用（例: `RPA001`） |
| **Terminal model** | 接続先が想定する端末モデルを選択。標準は 24×80（Model 2）|
| **Session scope** | `Local session`（同一 Bot 内）/ `Global session`（親子 Bot 共有）/ `Variable`（変数で動的指定） |
| **Wait for terminal prompt** | チェックありで接続後にプロンプト文字列が出るまで待機 |
| **Terminal prompt** | 待機対象のプロンプト文字列（例: `Sign On`）。上記チェック時に使用 |
| **Wait timeout（ms）** | 接続タイムアウト値（ミリ秒）。例: `30000`（30秒） |

### ステップ 3: セッション名を後続アクションで使用

Connect で指定したセッション名を、Get field・Set field・Send key 等すべての後続アクションの「Session name」欄に同じ名前で指定する。

---

## 2. アクション一覧と使い方

### Connect（接続）

ホスト機器への接続を確立する。セッション名を指定することで複数端末接続を管理できる。

- 対応端末: TN3270E、TN5250E、ANSI、VT220、VT100、Linux

---

### Disconnect（切断）

指定セッションの接続を切断する。

| パラメーター | 設定内容 |
|---|---|
| Session name | 切断するセッションの名前 |

---

### Send text（テキスト送信）

端末にテキストを送信する。画面のカーソル位置に文字を入力する。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| Text to send | 送信するテキスト（変数も使用可） |
| Send key after text | テキスト送信後にキー（Enter 等）を送るかどうか |

- 対応端末: TN3270E、TN5250E、ANSI、VT100

---

### Send key（キー送信）

ファンクションキー・特殊キーを端末に送信する。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| Key | 送信するキー名（ドロップダウンから選択） |

**IBM i でよく使うキー名の例:**

| キー名（UI 表記） | IBM i での意味 |
|---|---|
| `ENTER` | Enter / 送信 |
| `F1` ～ `F12` | PF1 ～ PF12 |
| `F13` ～ `F24` | PF13 ～ PF24（Shift+F1 ～ Shift+F12 相当） |
| `TAB` | 次フィールドへ移動 |
| `BACKTAB` | 前フィールドへ移動 |
| `PAGEUP` | Page Up（Pg Up） |
| `PAGEDOWN` | Page Down（Pg Dn） |
| `HOME` | ホーム（先頭フィールドへ） |
| `RESET` | キーボードロック解除（エラー後のリセット） |

- 対応端末: TN3270E、TN5250E、ANSI、VT100

---

### Get text（画面テキスト取得）

端末画面からテキストを取得して文字列変数に格納する。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| Get text from | 取得範囲の指定方法（下記参照） |
| 変数 | 取得テキストを格納する文字列型変数 |

**「Get text from」オプション:**

| オプション | 説明 |
|---|---|
| `Last line` | 最終行（ステータス行等）のテキストのみ取得 |
| `All lines` | 画面全体のテキストを取得 |
| `Lines from-to` | 開始行（Start row）〜終了行（End row）を指定して取得 |
| `Lines with column range` | 開始行・列〜終了行・列を指定して矩形範囲で取得 |

> **注意**: TN5250E で隠しフィールドにデータがある場合、Connect アクションの「Advanced Technology」オプションと「Include Hidden Text」を有効にしないと Get text が正しい値を返さないことがある。

---

### Get field（フィールド値取得）

画面上の特定フィールドの値を取得して変数に格納する。フィールドはインデックスまたは名前で指定する。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| Get field by | `Index`（番号）または `Name`（フィールド名） |
| Field index / Name | 対象フィールドのインデックス番号またはフィールド名 |
| 変数 | 取得値を格納する文字列型変数 |

- 対応端末: TN3270E、TN5250E のみ

---

### Set field（フィールドへの入力）

画面上の特定フィールドに値を設定する。カーソル移動なしに直接フィールドへ書き込める。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| Set field by | `Index`（番号）または `Name`（フィールド名） |
| Field index | **0 始まり**（3 番目のフィールドは `2`） |
| Field name | 行列形式で指定（例: 3 行 5 列のフィールドは `R3C5`） |
| Value | 設定する値（変数・Credential Vault も使用可） |
| Send enter key after setting field | チェックありで値設定後に Enter を送信 |
| Send a key after setting field | Enter 以外のキーを値設定後に送信 |

- 対応端末: TN3270E、TN5250E、ANSI、VT100

---

### Get all fields（全フィールド取得）

画面上のすべてのフィールドの値を取得してテーブル型変数に格納する。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| 変数 | 取得結果を格納するテーブル型変数 |

- 対応端末: TN3270E、TN5250E のみ

---

### Search field（フィールド検索）

フィールドの内容テキストに基づいてフィールドを検索し、インデックスまたは名前を取得する。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| Text | 検索するテキスト |
| Select field | `Index`（インデックス取得）または `Name`（名前取得） |
| 変数 | 取得したインデックス/名前を格納する変数 |

---

### Set key mappings（キーマッピング変更）

セッション単位でキーの既定マッピングを上書きする。後続のすべての `Send key` / `Send text` に影響する。新しいセッションでは再設定が必要。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| Key | 上書き対象のキー名（例: `F1`, `ENTER`） |
| Custom string / Macro | カスタム文字列またはマクロ（例: `{F1}`, `\x1b[11~`） |

---

### Set cursor position（カーソル移動）

端末画面の特定の行・列にカーソルを移動する。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| Row | 行番号（1 始まり）。5250 標準画面は 24 行 |
| Column | 列番号（1 始まり）。5250 標準画面は 80 列 |

---

### Wait（画面待機）

指定した条件が満たされるまで処理を待機する（画面の同期に使用）。

| パラメーター | 設定内容 |
|---|---|
| Session name | 対象セッション名 |
| Wait event | 待機条件（下記参照） |
| Text | 待機テキスト（条件によって使用） |
| Row / Column | カーソル位置（条件によって使用） |
| Wait timeout（ms） | タイムアウト値（ミリ秒） |

**「Wait event」の種類:**

| イベント | 説明 |
|---|---|
| Wait till text appears | **最終行**に指定テキストが現れるまで待機（画面全体ではない） |
| Wait till text disappears | **最終行**から指定テキストが消えるまで待機 |
| Wait till cursor moves to position | カーソルが指定行・列に移動するまで待機 |
| Wait till cursor moves out of position | カーソルが指定行・列から離れるまで待機 |
| Wait till screen gets blank | 画面がブランクになるまで待機 |
| Wait till screen contains text | 画面上にテキストが表示されるまで待機 |
| Wait till terminal prompt appears | 端末プロンプトが現れるまで待機 |
| Wait till terminal Ready State | 端末が Ready 状態になるまで待機 |

---

### Show terminal（端末表示切替）

端末画面の表示・非表示を切り替える。

---

## 3. 特定の画面・フィールドを特定する方法

### 方法 1: フィールドインデックス（最も確実）

5250 画面のフィールドは左上から順番にインデックスが振られる（1 始まり）。

- Connect アクションの「Show terminal window」を有効にして Bot を実行
- 画面に表示されたフィールドを目視で確認し、Get all fields でインデックスと値を一覧取得
- 取得した一覧から対象フィールドのインデックスを特定

### 方法 2: フィールド名

IBM i の 5250 画面定義（DDS: Data Description Specifications）にはフィールド名が定義されている。アプリケーション開発者にフィールド名を確認して指定できる。

### 方法 3: 行・列番号（Get text / Set cursor position）

フィールドではなく画面上の特定位置を直接指定する場合は行・列番号を使用する。

- 5250 標準画面: 24 行 × 80 列（または 27 行 × 132 列の拡張画面）
- コマンド行は通常 23 行目（画面レイアウトによって異なる）

### 方法 4: Search field アクション

フィールドに表示されているラベルテキストで検索してインデックスを動的に取得できる。画面レイアウトが変わっても追随しやすい。

---

## 4. 画面の待機・同期（Wait アクション）

5250 画面の応答を待つには必ず Wait アクションを使用すること。固定の遅延（Delay/Wait 秒数固定）より実際の画面状態を見て待つ方が安定・高速になる。

### 推奨パターン

```
[1] Connect アクション（Wait for terminal prompt + タイムアウト設定）
[2] Wait till text appears（例: "Sign On" の表示を待つ）
[3] Set field（ユーザー ID 入力）
[4] Set field（パスワード入力）
[5] Send key（ENTER）
[6] Wait till text appears（例: "Main Menu" または特定のメニュータイトルを待つ）
[7] ... 以降の業務操作
[8] Send key（F3 等でサインオフ）
[9] Wait till text appears（"Sign On" の再表示を待つ）
[10] Disconnect
```

### タイムアウト設定の目安

| 操作 | 推奨タイムアウト |
|---|---|
| 接続（Connect） | 30,000 ms（30秒） |
| サインオン後の初期画面 | 15,000 ms（15秒） |
| 通常の画面遷移 | 10,000 ms（10秒） |
| 重い処理（バッチ実行等） | 60,000 ms 以上（処理時間に応じて） |

---

## 5. IBM i / 5250 特有の注意点

### 文字コード（EBCDIC / CCSID）

- IBM i は内部で EBCDIC を使用しており、TN5250E プロトコルがコード変換を行う
- 日本語環境では CCSID 5026（大文字ラテン + カタカナ SBCS。**小文字ラテン文字は表示不可**）または CCSID 930（Latin + 全角日本語 SBCS/DBCS 混在）が使われることが多い
- 端末の文字セット設定を IBM i サーバーの CCSID 設定と一致させる必要がある
- 文字化けが発生する場合は Connect アクションの「Options」フィールドに `LineCodePage` パラメーターを追加する

### ファンクションキー（PF キー）

- IBM i では F1〜F24 の 24 個のプログラマブルファンクションキー（PF キー）を使用
- PF1〜PF12 は `F1`〜`F12` で送信
- PF13〜PF24 は `F13`〜`F24` で送信（Shift+F1〜F12 相当）
- **よく使う PF キーの例:**
  - F3: Exit（終了）
  - F4: Prompt（プロンプト・候補表示）
  - F5: Refresh（画面更新）
  - F12: Cancel（前画面に戻る）
  - F1: Help（ヘルプ）

### キーボードロック（Keyboard Inhibit）

- IBM i でエラーメッセージが表示されるとキーボードがロック（Input Inhibited）される
- 画面下部の OIA（Operator Information Area）に `X` 記号が表示されていたらロック状態
- ロック解除には `RESET` キーを Send key で送信する
- Wait アクションでエラーメッセージテキストを検出したら RESET を送信するエラーハンドリングを組み込むこと

### カーソル位置

- 5250 画面はブロックモード端末のため、フィールド間のカーソル移動は TAB / BACKTAB キーで行う
- Enter キーを押すと画面全体がサーバーに送信される（ストリームモードとは動作が異なる）
- Set field アクションはカーソル移動なしにフィールドに直接書き込めるため、Send text より効率的

### 複数セッション・親子 Bot 間共有

- Connect アクションで異なるセッション名を使えば複数の端末接続を同時に保持できる
- 親子 Bot 間でセッションを共有する場合は Connect の **Session scope** を `Global session` に設定する
  - 子 Bot からは同じセッション名で参照可能
  - 親 Bot / 子 Bot は同じ Terminal Emulator Package バージョンを使用すること

### Advanced Technology オプション

- Connect アクションの Advanced Technology オプション（高度な技術オプション）を有効にすると、隠しフィールドの読み取りなど拡張機能が利用できる
- Get text アクションで隠しフィールドのデータを取得する場合は「Include Hidden Text」と組み合わせて使用

---

## 6. よくある設定ミスと注意事項

### Connect 後すぐに操作を始めてエラーになる

Connect アクションの「Wait for terminal prompt」だけでは不十分な場合がある。接続後に必ず Wait アクション（Wait till text appears 等）でサインオン画面の出現を確認してから操作を開始すること。

### セッション名の不一致

Connect で指定したセッション名と、後続アクションのセッション名が違うとエラーになる。変数に格納して統一管理するのが安全。

### フィールドインデックスのズレ

画面によってフィールド数・順序が異なる。まず Get all fields で現在の画面のフィールド一覧を取得して確認してから自動化を組む。

### Send text とカーソル位置の依存

Send text はカーソルが当たっているフィールドにテキストを入力するため、直前のカーソル位置によって動作が変わる。フィールド指定ができる Set field の方が安定している（TN5250E では推奨）。

### PF キーが意図したとおりに動作しない

端末タイプが VT 系（VT100・VT220 等）になっている場合、PF キーの挙動が異なる（VT 系では F13〜F16 以降のキーコードが異なる）。TN5250E 接続では必ず Terminal type を `TN5250E` に設定すること。

### Bot が端末より速すぎる

5250 画面の応答速度は IBM i サーバーの処理速度に依存する。Bot の処理が速すぎて画面が描画される前に次のアクションが実行されるケースがある。固定 Delay ではなく Wait アクションで実際の画面テキスト・カーソル位置を確認してから進むように設計すること。

### 文字化け

日本語環境で文字化けが発生する場合、以下を確認する:
1. IBM i サーバーの CCSID 設定（WRKSYSVAL QCCSID で確認）
2. Connect アクションの文字セット設定
3. 端末エミュレーターの文字セット設定（Options フィールドの LineCodePage）

### 「AS400 Driver is Not Loaded」エラー

Terminal Emulator Package を使った 5250 接続（Telnet）では通常 JDBC ドライバーは不要だが、Database Package と混同して発生する場合がある。5250 接続には JDBC ドライバーは不要。

---

## 7. 実装パターン例

### ログオン → 業務画面操作 → ログオフ

```
[1]  Connect
       Session name: ibmi_session / Terminal type: TN5250E
       Host: 192.168.1.10 / Port: 23
       Session scope: Local session

[2]  Wait（Wait till screen contains text）
       Text: "サインオン"（ログオン画面の固定文言）
       Timeout: 30,000 ms

[3]  Get all fields ← 初回調査時のみ。ユーザー ID / パスワードの index を確認する

[4]  Set field（ユーザー ID 入力）
       By index: 0（最初の入力フィールド）
       Value: $userId（変数または Credential Vault）

[5]  Set field（パスワード入力 + Enter 送信）
       By index: 1
       Value: $password（Credential Vault 推奨）
       Send enter key after setting field: ON

[6]  Wait（Wait till screen contains text）
       Text: "メインメニュー"（ログオン後のメニュー画面固定文言）
       Timeout: 15,000 ms

[7]  Send text（メニュー番号入力）
       Text: "1"
       Send key after text: ENTER

[8]  Wait（Wait till screen contains text）
       Text: "業務画面タイトル"
       Timeout: 10,000 ms

[9]  --- 業務操作（Get field / Set field / Send key 等） ---

[10] Send key（F3 でサインオフ）
       Key: F3

[11] Wait（Wait till screen contains text）
       Text: "サインオン"（ログオン画面に戻ったことを確認）
       Timeout: 10,000 ms

[12] Disconnect
       Session name: ibmi_session
```

### エラーハンドリングの基本パターン

```
[操作前]
  Get text（Last line） → $lastLine に格納
  If $lastLine contains "エラー"
    → Send key: RESET
    → Wait（Wait till cursor moves to position: 待機フィールド行・列）

[例外時の後処理]
  Try/Catch の Catch ブロックに Disconnect を置き、
  異常終了時も必ず接続を切断する
```

---

## 参考 URL

| ページ | URL |
|---|---|
| Terminal Emulator Package（概要・全アクション） | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-terminal-emulator-command.html |
| Connect アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-terminal-connect-action.html |
| Send text アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-using-send-text-action.html |
| Send key アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-using-send-key-action.html |
| Get text アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-using-get-text-action.html |
| Get field アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/terminal-emulator-package-get-field-action.html |
| Get all fields アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/terminal-emulator-package-get-all-fields-action.html |
| Search field アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/terminal-emulator-package-search-field-action.html |
| Set field アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-using-set-field-action.html |
| Set cursor position アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/terminal-emulator-package-set-cursor-position-action.html |
| Set key mappings アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/using-set-key-mappings.html |
| Wait アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-terminal-wait-action.html |
| Disconnect アクション | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/terminal-emulator-package-disconnect-action.html |
| Terminal Emulator Package 更新履歴 | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/terminal-emulator-package-releases.html |
| セッション共有（親 Bot / 子 Bot） | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/sharing-sessions-across-bots.html |
| Automation Anywhere Community: ボット効率改善（メインフレーム） | https://community.automationanywhere.com/developers-forum-36/improving-bot-efficiency-for-mainframe-applications-90046 |
| RFC 1205: 5250 Telnet Interface | https://www.rfc-editor.org/rfc/rfc1205 |
| RFC 2877: 5250 Telnet Enhancements | https://www.rfc-editor.org/rfc/rfc2877 |
