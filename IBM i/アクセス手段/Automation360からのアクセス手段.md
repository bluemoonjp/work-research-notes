# Automation 360 から IBM i（AS/400）へアクセスする手段

Automation Anywhere の RPA ツール「Automation 360」から IBM i（AS/400）へアクセスする手段をまとめる。

## 前提

- Automation 360 には IBM i / AS/400 専用の公式パッケージ・コネクターは存在しない（2026年6月時点、Automation Anywhere Marketplace / Bot Store では確認できず）
- IBM i へのアクセスは、Automation 360 の汎用パッケージを IBM i 側の標準インターフェースに接続することで実現する
- Automation 360 の Bot Runner は Windows 上で動作するため、Windows 向けのドライバーや設定が前提となる

## 手段一覧

### 1. Database Package + JDBC（JTOpen / jt400.jar）

**Automation 360 での方法**
- Database Package の `Connect` アクションで接続タイプに「JDBC」を選択
- JDBC 接続文字列: `jdbc:as400://hostname/defaultschema`
- Driver クラス: `com.ibm.as400.access.AS400JDBCDriver`
- jt400.jar を Bot Runner マシンの指定パスに配置

**実現難易度**: 設定が必要

**できること**
- DB2 for i のテーブル・ビューへの SELECT / INSERT / UPDATE / DELETE
- IBM i Services（QSYS2、SYSTOOLS 等）を SQL で実行
- ストアドプロシージャ・QSYS2.QCMDEXC によるコマンド実行
- `Read from` / `Insert, Update, Delete` / `Run stored procedure` 等の DB Package アクション全般

**制約・注意点**
- jt400.jar（JTOpen）を Bot Runner マシンに手動配置が必要
- 「DB2 for i」が Database Package のサポートマトリックスに明記されているかは要検証（DB2 JDBC として動作する）
- DB2 LUW（Linux/UNIX/Windows 版）と IBM i（AS/400）では JDBC URL 形式が異なる

**参考 URL**
- [Database Package - Automation Anywhere Docs](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-database-command.html)
- [Database server support matrix](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/database-engine-support-matrix.html)
- [IBM Toolbox for Java / JTOpen](https://www.ibm.com/support/pages/ibm-toolbox-java-jtopen)
- [JDBC for IBM i (AS400) - IBM Support](https://www.ibm.com/support/pages/using-jdbc-connector-connect-db2-iseries-as400)

---

### 2. Database Package + ODBC（IBM i Access ODBC Driver）

**Automation 360 での方法**
- Database Package の `Connect` アクションで接続タイプに「ODBC」または「接続文字列」を選択
- 接続文字列例: `Driver={IBM i Access ODBC Driver};System=hostname;Uid=user;Pwd=pass;`
- Bot Runner マシンに IBM i Access Client Solutions（ACS）Windows Application Package をインストール

**実現難易度**: 設定が必要

**できること**
- Database Package での DB2 for i アクセス全般（JDBC と同様）
- ODBC 経由でのストアドプロシージャ呼び出し

**制約・注意点**
- ACS Windows Application Package（有償オプション）のインストールが必要
- Automation Anywhere Docs によると、ODBC は macOS では非対応
- 32bit / 64bit ドライバーの選択に注意（Bot Runner のアーキテクチャに合わせる）

**参考 URL**
- [Using Connect action for database](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-using-database-connect-action.html)
- [ODBC Driver for IBM i Access Client Solutions - IBM Support](https://www.ibm.com/support/pages/odbc-driver-ibm-i-access-client-solutions)

---

### 3. REST Web Services Package + IWS REST API

**Automation 360 での方法**
- IBM i 側の Integrated Web Services（IWS）サーバーで RPG/COBOL/CL プログラムを REST API として公開
- REST Web Services Package の GET / POST / PUT / DELETE / PATCH アクションで呼び出す
- 認証: Basic 認証 / OAuth 2.0 / NTLM に対応

**実現難易度**: 設定が必要

**できること**
- 既存 RPG / COBOL プログラムの呼び出し
- CL コマンドの実行（IWS 経由で API 化した場合）
- JSON / XML 形式でのデータ交換
- Automation 360 の変数・テーブル型への結果マッピング

**制約・注意点**
- IBM i 側で IWS サーバーのセットアップと API 定義が必要
- ネットワーク経路に HTTPS / SSL 設定が必要な場合がある

**参考 URL**
- [REST Web Services Package - Automation Anywhere Docs](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-rest-web-service.html)
- [Integrated Web Services for IBM i - IBM Support](https://www.ibm.com/support/pages/integrated-web-services-ibm-i-web-services-made-easy)
- [Building a REST service with IWS Server - IBM Developer（Part 1）](https://developer.ibm.com/tutorials/i-rest-web-services-server1/)

---

### 4. FTP/SFTP Package

**Automation 360 での方法**
- FTP/SFTP Package の `Connect` → `Put files` / `Get files` / `Put folders` / `Get folders` / `Create folder` アクション
- IBM i の FTP サーバー（標準搭載）または OpenSSH SFTP サーバーに接続

**実現難易度**: 簡単

**できること**
- IFS（統合ファイルシステム）へのファイルアップロード・ダウンロード
- CSV・帳票ファイルの入出力
- FTP QUOTE RCMD（FTP 経由での CL コマンド実行）は FTP Package の標準アクションでは不可

**制約・注意点**
- FTP（平文）より SFTP（暗号化）を推奨
- IBM i の IFS パス（例: `/home/user/`）と QSYS ライブラリパス（例: `/QSYS.LIB/MYLIB.LIB/`）の違いに注意
- 文字コード変換（EBCDIC ⇔ UTF-8）は IBM i 側の設定で制御

**参考 URL**
- [FTP/SFTP Package - Automation Anywhere Docs](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-ftp-sftp-command.html)
- [FTP/SFTP Package updates](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/ftp-sftp-package-releases.html)

---

### 5. Terminal Emulator Package + TN5250E（5250 グリーン画面操作）

**Automation 360 での方法**
- Terminal Emulator Package の `Connect` アクションで TN5250E を選択
- `Get field` / `Set field` / `Send key` / `Send text` / `Get screen text` 等のアクションで画面操作

**実現難易度**: 設定が必要

**できること**
- API / DB 化できない既存 5250 画面業務の自動化
- IBM i への CL コマンド入力・結果取得
- 画面上のデータ読み取り・入力の自動化

**制約・注意点**
- Automation 360 の Terminal Emulator Package は TN3270E、TN5250E、ANSI、VT220、VT100、Linux 端末タイプに対応
- 接続タイプは Telnet、SSH1、SSH2 をサポート
- 5250 画面の変更・カーソル位置・画面待機条件に依存するため、メンテナンスコストが高い
- API / DB アクセス手段がある場合は Terminal Emulator より安定性・保守性が高い

**参考 URL**
- [Terminal Emulator Package - Automation Anywhere Docs](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-terminal-emulator-command.html)
- [Using Connect action for Terminal Emulator](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-terminal-connect-action.html)

---

### 6. Terminal Emulator Package + SSH（PASE / QShell）

**Automation 360 での方法**
- Terminal Emulator Package の `Connect` アクションで接続タイプに SSH2 を選択
- IBM i の PASE（Portable Application Solutions Environment）または QShell でコマンド実行

**実現難易度**: 設定が必要

**できること**
- PASE / QShell 環境でのシェルコマンド実行
- `system` コマンドや `call` コマンドで CL コマンドを実行
- IFS ファイルの操作

**制約・注意点**
- IBM i で SSH サーバー（OpenSSH）の設定が必要
- PASE 環境と IBM i ネイティブ環境の違いを理解する必要がある（例: PASE の `ls` は IFS を対象にする）
- CL コマンドの直接実行は `system` コマンド経由

**参考 URL**
- [Terminal Emulator Package - Automation Anywhere Docs](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-terminal-emulator-command.html)
- [Configuring IBM i SSH/SFTP Clients for Public Key Authentication - IBM Support](https://www.ibm.com/support/pages/configuring-ibm-i-ssh-sftp-and-scp-clients-use-public-key-authentication)

---

### 7. Python Script Package + JTOpen / ibm_db 等

**Automation 360 での方法**
- Python Script Package の `Execute script` アクションで Python コードを実行
- `jt400.jar`（JTOpen）を Py4J 経由で呼び出すか、`pyodbc` / `ibm_db` で DB2 for i に接続

**実現難易度**: 難しい

**できること**
- JDBC / ODBC 経由でのデータアクセス（Python ライブラリ経由）
- JTOpen の Java API（プログラム呼び出し・データキュー等）を Py4J や Jython で利用
- Bot Runner マシンに Python 環境の構築が必要

**制約・注意点**
- Python Script Package は Bot Runner 上の Python 環境に依存
- Java ライブラリ（JTOpen）を Python から呼び出す場合は Py4J 等が必要で複雑
- Database Package + JDBC/ODBC の方がシンプルなケースが多い

**参考 URL**
- [Python Script Package - Automation Anywhere Docs](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-python-command.html)

---

### 8. VBScript Package + ActiveX COM（cwbx.dll）

**Automation 360 での方法**
- VBScript Package の `Execute script` アクションで VBScript を実行
- `CreateObject("cwbx.AS400System")` 等で ACS ActiveX COM オブジェクトを呼び出す

**実現難易度**: 難しい

**できること**
- データ転送（ホストから PC へのファイル転送）
- リモートコマンド（CL コマンド）実行
- データキューの読み書き
- メッセージキューの操作

**制約・注意点**
- Windows Bot Runner のみ対象（macOS / Linux では利用不可）
- ACS Windows Application Package のインストールが必要
- cwbx.dll は 32bit / 64bit の両対応だが、ビット数の選択に注意
- レガシー技術であり、新規開発での採用は推奨しない

**参考 URL**
- [VBScript Package - Automation Anywhere Docs](https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-vb-script.html)
- [IBM i Access for Windows Toolkit - ActiveX - IBM Support](https://www.ibm.com/support/pages/ibm-i-access-windows-toolkit-activex)
- [IBM i Access ActiveX Automation Objects and 64-bit - IBM Support](https://www.ibm.com/support/pages/ibm-i-access-active-x-automation-objects-and-64-bit-computing)

---

### 9. 5250 EHLLAPI（既存ターミナルエミュレーター経由）

**Automation 360 での方法**
- 既存の TN5250 エミュレーター（IBM ACS、Personal Communications 等）を起動した状態で、VBScript Package 等から EHLLAPI DLL を P/Invoke 的に呼び出す
- または AA の Terminal Emulator Package の TN5250E 機能（TN5250E を内包）を使う方が現実的

**実現難易度**: 難しい

**できること**
- 既存エミュレーターへの依存を維持したまま RPA を組む

**制約・注意点**
- Automation 360 の Terminal Emulator Package が TN5250E に対応しているため、EHLLAPI を選ぶ積極的な理由は少ない
- 外部エミュレーター依存・セッション名管理・32bit DLL の扱いなどが制約
- 既存エミュレーターが他業務でも使われている場合のみ検討

**参考 URL**
- [IBM i ACS 5250 / Personal Communications HACL Automation - IBM Support](https://www.ibm.com/support/pages/ibm-i-acs-5250-personal-communications-hacl-automation-object-programming-support)

---

### 10. IBM MQ（メッセージキュー）

**Automation 360 での方法**
- Automation 360 に標準の IBM MQ パッケージは確認できない
- REST Web Services Package 経由で IBM MQ REST API を呼び出す
- または Python Script / VBScript Package から MQ .NET ライブラリを呼び出す

**実現難易度**: 難しい

**できること**
- IBM i と外部システム間の非同期メッセージング

**制約・注意点**
- Automation 360 標準の IBM MQ アクションがないため、ラッパー実装が必要
- IBM MQ は別途ライセンスが必要

**参考 URL**
- [IBM MQ REST API - IBM Docs](https://www.ibm.com/docs/en/ibm-mq/9.2?topic=mq-rest-api)

---

### 11. データキュー（直接操作）

**Automation 360 での方法**
- Automation 360 標準アクションでは直接操作不可
- JTOpen（Java）を Python Script 経由で呼び出すか、IBM i 側で REST API 化（IWS 等）して REST Web Services Package から呼び出す

**実現難易度**: 難しい

**制約・注意点**
- Database Package + JDBC/JTOpen 経由での操作は DataQueue クラスが必要で標準 SQL では操作できない
- IBM i 側での API 化（RPG + IWS REST）が最もシンプルな経路

---

## 難易度・手段 比較一覧表

| IBM i アクセス手段 | Automation 360 でのパッケージ | 難易度 | 備考 |
|---|---|:---:|---|
| JDBC / JTOpen（DB2 for i） | Database Package | 設定が必要 | **最有力。SQL・ストアドプロシージャ・IBM i Services に対応** |
| ODBC（ACS ODBC Driver） | Database Package | 設定が必要 | ACS Windows Package が必要。JDBC と同等機能 |
| IBM i Services SQL（QSYS2 等） | Database Package（JDBC/ODBC 経由） | 簡単 | DB 接続できれば標準 SQL で運用情報取得・コマンド実行 |
| ストアドプロシージャ / QCMDEXC | Database Package | 設定が必要 | JDBC/ODBC 接続後に CALL で実行 |
| IWS REST API | REST Web Services Package | 設定が必要 | IBM i 側で API 化が必要。長期運用に最適 |
| SFTP | FTP/SFTP Package | 簡単 | ファイル連携なら最も手軽 |
| FTP | FTP/SFTP Package | 簡単 | セキュリティ面では SFTP を推奨 |
| 5250 / TN5250E | Terminal Emulator Package | 設定が必要 | API 化不可の既存業務向け。保守性は低 |
| SSH（PASE/QShell） | Terminal Emulator Package | 設定が必要 | CL コマンド・シェルスクリプト実行に有効 |
| Python Script + pyodbc / ibm_db | Python Script Package | 難しい | Database Package + JDBC/ODBC の方がシンプル |
| VBScript + cwbx.dll（ActiveX COM） | VBScript Package | 難しい | Windows 限定。レガシー技術のため非推奨 |
| 5250 EHLLAPI（外部エミュレーター） | VBScript Package 等 | 難しい | Terminal Emulator Package（TN5250E）で代替可能 |
| IBM MQ | REST Web Services Package 等 | 難しい | 標準パッケージなし。REST API ラッパー等が必要 |
| データキュー（直接操作） | （標準パッケージなし） | 難しい | IBM i 側で REST API 化が現実的 |
| .NET Data Provider | （標準パッケージなし） | 難しい | Database Package の ADO.NET 直接接続ではないため非対応 |

## 推奨手段

### 第 1 位: Database Package + JDBC（JTOpen）

SQL / IBM i Services / ストアドプロシージャを一元的に扱えるため、画面操作より安定し、監査・権限制御もしやすい。jt400.jar の配置のみで接続できる点も実用的。

### 第 2 位: REST Web Services Package + IWS REST API

業務ロジックを IBM i 側に閉じ込め、Automation 360 は API クライアントに徹する構成。長期運用や変更耐性では最も整理された方式。IBM i 側の API 設計コストが初期にかかる。

### 第 3 位: FTP/SFTP Package

ファイル連携が主体の業務なら最も手軽で堅牢。FTP ではなく SFTP（SSH ベース）を使用すること。

### 第 4 位: Terminal Emulator Package（TN5250E）

API / DB 化できない既存 5250 画面業務に限って使用する。保守コストが高いため、将来的な API 化を検討すること。

## 参考 URL

| ページ | URL |
|---|---|
| Automation 360 Database Package | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-database-command.html |
| Database Connect / JDBC・ODBC 設定 | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-using-database-connect-action.html |
| Database server support matrix | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/database-engine-support-matrix.html |
| REST Web Services Package | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-rest-web-service.html |
| FTP/SFTP Package | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-ftp-sftp-command.html |
| Terminal Emulator Package | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-terminal-emulator-command.html |
| VBScript Package | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-vb-script.html |
| Python Script Package | https://docs.automationanywhere.com/bundle/enterprise-v2019/page/enterprise-cloud/topics/aae-client/bot-creator/commands/cloud-python-command.html |
| IBM Toolbox for Java / JTOpen | https://www.ibm.com/support/pages/ibm-toolbox-java-jtopen |
| IBM i Access Client Solutions（ACS） | https://www.ibm.com/support/pages/ibm-i-access-client-solutions |
| Integrated Web Services for IBM i | https://www.ibm.com/support/pages/integrated-web-services-ibm-i-web-services-made-easy |
