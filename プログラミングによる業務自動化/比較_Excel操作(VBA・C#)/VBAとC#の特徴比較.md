# VBA と C# の特徴比較

「VBA は簡単、C# は難しい」というイメージがある。  
しかし「導入コストの低さ」と「処理の書きやすさ」は別の話だ。

この記事では [Excel操作を比較する](Excel操作を比較する/概要.md) のサンプルコードを題材に、
両言語の特徴を横断的に整理する。
各プログラムの実装詳細は [言語別解説.md](Excel操作を比較する/言語別解説.md) を参照。

---

## VBA の強み

VBA がうまく機能する場面は確かにある。

| 強み | 内容 |
|------|------|
| Excel 統合 | ファイルパス・ライブラリのインストールが不要 |
| セル操作の直感性 | `.Cells(行, 列).Value` で即アクセス |
| 実行の手軽さ | VBE ですぐ実行できる（コンパイル不要） |
| 配布コスト ゼロ | Excel ユーザーなら Runtime が既に存在する |

シンプルな読み書きや、一度きりの単発マクロであれば VBA の導入コストの低さは大きな強みになる。

---

## C# が扱いやすい点

### 1. データの抽象化 — インデックスから名前へ

VBA では Excel から読み込んだデータを配列のインデックスで管理する。

```vba
' VBA: 情報(5) が何の値かはコメントを読まないと分からない
情報(5) = CDbl(ws.Cells(i, 7).Value)   ' 単品原価
情報(6) = CDbl(ws.Cells(i, 8).Value)   ' 単品売価

' 別の関数で使うとき
原価 = CDbl(m(5))   ' (5) が単品原価だと覚えていないと読めない
売価 = CDbl(m(6))
```

C# では `record` で名前付きのデータ型を定義する。

```csharp
// C#: 型定義を一度書けば、どこでも名前でアクセスできる
record ProductMaster(
    string   商品コード,
    string   商品名,
    decimal  単品原価,
    decimal  単品売価
);

// 別のクラスで使うとき
原価 = m.単品原価;   // 名前が自己説明的
売価 = m.単品売価;
```

数値インデックスはコメントが唯一の手がかりだが、コメントはコードと乖離する。  
名前付きプロパティはコードそのものが意味を持つ。

---

### 2. LINQ が複雑なロジックを数行にまとめる

プログラム 2（Join + GroupBy + ソート）を例に比較する。

**VBA の実装規模:**

| 処理 | 実装方法 | おおよその行数 |
|------|----------|----------------|
| Join | Dictionary ルックアップ + 存在チェック | 〜20行 |
| GroupBy | 集計用 Dictionary + 手動累積 + `dic(key) = 既存` の取り出し戻し | 〜25行 |
| Sort | バブルソート（二重ループ + 行の入れ替え） | 〜20行 |
| **合計** | | **〜65行（ロジック部分のみ）** |

**C# の実装規模:**

```csharp
// Join + GroupBy + 集計 + ソートをひとつのチェーンで記述
var result = orders
    .Join(masters,
          o => o.商品コード,
          m => m.商品コード,
          (o, m) => new JoinedOrder(...))          // Join
    .GroupBy(r => new { 販売年月 = r.注文日.ToString("yyyy/MM"), r.商品コード })  // GroupBy
    .Select(g => new SalesSummary
    {
        売上金額 = g.Sum(r => (decimal)r.数量 * r.単品売価),
        利益額   = 売上金額 - 原価,
        利益率   = 売上金額 > 0 ? 利益額 / 売上金額 : 0m
    })
    .OrderBy(r => r.販売年月)                      // 販売年月 昇順
    .ThenByDescending(r => r.利益額)              // 利益額 降順
    .ToList();
```

「何をするか」だけを宣言すれば「どうするか」は LINQ が担う。  
バブルソートのような低レベルの実装を自分で書く必要がない。

---

### 3. 変数スコープが関数に閉じる

VBA の `Dim` は **サブルーチン全体** がスコープになる。ブロックスコープはない。  
そのため、ループの中で使う変数も Sub の先頭に宣言しなければならない。

```vba
' VBA: Sub の先頭にすべての Dim が集まる
Sub 実行_ジョイントグループ化()
    Dim wsマスタ    As Worksheet
    Dim ws注文      As Worksheet
    Dim dic集計     As Object
    Dim 注文コード  As String
    Dim 販売年月    As String
    Dim 集計キー    As String
    Dim 売上金額    As Double
    Dim 原価合計    As Double
    Dim 一時行(9)   As Variant
    '... 20行以上続く
End Sub
```

C# では各メソッド・クラスが独自のスコープを持ち、変数はそのメソッド内だけで宣言する。

```csharp
// C#: ジョイントグループ化の「エントリポイント」が読める範囲は 5 変数だけ
static void Main()
{
    var dicマスタ = マスタをDicに読み込む(workbook);   // 5変数
    var dic集計   = 注文を集計してDicに格納する(workbook, dicマスタ);
    var データ    = DicをDataに変換する(dic集計, 件数);
    バブルソート(データ, 件数);
    集計データを書き込む(ws出力, データ, 件数);
}

// 「マスタをDicに読み込む」メソッドに進んで初めてそこの変数が登場する
private static Dictionary<...> マスタをDicに読み込む(XLWorkbook workbook)
{
    var records = new List<ProductMaster>();   // ここだけの変数
    ...
}
```

コードを読む範囲が自然に限定される。

---

### 4. データソースが変わっても同じロジックが使える

VBA のコードは Excel シートに強く依存している。  
データの読み込みと処理ロジックが一体化しているため、データソースが変わると大幅な書き直しが必要になる。

```
VBA の依存関係:
  Excelシート → For ループ → If 条件 → 出力シート
  （すべてが密結合）
```

C# では「読み込み → データ型 → LINQ処理」という層に分かれている。

```
C# の依存関係:
  Excelシート
      ↓ MasterSheetReader（ここだけ変える）
  List<ProductMaster>
      ↓ LINQ（変えない）
  List<SalesSummary>
      ↓ SheetWriter（ここだけ変える）
  出力シート
```

Reader 層を差し替えるだけで、同じ LINQ ロジックが DB・CSV・API のデータにも使える。  
たとえば `業務システムからExcelダウンロードして集計` では Playwright でブラウザからデータを取得しているが、  
ダウンロード後は `SalesRecord` という record に変換してしまえば、集計コードは Excel 直読みの場合とまったく同じになる。

---

### 5. コードの再利用性

プログラム 4（1〜3 まとめ）でその差が如実に現れる。

**VBA:**  
プログラム 4 を「1 ファイルで完結」させるには、プログラム 1〜3 のロジックをそのままコピーするしかない。  
外部モジュールに分けると VBE へのインポート手順が利用者に求められる。

```vba
' VBA の 4_1から3まとめ_VBA.bas:
' P1〜P3 の全コードがこのファイルにも複製されている
Private Sub P1_実行()  ' 1_単純なWhere条件_VBA.bas と同じロジック
    ...
End Sub
```

**C#:**  
プログラム 1〜4 は同じ `record ProductMaster` 定義を共有する。  
プログラム 4 は各プログラムの処理クラスをそのまま呼び出すだけで済む。

```csharp
// C# の 4_1から3まとめ_CSHARP.cs:
// Prog1Processor, Prog2Processor, Prog3Processor は同じ record 定義の上で動く
static void Main()
{
    var masters = MasterSheetReader.Read(workbook);   // 1 度だけ読む
    var orders  = OrderSheetReader.Read(workbook);

    Prog1Processor.Run(workbook, masters);             // 既存クラスをそのまま呼ぶ
    Prog2Processor.Run(workbook, masters, orders);
    Prog3Processor.Run(workbook, masters);
}
```

---

## まとめ

「複雑な処理を書くときにどちらが楽か」という観点では、C# の方が扱いやすいことが多い。  
VBA の「Excel から直接動く」という特性は導入コストを下げるが、  
同時にデータとロジックが密結合になり、処理が複雑になるほどコードが読みにくくなる。

| 状況 | 向いている言語 |
|------|----------------|
| シンプルな読み書き・単純集計 | VBA（導入コストの低さが活きる） |
| 複雑なデータ変換・多段集計 | C# |
| 複数データソースの統合 | C# |
| 長期メンテ・チーム共有 | C# |
| Excel ユーザーへの単発マクロ配布 | VBA |
