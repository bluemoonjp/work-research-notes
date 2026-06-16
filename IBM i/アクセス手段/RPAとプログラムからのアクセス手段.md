# IBM i（AS/400）へ RPA・プログラムからアクセスする手段

IBM i（旧 AS/400、iSeries）へ人間ではなく RPA やプログラムからアクセスする際に利用できる手段をまとめる。

## 手段一覧

---

### 1. ODBC ドライバー（IBM i Access Client Solutions ODBC Driver）

**概要**
IBM i Access Client Solutions（ACS）の Windows Application Package に同梱される ODBC ドライバー。DB2 for i へ標準 SQL でアクセスできる。Windows・Linux・macOS 対応。

**主な用途**
- DB2 for i のテーブル・ビューへの SELECT / INSERT / UPDATE / DELETE
- ストアドプロシージャ・関数の呼び出し
- ODBC 対応ツール（Excel、Power BI、各種 ETL ツール）からのデータ取得

**C# / .NET サポート**
- `System.Data.Odbc`（NuGet）経由で利用可能
- 接続文字列例:
  ```
  Driver={IBM i Access ODBC Driver};System=192.168.1.1;Uid=user1;Pwd=pass1;CommitMode=0;
  ```
- .NET Framework・.NET Core（.NET 5+）いずれも対応

**参考 URL**
- [ODBC Driver for IBM i Access Client Solutions - IBM Support](https://www.ibm.com/support/pages/odbc-driver-ibm-i-access-client-solutions)
- [IbmiOdbcDataAccess - GitHub（C# サンプルクラス）](https://github.com/richardschoen/IbmiOdbcDataAccess)

---

### 2. IBM i .NET Data Provider（IBM.Data.DB2.iSeries）

**概要**
ACS Windows Application Package に同梱されるネイティブ ADO.NET プロバイダー。ODBC ブリッジ不要で DB2 for i へ直接接続できる。

**主な用途**
- Windows アプリ・サーバーサイド .NET から DB2 for i へのデータアクセス
- 高速・ローレイテンシーなデータベース操作

**C# / .NET サポート**
- `IBM.Data.DB2.iSeries` 名前空間を直接使用
- 接続文字列例:
  ```
  DataSource=192.168.1.1;UserID=user1;Password=pass1;DefaultCollection=MYLIB;
  ```
- .NET Core / .NET 5+ への対応は限定的。ACS Windows Application Package のインストールが必要。詳細は IBM サポートページを確認。

**参考 URL**
- [IBM i .NET Data Provider And .NET Core - IBM Support](https://www.ibm.com/support/pages/ibm-i-net-data-provider-and-net-core)
- [IBM .NET Data Providers for IBM DB2 UDB iSeries - IBM Support](https://www.ibm.com/support/pages/ibm-net-data-providers-ibm-db2-udb-iseries)

---

### 3. JDBC / JTOpen（IBM Toolbox for Java）

**概要**
オープンソースの Java ライブラリ。JDBC ドライバーに加え、DB 以外の IBM i オブジェクト（プログラム呼び出し、データキュー、IFS、コマンド実行、スプールファイル等）を操作できる。

**主な用途**
- Java・Kotlin アプリからの DB2 for i アクセス
- RPG/CL プログラムの呼び出し（ProgramCall クラス）
- データキューの読み書き（DataQueue クラス）
- IFS ファイル操作

**C# / .NET サポート**
- JTOpen は Java ライブラリのため、C# から直接は使用不可
- IKVM（Java→.NET 変換）や Jni4net 経由での利用は技術的には可能だが実用的ではない
- 代替として ODBC / .NET Data Provider を推奨

**参考 URL**
- [JTOpen Project（SourceForge）](https://jt400.sourceforge.net/)
- [AS400JDBCDriver JavaDoc](https://javadoc.io/doc/net.sf.jt400/jt400/latest/com/ibm/as400/access/AS400JDBCDriver.html)
- [IBM Toolbox for Java 概要 - IBM Docs](https://www.ibm.com/docs/en/ssw_ibm_i_75/rzahh/javadoc/overview-summary.html)

---

### 4. Integrated Web Services（IWS）/ REST・SOAP API

**概要**
IBM i 上の ILE プログラム（RPG、COBOL、C/C++）を Web サービス（REST / SOAP）として公開する仕組み。IBM i 内蔵の IWS サーバーが HTTP リクエストを受け付け、ILE プログラムを実行して結果を返す。

**主な用途**
- 既存 RPG/COBOL プログラムを REST API 化してモダン化
- 外部システム・クラウドサービスとの HTTP ベース連携
- JSON/XML のデータ交換

**C# / .NET サポート**
- IBM i 側が REST API を公開するため、C# の `HttpClient` で標準的に呼び出し可能
- 認証は Basic 認証・OAuth 等に対応
- IBM i 側のセットアップが必要（IWS サーバーの設定とプログラムの展開）

**参考 URL**
- [Integrated Web Services for IBM i - IBM Support](https://www.ibm.com/support/pages/integrated-web-services-ibm-i-web-services-made-easy)
- [Building a REST service with IWS Server - IBM Developer（Part 1）](https://developer.ibm.com/tutorials/i-rest-web-services-server1/)

---

### 5. SSH / SFTP

**概要**
IBM i は OpenSSH を内蔵しており、SSH 接続でコマンド実行（PASE 環境）や SFTP でのファイル転送が可能。

**主な用途**
- CL コマンド・シェルスクリプトのリモート実行
- IFS（統合ファイルシステム）へのファイルアップロード・ダウンロード
- 自動化バッチ処理

**C# / .NET サポート**
- SSH.NET ライブラリ（NuGet: `SSH.NET`）で SSH 接続・コマンド実行・SFTP 転送が可能
- 鍵認証・パスワード認証いずれも対応

**参考 URL**
- [Configuring IBM i SSH/SFTP Clients for Public Key Authentication - IBM Support](https://www.ibm.com/support/pages/configuring-ibm-i-ssh-sftp-and-scp-clients-use-public-key-authentication)
- [How to Configure and Use SSH on IBM i - Seiden Group](https://www.seidengroup.com/how-to-configure-and-use-ssh-on-ibm-i/)
- [SSH.NET - GitHub](https://github.com/sshnet/SSH.NET)

---

### 6. FTP / FTPS

**概要**
IBM i は FTP サーバーを標準搭載。FTP コマンドや NAMEFMT オプションを使い、IFS やライブラリーファイルの転送が可能。FTPS（FTP over TLS）にも対応。

**主な用途**
- ファイル・データの一括転送
- バッチ処理での入出力ファイル連携
- CL コマンドの実行（QUOTE RCMD）

**C# / .NET サポート**
- FluentFTP ライブラリ（NuGet: `FluentFTP`）で FTP/FTPS を実装可能
- .NET 標準の `FtpWebRequest` でも利用可能（非推奨、.NET 6+ では削除傾向）

**参考 URL**
- [FluentFTP - GitHub](https://github.com/robinrodricks/FluentFTP)

---

### 7. IBM MQ（WebSphere MQ）

**概要**
IBM MQ は IBM i 上でネイティブ動作するエンタープライズメッセージングミドルウェア。IBM i と他システム間で非同期のメッセージ交換が可能。

**主な用途**
- 基幹系と外部システム間の疎結合連携
- トランザクション保証が必要な非同期処理
- 高可用性・信頼性が求められるエンタープライズ統合

**C# / .NET サポート**
- IBM MQ Classes for .NET（XMS .NET）で接続可能
- NuGet: `IBMXMSDotnetClient` または `IBMWMQDotnetClient`
- JMS と同等のインターフェース（XMS API）を .NET で提供

**参考 URL**
- [IBM Message Service Client for .NET v9.0 PDF](https://public.dhe.ibm.com/software/integration/wmq/docs/V9.0/PDFs/mq90.xms.pdf)
- [Integrate IBM message queues with Azure - Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/example-scenario/mainframe/integrate-ibm-message-queues-azure)
- [C# .NET Code to Get a Message from Remote Queue Manager](https://www.capitalware.com/rl_blog/?p=4999)

---

### 8. データキュー（Data Queue）

**概要**
IBM i 固有のオブジェクト。RPG/CL プログラムと外部プログラムがリアルタイムでメッセージを交換できる仕組み。FIFO・LIFO・キーつきキューをサポート。

**主な用途**
- IBM i 上の RPG プログラムとの非同期・リアルタイムデータ交換
- トリガー起動の受け渡し
- バッファリング処理

**C# / .NET サポート**
- JTOpen（Java）では `DataQueue` クラスで直接操作可能
- C# からは ACS Windows Application Package の ActiveX（cwbx.dll）COM オブジェクト経由でアクセス可能
- ネイティブ .NET ライブラリは公式には存在しないため、COM Interop か JTOpen + IKVM が必要

**参考 URL**
- [DataQueue - JTOpen JavaDoc](https://jt400.sourceforge.net/doc/com/ibm/as400/access/DataQueue.html)
- [IBM i Access for Windows Toolkit - Remote Command - IBM Support](https://www.ibm.com/support/pages/ibm-i-access-windows-toolkit-remote-command)

---

### 9. ActiveX COM オブジェクト（cwbx.dll）

**概要**
ACS Windows Application Package に含まれる ActiveX COM ライブラリ。データ転送・リモートコマンド実行・データキュー・メッセージキューなど IBM i 固有の機能を Windows クライアントから操作できる。

**主な用途**
- Windows アプリ・VBA マクロからの IBM i 操作
- データ転送の自動化
- リモートコマンド（CL コマンド）実行

**C# / .NET サポート**
- COM Interop で利用可能（32bit 制限あり、64bit 環境では注意が必要）
- `Type.GetTypeFromProgID("cwbx.AS400System")` 等で参照
- ACS Windows Application Package のインストールが前提

**参考 URL**
- [IBM i Access for Windows Toolkit - ActiveX - IBM Support](https://www.ibm.com/support/pages/ibm-i-access-windows-toolkit-activex)
- [IBM i Access ActiveX Automation Objects and 64-bit Computing - IBM Support](https://www.ibm.com/support/pages/ibm-i-access-active-x-automation-objects-and-64-bit-computing)

---

### 10. 5250 ターミナルエミュレーター + EHLLAPI（RPA 向け）

**概要**
TN5250（Telnet 5250）プロトコルで IBM i に接続する端末エミュレーター。EHLLAPI（Emulator High Level Language API）を使うと、プログラムから画面を操作・スクレイピングできる。

**主な用途**
- RPA ツール（UiPath、Power Automate、IBM RPA 等）での画面自動操作
- レガシーアプリのテスト自動化
- 人間が行っていた 5250 画面操作の自動化

**C# / .NET サポート**
- EHLLAPI 経由（32bit DLL）: P/Invoke で C# から呼び出し可能
- ACS EHLLAPI Bridge を使用すると ACS 5250 エミュレーターと連携可能
- UiPath Terminal Activities（Direct Connection / EHLLAPI プロバイダー）で TN5250 自動化可能

**参考 URL**
- [IBM RPA Terminal Emulator Automation - IBM Documentation](https://www.ibm.com/docs/en/rpa/23.0.x?topic=tasks-terminal-emulator-automation)
- [TN5250 Automation - LegacyBridge](https://legacybridge.software/use-cases/tn5250-automation/)
- [IBM i ACS 5250 / Personal Communications HACL Automation - IBM Support](https://www.ibm.com/support/pages/ibm-i-acs-5250-personal-communications-hacl-automation-object-programming-support)
- [UiPath Terminal Activities for IBM iAccess - UiPath Community](https://forum.uipath.com/t/automating-ibm-iaccess-client-solution-emulator-using-uipath-terminal-activities/502844)

---

### 11. Microsoft Host Integration Server（HIS）/ Transaction Integrator

**概要**
Microsoft が提供するレガシー統合製品。IBM i の RPG / COBOL プログラムを .NET アプリから直接呼び出せる Transaction Integrator（TI）機能を持つ。HIDX（Host Integration Designer XML）メタデータファイルを作成してプログラム定義を記述する。

**主な用途**
- .NET アプリから RPG / COBOL プログラムを直接呼び出し
- DPC（Distributed Program Calls）サーバー経由での IBM i プログラム統合
- Azure Logic Apps の IBM i Program Call コネクター（内部的に HIS/DPC を使用）

**C# / .NET サポート**
- HIS Transaction Integrator を使用し、.NET クライアントコードを生成
- Azure Logic Apps の IBM i Program Call コネクターとして利用可能
- HIDX ファイル生成には HIS Designer for Logic Apps ツールを使用

**参考 URL**
- [Access COBOL & RPG Programs from Azure Logic Apps - Microsoft Learn](https://learn.microsoft.com/en-us/azure/connectors/integrate-ibmi-apps-distributed-program-calls)
- [What is HIS - Host Integration Server - Microsoft Learn](https://learn.microsoft.com/en-us/host-integration-server/what-is-his)
- [IBM i Program Call Connector Reference - Microsoft Learn](https://learn.microsoft.com/en-us/azure/logic-apps/connectors/built-in/reference/ibmiprogramcall/)

---

### 12. IBM i Services（SQL Services / QSYS2 / SYSTOOLS）

**概要**
SQL で IBM i のシステム情報・管理情報（ジョブ、スプール、IFS、メッセージ、データキュー、権限、PTF 等）を参照・操作できる組み込みサービス。ODBC や .NET Provider から通常の SQL として呼び出せる。

**主な用途**
- ジョブ・スプール・出力キューの一覧取得
- IFS ファイルの一覧・読み込み
- CL コマンドの実行（`QSYS2.QCMDEXC`）
- RPA での画面スクレイピングの代替
- 運用監視・管理情報の自動収集

**C# / .NET サポート**
- ODBC または .NET Data Provider 経由で利用可能
- SQL 文として `SELECT * FROM QSYS2.JOB_INFO` や `CALL QSYS2.QCMDEXC('SBMJOB ...', 100)` のように実行
- IBM i のリリース・PTF レベルにより使えるサービスが異なる

**参考 URL**
- [IBM i Services - IBM Support](https://www.ibm.com/support/pages/ibm-i-services)
- [QSYS2.QCMDEXC - IBM Documentation](https://www.ibm.com/docs/en/i/7.4.0?topic=ssw_ibm_i_74/rzajq/rzajqudfs.htm)

---

### 13. ストアドプロシージャ / QSYS2.QCMDEXC による RPG/CL 呼び出し

**概要**
SQL の `CALL` ステートメントを通じて IBM i 上の RPG・COBOL・CL プログラムを呼び出す方式。`QSYS2.QCMDEXC` を使うと SQL 経由で CL コマンドを実行できる。

**主な用途**
- 既存 RPG/COBOL プログラムを改修最小限で外部から呼び出す
- バッチジョブの起動（SBMJOB コマンド）
- データ操作と業務ロジックを一括で実行

**C# / .NET サポート**
- ODBC または .NET Data Provider から `DbCommand` で `CALL` 実行
- IN/OUT パラメーターも標準の ADO.NET パラメーター機構で扱える

**参考 URL**
- [IBM i Services - IBM Support](https://www.ibm.com/support/pages/ibm-i-services)

---

### 14. OLE DB Provider

**概要**
ACS Windows Application Package に含まれる COM/OLE DB プロバイダー。DB2 for i への接続を提供する。主にレガシーな Windows 環境向け。

**主な用途**
- 既存の VB6・Classic ASP・COM ベース業務アプリからの DB 接続
- ADODB（ADO 2.x）を使用するレガシーアプリの保守

**C# / .NET サポート**
- `System.Data.OleDb` 経由で利用可能（Windows のみ）
- 新規開発での採用優先度は低い。ODBC または .NET Data Provider を推奨

**参考 URL**
- [ACS Windows Application Package 情報 - IBM Support](https://www.ibm.com/support/pages/ibm-i-access-acs-windows-information)

---

### 15. SMB / IBM i NetServer（Windows 共有フォルダ）

**概要**
IBM i の IFS（統合ファイルシステム）を Windows 共有フォルダとして公開する仕組み。IBM i NetServer 機能を使い、UNC パスでアクセスできる。

**主な用途**
- RPA ツールからの CSV・帳票ファイルの配置・取得
- Windows アプリからのファイル連携
- Excel・帳票出力の共有フォルダ経由受け渡し

**C# / .NET サポート**
- UNC パス（例: `\\ibmiserver\sharename\file.csv`）を通常の `System.IO` API でアクセス可能
- ネットワーク認証（IBM i のユーザー権限）の設定が必要

**参考 URL**
- [IBM i Services - IFS サービス - IBM Support](https://www.ibm.com/support/pages/ibm-i-services)

---

### 16. カスタム TCP/IP ソケット

**概要**
IBM i 側で RPG・C・Java・Node.js などのソケットサーバープログラムを作成し、外部クライアントが独自プロトコルで通信する方式。

**主な用途**
- 低遅延が求められる専用端末・製造ライン連携
- 既存の独自プロトコルが存在する場合の保守・拡張

**C# / .NET サポート**
- `TcpClient` / `Socket` クラスで実装可能
- 文字コード（EBCDIC⇔Unicode）変換の実装が必要

**参考 URL**
- [Socket Programming - IBM Documentation (IBM i 7.4)](https://www.ibm.com/docs/en/i/7.4.0?topic=communications-socket-programming)

---

### 17. DDM / DRDA（分散データ管理）

**概要**
IBM の分散データ管理アーキテクチャ。DDM は IBM i ネイティブファイル（物理ファイル・論理ファイル）へのリモートアクセス。DRDA は DDM の拡張で SQL / DB2 への分散アクセス。JDBC/ODBC の内部プロトコルとしても使用される。

**主な用途**
- IBM i システム間のネイティブファイルアクセス
- DB2 分散クエリ
- 主に内部プロトコルとして ODBC/JDBC が上位層で使用

**C# / .NET サポート**
- 通常は ODBC / .NET Data Provider を介して間接的に使用
- 直接 DRDA プロトコルを扱う .NET ライブラリは一般的ではない

**参考 URL**
- [DRDA and DDM Overview - IBM Documentation (IBM i 7.5)](https://www.ibm.com/docs/en/i/7.5.0?topic=programming-drda-ddm-overview)

---

### 13. IBM i RSE API（Remote System Explorer API）

**概要**
IBM i オブジェクト（QSYS オブジェクト・IFS ファイル・CL コマンド・DB ファイル）を REST API で操作できる仕組み。IBM Rational Developer for i（RDi）等のツールが内部で使用。

**主な用途**
- IFS ファイルの読み書き
- CL コマンドのリモート実行
- オブジェクトの参照・操作

**C# / .NET サポート**
- HTTP/REST ベースのため `HttpClient` で利用可能
- 認証設定と RSE API の有効化が必要

**参考 URL**
- [IBM i RSE API Administration and Programming Guide (PDF)](https://www.ibm.com/support/pages/system/files/inline-files/IWS-rseapi.pdf)

---

## 比較一覧表

| 手段 | 主な用途 | C# サポート | 特記事項 |
|------|----------|------------|---------|
| ODBC（ACS ODBC Driver） | DB2 データアクセス（SQL） | ○（System.Data.Odbc） | Windows/Linux/macOS 対応。最も汎用的 |
| .NET Data Provider（IBM.Data.DB2.iSeries） | DB2 データアクセス（SQL） | ○（ネイティブ ADO.NET） | ACS Windows Package 必須。.NET Core は要確認 |
| JDBC / JTOpen | DB・IBM i オブジェクト全般（Java） | △（Java のみ） | RPG 呼び出し・データキュー等も対応。C# には不向き |
| IWS / REST・SOAP API | RPG/COBOL の Web API 化 | ○（HttpClient） | IBM i 側の IWS サーバー設定が必要 |
| SSH / SFTP | コマンド実行・ファイル転送 | ○（SSH.NET ライブラリ） | PASE 環境でのコマンド実行も可能 |
| FTP / FTPS | ファイル転送 | ○（FluentFTP ライブラリ） | セキュリティ面では SFTP 推奨 |
| IBM MQ | 非同期メッセージング | ○（IBM MQ .NET / XMS） | エンタープライズ統合向け。別途 MQ 製品が必要 |
| データキュー | RPG との非同期データ交換 | △（cwbx.dll COM Interop） | IBM i 固有の仕組み。JTOpen（Java）での利用が標準 |
| ActiveX COM（cwbx.dll） | IBM i 固有機能全般（Windows） | ○（COM Interop） | 32bit 制限あり。レガシー寄り |
| 5250 EHLLAPI（RPA） | 画面操作の自動化 | ○（P/Invoke / UiPath 等） | RPA ツールが対応。画面変更に弱い |
| HIS / Transaction Integrator | RPG/COBOL の直接呼び出し | ○（.NET TI クライアント） | Azure Logic Apps IBM i Connector としても利用可能 |
| IBM i Services（SQL Services） | 運用情報取得・CL コマンド実行 | ○（ODBC / .NET Provider 経由） | 画面スクレイピングの代替として有効 |
| ストアドプロシージャ / QCMDEXC | RPG/CL の呼び出し・コマンド実行 | ○（ADO.NET CALL） | 既存資産を最小改修で外部から呼べる |
| OLE DB Provider | DB2 アクセス（レガシー Windows） | ○（System.Data.OleDb） | 新規では ODBC / .NET Provider を推奨 |
| SMB / IBM i NetServer | IFS ファイルの Windows 共有 | ○（System.IO UNC パス） | RPA との相性が良い。認証設定が必要 |
| カスタム TCP/IP ソケット | 独自プロトコル連携 | ○（TcpClient / Socket） | 新規では REST/MQ を優先すること |
| DDM / DRDA | 分散 DB・ネイティブファイルアクセス | △（内部プロトコル） | 通常は ODBC/JDBC の下位プロトコルとして使用 |
| RSE API | IFS・CL コマンド・オブジェクト操作 | ○（HttpClient） | 主に開発ツール向け。本番用途は要確認 |

## 選定指針

- **DB2 データの読み書きのみ**: ODBC か .NET Data Provider が最もシンプル
- **RPG/COBOL プログラムを呼び出したい**: IWS REST API（IBM i 側改修あり）またはストアドプロシージャ / HIS/DPC（改修最小限）
- **CL コマンドをリモート実行したい**: `QSYS2.QCMDEXC`（SQL 経由）または SSH（PASE 経由）
- **ファイル転送・コマンド実行**: SSH/SFTP が現代的で安全
- **Windows/RPA からのファイル連携**: SMB / IBM i NetServer が最もシンプル
- **既存 5250 画面の RPA 自動化**: 5250 + EHLLAPI / UiPath Terminal Activities（改修不要だが変更に弱い）
- **非同期・高信頼性メッセージング**: IBM MQ
- **クラウド（Azure）との連携**: Azure Logic Apps IBM i Program Call コネクター
- **運用情報の取得・監視**: IBM i Services（SQL Services）を ODBC 経由で活用
