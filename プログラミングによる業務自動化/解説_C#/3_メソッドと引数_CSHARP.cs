// =============================================================================
// 3: メソッドと引数 - コードサンプル
// =============================================================================

MethodSamples.RunAll();

class MethodSamples
{
    public static void RunAll()
    {
        Console.WriteLine("=== 3: メソッドと引数 ===\n");

        DemonstrateOverload();
        DemonstrateDefaultAndNamedArgs();
        DemonstrateOutRefIn();
        DemonstrateParams();
        DemonstrateLocalFunction();
    }

    // -------------------------------------------------------------------------
    // 1. オーバーロード
    // -------------------------------------------------------------------------
    static void DemonstrateOverload()
    {
        Console.WriteLine("--- オーバーロード ---");

        // 引数の型によって自動的に対応するメソッドが選ばれる
        Console.WriteLine(Add(1, 2));          // → 3     (int 版)
        Console.WriteLine(Add(1.5, 2.5));      // → 4     (double 版)
        Console.WriteLine(Add("Hello", " C#")); // → Hello C# (string 版)

        // 引数の数によるオーバーロード
        Console.WriteLine(Describe("Alice"));             // → 名前: Alice
        Console.WriteLine(Describe("Alice", 30));         // → 名前: Alice, 年齢: 30
        Console.WriteLine(Describe("Alice", 30, "東京")); // → 名前: Alice, 年齢: 30, 場所: 東京

        Console.WriteLine();
    }

    // 型によるオーバーロード
    static int Add(int a, int b) => a + b;
    static double Add(double a, double b) => a + b;
    static string Add(string a, string b) => a + b;

    // 引数の数によるオーバーロード
    static string Describe(string name) => $"名前: {name}";
    static string Describe(string name, int age) => $"名前: {name}, 年齢: {age}";
    static string Describe(string name, int age, string location)
        => $"名前: {name}, 年齢: {age}, 場所: {location}";

    // -------------------------------------------------------------------------
    // 2. デフォルト引数と名前付き引数
    // -------------------------------------------------------------------------
    static void DemonstrateDefaultAndNamedArgs()
    {
        Console.WriteLine("--- デフォルト引数・名前付き引数 ---");

        // デフォルト引数: 省略した引数にはデフォルト値が使われる
        Log("起動");                                  // level="INFO", timestamp=true
        Log("警告が発生", "WARN");                    // timestamp=true
        Log("詳細ログ", "DEBUG", timestamp: false);   // 名前付き引数で timestamp だけ変える

        // 名前付き引数: 引数の順序を変えられる
        var rect = CreateRectangle(height: 50, width: 100, y: 20, x: 10);
        Console.WriteLine($"Rectangle: {rect}");

        Console.WriteLine();
    }

    // デフォルト引数を持つメソッド
    static void Log(string message, string level = "INFO", bool timestamp = true)
    {
        string prefix = timestamp ? $"[{DateTime.Now:HH:mm:ss}] " : "";
        Console.WriteLine($"{prefix}[{level}] {message}");
    }

    static (int X, int Y, int Width, int Height) CreateRectangle(
        int x = 0, int y = 0, int width = 100, int height = 100)
        => (x, y, width, height);

    // -------------------------------------------------------------------------
    // 3. out / ref / in パラメータ
    // -------------------------------------------------------------------------
    static void DemonstrateOutRefIn()
    {
        Console.WriteLine("--- out / ref / in ---");

        // out: メソッドが値を出力する（int.TryParse スタイル）
        string[] inputs = { "42", "hello", "100", "-5" };
        foreach (string s in inputs)
        {
            // out 変数はインラインで宣言できる（C# 7+）
            if (TryParsePositive(s, out int parsed))
                Console.WriteLine($"  TryParsePositive(\"{s}\") → {parsed}");
            else
                Console.WriteLine($"  TryParsePositive(\"{s}\") → 失敗");
        }

        // 除算の out バージョン
        if (TryDivide(10, 3, out int q, out int r))
            Console.WriteLine($"  10 ÷ 3 = {q} 余り {r}");
        if (!TryDivide(5, 0, out _, out _))  // _ で不要な out を捨てる
            Console.WriteLine("  5 ÷ 0 → ゼロ除算エラー");

        Console.WriteLine();

        // ref: 呼び出し元の変数を直接変更する
        int counter = 0;
        IncrementByRef(ref counter);
        IncrementByRef(ref counter);
        Console.WriteLine($"  ref counter={counter}"); // → 2

        // 値渡し（コピー）との違い
        int counterCopy = 0;
        IncrementByValue(counterCopy);
        Console.WriteLine($"  値渡し counterCopy={counterCopy}"); // → 0（変わらない）

        Console.WriteLine();

        // in: 読み取り専用参照渡し（大きな struct を効率よく渡す）
        var p1 = new LargePoint(0, 0);
        var p2 = new LargePoint(3, 4);
        double dist = ComputeDistance(in p1, in p2);
        Console.WriteLine($"  距離 ({p1}) → ({p2}): {dist}");

        Console.WriteLine();
    }

    // out: 正の整数のみパース
    static bool TryParsePositive(string input, out int result)
    {
        if (int.TryParse(input, out result) && result > 0)
            return true;
        result = 0;  // out は失敗時も代入が必要
        return false;
    }

    // out: 商と余りを同時に返す
    static bool TryDivide(int dividend, int divisor, out int quotient, out int remainder)
    {
        if (divisor == 0)
        {
            quotient = 0;
            remainder = 0;
            return false;
        }
        quotient = dividend / divisor;
        remainder = dividend % divisor;
        return true;
    }

    // ref: 参照渡し（呼び出し元の変数を変更）
    static void IncrementByRef(ref int value) => value++;

    // 値渡し: コピーが渡されるので呼び出し元は変わらない
    static void IncrementByValue(int value) => value++;

    // in: 読み取り専用参照渡し（LargePoint を安全に渡す）
    static double ComputeDistance(in LargePoint p1, in LargePoint p2)
    {
        // p1.X = 0; // コンパイルエラー: in パラメータは変更できない
        return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
    }

    // -------------------------------------------------------------------------
    // 4. params（可変長引数）
    // -------------------------------------------------------------------------
    static void DemonstrateParams()
    {
        Console.WriteLine("--- params ---");

        // 任意の個数の引数を渡せる
        Console.WriteLine($"Sum(1,2,3)           = {Sum(1, 2, 3)}");
        Console.WriteLine($"Sum(10,20,30,40,50)  = {Sum(10, 20, 30, 40, 50)}");
        Console.WriteLine($"Sum()                = {Sum()}");  // 0 個でも OK

        // 配列を直接渡すこともできる
        int[] data = { 5, 10, 15, 20 };
        Console.WriteLine($"Sum(配列)             = {Sum(data)}");

        // 文字列の結合
        Console.WriteLine(Concat("-", "apple", "banana", "cherry"));

        Console.WriteLine();
    }

    static int Sum(params int[] numbers) => numbers.Sum();

    static string Concat(string separator, params string[] values)
        => string.Join(separator, values);

    // -------------------------------------------------------------------------
    // 5. ローカル関数
    // -------------------------------------------------------------------------
    static void DemonstrateLocalFunction()
    {
        Console.WriteLine("--- ローカル関数 ---");

        // フィボナッチ数列（ローカル関数で再帰）
        int[] fibs = GetFibonacci(8);
        Console.WriteLine($"フィボナッチ: {string.Join(", ", fibs)}");

        // 偶数フィルタ（ローカル関数でロジックをカプセル化）
        int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        IEnumerable<int> evens = GetEvenNumbers(numbers);
        Console.WriteLine($"偶数: {string.Join(", ", evens)}");

        // ローカル関数はクロージャ（外側の変数を捕捉）できる
        int multiplier = 3;
        int[] tripled = numbers.Select(n => MultiplyLocal(n)).ToArray();
        Console.WriteLine($"×{multiplier}: {string.Join(", ", tripled)}");

        // ローカル関数は外側のスコープの変数にアクセスできる
        int MultiplyLocal(int n) => n * multiplier;

        Console.WriteLine();
    }

    // ローカル関数を含む外側のメソッド（再帰のデモ）
    static int[] GetFibonacci(int count)
    {
        var result = new int[count];
        for (int i = 0; i < count; i++)
            result[i] = Fib(i);
        return result;

        // ローカル関数: 再帰が可能
        int Fib(int n)
        {
            if (n <= 1) return n;
            return Fib(n - 1) + Fib(n - 2);
        }
    }

    // yield return を使うローカル関数の例
    static IEnumerable<int> GetEvenNumbers(int[] input)
    {
        // IsEven はこのメソッド内でしか使わないローカル関数
        bool IsEven(int n) => n % 2 == 0;

        foreach (int n in input)
        {
            if (IsEven(n))
                yield return n;
        }
    }
}

// -------------------------------------------------------------------------
// サポート型
// -------------------------------------------------------------------------

// in パラメータのデモ用（大きな struct を模した型）
struct LargePoint
{
    public int X { get; }
    public int Y { get; }

    // 実際には多数のフィールドを持つ大きな struct を想定
    public LargePoint(int x, int y) { X = x; Y = y; }

    public override string ToString() => $"({X}, {Y})";
}
