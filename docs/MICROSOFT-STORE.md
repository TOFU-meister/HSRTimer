# HSRTimer の Microsoft Store 版プレビュー

移植元はフォークの `dev`、コミット `0eead2f51b9e322f719669e26c8b33744141e61b` です。調査時点の TwilightCup/HSRTimer の `dev` も同じコミットでした。対象は Human: Fall Flat Microsoft Store 版 Windows x64、Unity 2019.4.24f1、BepInEx 6.0.0-be.793、.NET 6 です。

## ビルド

ゲームを BepInEx 6 の IL2CPP 版で一度起動して `BepInEx/interop` を生成します。ゲームと同じインストールの `core` と `interop` を使用します。`BepInExDir` にはその親フォルダーを指定します。このビルドは .NET 6 SDK を使用します。

```powershell
$BepInExDir = 'C:\Path\To\HumanFallFlat\BepInEx'
dotnet build .\src\HSRTimer\HSRTimer.csproj -c Release -p:StoreIl2Cpp=true "-p:BepInExDir=$BepInExDir"
dotnet run --project .\tests\StoreLogic\StoreLogic.csproj -c Release
```

出力は `src/HSRTimer/bin/store/Release/net6.0/HSRTimer.dll` です。DLL を配布する場合は同梱の `LICENSE` も含めます。参照用のゲーム DLL と BepInEx DLL は出力に含まれません。初回テスト時は既定の `StorePreview=true` でビルドします。

## 機能の状態

| 機能 | このブランチの状態 | Windows で確認する内容 |
| --- | --- | --- |
| 起動、Unity コールバック | BepInEx 6 の永続コンポーネントホストを使用。IL2CPP 用のコンストラクターを追加 | 起動とシーン遷移後の `Update`、`FixedUpdate`、`OnGUI` |
| LRT、RT、Wake Up、セグメント、フレーム表示 | `dev` の `GameClock`、`TimerCore`、`RunState` を使用。Store 版の `AfterLoad(int,int)` と `Fall(HumanBase,bool)` に接続 | 完走、失敗した落下、ロード、ポーズ、再開、リセットと一キーリトライ。Full Game、IL、各 Co-op ロールでの境界 |
| HUD、設定、ローカライズ、プリセット | HUD、IMGUI、設定タブ、言語、レイアウトを維持。Store プレビューの設定は専用ディレクトリへ保存 | Home、ドラッグ、複数列、カスタムテキスト、設定の再起動後の反映、進捗表示の中央寄せ |
| カテゴリ、タグ、無効判定 | `dev` のルールを保持。Jumpless のローカル操作判定にネイティブオブジェクトの同一性を使用 | Checkpoint、NoCheckpoint、Jumpless、Voiceline、Glitchless、NoEC、Co-op とチート検出 |
| Subsegment と PB 参照 | サンプリングと比較を維持。JSON/JSONL は .NET 側で読み書きする形式に移植。IL・複数ステージ PB の保存を既定で停止 | 旧形式の参照の読み込み、比較、Leaderboard 表示、キャンペーン進行 |
| Markers、PB、プリセット | マーカーの定義・評価・表示を維持。プレビューでは自動およびコンソール経由の PB 更新・消去と、PB を含むマーカーのプリセット保存・復元を停止 | チェックポイント、プレーン、球、オブジェクトへの追従、複数プレイヤー。設定変更後も PB フィールドの値が保存前と一致すること |
| ゲーム内コンソール | IL2CPP デリゲートで `hsr` と `hsrtimer` を登録 | コマンド登録、`hsr clock`、`hsr status`、コンソールが開いている間の入力 |
| 外部プラグイン連携 | タグと設定タブの拡張 API、LevelCollections の既存連携を保持 | Microsoft Store に対応した外部プラグインを入れた場合の登録、コレクション進行とリトライ |
| アップデーター | リリース情報の確認コードを維持。Store ビルドの自動インストールは専用リリース配布元が必要 | リリース情報を表示できること。Mono 版 DLL を Store 環境へ適用しないこと |

ゲーム側の登録・動作はこのプレビューで実機未検証です。Phase 5.51 で確認したのは診断コンポーネントの起動、`FixedUpdate`、`AfterLoad` と `Fall` のフック、およびゲーム状態の観測です。それらの結果はプレビュー本体の実機テストとは別です。

## 初回の実機テスト

1. ゲームを終了し、従来の `BepInEx/config/HSRTimer` と過去の検証版設定・記録をバックアップします。既存プラグインと診断 DLL を `BepInEx/plugins` の外へ一時移動します。
2. 実機の `core` と `interop` で Release ビルドします。`HSRTimer.dll` 1 個を `BepInEx/plugins/HSRTimer-MicrosoftStore/` へ置きます。言語ファイルは DLL に埋め込まれた英語・簡体字中国語で起動できます。
3. ゲームを起動し、`BepInEx/LogOutput.log` で `HSRTimer Microsoft Store` のロード、`PB writes: False`、`GameAfterLoadBoundaryPatch` と `GameFallBoundaryPatch` のインストールを確認します。失敗時はログ全文を保存します。Home で設定を開き、HUD の変更を確認します。
4. Mansion で通常の落下、ポーズと再開、チェックポイントの再読み込み、通常完走を行います。コンソールで `hsr clock status` と `hsr clock history 30` を採取します。入力を再現できる条件で複数回完走し、ロードの `load`、終了の `pass`、セグメントの確定値を比較します。同じ物理ステップに複数回 `Fall` が来ても終了値が更新されないことを確認します。フレーム表示でも試し、ポーズ中に LRT は静止し RT は進むことを確認します。
5. 必要に応じて、既存の設定・参照 PB・マーカーファイルのコピーを専用の `BepInEx/config/HSRTimer-MicrosoftStore` に置きます。`settings.ini` の `[Markers]` にある `Path` は専用フォルダー配下のパスを指定します。外部の参照 PB を読み込む場合もコピーを使用します。
6. 参照 PB より速い試行を完走し、IL および利用した場合は Full Game の `meta.json` とサンプル、マーカーの `pb` と `pb_times` が更新されていないことを確認します。従来の記録・設定はテスト前後でファイルハッシュを比較します。テストの次段階としてタグ、セグメント比較、マーカー、レベル連続遷移と Co-op を個別に確認します。

## 今回の検証

- BepInEx be.793 と入手済みの Store 版 interop（ゲーム、HumanAPI、CoreModule、IMGUI、TextRendering、UI）を使い、未入手のモジュールを Unity 2019.4.24 用の参照および補助生成した `Il2Cppmscorlib` で補った .NET 6 Release コンパイルは成功しました。この補助参照版の DLL は Windows のテスト用には使用できません。実機生成済みの `BepInEx/interop` 一式でのビルド結果は未確認です。
- 自動テストでは、物理ステップ内での Tick の共有、`FixedUpdate` より前と後に終了境界が来た場合の同値性、重複した終了通知、リトライ中と待機中の分岐、開始時点とポーズ時間のスナップショット、PB の JSON/JSONL 互換読み込み、プレビューの PB ガードとマーカー保存先の制限を検証します。Unity のネイティブフックの発火順と描画は Windows 側のテスト項目です。
