# Distortion ver2

プロジェクトに導入済みのNOVA Shader 3.6.0を基に、標準DistortionのUI・機能を維持し、UnlitのRim／LuminanceをTransparencyへ追加したShaderです。

- Shader名: `Nova/Particles/Distortion ver2`
- 配置先: `Assets/Shader/Distortion_ver2/`
- 元のNOVAパッケージは変更していません。
- Shader名、プロパティ名、既存の.meta GUIDを維持しているため、既存マテリアルの参照・設定値は引き継がれます。

## Inspector

標準DistortionのRender Settings、Distortion、Flow Mapping、Alpha Transitionの描画処理を使用しています。テクスチャ欄、Offset Coords、各パラメータ右側のCustom Coord、折りたたみ、Undo／Redo、キーワード設定も維持しています。

NOVAの描画用ヘルパーはinternalのため、必要な部分をプロジェクト側の既存Editorファイル内に複製しています。リフレクションやNOVA本体の改変は使用していません。

Transparencyの配置はUnlitと同じです。

1. Rim: Progress / Sharpness / Inverse
2. Luminance: Progress / Sharpness / Inverse
3. Soft Particles
4. Depth Fade

Rim／LuminanceのProgressとSharpnessにも、標準と同じCustom Coord欄があります。Custom Dataは指定した値へ加算されます。新規マテリアルでは両機能OFF、Progress・SharpnessはUnlitと同じ0.5です。既存マテリアルに保存された値は自動変更しません。

## 動作

RimはUnlitの透明度処理と同じ`abs(N・V)`を入力にします。Inverse OFFでは正面側を残して輪郭を弱め、Inverse ONでは輪郭側を残します。球体の輪郭をぼかす場合は、Rim ON、Progress 0.5、Sharpness 0、Inverse OFFから調整できます。

LuminanceはDistortion TextureのRGB輝度を入力に、Unlitと同じProgress／Sharpness／Inverseの曲線を適用します。背景の輝度ではありません。Use As Normal MapがONの場合は、展開して0〜1へ戻したRGBを使用します。DistortionにはUnlitのTintやEmissionがないため、それらを含むUnlitの最終色とは入力が異なります。

Distortionの描画先は加算式の歪みベクトルバッファなので、係数は出力Alphaではなく歪み量へ乗算します。Rim、Luminance、Alpha Transition、Soft Particles、Depth Fadeは併用できます。Rim／LuminanceがOFFの場合は標準Distortionと同じ描画になります。

Sharpnessが1の場合に限り、幅ゼロのsmoothstepによる不定値を避けるため段階的な閾値処理を使います。通常範囲の曲線はUnlitと同じです。

## Particle System

Rimには法線が必要です。球体Meshを使うParticle SystemではCustom Vertex StreamsへNormalを含めます。Custom1／Custom2を使う場合、NOVAと同じ配置にします。

`Position / Normal / Color / UV / UV2 / Custom1.xyzw / Custom2.xyzw`

UV2でTEXCOORD0の後半を埋め、Custom1／Custom2をTEXCOORD1／2へ対応させます。Mesh GPU InstancingもNOVAのデータ配置を使用します。通常のParticle ColorのAlphaは、元のDistortionと同様に歪みのフェードには使用しません。

## 検証と制約

検証結果は`validation~/validation.txt`を参照してください。独立した検証用プロジェクトでShaderコンパイルとGPU描画を確認しており、ユーザーのシーンを開いての見た目調整は行っていません。

利点: 標準の操作配置・Custom Coordを維持し、透明度のマスクを追加できます。

制約: NOVAの共通HLSLとEditor APIへ依存するため、NOVA更新時は互換性確認が必要です。旧ver2の独自Inspectorから標準の操作配置へ変わります。

参照元（実装はプロジェクト内の導入済みパッケージと照合）:

- https://github.com/CyberAgentGameEntertainment/NovaShader/blob/3.6.0/Assets/Nova/Editor/Core/Scripts/ParticlesDistortionGUI.cs
- https://github.com/CyberAgentGameEntertainment/NovaShader/blob/3.6.0/Assets/Nova/Editor/Core/Scripts/ParticlesUberCommonGUI.cs
- https://github.com/CyberAgentGameEntertainment/NovaShader/blob/3.6.0/Assets/Nova/Runtime/Core/Shaders/ParticlesUberUnlit.hlsl

ライセンスは`NOVA-LICENSE.txt`を参照してください。
