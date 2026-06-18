# 拡張メソッドと using

## この記事で学ぶこと

- 拡張メソッドの定義方法（`static class` + `this` パラメータ）
- 拡張メソッドの用途と LINQ の実装原理
- `using` ディレクティブによる名前空間インポート
- `using static` による静的メンバーの直接参照
- `global using`（C# 10+）
- `IDisposable` パターン（リソース解放の規約）
- `using` ステートメントと `using` 宣言（C# 8+）の比較
- `IAsyncDisposable` と `await using`（C# 8+）

---

## 拡張メソッドとは

拡張メソッドを使うと、**既存のクラスを継承したりソースコードを変更せずに**、新しいメソッドを追加したように見せることができます。

### 定義方法

1. `static class`（静的クラス）に定義する
2. 第 1 引数に `this` キーワードをつけて対象型を指定する

```csharp
public static class StringExtensions
{
    // string 型に IsNullOrEmpty メソッドを追加
    public static bool IsNullOrEmpty(this string? value)
        => string.IsNullOrEmpty(value);

    // string 型に TruncateAt メソッドを追加
    public static string TruncateAt(this string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "…";
}
```

### 使用方法

```csharp
string name = "Hello, World!";
bool empty = name.IsNullOrEmpty();          // false
string short_ = name.TruncateAt(5);        // "Hello…"
```

呼び出し側から見ると、まるでそのクラスに最初からメソッドがあるように見えます。

---

## LINQ の実装原理

`Where`、`Select`、`OrderBy` などの LINQ メソッドはすべて拡張メソッドです。`System.Linq.Enumerable` クラスに定義された、`IEnumerable<T>` への拡張メソッドが正体です。

```csharp
// これは実際には拡張メソッドの呼び出し
var result = list.Where(x => x > 0).Select(x => x * 2);

// 通常のメソッド呼び出しに展開すると
var result2 = Enumerable.Select(Enumerable.Where(list, x => x > 0), x => x * 2);
```

---

## 拡張メソッドの用途

| 用途 | 例 |
|---|---|
| 外部ライブラリ・標準型の補完 | `string` に業務固有のバリデーション追加 |
| インターフェースへのデフォルト実装提供 | `IEnumerable<T>` への LINQ |
| 流暢な API（メソッドチェーン）の構築 | `builder.UseDatabase().UseCache()` |

**注意:** 拡張メソッドはインスタンスメソッドより**優先度が低い**です。同名のインスタンスメソッドがある場合はそちらが呼ばれます。

---

## `using` ディレクティブ（名前空間インポート）

```csharp
using System;
using System.Collections.Generic;
using System.IO;
```

名前空間をインポートすることで、完全修飾名（`System.IO.File`）を省略して `File` と書けます。

---

## `using static`（静的メンバーの直接参照）

静的クラスのメンバーをクラス名なしで使えるようにします。

```csharp
using static System.Console;
using static System.Math;

WriteLine("Hello");        // Console.WriteLine("Hello") と同じ
double r = Sqrt(2.0);     // Math.Sqrt(2.0) と同じ
```

数学関数や定数を多用するコードで記述量を減らせますが、どのクラスのメンバーかわかりにくくなるため、使いすぎには注意が必要です。

---

## `global using`（C# 10+）

ファイルの先頭に `global` を付けると、プロジェクト全体で有効な `using` になります。

```csharp
// GlobalUsings.cs（専用ファイルに集約するのが慣習）
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.IO;
```

**.NET 6+ の暗黙的 `global using`:** プロジェクトファイルで `<ImplicitUsings>enable</ImplicitUsings>` を設定すると、`System`、`System.IO`、`System.Linq` などがプロジェクト全体で自動的にインポートされます。

---

## `IDisposable` パターン

ファイルハンドル・データベース接続・ネットワークソケットなど、マネージドヒープ以外のリソース（アンマネージドリソース）を保持するクラスは `IDisposable` を実装する規約があります。

```csharp
public class ResourceHolder : IDisposable
{
    private bool _disposed = false;
    private readonly FileStream _stream;

    public ResourceHolder(string path)
    {
        _stream = new FileStream(path, FileMode.Open);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _stream.Dispose(); // マネージドリソースを解放
        }
        _disposed = true;
    }
}
```

`GC.SuppressFinalize` を呼ぶことで、GC によるファイナライザ呼び出しを省略してパフォーマンスを向上させます。

---

## `using` ステートメント vs `using` 宣言

### `using` ステートメント（ブロック形式）

```csharp
using (var conn = new SqlConnection(connectionString))
{
    conn.Open();
    // ブロックを抜けると conn.Dispose() が呼ばれる
}
// conn はここでは使えない
```

### `using` 宣言（C# 8+、スコープ形式）

```csharp
using var conn = new SqlConnection(connectionString);
conn.Open();
// メソッド（またはブロック）の終わりに conn.Dispose() が呼ばれる
```

**使い分けの基準:**
- 早期にリソースを解放したい → ブロック形式
- スコープ全体で使い続けてよい → `using` 宣言（ネストが浅くなり読みやすい）

---

## `IAsyncDisposable` と `await using`（C# 8+）

非同期のリソース解放が必要な場合（例: ネットワーク接続の非同期クローズ）は `IAsyncDisposable` を実装します。

```csharp
public class AsyncResource : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await FlushAsync();
        // 非同期クリーンアップ処理
    }

    private async Task FlushAsync() { /* ... */ }
}

// 呼び出し側
await using var resource = new AsyncResource();
// await using ブロックを抜けると DisposeAsync() が await される
```

**`ValueTask` の使用:** `DisposeAsync` の戻り値型は `Task` ではなく `ValueTask` が推奨されています。頻繁に呼ばれる場合にヒープ割り当てを避けられます。

---

## よくある落とし穴

- **拡張メソッドと同名のインスタンスメソッドの優先度**: インスタンスメソッドが常に優先されます。意図せず拡張メソッドが呼ばれない場合はこれを疑ってください。
- **`Dispose()` を複数回呼ぶ**: `Dispose()` は複数回呼ばれても安全に動作するよう設計すべきです（`_disposed` フラグで制御）。
- **`using` 宣言のスコープ**: `using` 宣言はブロックの末尾で解放されます。早期解放が必要な場合はブロック形式を使ってください。
- **非同期コンテキストでの `IDisposable`**: `async` メソッド内で同期の `using` を使うことは問題ありませんが、リソースが非同期解放をサポートしている場合は `await using` を優先してください。

---

## コードサンプル

同ディレクトリの `15_拡張メソッドとusing_CSHARP.cs` を参照してください。
