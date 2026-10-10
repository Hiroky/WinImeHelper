# ImeHelper

Windows で US 配列キーボード + 日本語 IME / 英字 IME を切り替えて使う人向けの常駐ツールです。

日本語 IME が選択されているときは **IME を常に ON (ひらがな)** に保ちます。
これにより「英字 IME = 半角」「日本語 IME = 全角」と状態が 1 対 1 になり、日本語 IME の半角状態と英字 IME の区別に迷わなくなります。

## 動作

- 前面ウィンドウの入力言語が日本語 (`0x0411`) のとき、IME が OFF になっていたら自動で ON に戻します
- チェックはウィンドウ切り替え時 (`SetWinEventHook`) と定期ポーリング (標準 0.5 秒 / 高頻度 0.1 秒) で行います
- 英字 IME など日本語以外の入力言語のときは何もしません

## 動作環境

- Windows 10 / 11
- .NET 10 Desktop Runtime (ビルドには .NET 10 SDK)

## ビルド

```powershell
dotnet build ImeHelper.sln -c Release
```

実行ファイル: `ImeHelper\bin\Release\net10.0-windows\ImeHelper.exe`

Visual Studio で `ImeHelper.sln` を開いてビルドすることもできます。

## 使い方

`ImeHelper.exe` を起動するとタスクトレイに常駐します。

| アイコン | 状態 |
| --- | --- |
| 青い「あ」 | 有効 |
| 灰色の「あ」 | 停止中 |

- **左クリック**: 有効 / 停止 を切り替え
- **右クリック**: メニュー
  - 有効
  - チェック間隔: 高頻度 (0.1秒) / 標準 (0.5秒)
  - Windows 起動時に実行 (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` に登録)
  - 終了

設定は `HKCU\Software\ImeHelper` に保存されます。

> Windows 11 では新しいトレイアイコンは初期状態で隠れています。タスクバー右下の「^」内を確認するか、タスクバーの設定で常時表示にしてください。

## 制限事項

- 管理者権限で動作しているアプリには効きません (ImeHelper 自体を管理者権限で起動すれば効きます)
- 一部の UWP アプリや TSF 専用のアプリでは効かない場合があります
- 新しく開いたウィンドウでは一瞬だけ半角状態が見えることがあります
- 日本語 IME で直接半角英字を入力することはできなくなります (英字は英字 IME で入力する前提)

## 構成

| ファイル | 内容 |
| --- | --- |
| `ImeHelper/Program.cs` | エントリポイント、タスクトレイ常駐、スタートアップ登録 |
| `ImeHelper/ImeEnforcer.cs` | IME 状態の監視と ON への強制 |
| `ImeHelper/NativeMethods.cs` | Win32 / IMM32 API の P/Invoke 宣言 |
