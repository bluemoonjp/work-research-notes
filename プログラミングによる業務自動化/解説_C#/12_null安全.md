# null 安全

## この記事で学ぶこと

- null 参照が引き起こす問題とその歴史的背景
- C# 8+ の Nullable 参照型（`#nullable enable`）による静的解析
- `string?` 型注釈の意味と使い方
- null 条件演算子 `?.`、null 合体演算子 `??`、null 合体代入演算子 `??=`
- null 非許容アサーション演算子 `!`（null forgiveness）
- パターンマッチングによる null チェック（`is null` / `is not null`）
- Nullable 値型（`int?` / `Nullable<int>`）

---

## null 参照の問題

null 参照を発明したことは、1965 年に ALGOL の型システムを設計した C. A. R. Hoare 自身が「十億ドルの失敗（billion-dollar mistake）」と呼んでいます。null を許容する参照型は、実行時に `NullReferenceException` を引き起こし、静的な型チェックだけでは防げないからです。

C# はもともと null を参照型に自由に代入できる言語でしたが、C# 8.0（2019 年）で **Nullable 参照型（Nullable Reference Types）** が導入され、コンパイラが null の流れを追跡して警告を出せるようになりました。

---

## Nullable 参照型（`#nullable enable`）

```csharp
#nullable enable
```

このディレクティブをファイルの先頭に書くか、プロジェクトファイルで `<Nullable>enable</Nullable>` を設定すると、Nullable 参照型の解析が有効になります。

| 型 | null を代入できるか |
|---|---|
| `string` | 不可（コンパイラ警告） |
| `string?` | 可 |
| `int` | 不可（値型なので元々 null なし） |
| `int?` | 可（Nullable 値型） |

### `string?` 型注釈

```csharp
string  name   = null;   // 警告: null を非許容参照型に代入
string? name2  = null;   // OK: null 許容として明示
```

---

## null 条件演算子 `?.`

オブジェクトが null のとき、メンバーアクセスやメソッド呼び出しをスキップして `null` を返します。

```csharp
string? text = GetText();    // null かもしれない
int? length  = text?.Length; // text が null なら null、そうでなければ Length
```

**他言語との違い:** Java や Python では null/None チェックを if 文で書くのが一般的です。C# の `?.` はその記述を大幅に短縮します。

---

## null 合体演算子 `??`

左辺が null のとき、右辺の値を返します。

```csharp
string name = inputName ?? "名無し"; // inputName が null なら "名無し"
```

連鎖させることもできます。

```csharp
string result = a ?? b ?? c ?? "デフォルト";
```

---

## null 合体代入演算子 `??=`（C# 8+）

左辺が null のときだけ、右辺を代入します。

```csharp
List<string>? items = null;
items ??= new List<string>(); // null のときだけ初期化
items.Add("追加");
```

---

## null 非許容アサーション演算子 `!`（null forgiveness）

コンパイラの警告を黙らせるために使います。**確実に null でないとわかっているが、コンパイラが証明できない場合**に限定して使用してください。

```csharp
string? value = FindValue();
// 直前の処理で null でないことが保証されているとする
string definitelyNotNull = value!; // ! で null 非許容として扱う
```

**落とし穴:** `!` は実行時の保護ではありません。実際に null だった場合は `NullReferenceException` が発生します。多用は避け、`??` や条件チェックで代替できないか先に検討してください。

---

## パターンマッチングによる null チェック

C# 7+ のパターンマッチングを使うと、null チェックを明示的かつ読みやすく書けます。

```csharp
// 旧来の書き方
if (name != null) { ... }

// is null / is not null（C# 9+）
if (name is null) { ... }
if (name is not null) { ... }
```

**`== null` との違い:** `is null` は演算子オーバーロードを無視して null を直接チェックします。カスタム型で `==` をオーバーロードしている場合でも安全に使えます。

---

## Nullable 値型（`int?` / `Nullable<int>`）

値型（`int`, `bool`, `DateTime` など）はもともと null を持てません。`?` サフィックスを付けると `Nullable<T>` 構造体でラップされます。

```csharp
int? age = null; // データベースから取得した値が NULL かもしれない場合など
if (age.HasValue)
{
    Console.WriteLine(age.Value); // 実際の値
}
int actual = age ?? 0; // null のとき 0 を使う
```

---

## よくある落とし穴

- **`#nullable enable` を有効にしても実行時エラーはなくならない**: あくまで静的解析の補助です。ライブラリや外部データ（JSON デシリアライズ等）から来る null には引き続き注意が必要です。
- **`!` の乱用**: null forgiveness 演算子は「コンパイラへの嘘」です。本当に null でない確証がある場合だけ使用してください。
- **`?.` の戻り値型**: `obj?.Method()` の戻り値は常に nullable になります。受け取る変数は `?` 付きの型にするか、`??` で既定値を与えてください。

---

## 使い分けの基準

| 状況 | 推奨 |
|---|---|
| null の可能性があるメンバーにアクセス | `?.` |
| null のとき既定値を使いたい | `??` |
| null のときだけ初期化したい | `??=` |
| null チェック後に型を確定させたい | `is not null` パターン |
| null でないと確信できるが警告が出る | `!`（最終手段） |

---

## コードサンプル

同ディレクトリの `12_null安全_CSHARP.cs` を参照してください。
