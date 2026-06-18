// 15_拡張メソッドとusing_CSHARP.cs
// 拡張メソッドと using に関するサンプルコード
// コンソールアプリ（.NET 8 以降を想定）

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// ───────────────────────────────────────────────
// サンプル 1: 拡張メソッドの定義と使用
// ───────────────────────────────────────────────

// 拡張メソッドは static class の中に定義する
static class StringExtensions
{
    // string? に null チェック付きの IsNullOrEmpty を追加
    public static bool IsNullOrEmpty(this string? value)
        => string.IsNullOrEmpty(value);

    // 文字列を指定文字数で切り詰める（超過時は末尾に "…" を追加）
    public static string TruncateAt(this string value, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }

    // 文字列をタイトルケースに変換（英語の単語先頭を大文字化）
    public static string ToTitleCase(this string value)
        => string.Join(" ", value.Split(' ')
            .Select(word => word.Length == 0 ? word : char.ToUpper(word[0]) + word[1..].ToLower()));
}

static class EnumerableExtensions
{
    // IEnumerable<T> に「重複を除いたリスト」を返す拡張メソッドを追加
    public static List<T> ToUniqueList<T>(this IEnumerable<T> source)
        => source.Distinct().ToList();

    // null 要素を除外して返す
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        => source.Where(x => x is not null).Select(x => x!);
}

class ExtensionMethodSamples
{
    public static void Run()
    {
        Console.WriteLine("=== 拡張メソッドのサンプル ===");

        // StringExtensions の使用
        string? nullStr = null;
        Console.WriteLine($"nullStr.IsNullOrEmpty() = {nullStr.IsNullOrEmpty()}");  // true

        string title = "hello world from csharp";
        Console.WriteLine($"TruncateAt(10) = '{title.TruncateAt(10)}'");           // hello wor…
        Console.WriteLine($"ToTitleCase()  = '{title.ToTitleCase()}'");            // Hello World From Csharp

        // EnumerableExtensions の使用
        var tags = new List<string?> { "C#", "LINQ", "C#", null, ".NET", "LINQ" };
        var unique = tags.WhereNotNull().ToUniqueList();
        Console.WriteLine($"重複・null 除去: [{string.Join(", ", unique)}]");

        // 拡張メソッドはメソッドチェーンに自然に組み込める
        var result = new[] { "  hello  ", "  world  ", null, "  csharp  " }
            .WhereNotNull()
            .Select(s => s.Trim())
            .Select(s => s.ToTitleCase())
            .ToList();
        Console.WriteLine($"チェーン結果: [{string.Join(", ", result)}]");
    }
}

// ───────────────────────────────────────────────
// サンプル 2: IDisposable の実装
// ───────────────────────────────────────────────

// アンマネージドリソースを保持するクラスの IDisposable 実装パターン
class ManagedResource : IDisposable
{
    private bool _disposed = false;
    private readonly string _name;

    // 内部で Stream 等のリソースを持つことを想定（ここでは文字列で代替）
    public ManagedResource(string name)
    {
        _name = name;
        Console.WriteLine($"  [{_name}] 生成");
    }

    public void DoWork()
    {
        if (_disposed) throw new ObjectDisposedException(_name);
        Console.WriteLine($"  [{_name}] 処理中...");
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this); // ファイナライザの呼び出しを抑制
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // マネージドリソース（他の IDisposable 等）を解放
            Console.WriteLine($"  [{_name}] Dispose 呼び出し（マネージドリソース解放）");
        }
        // アンマネージドリソースがあればここで解放（今回は省略）

        _disposed = true;
    }

    // ファイナライザ（using や Dispose() が忘れられた最後の砦）
    ~ManagedResource()
    {
        Dispose(disposing: false); // GC からの呼び出しなのでマネージドリソースは触らない
    }
}

class DisposableSamples
{
    public static void Run()
    {
        Console.WriteLine("\n=== IDisposable のサンプル ===");

        // using ステートメント（ブロック形式）
        Console.WriteLine("-- using ステートメント --");
        using (var res = new ManagedResource("BlockUsing"))
        {
            res.DoWork();
        } // ここで Dispose() が呼ばれる
        Console.WriteLine("  ブロックを抜けた");

        // using 宣言（C# 8+、スコープ形式）
        Console.WriteLine("-- using 宣言 --");
        using var res2 = new ManagedResource("DeclarationUsing");
        res2.DoWork();
        // メソッドスコープ末尾で Dispose() が呼ばれる

        Console.WriteLine("-- using 宣言後、メソッドスコープ終了で Dispose される --");
    }
}

// ───────────────────────────────────────────────
// サンプル 3: using ステートメント vs using 宣言の比較
// ───────────────────────────────────────────────
class UsingComparisonSamples
{
    public static void Run()
    {
        Console.WriteLine("\n=== using ステートメント vs using 宣言 ===");

        string tempFile = Path.GetTempFileName();

        try
        {
            // ブロック形式: 早期解放が必要なときに向いている
            using (var writer = new StreamWriter(tempFile))
            {
                writer.WriteLine("ブロック形式で書いた行");
            } // ← writer.Dispose() ここで確実に呼ばれる

            // この時点で writer は解放済み。別の操作（読み取り等）が安全にできる
            string written = File.ReadAllText(tempFile);
            Console.WriteLine($"ブロック形式: '{written.Trim()}'");

            // 宣言形式: スコープ全体で使い続けてよいときに向いている（ネストが浅くなる）
            using var reader = new StreamReader(tempFile);
            string read = reader.ReadToEnd();
            Console.WriteLine($"宣言形式で読み取り: '{read.Trim()}'");
            // メソッド末尾で reader.Dispose() が呼ばれる
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

// ───────────────────────────────────────────────
// サンプル 4: IAsyncDisposable と await using
// ───────────────────────────────────────────────

// 非同期リソースの実装例（ネットワーク接続の非同期クローズを模擬）
class AsyncResource : IAsyncDisposable
{
    private readonly string _name;

    public AsyncResource(string name)
    {
        _name = name;
        Console.WriteLine($"  [{_name}] 非同期リソース生成");
    }

    public async Task WriteAsync(string data)
    {
        await Task.Delay(10); // 非同期 I/O の模擬
        Console.WriteLine($"  [{_name}] 書き込み: {data}");
    }

    // IAsyncDisposable の実装
    public async ValueTask DisposeAsync()
    {
        // ValueTask: 頻繁呼び出し時のヒープ割り当てを避けるために使用
        await Task.Delay(10); // 非同期クリーンアップの模擬
        Console.WriteLine($"  [{_name}] DisposeAsync 呼び出し");
    }
}

class AsyncDisposableSamples
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== IAsyncDisposable と await using ===");

        // await using ステートメント（ブロック形式）
        Console.WriteLine("-- await using ステートメント --");
        await using (var res = new AsyncResource("AsyncBlock"))
        {
            await res.WriteAsync("非同期データ 1");
        } // ← await DisposeAsync() が呼ばれる
        Console.WriteLine("  非同期ブロックを抜けた");

        // await using 宣言（C# 8+）
        Console.WriteLine("-- await using 宣言 --");
        await using var res2 = new AsyncResource("AsyncDecl");
        await res2.WriteAsync("非同期データ 2");
        // メソッドスコープ末尾で await DisposeAsync() が呼ばれる
    }
}

// ───────────────────────────────────────────────
// using static と global using の示例（コメントで説明）
// ───────────────────────────────────────────────

// ファイル先頭に書くことで Console, Math 等を省略できる
// using static System.Console;
// using static System.Math;
//
// 呼び出し側:
//   WriteLine("省略形");   // Console.WriteLine の代わり
//   double r = Sqrt(2.0); // Math.Sqrt の代わり

// global using は専用ファイル（GlobalUsings.cs 等）に集約するのが慣習
// global using System;
// global using System.Linq;

// ───────────────────────────────────────────────
// エントリポイント
// ───────────────────────────────────────────────
class Program
{
    static async Task Main()
    {
        ExtensionMethodSamples.Run();
        DisposableSamples.Run();
        UsingComparisonSamples.Run();
        await AsyncDisposableSamples.RunAsync();
    }
}
