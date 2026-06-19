using System;
using System.IO;

// ========================================
// 例外処理 サンプルコード
// ========================================

class ExceptionSamples
{
    static void Main()
    {
        BasicTryCatchFinally();
        MultiCatchDemo();
        CustomExceptionDemo();
        ThrowVsThrowEx();
        ExceptionFilterDemo();
    }

    // ----------------------------------------
    // 1. 基本的な try / catch / finally
    // ----------------------------------------
    static void BasicTryCatchFinally()
    {
        Console.WriteLine("=== try / catch / finally ===");

        // 正常系
        RunDivide(10, 2);

        // ゼロ除算
        RunDivide(10, 0);
    }

    static void RunDivide(int a, int b)
    {
        try
        {
            int result = Divide(a, b);
            Console.WriteLine($"{a} / {b} = {result}");
        }
        catch (DivideByZeroException ex)
        {
            // 具体的な例外を先に捕捉する
            Console.WriteLine($"[エラー] ゼロ除算: {ex.Message}");
        }
        catch (Exception ex)
        {
            // より上位の例外は後で捕捉する
            Console.WriteLine($"[エラー] 予期しない例外: {ex.Message}");
        }
        finally
        {
            // 例外の有無にかかわらず実行される
            Console.WriteLine("  -> finally 実行");
        }
    }

    static int Divide(int a, int b) => a / b; // b=0 で DivideByZeroException

    // ----------------------------------------
    // 2. 複数の catch / ArgumentException 系
    // ----------------------------------------
    static void MultiCatchDemo()
    {
        Console.WriteLine("\n=== 複数の catch ===");

        ProcessAge(25);   // 正常
        ProcessAge(-1);   // ArgumentOutOfRangeException
        ProcessAge(200);  // ArgumentOutOfRangeException
        ProcessAge(30);   // 正常
    }

    static void ProcessAge(int age)
    {
        try
        {
            ValidateAge(age);
            Console.WriteLine($"年齢 {age} は有効です。");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"[エラー] 範囲外: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            // ArgumentOutOfRangeException の親クラス（後に書く）
            Console.WriteLine($"[エラー] 引数不正: {ex.Message}");
        }
    }

    static void ValidateAge(int age)
    {
        // 引数の前提条件違反は ArgumentException 系を使う
        if (age < 0 || age > 150)
            throw new ArgumentOutOfRangeException(nameof(age), age, "年齢は 0〜150 の範囲で指定してください。");
    }

    // ----------------------------------------
    // 3. カスタム例外クラスの定義と使用
    // ----------------------------------------
    static void CustomExceptionDemo()
    {
        Console.WriteLine("\n=== カスタム例外 ===");

        try
        {
            var order = FindOrder(999); // 存在しない ID
            Console.WriteLine($"注文: {order}");
        }
        catch (OrderNotFoundException ex)
        {
            // カスタム例外のプロパティ（OrderId）を使える
            Console.WriteLine($"[エラー] 注文 ID {ex.OrderId} が見つかりません。");
        }
    }

    static string FindOrder(int orderId)
    {
        // 実際には DB を検索するが、ここではシンプルに例外を投げる
        if (orderId != 1)
            throw new OrderNotFoundException(orderId);
        return "注文 #1: リンゴ × 3";
    }

    // ----------------------------------------
    // 4. throw vs throw ex（スタックトレース）
    // ----------------------------------------
    static void ThrowVsThrowEx()
    {
        Console.WriteLine("\n=== throw vs throw ex ===");

        // throw: 元のスタックトレースを保持する（推奨）
        try
        {
            RethrowCorrectly();
        }
        catch (Exception ex)
        {
            // StackTrace には OriginalMethod の情報が含まれる
            Console.WriteLine($"[throw] StackTrace の先頭行:\n  {ex.StackTrace?.Split('\n')[0].Trim()}");
        }

        // throw ex: スタックトレースがリセットされる（非推奨）
        try
        {
            RethrowWrongly();
        }
        catch (Exception ex)
        {
            // StackTrace が RethrowWrongly の行から始まってしまう
            Console.WriteLine($"[throw ex] StackTrace の先頭行:\n  {ex.StackTrace?.Split('\n')[0].Trim()}");
        }
    }

    static void OriginalMethod()
    {
        throw new InvalidOperationException("元の例外");
    }

    static void RethrowCorrectly()
    {
        try { OriginalMethod(); }
        catch (Exception ex)
        {
            Console.WriteLine($"  ログ記録: {ex.Message}");
            throw; // 元のスタックトレースを保持
        }
    }

    static void RethrowWrongly()
    {
        try { OriginalMethod(); }
        catch (Exception ex)
        {
            Console.WriteLine($"  ログ記録: {ex.Message}");
            throw ex; // スタックトレースがここでリセットされる（非推奨）
        }
    }

    // ----------------------------------------
    // 5. 例外フィルター（when 句）
    // ----------------------------------------
    static void ExceptionFilterDemo()
    {
        Console.WriteLine("\n=== 例外フィルター (when) ===");

        foreach (int code in new[] { 200, 400, 404, 500 })
        {
            HandleHttpResponse(code);
        }
    }

    static void HandleHttpResponse(int statusCode)
    {
        try
        {
            SimulateHttpRequest(statusCode);
        }
        catch (HttpException ex) when (ex.StatusCode == 404)
        {
            Console.WriteLine($"  [{statusCode}] リソースが見つかりません (404)");
        }
        catch (HttpException ex) when (ex.StatusCode >= 500)
        {
            Console.WriteLine($"  [{statusCode}] サーバーエラー ({ex.StatusCode})");
        }
        catch (HttpException ex)
        {
            Console.WriteLine($"  [{statusCode}] HTTP エラー ({ex.StatusCode}): {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [{statusCode}] 予期しないエラー: {ex.Message}");
        }
    }

    static void SimulateHttpRequest(int statusCode)
    {
        if (statusCode >= 400)
            throw new HttpException(statusCode, $"HTTP {statusCode}");
        Console.WriteLine($"  [{statusCode}] 成功");
    }
}

// ---- カスタム例外クラス ----

/// <summary>注文が見つからない場合の例外</summary>
class OrderNotFoundException : Exception
{
    /// <summary>見つからなかった注文 ID</summary>
    public int OrderId { get; }

    public OrderNotFoundException(int orderId)
        : base($"注文 ID {orderId} が見つかりません。")
    {
        OrderId = orderId;
    }

    // 内部例外を持つコンストラクタ（DB 例外などをラップするとき）
    public OrderNotFoundException(int orderId, Exception innerException)
        : base($"注文 ID {orderId} が見つかりません。", innerException)
    {
        OrderId = orderId;
    }
}

/// <summary>HTTP ステータスコードを持つ例外（サンプル用）</summary>
class HttpException : Exception
{
    public int StatusCode { get; }

    public HttpException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
