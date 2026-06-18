using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ========================================
// 非同期処理（async / await）サンプルコード
// ========================================

class AsyncSamples
{
    static async Task Main()
    {
        await BasicAsyncAwait();
        await CancellationTokenDemo();
        await WhenAllDemo();
        await WhenAnyTimeoutDemo();
        AsyncVoidProblemDemo();
    }

    // ----------------------------------------
    // 1. 基本的な async / await
    // ----------------------------------------
    static async Task BasicAsyncAwait()
    {
        Console.WriteLine("=== 基本的な async / await ===");

        Console.WriteLine("処理開始");

        // await: 完了まで待ちつつスレッドは解放する
        string result = await SimulateIoAsync("データA", delayMs: 500);
        Console.WriteLine($"取得結果: {result}");

        // Task<T> を返すメソッドの戻り値を await で取り出す
        int count = await CountItemsAsync();
        Console.WriteLine($"件数: {count}");

        Console.WriteLine("処理終了");
    }

    // I/O 処理をシミュレートする非同期メソッド
    static async Task<string> SimulateIoAsync(string name, int delayMs)
    {
        Console.WriteLine($"  [{name}] 開始 (スレッド: {Environment.CurrentManagedThreadId})");
        await Task.Delay(delayMs); // ネットワーク待ちや DB 待ちに相当
        Console.WriteLine($"  [{name}] 完了 (スレッド: {Environment.CurrentManagedThreadId})");
        return $"結果_{name}";
    }

    static async Task<int> CountItemsAsync()
    {
        await Task.Delay(100);
        return 42;
    }

    // ----------------------------------------
    // 2. CancellationToken によるキャンセル
    // ----------------------------------------
    static async Task CancellationTokenDemo()
    {
        Console.WriteLine("\n=== CancellationToken ===");

        // --- 正常終了 ---
        Console.WriteLine("< 正常終了 >");
        using var cts1 = new CancellationTokenSource();
        await LongRunningAsync("タスクA", steps: 3, cts1.Token);

        // --- タイムアウトでキャンセル ---
        Console.WriteLine("\n< タイムアウトでキャンセル >");
        using var cts2 = new CancellationTokenSource();
        cts2.CancelAfter(TimeSpan.FromMilliseconds(350)); // 350ms 後にキャンセル
        try
        {
            await LongRunningAsync("タスクB", steps: 10, cts2.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  -> キャンセルされました。");
        }

        // --- 手動でキャンセル ---
        Console.WriteLine("\n< 手動キャンセル >");
        using var cts3 = new CancellationTokenSource();

        // バックグラウンドで 200ms 後にキャンセルする
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            Console.WriteLine("  -> Cancel() を呼び出します");
            cts3.Cancel();
        });

        try
        {
            await LongRunningAsync("タスクC", steps: 10, cts3.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  -> キャンセルされました。");
        }
    }

    // CancellationToken を受け取って定期的にチェックする処理
    static async Task LongRunningAsync(string name, int steps, CancellationToken ct)
    {
        for (int i = 1; i <= steps; i++)
        {
            // キャンセルが要求されたら OperationCanceledException をスロー
            ct.ThrowIfCancellationRequested();

            Console.WriteLine($"  {name}: ステップ {i}/{steps}");
            await Task.Delay(100, ct); // Delay 中のキャンセルも検知できる
        }
        Console.WriteLine($"  {name}: 完了");
    }

    // ----------------------------------------
    // 3. Task.WhenAll — 複数タスクの並列実行
    // ----------------------------------------
    static async Task WhenAllDemo()
    {
        Console.WriteLine("\n=== Task.WhenAll ===");

        var start = DateTime.Now;

        // 3 つのタスクを同時に開始
        var t1 = SimulateIoAsync("タスク1", delayMs: 300);
        var t2 = SimulateIoAsync("タスク2", delayMs: 500);
        var t3 = SimulateIoAsync("タスク3", delayMs: 200);

        // すべて完了するまで待つ（最長の 500ms で揃う）
        string[] results = await Task.WhenAll(t1, t2, t3);

        var elapsed = (DateTime.Now - start).TotalMilliseconds;
        Console.WriteLine($"全タスク完了 (経過: {elapsed:F0}ms)");
        foreach (var r in results) Console.WriteLine($"  {r}");

        // 直列で実行した場合との比較: 300+500+200 = 1000ms かかるところを ~500ms で完了
    }

    // ----------------------------------------
    // 4. Task.WhenAny — タイムアウトパターン
    // ----------------------------------------
    static async Task WhenAnyTimeoutDemo()
    {
        Console.WriteLine("\n=== Task.WhenAny (タイムアウトパターン) ===");

        // --- タイムアウトしない場合 ---
        Console.WriteLine("< タイムアウトしない >");
        await FetchWithTimeoutAsync(delayMs: 300, timeoutMs: 1000);

        // --- タイムアウトする場合 ---
        Console.WriteLine("\n< タイムアウトする >");
        await FetchWithTimeoutAsync(delayMs: 2000, timeoutMs: 500);
    }

    static async Task FetchWithTimeoutAsync(int delayMs, int timeoutMs)
    {
        var fetchTask   = SimulateIoAsync("fetch", delayMs);
        var timeoutTask = Task.Delay(timeoutMs);

        // どちらか早い方が返ったら続行
        Task completed = await Task.WhenAny(fetchTask, timeoutTask);

        if (completed == timeoutTask)
        {
            Console.WriteLine($"  タイムアウト ({timeoutMs}ms)");
        }
        else
        {
            string result = await fetchTask; // すでに完了しているので即座に返る
            Console.WriteLine($"  成功: {result}");
        }
    }

    // ----------------------------------------
    // 5. async void の問題点
    // ----------------------------------------
    static void AsyncVoidProblemDemo()
    {
        Console.WriteLine("\n=== async void の問題 ===");

        // async void を呼び出しても await できないため、
        // 例外が捕捉されずアプリがクラッシュする（サンプルではコメントアウト）
        // FireAndForgetWrong(); // 危険: 例外が飲み込まれる

        // OK パターン 1: async Task にする
        // Task t = FireAndForgetCorrect();
        // await t; など

        // OK パターン 2: 「fire and forget」が必要なら例外処理を内部で行う
        _ = SafeFireAndForgetAsync();

        Console.WriteLine("呼び出し直後（async void の完了は待てない）");
    }

    // NG: async void — 例外がどこにも届かない
    // ReSharper や .NET Analyzer も警告を出す
    static async void FireAndForgetWrong()
    {
        await Task.Delay(100);
        // throw new Exception("捕捉できない例外"); // コメントアウト: 実行するとクラッシュ
    }

    // OK: fire and forget が必要なときは内部で try/catch する
    static async Task SafeFireAndForgetAsync()
    {
        try
        {
            await Task.Delay(100);
            Console.WriteLine("  SafeFireAndForget 完了");
        }
        catch (Exception ex)
        {
            // ここで例外を記録・処理する
            Console.WriteLine($"  SafeFireAndForget エラー: {ex.Message}");
        }
    }
}
