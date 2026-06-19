// 7_デリゲートとラムダ式_CSHARP.cs
// 「デリゲートとラムダ式」記事のコードサンプル

using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================
// 1. Func<> / Action<> / Predicate<> のサンプル
// ============================================================

class DelegateBasics
{
    public static void Run()
    {
        Console.WriteLine("--- Func / Action / Predicate ---");

        // Func<TResult>: 引数なし、戻り値あり
        Func<DateTime> now = () => DateTime.Now;
        Console.WriteLine($"現在時刻: {now():HH:mm:ss}");

        // Func<T, TResult>: 変換
        Func<string, int> strLen = s => s.Length;
        Console.WriteLine($"文字列長: {strLen("Hello, C#")}");

        // Func<T1, T2, TResult>: 2引数
        Func<int, int, int> add = (a, b) => a + b;
        Console.WriteLine($"3 + 4 = {add(3, 4)}");

        // Action<T>: 戻り値なし（副作用のみ）
        Action<string> log = msg => Console.WriteLine($"[LOG] {msg}");
        log("処理開始");

        // Predicate<T>: bool を返す（Func<T, bool> と等価）
        Predicate<int> isEven = n => n % 2 == 0;
        Console.WriteLine($"6 は偶数: {isEven(6)}");
        Console.WriteLine($"7 は偶数: {isEven(7)}");

        // LINQ での Func / Predicate 利用
        var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var evens = numbers.Where(isEven.Invoke).Select(n => n * n).ToArray();
        Console.WriteLine($"偶数の二乗: [{string.Join(", ", evens)}]");
    }
}

// ============================================================
// 2. ラムダ式のサンプル（式形式・ブロック形式）
// ============================================================

class LambdaExpressions
{
    public static void Run()
    {
        Console.WriteLine("--- ラムダ式 ---");

        // 式形式（Expression Lambda）: 1行で収まる処理
        Func<double, double> square  = x => x * x;
        Func<int, int, int>  max     = (a, b) => a > b ? a : b;

        Console.WriteLine($"square(5.0): {square(5.0)}");
        Console.WriteLine($"max(8, 3):   {max(8, 3)}");

        // ブロック形式（Statement Lambda）: 複数処理
        Func<int, string> classify = n =>
        {
            if (n < 0) return "負の数";
            if (n == 0) return "ゼロ";
            if (n < 10) return "一桁の正数";
            return "二桁以上の正数";
        };

        foreach (var val in new[] { -5, 0, 7, 42 })
            Console.WriteLine($"  {val} → {classify(val)}");

        // 引数なしラムダ
        Func<string> greeting = () => $"こんにちは！ ({DateTime.Now:HH:mm})";
        Console.WriteLine(greeting());

        // メソッドグループ（既存メソッドをデリゲートとして渡す）
        Func<string, string> upper = string.Empty.ToUpperInvariant; // NG: インスタンスメソッドは不可
        // 正しい書き方: メソッドグループ
        Func<double, double> absVal = Math.Abs;
        Console.WriteLine($"Math.Abs(-3.7): {absVal(-3.7)}");
    }
}

// ============================================================
// 3. マルチキャストデリゲート
// ============================================================

class MulticastDemo
{
    public static void Run()
    {
        Console.WriteLine("--- マルチキャストデリゲート ---");

        Action<string> logger  = msg => Console.WriteLine($"[LOG]   {msg}");
        Action<string> auditor = msg => Console.WriteLine($"[AUDIT] {msg}");
        Action<string> notifier = msg => Console.WriteLine($"[NOTIFY] {msg}");

        // += で連結
        Action<string> combined = logger;
        combined += auditor;
        combined += notifier;

        combined("ユーザーログイン");
        Console.WriteLine();

        // -= で解除
        combined -= auditor;
        Console.WriteLine("auditor を解除後:");
        combined("ユーザーログアウト");
    }
}

// ============================================================
// 4. イベントの定義と購読
// ============================================================

// カスタムイベントデータ
class TemperatureChangedEventArgs : EventArgs
{
    public double OldTemperature { get; }
    public double NewTemperature { get; }

    public TemperatureChangedEventArgs(double old, double newTemp)
    {
        OldTemperature = old;
        NewTemperature = newTemp;
    }
}

// パブリッシャー: イベントを発行する側
class Thermometer
{
    private double _temperature;

    // event キーワード: 外部からは += / -= のみ可能
    public event EventHandler<TemperatureChangedEventArgs>? TemperatureChanged;

    public double Temperature
    {
        get => _temperature;
        set
        {
            if (Math.Abs(value - _temperature) < 0.01) return; // 変化なしはスキップ

            var args = new TemperatureChangedEventArgs(_temperature, value);
            _temperature = value;

            // イベント発火（null 条件演算子で null チェック）
            TemperatureChanged?.Invoke(this, args);
        }
    }
}

// サブスクライバー: イベントを受け取る側
class Alarm
{
    private readonly double _threshold;

    public Alarm(double threshold) => _threshold = threshold;

    public void OnTemperatureChanged(object? sender, TemperatureChangedEventArgs e)
    {
        if (e.NewTemperature >= _threshold)
            Console.WriteLine($"  [警報] 温度が閾値({_threshold}°C)を超えました: {e.NewTemperature}°C");
    }
}

class EventDemo
{
    public static void Run()
    {
        Console.WriteLine("--- イベント ---");

        var thermometer = new Thermometer();
        var alarm       = new Alarm(30.0);

        // サブスクライバー登録
        thermometer.TemperatureChanged += (sender, e) =>
            Console.WriteLine($"  温度変化: {e.OldTemperature:F1}°C → {e.NewTemperature:F1}°C");

        thermometer.TemperatureChanged += alarm.OnTemperatureChanged;

        // 温度を変化させる
        thermometer.Temperature = 20.0; // 変化あり → イベント発火
        thermometer.Temperature = 25.5;
        thermometer.Temperature = 32.0; // 閾値超え → 警報も鳴る
        thermometer.Temperature = 32.0; // 変化なし → イベント発火なし
    }
}

// ============================================================
// 5. クロージャのサンプル
// ============================================================

class ClosureDemo
{
    public static void Run()
    {
        Console.WriteLine("--- クロージャ（変数キャプチャ） ---");

        // 基本: 外部変数をキャプチャ
        int multiplier = 3;
        Func<int, int> triple = x => x * multiplier; // multiplier を参照キャプチャ

        Console.WriteLine($"multiplier=3: triple(5) = {triple(5)}"); // 15

        multiplier = 10; // 変数を書き換えるとラムダの結果も変わる
        Console.WriteLine($"multiplier=10: triple(5) = {triple(5)}"); // 50

        Console.WriteLine();

        // for ループでのキャプチャ（落とし穴）
        Console.WriteLine("[NG] for ループ変数をそのままキャプチャ:");
        var ngActions = new List<Action>();
        for (int i = 0; i < 3; i++)
        {
            ngActions.Add(() => Console.Write($"{i} ")); // i を参照キャプチャ → ループ後は i=3
        }
        foreach (var a in ngActions) a();
        Console.WriteLine("← 全部 3（期待値: 0, 1, 2）");

        Console.WriteLine();

        // 修正: ループごとにローカル変数にコピー
        Console.WriteLine("[OK] ローカル変数にコピーしてキャプチャ:");
        var okActions = new List<Action>();
        for (int i = 0; i < 3; i++)
        {
            int captured = i; // イテレーションごとに独立したコピー
            okActions.Add(() => Console.Write($"{captured} "));
        }
        foreach (var a in okActions) a();
        Console.WriteLine("← 0, 1, 2（期待通り）");

        Console.WriteLine();

        // foreach は C# 5+ で各イテレーションが独立 → 問題なし
        Console.WriteLine("[OK] foreach はそのままでよい:");
        var items = new[] { "apple", "banana", "cherry" };
        var foreachActions = new List<Action>();
        foreach (var item in items)
        {
            foreachActions.Add(() => Console.Write($"{item} ")); // item は各イテレーションで独立
        }
        foreach (var a in foreachActions) a();
        Console.WriteLine();

        Console.WriteLine();

        // カウンタを持つファクトリ（クロージャの実用例）
        Console.WriteLine("[実用] クロージャでカウンタを生成:");
        Func<Func<int>> makeCounter = () =>
        {
            int count = 0;              // このスコープの変数をキャプチャ
            return () => ++count;       // 呼ぶたびにインクリメント
        };

        var counterA = makeCounter();
        var counterB = makeCounter(); // 独立したカウンタ

        Console.WriteLine($"counterA: {counterA()}, {counterA()}, {counterA()}"); // 1, 2, 3
        Console.WriteLine($"counterB: {counterB()}, {counterB()}");               // 1, 2（独立）
    }
}

// ============================================================
// エントリーポイント
// ============================================================
class Program
{
    static void Main()
    {
        Console.WriteLine("=== 1. Func / Action / Predicate ===");
        DelegateBasics.Run();

        Console.WriteLine();
        Console.WriteLine("=== 2. ラムダ式 ===");
        LambdaExpressions.Run();

        Console.WriteLine();
        Console.WriteLine("=== 3. マルチキャストデリゲート ===");
        MulticastDemo.Run();

        Console.WriteLine();
        Console.WriteLine("=== 4. イベント ===");
        EventDemo.Run();

        Console.WriteLine();
        Console.WriteLine("=== 5. クロージャ ===");
        ClosureDemo.Run();
    }
}
