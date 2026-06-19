// =============================================================================
// 0: パラダイム概観 - コードサンプル
// =============================================================================
// このファイルでは以下を示します。
//   1. トップレベルステートメント（C# 9+）による Hello World
//   2. 従来のエントリポイント（Main メソッド）との比較
//   3. 名前空間の宣言方法（ブロック形式 vs ファイルスコープ形式）
// =============================================================================

// --- 1. トップレベルステートメント（C# 9+） ---
// Program.cs に Main メソッドを書かなくても、直接処理を書ける。
// コンパイラが自動的に Main メソッドを生成する。
Console.WriteLine("Hello, World!");

// using も不要（グローバル using が暗黙的に適用される .NET 6+）
// ただし明示的に書くことも可能:
// using System;

// エントリポイントに引数を受け取る場合は args を使う（自動的に利用可能）
if (args.Length > 0)
{
    Console.WriteLine($"引数: {string.Join(", ", args)}");
}

// --- サンプルクラスを呼び出す ---
ProgramSamples.RunAll();

// =============================================================================
// 従来のエントリポイントとの比較（コメントとして掲載）
// =============================================================================
// 従来（C# 8 以前）の書き方:
//
// using System;
//
// namespace MyApp
// {
//     class Program
//     {
//         static void Main(string[] args)
//         {
//             Console.WriteLine("Hello, World!");
//         }
//     }
// }
//
// トップレベルステートメントを使うと上記の定型コードが不要になる。
// 1 プロジェクトにつきトップレベルステートメントを持てるのは 1 ファイルのみ。

// =============================================================================
// 名前空間の宣言方法
// =============================================================================

// ---- ブロック形式（C# 9 以前から使われる従来の書き方） ----
namespace Traditional
{
    class OldStyleClass
    {
        // クラスの内容はここに書く
        public void Greet() => Console.WriteLine("ブロック形式の名前空間");
    }
}

// ---- ファイルスコープ形式（C# 10+。ファイル全体に名前空間が適用される） ----
// ファイル内で 1 回だけ宣言でき、セミコロンで終わる。
// インデントが 1 段少なくなるため、コードが読みやすくなる。
//
// namespace MyApp.Samples;  // ← ファイルスコープ形式（このファイルでは使えないためコメント）

// =============================================================================
// サンプル実行クラス
// =============================================================================
// このファイルのサンプルクラスは Global 名前空間（名前空間なし）に属する。
// 実用コードでは必ず適切な名前空間を付ける。

/// <summary>
/// このファイルのサンプルをまとめて実行するクラス。
/// </summary>
class ProgramSamples
{
    public static void RunAll()
    {
        Console.WriteLine();
        Console.WriteLine("=== C# パラダイム概観 サンプル ===");

        // 静的型付けの例
        DemonstrateStaticTyping();

        // オブジェクト指向の例
        DemonstrateOOP();

        // 関数型スタイルの例
        DemonstrateFunctionalStyle();
    }

    // 静的型付け: 変数の型はコンパイル時に決まる
    static void DemonstrateStaticTyping()
    {
        Console.WriteLine("\n--- 静的型付け ---");

        int count = 10;              // int 型として確定
        string message = "Hello";   // string 型として確定
        var inferred = 3.14;        // double 型に推論される（var = 型推論。動的型付けではない）

        // count = "text";  // コンパイルエラー: int に string は代入できない

        Console.WriteLine($"count={count}, message={message}, inferred={inferred}");
        Console.WriteLine($"inferred の型: {inferred.GetType().Name}"); // → Double
    }

    // オブジェクト指向: クラスとインターフェース
    static void DemonstrateOOP()
    {
        Console.WriteLine("\n--- オブジェクト指向 ---");

        // インターフェースを介してオブジェクトを扱う（ポリモーフィズム）
        IGreeter greeter = new JapaneseGreeter();
        greeter.Greet("World");

        greeter = new EnglishGreeter();
        greeter.Greet("World");
    }

    // 関数型スタイル: ラムダ式と LINQ
    static void DemonstrateFunctionalStyle()
    {
        Console.WriteLine("\n--- 関数型スタイル（ラムダ式・LINQ） ---");

        int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // LINQ: 偶数だけ抽出して 2 倍にし、合計を求める
        int result = numbers
            .Where(n => n % 2 == 0)   // フィルタ（ラムダ式）
            .Select(n => n * 2)        // 変換
            .Sum();                    // 集計

        Console.WriteLine($"偶数 × 2 の合計: {result}"); // → 60
    }
}

// --- ポリモーフィズムのデモ用インターフェース ---
interface IGreeter
{
    void Greet(string name);
}

class JapaneseGreeter : IGreeter
{
    public void Greet(string name) => Console.WriteLine($"こんにちは、{name}！");
}

class EnglishGreeter : IGreeter
{
    public void Greet(string name) => Console.WriteLine($"Hello, {name}!");
}
