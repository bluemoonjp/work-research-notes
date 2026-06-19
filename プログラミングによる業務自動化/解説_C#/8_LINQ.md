# LINQ (Language Integrated Query)

## この記事で学ぶこと

- LINQ の概要とできること
- クエリ構文とメソッド構文の違いと使い分け
- よく使う主要メソッド（Where / Select / GroupBy / Join など）
- 遅延評価（Deferred Execution）の仕組みと落とし穴
- `IEnumerable<T>` と `IQueryable<T>` の違い

## LINQ とは

LINQ（Language Integrated Query）は、コレクション・データベース・XML などさまざまなデータソースを統一した構文でクエリできる C# の機能です。  
`using System.Linq;` を追加するだけで、配列・`List<T>` などの `IEnumerable<T>` に対してクエリが使えます。

```csharp
using System.Linq;
```

## クエリ構文 vs メソッド構文

LINQ には **クエリ構文**（SQL に似た書き方）と **メソッド構文**（ラムダ式を使う書き方）の 2 種類があります。

| | クエリ構文 | メソッド構文 |
|---|---|---|
| 可読性 | SQL に慣れた人には直感的 | ラムダ式に慣れると簡潔 |
| 表現力 | `group by` / `join` は記述しやすい | すべての LINQ メソッドに対応 |
| 実用場面 | 複雑な結合・グループ化 | 単純なフィルタ・変換 |

**クエリ構文**

```csharp
var result = from p in products
             where p.Price > 1000
             orderby p.Name
             select p.Name;
```

**メソッド構文（同等）**

```csharp
var result = products
    .Where(p => p.Price > 1000)
    .OrderBy(p => p.Name)
    .Select(p => p.Name);
```

実際のコードでは**メソッド構文が主流**です。`GroupBy` や `Join` を含む複雑なクエリではクエリ構文が読みやすい場合があります。

## 主要メソッド

### フィルタリング

| メソッド | 説明 |
|---|---|
| `Where(predicate)` | 条件に一致する要素を返す |
| `Distinct()` | 重複を除いた要素を返す |
| `Take(n)` | 先頭 n 件を返す |
| `Skip(n)` | 先頭 n 件をスキップして残りを返す |

### 変換・射影

| メソッド | 説明 |
|---|---|
| `Select(selector)` | 各要素を別の型・値に変換する |
| `SelectMany(selector)` | ネストしたコレクションを平坦化する |

### 並べ替え

| メソッド | 説明 |
|---|---|
| `OrderBy(key)` | 昇順に並べ替える |
| `OrderByDescending(key)` | 降順に並べ替える |
| `ThenBy(key)` | 第 2 ソートキーを指定する |

### グループ化・結合

| メソッド | 説明 |
|---|---|
| `GroupBy(key)` | キーでグループ化する |
| `Join(...)` | 2 つのコレクションを内部結合する |
| `GroupJoin(...)` | 左外部結合に相当する |

### 集計・検索

| メソッド | 説明 |
|---|---|
| `Count()` / `Count(predicate)` | 要素数を返す |
| `Sum(selector)` | 合計を返す |
| `Min(selector)` / `Max(selector)` | 最小値・最大値を返す |
| `Average(selector)` | 平均を返す |
| `Any(predicate)` | 条件に合う要素が 1 つでもあれば `true` |
| `All(predicate)` | すべての要素が条件を満たせば `true` |
| `First(predicate)` | 最初の要素（なければ例外） |
| `FirstOrDefault(predicate)` | 最初の要素（なければ `default`） |
| `Single(predicate)` | 1 件だけの要素（0 件・2 件以上で例外） |

### マテリアライズ

| メソッド | 説明 |
|---|---|
| `ToList()` | `List<T>` に変換して即時評価 |
| `ToArray()` | 配列に変換して即時評価 |
| `ToDictionary(keySelector)` | `Dictionary<TKey,TValue>` に変換 |

## 遅延評価（Deferred Execution）

LINQ クエリは **定義した時点では実行されません**。`foreach` でイテレートするか `ToList()` などで明示的に評価するまで、クエリは実行されません。

```csharp
var query = products.Where(p => p.Price > 1000); // この時点では未実行

products.Add(new Product { Name = "新製品", Price = 2000 }); // データを追加

foreach (var p in query) // ここで初めてクエリが実行される
{
    Console.WriteLine(p.Name); // 新製品も含まれる
}
```

### よくある落とし穴

**落とし穴 1: 複数回イテレートすると毎回クエリが実行される**

```csharp
var query = products.Where(p => p.Price > 1000);

int count = query.Count();   // 1 回目の評価
var list  = query.ToList();  // 2 回目の評価（DB クエリなら 2 回発行される）

// 対策: 一度 ToList() してから使い回す
var cached = products.Where(p => p.Price > 1000).ToList();
```

**落とし穴 2: ループ変数のキャプチャ**

```csharp
var queries = new List<IEnumerable<int>>();
for (int i = 0; i < 3; i++)
{
    queries.Add(Enumerable.Range(0, 5).Where(x => x == i)); // i はループ終了後の値を参照
}
// queries を評価するとき i はすでに 3
```

## IEnumerable<T> vs IQueryable<T>

| | `IEnumerable<T>` | `IQueryable<T>` |
|---|---|---|
| 名前空間 | `System.Collections.Generic` | `System.Linq` |
| 処理場所 | メモリ（インプロセス） | データソース側（DB など） |
| 典型的な使途 | オブジェクトコレクション | Entity Framework などの ORM |
| Where の動作 | C# で評価 | SQL に変換して DB で評価 |

`IQueryable<T>` を使うと `Where` などの条件が SQL に変換されて DB 側で絞り込まれるため、大量データを扱う場合に `IEnumerable<T>` にキャストしてから LINQ を使うとパフォーマンスが大きく落ちます。

```csharp
// NG: DB から全件取得してからメモリ上でフィルタ
var result = dbContext.Products.AsEnumerable().Where(p => p.Price > 1000);

// OK: DB 側でフィルタして必要な件数だけ取得
var result = dbContext.Products.Where(p => p.Price > 1000);
```

## コードサンプル

→ [`8_LINQ_CSHARP.cs`](8_LINQ_CSHARP.cs) を参照してください。

## 参考リンク

- [LINQ (C#) - Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/csharp/linq/)
- [標準クエリ演算子の概要 - Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/csharp/linq/standard-query-operators/)
- [Enumerable クラス - Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/api/system.linq.enumerable)
