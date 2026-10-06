# ParameterModelの接続と更新

## 現在の責務

- `VGaugePlaceModel`：ゲージ現在値・上限、ブースト引き継ぎ量、消費中／中断中の残量と消費レート。
- `SpeedPlaceModel`：0～99.9の表示対象速度と、攻撃中などの速度表示上書き。物理速度は従来どおり`PlayerMotor`が管理する。
- `PlayerRootController`：Inspectorのモデル参照を`PlayerStateMachineComponent.Initialize`へ渡す。
- `PlayerStateMachineComponent`：既存State APIをモデルへ委譲し、更新タイミングとUIへの反映を担当する。
- `VGaugeUI`／`VSpeedUI`：渡された値の描画のみ。モデルの生成・値の保持・初期化は行わない。

## Unityの設定

`Prot_Stage1`の`PlayerRootController`は、配置済みの`References/ParameterModel`にある2つのコンポーネントを参照するよう設定済み。
ゲージ上限は`VGaugePlaceModel`の`Max Gauge`で設定する（既存値100を移行）。

別Sceneでは、PlayerRootControllerの`V Gauge Place Model`と`Speed Place Model`へコンポーネントを割り当てる。
未設定の場合はそのPlayer配下から取得する。モデル未配置の既存Prefabとの互換性のため、見つからないモデルだけPlayer自身へ実行時に追加する。Scene全体の検索は行わない。

## 更新順序とAPI

1. 初期化時にモデルの実行時データをリセットする。モデルのAwake／Startには依存しない。
2. チャージ開始・更新・中断・被弾は`Owner.VGaugePlaceModel.SetGaugeRate(...)`を使う。UIが未設定でも更新する。
3. ブースト消費は従来どおり`FixedUpdate`で`ConsumeBoostGauge(Time.fixedDeltaTime)`を呼ぶ。
4. `LateUpdate`で水平速度を`SpeedPlaceModel.UpdateSpeed`へ渡し、毎描画フレーム保存する。
5. `RefreshParameterDisplays`でモデルから取得した値をUIへ渡す。UIがなくてもモデルは更新される。

```csharp
// State内：従来のプロパティは同じ形で利用可能。
Owner.CarriedBoostGaugeRate = chargeRate;
float remainingRate = Owner.SuspendedBoostGaugeRate;

// モデルにもゲッター・セッターとプロパティを用意。
Owner.VGaugePlaceModel.GaugeRate = 0.5f;
int gauge = Owner.VGaugePlaceModel.GetGauge();
float speed = Owner.SpeedPlaceModel.GetSpeed();
```

ゲージの現在値は従来どおり整数に丸める。ゲーム進行用の引き継ぎ量・残量はfloatのまま保持し、表示の丸めでブースト持続時間が変わらないようにしている。
速度表示の上書きは既存の`SetSpeedDisplayOverride`／`ClearSpeedDisplayOverride`からモデルへ委譲する。
UIの旧`SetGauge`／`AddGauge`／`GetGauge`、`GetSpeed`はモデル側のAPIを使用する。

## 次の作業者向け

今回MVP、Presenter、イベント通知は追加していない。
モデルはUI・State・PlayerMotorを参照しない。表示値の接続は`RefreshParameterDisplays`に集約しているため、将来の変更はここを入口に検討できる。
刃のチャージ演出は既存どおりStateから`VGaugeUI.SetCharging`を呼ぶ。移動・チャージ時間・状態遷移は今回変更していない。

確認項目：チャージ、ブースト消費、中断と復帰、ジャンプ／被弾のリセット、通常速度と攻撃時の表示上書き、UIを外した状態のモデル更新。