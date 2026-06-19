using System;
using System.Collections.Generic;
using System.Linq;

// ========================================
// LINQ サンプルコード
// ========================================

// ---- データモデル ----
record Product(string Name, string Category, decimal Price, int Stock);
record Order(int Id, string ProductName, int Quantity);

class LinqSamples
{
    static readonly List<Product> Products = new()
    {
        new("りんご",   "果物", 150,  100),
        new("バナナ",   "果物",  80,  200),
        new("にんじん", "野菜",  60,  150),
        new("ほうれん草","野菜", 120,   80),
        new("牛乳",     "乳製品",180,   50),
        new("チーズ",   "乳製品",350,   30),
        new("オレンジ", "果物", 200,   70),
    };

    static void Main()
    {
        QuerySyntaxVsMethodSyntax();
        WhereAndSelect();
        OrderByAndGroupBy();
        AggregationMethods();
        FirstOrDefaultPattern();
        DeferredExecutionDemo();
        JoinDemo();
    }

    // ----------------------------------------
    // 1. クエリ構文 vs メソッド構文
    // ----------------------------------------
    static void QuerySyntaxVsMethodSyntax()
    {
        Console.WriteLine("=== クエリ構文 vs メソッド構文 ===");

        // クエリ構文: SQL に近い書き方
        var queryStyle =
            from p in Products
            where p.Price > 100
            orderby p.Price descending
            select p.Name;

        // メソッド構文: 同等のクエリ（実務では主流）
        var methodStyle = Products
            .Where(p => p.Price > 100)
            .OrderByDescending(p => p.Price)
            .Select(p => p.Name);

        Console.WriteLine("クエリ構文:");
        foreach (var name in queryStyle) Console.WriteLine($"  {name}");

        Console.WriteLine("メソッド構文 (同じ結果):");
        foreach (var name in methodStyle) Console.WriteLine($"  {name}");
    }

    // ----------------------------------------
    // 2. Where / Select
    // ----------------------------------------
    static void WhereAndSelect()
    {
        Console.WriteLine("\n=== Where / Select ===");

        // Where: 条件フィルタ
        var expensive = Products.Where(p => p.Price >= 150);
        Console.WriteLine("150円以上:");
        foreach (var p in expensive) Console.WriteLine($"  {p.Name} {p.Price}円");

        // Select: 射影（型変換）
        var summary = Products
            .Where(p => p.Category == "果物")
            .Select(p => new { p.Name, Total = p.Price * p.Stock }); // 匿名型へ変換

        Console.WriteLine("果物の在庫金額:");
        foreach (var s in summary) Console.WriteLine($"  {s.Name}: {s.Total}円");

        // SelectMany: ネストを平坦化
        var tags = new List<List<string>>
        {
            new() { "C#", ".NET" },
            new() { "LINQ", "ラムダ" },
        };
        var flat = tags.SelectMany(t => t); // IEnumerable<string> に平坦化
        Console.WriteLine("タグ一覧: " + string.Join(", ", flat));
    }

    // ----------------------------------------
    // 3. OrderBy / GroupBy
    // ----------------------------------------
    static void OrderByAndGroupBy()
    {
        Console.WriteLine("\n=== OrderBy / GroupBy ===");

        // OrderBy + ThenBy: 複数キーでソート
        var sorted = Products
            .OrderBy(p => p.Category)
            .ThenByDescending(p => p.Price);

        Console.WriteLine("カテゴリ昇順 → 価格降順:");
        foreach (var p in sorted)
            Console.WriteLine($"  [{p.Category}] {p.Name} {p.Price}円");

        // GroupBy: カテゴリ別に集計
        var grouped = Products
            .GroupBy(p => p.Category)
            .Select(g => new
            {
                Category = g.Key,
                Count    = g.Count(),
                AvgPrice = g.Average(p => p.Price),
            });

        Console.WriteLine("\nカテゴリ別集計:");
        foreach (var g in grouped)
            Console.WriteLine($"  {g.Category}: {g.Count}件, 平均{g.AvgPrice:F0}円");
    }

    // ----------------------------------------
    // 4. 集計メソッド（Count / Sum / Any / All）
    // ----------------------------------------
    static void AggregationMethods()
    {
        Console.WriteLine("\n=== 集計メソッド ===");

        int total       = Products.Count();
        int fruitCount  = Products.Count(p => p.Category == "果物");
        decimal sumAll  = Products.Sum(p => p.Price);
        decimal maxPrice = Products.Max(p => p.Price);

        Console.WriteLine($"総件数: {total}, 果物件数: {fruitCount}");
        Console.WriteLine($"価格合計: {sumAll}円, 最高値: {maxPrice}円");

        bool anyExpensive = Products.Any(p => p.Price > 300);  // 1 つでも条件を満たすか
        bool allPositive  = Products.All(p => p.Price > 0);    // すべて条件を満たすか
        Console.WriteLine($"300円超あり: {anyExpensive}, 全品正値: {allPositive}");

        // ToDictionary: キーが重複するとキー重複例外になるので注意
        var dict = Products.ToDictionary(p => p.Name, p => p.Price);
        Console.WriteLine($"りんごの価格: {dict["りんご"]}円");
    }

    // ----------------------------------------
    // 5. FirstOrDefault と null チェック
    // ----------------------------------------
    static void FirstOrDefaultPattern()
    {
        Console.WriteLine("\n=== FirstOrDefault / null チェック ===");

        // First: 見つからなければ InvalidOperationException
        // FirstOrDefault: 見つからなければ null（参照型）または default（値型）
        Product? found = Products.FirstOrDefault(p => p.Name == "牛乳");
        if (found is not null)
            Console.WriteLine($"見つかった: {found.Name} {found.Price}円");

        Product? notFound = Products.FirstOrDefault(p => p.Name == "存在しない商品");
        // null チェックしないと NullReferenceException になる
        Console.WriteLine($"存在しない商品: {notFound?.Name ?? "(null)"}");

        // C# 6 以降の null 条件演算子で安全にアクセス
        decimal? price = notFound?.Price; // notFound が null なら null
        Console.WriteLine($"価格: {price?.ToString() ?? "N/A"}");
    }

    // ----------------------------------------
    // 6. 遅延評価（Deferred Execution）
    // ----------------------------------------
    static void DeferredExecutionDemo()
    {
        Console.WriteLine("\n=== 遅延評価 ===");

        var list = new List<int> { 1, 2, 3, 4, 5 };

        // クエリを定義した時点では実行されない
        var query = list.Where(x =>
        {
            Console.WriteLine($"  評価中: {x}");
            return x % 2 == 0;
        });

        Console.WriteLine("クエリ定義直後 (まだ実行されていない)");

        list.Add(6); // クエリ評価前にデータを追加

        Console.WriteLine("foreach でイテレート開始:");
        foreach (var x in query) // ここで初めて評価される
            Console.WriteLine($"  結果: {x}");
        // → 6 も含まれる

        // ToList() で即時評価してキャッシュ
        Console.WriteLine("\nToList() で即時評価:");
        var snapshot = list.Where(x => x % 2 == 0).ToList();
        list.Add(8); // スナップショット後に追加しても影響しない
        Console.WriteLine($"スナップショット件数: {snapshot.Count}"); // 8 は含まれない
    }

    // ----------------------------------------
    // 7. Join（内部結合）
    // ----------------------------------------
    static void JoinDemo()
    {
        Console.WriteLine("\n=== Join ===");

        var orders = new List<Order>
        {
            new(1, "りんご",   3),
            new(2, "牛乳",     2),
            new(3, "存在しない", 1), // Products に存在しない（内部結合では除外される）
            new(4, "チーズ",   5),
        };

        // Join: 内部結合（両方に存在するものだけ）
        var joined = orders.Join(
            Products,
            order   => order.ProductName,  // 外部キー（orders 側）
            product => product.Name,        // 内部キー（products 側）
            (order, product) => new         // 結果の射影
            {
                order.Id,
                order.ProductName,
                order.Quantity,
                product.Price,
                Total = order.Quantity * product.Price,
            });

        Console.WriteLine("注文明細:");
        foreach (var j in joined)
            Console.WriteLine($"  #{j.Id} {j.ProductName} × {j.Quantity} = {j.Total}円");
    }
}
