// 13_コレクション_CSHARP.cs
// コレクションに関するサンプルコード
// コンソールアプリ（.NET 8 以降を想定）

using System;
using System.Collections.Generic;
using System.Linq;

// ───────────────────────────────────────────────
// サンプル 1: 配列（T[]）の基本操作
// ───────────────────────────────────────────────
class ArraySamples
{
    public static void Run()
    {
        Console.WriteLine("=== 配列 ===");

        // 宣言と初期化
        int[] numbers = { 5, 3, 8, 1, 9, 2 };

        // インデックスアクセス
        Console.WriteLine($"先頭: {numbers[0]}, 末尾: {numbers[^1]}"); // ^1 は末尾から 1 番目（C# 8+）

        // 長さ
        Console.WriteLine($"長さ: {numbers.Length}");

        // ソート（元の配列を変更する）
        Array.Sort(numbers);
        Console.WriteLine($"ソート後: [{string.Join(", ", numbers)}]");

        // 2 次元配列
        int[,] matrix = { { 1, 2 }, { 3, 4 } };
        Console.WriteLine($"matrix[1,1] = {matrix[1, 1]}");

        // ジャグ配列（行ごとに長さが異なる）
        int[][] jagged = new int[3][];
        jagged[0] = new int[] { 1 };
        jagged[1] = new int[] { 2, 3 };
        jagged[2] = new int[] { 4, 5, 6 };
        Console.WriteLine($"jagged[2][2] = {jagged[2][2]}");
    }
}

// ───────────────────────────────────────────────
// サンプル 2: List<T> の基本操作
// ───────────────────────────────────────────────
class ListSamples
{
    public static void Run()
    {
        Console.WriteLine("\n=== List<T> ===");

        var fruits = new List<string> { "Apple", "Banana", "Cherry" };

        // 追加
        fruits.Add("Date");
        fruits.AddRange(new[] { "Elderberry", "Fig" });

        // 挿入・削除
        fruits.Insert(1, "Avocado"); // インデックス 1 に挿入
        fruits.Remove("Banana");     // 最初に見つかった要素を削除
        fruits.RemoveAt(0);          // インデックス 0 の要素を削除

        // 検索
        bool has = fruits.Contains("Cherry");
        int idx  = fruits.IndexOf("Fig");
        Console.WriteLine($"Cherry あり: {has}, Fig のインデックス: {idx}");

        // ソート
        fruits.Sort();
        Console.WriteLine($"ソート: [{string.Join(", ", fruits)}]");

        // Count（Length ではなく Count）
        Console.WriteLine($"要素数: {fruits.Count}");

        // foreach
        Console.Write("全要素: ");
        foreach (var f in fruits)
            Console.Write(f + " ");
        Console.WriteLine();
    }
}

// ───────────────────────────────────────────────
// サンプル 3: Dictionary<TKey, TValue>
// ───────────────────────────────────────────────
class DictionarySamples
{
    public static void Run()
    {
        Console.WriteLine("\n=== Dictionary<TKey, TValue> ===");

        // コレクション初期化子
        var scores = new Dictionary<string, int>
        {
            ["Alice"] = 90,
            ["Bob"]   = 75,
            ["Carol"] = 85,
        };

        // 追加・上書き
        scores["Dave"]  = 80;
        scores["Alice"] = 95; // 上書き

        // 安全な取得（キーがなくても例外にならない）
        if (scores.TryGetValue("Eve", out int eveScore))
            Console.WriteLine($"Eve: {eveScore}");
        else
            Console.WriteLine("Eve は存在しません");

        // キー・値の列挙
        foreach (var (name, score) in scores)
            Console.WriteLine($"  {name}: {score}");

        // キー一覧・値一覧
        Console.WriteLine($"キー数: {scores.Keys.Count}");

        // キーの存在確認
        Console.WriteLine($"Bob 存在: {scores.ContainsKey("Bob")}");

        // 削除
        scores.Remove("Bob");
        Console.WriteLine($"Bob 削除後: {scores.ContainsKey("Bob")}");
    }
}

// ───────────────────────────────────────────────
// サンプル 4: HashSet<T> による重複排除
// ───────────────────────────────────────────────
class HashSetSamples
{
    public static void Run()
    {
        Console.WriteLine("\n=== HashSet<T> ===");

        // 重複を含むリストから重複なしコレクションを作る
        var tags = new List<string> { "C#", "LINQ", "C#", ".NET", "LINQ", "C#" };
        var uniqueTags = new HashSet<string>(tags);
        Console.WriteLine($"元のリスト: {tags.Count} 件 → ユニーク: {uniqueTags.Count} 件");
        Console.WriteLine($"  [{string.Join(", ", uniqueTags)}]");

        // 高速な存在チェック（O(1)）
        Console.WriteLine($"C# 含む: {uniqueTags.Contains("C#")}");
        Console.WriteLine($"Java 含む: {uniqueTags.Contains("Java")}");

        // 集合演算
        var setA = new HashSet<int> { 1, 2, 3, 4 };
        var setB = new HashSet<int> { 3, 4, 5, 6 };

        // コピーを作って演算（元の setA を変えないため）
        var intersect = new HashSet<int>(setA);
        intersect.IntersectWith(setB);         // 積集合: {3, 4}

        var union = new HashSet<int>(setA);
        union.UnionWith(setB);                 // 和集合: {1,2,3,4,5,6}

        var except = new HashSet<int>(setA);
        except.ExceptWith(setB);               // 差集合: {1, 2}

        Console.WriteLine($"積集合: [{string.Join(", ", intersect)}]");
        Console.WriteLine($"和集合: [{string.Join(", ", union)}]");
        Console.WriteLine($"差集合: [{string.Join(", ", except)}]");
    }
}

// ───────────────────────────────────────────────
// サンプル 5: コレクション初期化子 + LINQ
// ───────────────────────────────────────────────
class CollectionInitializerAndLinqSamples
{
    record Person(string Name, int Age); // C# 9+ record

    public static void Run()
    {
        Console.WriteLine("\n=== コレクション初期化子 + LINQ ===");

        // コレクション初期化子
        var people = new List<Person>
        {
            new("Alice", 30),
            new("Bob",   25),
            new("Carol", 35),
            new("Dave",  28),
        };

        // LINQ: フィルタ・変換・ソート
        var result = people
            .Where(p => p.Age >= 28)           // 28 歳以上に絞り込み
            .OrderBy(p => p.Age)               // 年齢順にソート
            .Select(p => $"{p.Name}({p.Age})") // 文字列に変換
            .ToList();

        Console.WriteLine("28 歳以上（年齢順）:");
        result.ForEach(s => Console.WriteLine($"  {s}"));

        // 集計
        double avg = people.Average(p => p.Age);
        int    max = people.Max(p => p.Age);
        Console.WriteLine($"平均年齢: {avg}, 最大年齢: {max}");

        // グループ化
        var byAge = people
            .GroupBy(p => p.Age >= 30 ? "30歳以上" : "30歳未満")
            .ToDictionary(g => g.Key, g => g.Select(p => p.Name).ToList());

        foreach (var (key, names) in byAge)
            Console.WriteLine($"  {key}: {string.Join(", ", names)}");

        // C# 12+ コレクション式（配列への代入）
        int[] primes = [2, 3, 5, 7, 11];
        int[] evens  = [2, 4, 6];
        int[] odds   = [1, 3, 5];
        int[] merged = [..evens, ..odds]; // スプレッド
        Console.WriteLine($"primes: [{string.Join(", ", primes)}]");
        Console.WriteLine($"merged: [{string.Join(", ", merged)}]");
    }
}

// ───────────────────────────────────────────────
// エントリポイント
// ───────────────────────────────────────────────
class Program
{
    static void Main()
    {
        ArraySamples.Run();
        ListSamples.Run();
        DictionarySamples.Run();
        HashSetSamples.Run();
        CollectionInitializerAndLinqSamples.Run();
    }
}
