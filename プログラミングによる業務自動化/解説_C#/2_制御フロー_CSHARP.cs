// =============================================================================
// 2: 制御フロー - コードサンプル
// =============================================================================

ControlFlowSamples.RunAll();

class ControlFlowSamples
{
    public static void RunAll()
    {
        Console.WriteLine("=== 2: 制御フロー ===\n");

        DemonstrateIfElse();
        DemonstrateLoops();
        DemonstrateSwitchExpression();
        DemonstratePatternMatching();
        DemonstrateBreakContinue();
        DemonstrateGuardClause();
    }

    // -------------------------------------------------------------------------
    // 1. if / else と三項演算子
    // -------------------------------------------------------------------------
    static void DemonstrateIfElse()
    {
        Console.WriteLine("--- if / else ---");

        int score = 75;

        // 基本的な if / else if / else
        if (score >= 90)
            Console.WriteLine("A");
        else if (score >= 70)
            Console.WriteLine("B");
        else
            Console.WriteLine("C");

        // 三項演算子（条件 ? 真の値 : 偽の値）
        string result = score >= 70 ? "合格" : "不合格";
        Console.WriteLine($"判定: {result}");

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 2. ループ
    // -------------------------------------------------------------------------
    static void DemonstrateLoops()
    {
        Console.WriteLine("--- ループ ---");

        // for: インデックスが必要な場合
        Console.Write("for:     ");
        for (int i = 0; i < 5; i++)
            Console.Write($"{i} ");
        Console.WriteLine();

        // foreach: コレクションの走査（インデックス不要）
        string[] fruits = { "apple", "banana", "cherry" };
        Console.Write("foreach: ");
        foreach (string fruit in fruits)
            Console.Write($"{fruit} ");
        Console.WriteLine();

        // foreach + インデックス（LINQ の Select を使う）
        Console.WriteLine("インデックス付き foreach:");
        foreach (var (item, index) in fruits.Select((v, i) => (v, i)))
            Console.WriteLine($"  [{index}] {item}");

        // while
        Console.Write("while:   ");
        int n = 0;
        while (n < 5)
        {
            Console.Write($"{n} ");
            n++;
        }
        Console.WriteLine();

        // do-while: 必ず 1 回は実行される
        int m = 0;
        do
        {
            Console.WriteLine($"do-while: m={m}");
            m++;
        } while (m < 1);

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 3. switch 式（C# 8+）
    // -------------------------------------------------------------------------
    static void DemonstrateSwitchExpression()
    {
        Console.WriteLine("--- switch 式 ---");

        // 従来の switch 文との比較
        DayOfWeek today = DateTime.Today.DayOfWeek;

        // 従来の switch 文（C# 7 以前スタイル）
        string resultOld;
        switch (today)
        {
            case DayOfWeek.Saturday:
            case DayOfWeek.Sunday:
                resultOld = "休日";
                break;
            default:
                resultOld = "平日";
                break;
        }
        Console.WriteLine($"switch 文:  {today} → {resultOld}");

        // switch 式（C# 8+）: 簡潔でかつ式として値を返せる
        string resultNew = today switch
        {
            DayOfWeek.Saturday or DayOfWeek.Sunday => "休日",
            _ => "平日"  // _ はデフォルト（ワイルドカード）
        };
        Console.WriteLine($"switch 式:  {today} → {resultNew}");

        // 数値の範囲パターン
        int score = 82;
        string grade = score switch
        {
            >= 90 => "A",
            >= 70 => "B",
            >= 50 => "C",
            _     => "D"
        };
        Console.WriteLine($"score={score} → 評価: {grade}");

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 4. パターンマッチング
    // -------------------------------------------------------------------------
    static void DemonstratePatternMatching()
    {
        Console.WriteLine("--- パターンマッチング ---");

        // 型パターン: is
        object[] items = { 42, "hello", 3.14, true, null! };
        foreach (var item in items)
        {
            // is 演算子で型をチェックし、同時に変数に束縛する
            string desc = item switch
            {
                int n    => $"整数 {n}",
                string s => $"文字列 \"{s}\"",
                double d => $"浮動小数点 {d}",
                bool b   => $"真偽値 {b}",
                null     => "null",
                _        => $"その他 {item.GetType().Name}"
            };
            Console.WriteLine($"  {desc}");
        }

        Console.WriteLine();

        // when 句: 追加条件を指定する
        Console.WriteLine("when 句:");
        int[] numbers = { -5, 0, 50, 150 };
        foreach (int num in numbers)
        {
            string category = num switch
            {
                < 0           => "負数",
                0             => "ゼロ",
                > 0 and <= 100 => "1〜100",
                _             => "100 超"
            };
            Console.WriteLine($"  {num,4} → {category}");
        }

        Console.WriteLine();

        // プロパティパターン
        Console.WriteLine("プロパティパターン:");
        var points = new[] { new Point(0, 0), new Point(0, 3), new Point(4, 0), new Point(2, 3) };
        foreach (var p in points)
        {
            string location = p switch
            {
                { X: 0, Y: 0 }       => "原点",
                { X: 0 }             => "Y 軸上",
                { Y: 0 }             => "X 軸上",
                { X: > 0, Y: > 0 }  => "第 1 象限",
                _                    => "その他"
            };
            Console.WriteLine($"  {p} → {location}");
        }

        Console.WriteLine();

        // 位置パターン（record の分解）
        Console.WriteLine("位置パターン:");
        foreach (var p in points)
        {
            string location2 = p switch
            {
                (0, 0) => "原点",
                (0, _) => "Y 軸上",
                (_, 0) => "X 軸上",
                _      => "第 1 象限以外"
            };
            Console.WriteLine($"  {p} → {location2}");
        }

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 5. break / continue
    // -------------------------------------------------------------------------
    static void DemonstrateBreakContinue()
    {
        Console.WriteLine("--- break / continue ---");

        Console.Write("出力: ");
        for (int i = 0; i < 10; i++)
        {
            if (i == 3) continue;  // i=3 をスキップ
            if (i == 7) break;     // i=7 でループ終了
            Console.Write($"{i} ");
        }
        Console.WriteLine();  // → 0 1 2 4 5 6

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 6. ガード節（早期リターン）
    // -------------------------------------------------------------------------
    static void DemonstrateGuardClause()
    {
        Console.WriteLine("--- ガード節 ---");

        // テストデータ
        string?[] inputs = { null, "", "banana", "apple", "AVOCADO" };

        foreach (var input in inputs)
        {
            string result = ProcessWithGuard(input);
            Console.WriteLine($"  input={input ?? "null",-10} → {result}");
        }

        Console.WriteLine();
    }

    // NG 例: 深いネスト（コメントとして参考掲載）
    // static string ProcessNested(string? input)
    // {
    //     if (input != null)
    //     {
    //         if (input.Length > 0)
    //         {
    //             if (input.StartsWith("a", StringComparison.OrdinalIgnoreCase))
    //             {
    //                 return input.ToUpper();
    //             }
    //         }
    //     }
    //     return "(スキップ)";
    // }

    // 推奨: ガード節で早期リターン
    static string ProcessWithGuard(string? input)
    {
        // 各ガード節で異常ケースを先に排除する
        if (input is null) return "(null のためスキップ)";
        if (input.Length == 0) return "(空文字のためスキップ)";
        if (!input.StartsWith("a", StringComparison.OrdinalIgnoreCase))
            return "(a 始まりでないためスキップ)";

        // 正常系はここだけ
        return input.ToUpper();
    }
}

// -------------------------------------------------------------------------
// サポート型
// -------------------------------------------------------------------------
record Point(int X, int Y)
{
    public override string ToString() => $"({X}, {Y})";
}
