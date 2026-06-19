// 12_null安全_CSHARP.cs
// null 安全に関するサンプルコード
// コンソールアプリ（.NET 8 以降を想定）

#nullable enable

using System;

// ───────────────────────────────────────────────
// サンプル 1: null 条件演算子 (?.) と null 合体演算子 (??)
// ───────────────────────────────────────────────
class NullConditionalSamples
{
    // null を返すかもしれないメソッド
    static string? FindUser(int id)
        => id == 1 ? "Alice" : null;

    public static void Run()
    {
        Console.WriteLine("=== ?. と ?? のサンプル ===");

        string? user = FindUser(2); // null が返る

        // ?. を使うと null のときメソッド呼び出しをスキップし null を返す
        int? length = user?.Length;
        Console.WriteLine($"length = {length}"); // length =

        // ?? で null のときの既定値を指定
        string display = user ?? "（ユーザーなし）";
        Console.WriteLine($"display = {display}"); // display = （ユーザーなし）

        // ?. と ?? を組み合わせる
        string upper = user?.ToUpper() ?? "UNKNOWN";
        Console.WriteLine($"upper = {upper}"); // upper = UNKNOWN

        // チェーンすることもできる
        Order? order = null;
        string? city = order?.Address?.City;
        Console.WriteLine($"city = {city ?? "（住所なし）"}");
    }
}

// サポートクラス（NullConditionalSamples 用）
class Order
{
    public Address? Address { get; set; }
}

class Address
{
    public string? City { get; set; }
}

// ───────────────────────────────────────────────
// サンプル 2: null 合体代入演算子 (??=)
// ───────────────────────────────────────────────
class NullCoalescingAssignmentSamples
{
    public static void Run()
    {
        Console.WriteLine("\n=== ??= のサンプル ===");

        // null のときだけ初期化（よくある遅延初期化パターン）
        System.Collections.Generic.List<string>? items = null;
        items ??= new System.Collections.Generic.List<string>();
        items.Add("初期化後に追加");
        Console.WriteLine($"items[0] = {items[0]}");

        // すでに値があれば代入されない
        string? name = "Alice";
        name ??= "Bob"; // 代入されない
        Console.WriteLine($"name = {name}"); // Alice
    }
}

// ───────────────────────────────────────────────
// サンプル 3: Nullable 参照型（#nullable enable）
// ───────────────────────────────────────────────
class NullableReferenceSamples
{
    // 戻り値が null になり得る場合は string? で明示する
    static string? GetMiddleName(bool hasMiddle)
        => hasMiddle ? "Robert" : null;

    // 引数が null を受け入れない場合は string（非 nullable）で受け取る
    static int GetNameLength(string name) => name.Length;

    public static void Run()
    {
        Console.WriteLine("\n=== Nullable 参照型のサンプル ===");

        string? middle = GetMiddleName(false); // null かもしれない

        // null チェックしてから非 nullable を期待するメソッドに渡す
        if (middle != null)
        {
            int len = GetNameLength(middle); // ここでは middle は string 扱い
            Console.WriteLine($"middle name length = {len}");
        }
        else
        {
            Console.WriteLine("middle name なし");
        }

        // null forgiveness 演算子 ! (使用は最小限に)
        // 「自分は null でないと保証できる」という意思表示
        string? value = GetMiddleName(true);
        string definite = value!; // コンパイラ警告を抑制（実際に null なら実行時例外）
        Console.WriteLine($"definite = {definite}");
    }
}

// ───────────────────────────────────────────────
// サンプル 4: パターンマッチングによる null チェック
// ───────────────────────────────────────────────
class PatternMatchingNullSamples
{
    static void Describe(string? input)
    {
        // is null / is not null（C# 9+）
        if (input is null)
        {
            Console.WriteLine("input は null です");
            return;
        }

        if (input is not null)
        {
            // ここでは input は string（null でない）と確定している
            Console.WriteLine($"input の長さ = {input.Length}");
        }

        // switch 式でのパターンマッチング
        string result = input switch
        {
            null               => "null",
            { Length: 0 }      => "空文字",
            { Length: <= 5 }   => "短い文字列",
            _                  => "長い文字列",
        };
        Console.WriteLine($"分類: {result}");
    }

    public static void Run()
    {
        Console.WriteLine("\n=== パターンマッチングによる null チェック ===");
        Describe(null);
        Describe("");
        Describe("Hi");
        Describe("Hello, World!");
    }
}

// ───────────────────────────────────────────────
// サンプル 5: Nullable 値型（int?）
// ───────────────────────────────────────────────
class NullableValueTypeSamples
{
    // DB から取得した年齢（NULL かもしれない）を模擬
    static int? GetAge(bool hasValue)
        => hasValue ? 30 : null;

    public static void Run()
    {
        Console.WriteLine("\n=== Nullable 値型のサンプル ===");

        int? age = GetAge(false);

        // HasValue / Value でアクセス
        if (age.HasValue)
        {
            Console.WriteLine($"年齢: {age.Value}");
        }
        else
        {
            Console.WriteLine("年齢: 不明");
        }

        // ?? で既定値
        int displayAge = age ?? 0;
        Console.WriteLine($"表示用年齢: {displayAge}");

        // Nullable 値型の算術演算（片方が null なら結果も null）
        int? a = 10;
        int? b = null;
        int? sum = a + b; // null
        Console.WriteLine($"10 + null = {sum?.ToString() ?? "null"}");

        // GetValueOrDefault
        int safe = age.GetValueOrDefault(99);
        Console.WriteLine($"GetValueOrDefault(99) = {safe}");
    }
}

// ───────────────────────────────────────────────
// エントリポイント
// ───────────────────────────────────────────────
class Program
{
    static void Main()
    {
        NullConditionalSamples.Run();
        NullCoalescingAssignmentSamples.Run();
        NullableReferenceSamples.Run();
        PatternMatchingNullSamples.Run();
        NullableValueTypeSamples.Run();
    }
}
