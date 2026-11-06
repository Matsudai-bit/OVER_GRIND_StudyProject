// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using Nova.Editor.Core.Scripts;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine;
using PropertyNames = Nova.Editor.Core.Scripts.MaterialPropertyNames;

namespace ProjectEffect.Editor
{
    /// <summary>
    ///     GUI for a material assigned the ParticlesDistortion Shader.
    /// </summary>
    public sealed class DistortionVer2GUI : ParticlesGUI
    {

        public const string ShaderName = "Nova/Particles/Distortion ver2";

        public static void Setup(Material material)
        {
            if (!material || !material.shader || material.shader.name != ShaderName) return;
            ParticlesDistortionMaterialPostProcessor.SetupMaterialKeywords(material);
            SetTransparencyKeyword(material, "_TRANSPARENCY_BY_RIM", "_RimTransparencyEnabled");
            SetTransparencyKeyword(material, "_TRANSPARENCY_BY_LUMINANCE", "_LuminanceTransparencyEnabled");
        }

        private static void SetTransparencyKeyword(Material material, string keyword, string property)
        {
            if (material.GetFloat(property) > 0.5f) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            base.OnGUI(editor, properties);
            if (EditorGUI.EndChangeCheck())
                foreach (UnityEngine.Object target in editor.targets) Setup((Material)target);
        }

        protected override void SetupProperties(MaterialProperty[] properties)
        {
            // Render Settings
            _cullProp.Setup(properties);
            _zTestProp.Setup(properties);

            // Distortion
            _baseMapProp.Setup(properties);
            _baseMapOffsetXCoordProp.Setup(properties);
            _baseMapOffsetYCoordProp.Setup(properties);
            _baseMapChannelsXProp.Setup(properties);
            _baseMapChannelsYProp.Setup(properties);
            _baseMapRotationProp.Setup(properties);
            _baseMapRotationCoordProp.Setup(properties);
            _baseMapRotationOffsetsProp.Setup(properties);
            _baseMapMirrorSamplingProp.Setup(properties);
            _baseMapUnpackNormalProp.Setup(properties);
            _distortionIntensityModeProp.Setup(properties);
            _distortionIntensityProp.Setup(properties);
            _distortionIntensityCoordProp.Setup(properties);
            _distortionIntensityXProp.Setup(properties);
            _distortionIntensityXCoordProp.Setup(properties);
            _distortionIntensityYProp.Setup(properties);
            _distortionIntensityYCoordProp.Setup(properties);

            // Flow Map
            _flowMapProp.Setup(properties);
            _flowMapOffsetXCoordProp.Setup(properties);
            _flowMapOffsetYCoordProp.Setup(properties);
            _flowMapChannelsXProp.Setup(properties);
            _flowMapChannelsYProp.Setup(properties);
            _flowIntensityProp.Setup(properties);
            _flowIntensityCoordProp.Setup(properties);
            _flowMapTargetProp.Setup(properties);
            _flowMapRotationProp.Setup(properties);
            _flowMapRotationCoordProp.Setup(properties);
            _flowMapRotationOffsetsProp.Setup(properties);

            // Alpha Transition
            _alphaTransitionModeProp.Setup(properties);
            _alphaTransitionMapProp.Setup(properties);
            _alphaTransitionMapOffsetXCoordProp.Setup(properties);
            _alphaTransitionMapOffsetYCoordProp.Setup(properties);
            _alphaTransitionMapChannelsXProp.Setup(properties);

            _alphaTransitionProgressProp.Setup(properties);
            _alphaTransitionProgressCoordProp.Setup(properties);
            _dissolveSharpnessProp.Setup(properties);
            _alphaTransitionMapRotationProp.Setup(properties);
            _alphaTransitionMapRotationCoordProp.Setup(properties);
            _alphaTransitionMapRotationOffsetsProp.Setup(properties);

            // Transparency
            _rimTransparencyEnabledProp.Setup(properties);
            _rimTransparencyProgressProp.Setup(properties);
            _rimTransparencyProgressCoordProp.Setup(properties);
            _rimTransparencySharpnessProp.Setup(properties);
            _rimTransparencySharpnessCoordProp.Setup(properties);
            _inverseRimTransparencyProp.Setup(properties);
            _luminanceTransparencyEnabledProp.Setup(properties);
            _luminanceTransparencyProgressProp.Setup(properties);
            _luminanceTransparencyProgressCoordProp.Setup(properties);
            _luminanceTransparencySharpnessProp.Setup(properties);
            _luminanceTransparencySharpnessCoordProp.Setup(properties);
            _inverseLuminanceTransparencyProp.Setup(properties);
            _softParticlesEnabledProp.Setup(properties);
            _softParticlesIntensityProp.Setup(properties);
            _depthFadeEnabledProp.Setup(properties);
            _depthFadeNearProp.Setup(properties);
            _depthFadeFarProp.Setup(properties);
            _depthFadeWidthProp.Setup(properties);
        }

        protected override void Initialize(MaterialEditor editor, MaterialProperty[] properties)
        {
            var prefsKeyPrefix = $"{GetType().Namespace}.{GetType().Name}.";
            var renderSettingsFoldoutKey = $"{prefsKeyPrefix}{nameof(RenderSettingsFoldout)}";
            var distortionFoldoutKey = $"{prefsKeyPrefix}{nameof(DistortionFoldout)}";
            var flowMapFoldoutKey = $"{prefsKeyPrefix}{nameof(FlowMapFoldout)}";
            var alphaTransitionFoldoutKey = $"{prefsKeyPrefix}{nameof(AlphaTransitionFoldout)}";
            var transparencyFoldoutKey = $"{prefsKeyPrefix}{nameof(TransparencyFoldout)}";

            RenderSettingsFoldout = new DistortionVer2BoolPreference(renderSettingsFoldoutKey, true);
            DistortionFoldout = new DistortionVer2BoolPreference(distortionFoldoutKey, true);
            FlowMapFoldout = new DistortionVer2BoolPreference(flowMapFoldoutKey, true);
            AlphaTransitionFoldout = new DistortionVer2BoolPreference(alphaTransitionFoldoutKey, true);
            TransparencyFoldout = new DistortionVer2BoolPreference(transparencyFoldoutKey, true);
        }

        protected override void DrawGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            using (var foldoutScope =
                   new DistortionVer2EditorUtility.FoldoutHeaderScope(RenderSettingsFoldout.Value, "Render Settings"))
            {
                if (foldoutScope.Foldout) DrawRenderSettingsProperties(editor, properties);

                RenderSettingsFoldout.Value = foldoutScope.Foldout;
            }

            using (var foldoutScope =
                   new DistortionVer2EditorUtility.FoldoutHeaderScope(DistortionFoldout.Value, "Distortion"))
            {
                if (foldoutScope.Foldout) DrawDistortionProperties(editor, properties);

                DistortionFoldout.Value = foldoutScope.Foldout;
            }

            using (var foldoutScope =
                   new DistortionVer2EditorUtility.FoldoutHeaderScope(FlowMapFoldout.Value, "Flow Mapping"))
            {
                if (foldoutScope.Foldout) DrawFlowMapProperties(editor, properties);

                FlowMapFoldout.Value = foldoutScope.Foldout;
            }

            using (var foldoutScope =
                   new DistortionVer2EditorUtility.FoldoutHeaderScope(AlphaTransitionFoldout.Value, "Alpha Transition"))
            {
                if (foldoutScope.Foldout) DrawAlphaTransitionProperties(editor, properties);

                AlphaTransitionFoldout.Value = foldoutScope.Foldout;
            }

            using (var foldoutScope =
                   new DistortionVer2EditorUtility.FoldoutHeaderScope(TransparencyFoldout.Value, "Transparency"))
            {
                if (foldoutScope.Foldout) DrawTransparencyProperties(editor, properties);

                TransparencyFoldout.Value = foldoutScope.Foldout;
            }
        }

        protected override void MaterialChanged(Material material)
        {
            Setup(material);
        }

        private void DrawRenderSettingsProperties(MaterialEditor editor, MaterialProperty[] properties)
        {
            DistortionVer2EditorUtility.DrawEnumProperty<RenderFace>(editor, "Render Face", _cullProp.Value);
            DistortionVer2EditorUtility.DrawEnumProperty<ZTest>(editor, "ZTest", _zTestProp.Value);
        }

        private void DrawDistortionProperties(MaterialEditor editor, MaterialProperty[] properties)
        {
            DistortionVer2EditorUtility.DrawTexture<CustomCoord>(editor, _baseMapProp.Value, _baseMapOffsetXCoordProp.Value,
                _baseMapOffsetYCoordProp.Value,
                _baseMapChannelsXProp.Value, _baseMapChannelsYProp.Value);

            DistortionVer2EditorUtility.DrawEnumProperty<DistortionIntensityMode>(editor, "Intensity Mode",
                _distortionIntensityModeProp.Value);
            var intensityMode = (DistortionIntensityMode)_distortionIntensityModeProp.Value.floatValue;
            using (new EditorGUI.IndentLevelScope())
            {
                if (intensityMode == DistortionIntensityMode.Single)
                {
                    DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Intensity",
                        _distortionIntensityProp.Value, _distortionIntensityCoordProp.Value);
                }
                else
                {
                    DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Intensity X",
                        _distortionIntensityXProp.Value, _distortionIntensityXCoordProp.Value);
                    DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Intensity Y",
                        _distortionIntensityYProp.Value, _distortionIntensityYCoordProp.Value);
                }
            }

            DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Rotation",
                _baseMapRotationProp.Value, _baseMapRotationCoordProp.Value);
            using (new EditorGUI.IndentLevelScope())
            {
                DistortionVer2EditorUtility.DrawVector2Property(editor, "Offset", _baseMapRotationOffsetsProp.Value);
            }

            DistortionVer2EditorUtility.DrawToggleProperty(editor, "Mirror Sampling", _baseMapMirrorSamplingProp.Value);
            DistortionVer2EditorUtility.DrawToggleProperty(editor, "Use As Normal Map", _baseMapUnpackNormalProp.Value);
        }

        private void DrawFlowMapProperties(MaterialEditor editor, MaterialProperty[] properties)
        {
            DistortionVer2EditorUtility.DrawTexture<CustomCoord>(editor, _flowMapProp.Value, _flowMapOffsetXCoordProp.Value,
                _flowMapOffsetYCoordProp.Value, _flowMapChannelsXProp.Value, _flowMapChannelsYProp.Value);
            DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Rotation",
                _flowMapRotationProp.Value, _flowMapRotationCoordProp.Value);
            using (new EditorGUI.IndentLevelScope())
            {
                DistortionVer2EditorUtility.DrawVector2Property(editor, "Offset", _flowMapRotationOffsetsProp.Value);
            }
            DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Intensity", _flowIntensityProp.Value,
                _flowIntensityCoordProp.Value);
            DistortionVer2EditorUtility.DrawEnumFlagsProperty<FlowMapTargetDistortion>(editor, "Targets",
                _flowMapTargetProp.Value);
        }

        private void DrawAlphaTransitionProperties(MaterialEditor editor, MaterialProperty[] properties)
        {
            DistortionVer2EditorUtility.DrawEnumProperty<AlphaTransitionMode>(editor, "Mode",
                _alphaTransitionModeProp.Value);
            var mode = (AlphaTransitionMode)_alphaTransitionModeProp.Value.floatValue;
            if (mode != AlphaTransitionMode.None)
            {
                DistortionVer2EditorUtility.DrawTexture<CustomCoord>(editor, _alphaTransitionMapProp.Value,
                    _alphaTransitionMapOffsetXCoordProp.Value, _alphaTransitionMapOffsetYCoordProp.Value,
                    _alphaTransitionMapChannelsXProp.Value, null);
                DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Rotation",
                    _alphaTransitionMapRotationProp.Value, _alphaTransitionMapRotationCoordProp.Value);
                using (new EditorGUI.IndentLevelScope())
                {
                    DistortionVer2EditorUtility.DrawVector2Property(editor, "Offset", _alphaTransitionMapRotationOffsetsProp.Value);
                }

                DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Progress",
                    _alphaTransitionProgressProp.Value, _alphaTransitionProgressCoordProp.Value);
                if (mode == AlphaTransitionMode.Dissolve)
                    editor.ShaderProperty(_dissolveSharpnessProp.Value, "Edge Sharpness");
            }
        }

        private void DrawTransparencyProperties(MaterialEditor editor, MaterialProperty[] properties)
        {
            DistortionVer2EditorUtility.DrawToggleProperty(editor, "Rim", _rimTransparencyEnabledProp.Value);
            if (_rimTransparencyEnabledProp.Value.floatValue > 0.5f)
                using (new EditorGUI.IndentLevelScope())
                {
                    DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Progress",
                        _rimTransparencyProgressProp.Value, _rimTransparencyProgressCoordProp.Value);
                    DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Sharpness",
                        _rimTransparencySharpnessProp.Value, _rimTransparencySharpnessCoordProp.Value);
                    DistortionVer2EditorUtility.DrawToggleProperty(editor, "Inverse",
                        _inverseRimTransparencyProp.Value);
                }

            DistortionVer2EditorUtility.DrawToggleProperty(editor, "Luminance",
                _luminanceTransparencyEnabledProp.Value);
            if (_luminanceTransparencyEnabledProp.Value.floatValue > 0.5f)
                using (new EditorGUI.IndentLevelScope())
                {
                    DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Progress",
                        _luminanceTransparencyProgressProp.Value,
                        _luminanceTransparencyProgressCoordProp.Value);
                    DistortionVer2EditorUtility.DrawPropertyAndCustomCoord<CustomCoord>(editor, "Sharpness",
                        _luminanceTransparencySharpnessProp.Value,
                        _luminanceTransparencySharpnessCoordProp.Value);
                    DistortionVer2EditorUtility.DrawToggleProperty(editor, "Inverse",
                        _inverseLuminanceTransparencyProp.Value);
                }

            DistortionVer2EditorUtility.DrawToggleProperty(editor, "Soft Particles", _softParticlesEnabledProp.Value);
            var softParticlesEnabled = _softParticlesEnabledProp.Value.floatValue >= 0.5f;
            if (softParticlesEnabled)
                using (new EditorGUI.IndentLevelScope())
                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    editor.ShaderProperty(_softParticlesIntensityProp.Value, "Intensity");
                    if (changeCheckScope.changed)
                        _softParticlesIntensityProp.Value.floatValue =
                            Mathf.Max(0, _softParticlesIntensityProp.Value.floatValue);
                }

            DistortionVer2EditorUtility.DrawToggleProperty(editor, "Depth Fade", _depthFadeEnabledProp.Value);
            var depthFadeEnabled = _depthFadeEnabledProp.Value.floatValue >= 0.5f;
            if (depthFadeEnabled)
                using (new EditorGUI.IndentLevelScope())
                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    DistortionVer2EditorUtility.DrawTwoFloatProperties("Distance", _depthFadeNearProp.Value, "Near",
                        _depthFadeFarProp.Value, "Far", editor);
                    editor.ShaderProperty(_depthFadeWidthProp.Value, "Width");
                }
        }

        #region Render Settings Material Properties

        private readonly Property _cullProp = new(PropertyNames.Cull);
        private readonly Property _zTestProp = new(PropertyNames.ZTest);

        #endregion

        #region Foldout Properties

        private DistortionVer2BoolPreference RenderSettingsFoldout { get; set; }
        private DistortionVer2BoolPreference DistortionFoldout { get; set; }
        private DistortionVer2BoolPreference FlowMapFoldout { get; set; }
        private DistortionVer2BoolPreference AlphaTransitionFoldout { get; set; }
        private DistortionVer2BoolPreference TransparencyFoldout { get; set; }

        #endregion

        #region Distortion Material Properties

        private readonly Property _baseMapProp = new(PropertyNames.BaseMap);
        private readonly Property _baseMapOffsetXCoordProp = new(PropertyNames.BaseMapOffsetXCoord);
        private readonly Property _baseMapOffsetYCoordProp = new(PropertyNames.BaseMapOffsetYCoord);
        private readonly Property _baseMapChannelsXProp = new(PropertyNames.BaseMapChannelsX);
        private readonly Property _baseMapChannelsYProp = new(PropertyNames.BaseMapChannelsY);
        private readonly Property _baseMapRotationProp = new(PropertyNames.BaseMapRotation);
        private readonly Property _baseMapRotationCoordProp = new(PropertyNames.BaseMapRotationCoord);
        private readonly Property _baseMapRotationOffsetsProp = new(PropertyNames.BaseMapRotationOffsets);
        private readonly Property _baseMapMirrorSamplingProp = new(PropertyNames.BaseMapMirrorSampling);
        private readonly Property _baseMapUnpackNormalProp = new(PropertyNames.BaseMapUnpackNormal);
        private readonly Property _distortionIntensityModeProp = new(PropertyNames.DistortionIntensityMode);
        private readonly Property _distortionIntensityProp = new(PropertyNames.DistortionIntensity);
        private readonly Property _distortionIntensityCoordProp = new(PropertyNames.DistortionIntensityCoord);
        private readonly Property _distortionIntensityXProp = new(PropertyNames.DistortionIntensityX);
        private readonly Property _distortionIntensityXCoordProp = new(PropertyNames.DistortionIntensityXCoord);
        private readonly Property _distortionIntensityYProp = new(PropertyNames.DistortionIntensityY);
        private readonly Property _distortionIntensityYCoordProp = new(PropertyNames.DistortionIntensityYCoord);

        #endregion

        #region Flow Map Material Properties

        private readonly Property _flowMapProp = new(PropertyNames.FlowMap);
        private readonly Property _flowMapOffsetXCoordProp = new(PropertyNames.FlowMapOffsetXCoord);
        private readonly Property _flowMapOffsetYCoordProp = new(PropertyNames.FlowMapOffsetYCoord);
        private readonly Property _flowMapChannelsXProp = new(PropertyNames.FlowMapChannelsX);
        private readonly Property _flowMapChannelsYProp = new(PropertyNames.FlowMapChannelsY);
        private readonly Property _flowIntensityProp = new(PropertyNames.FlowIntensity);
        private readonly Property _flowIntensityCoordProp = new(PropertyNames.FlowIntensityCoord);
        private readonly Property _flowMapTargetProp = new(PropertyNames.FlowMapTarget);
        private readonly Property _flowMapRotationProp = new(PropertyNames.FlowMapRotation);
        private readonly Property _flowMapRotationCoordProp = new(PropertyNames.FlowMapRotationCoord);
        private readonly Property _flowMapRotationOffsetsProp = new(PropertyNames.FlowMapRotationOffsets);

        #endregion

        #region Alpha Transition Material Properties

        private readonly Property _alphaTransitionModeProp = new(PropertyNames.AlphaTransitionMode);
        private readonly Property _alphaTransitionMapProp = new(PropertyNames.AlphaTransitionMap);

        private readonly Property _alphaTransitionMapOffsetXCoordProp =
            new(PropertyNames.AlphaTransitionMapOffsetXCoord);

        private readonly Property _alphaTransitionMapOffsetYCoordProp =
            new(PropertyNames.AlphaTransitionMapOffsetYCoord);

        private readonly Property _alphaTransitionMapChannelsXProp = new(PropertyNames.AlphaTransitionMapChannelsX);

        private readonly Property _alphaTransitionProgressProp = new(PropertyNames.AlphaTransitionProgress);

        private readonly Property _alphaTransitionProgressCoordProp = new(PropertyNames.AlphaTransitionProgressCoord);

        private readonly Property _dissolveSharpnessProp = new(PropertyNames.DissolveSharpness);
        private readonly Property _alphaTransitionMapRotationProp = new(PropertyNames.AlphaTransitionMapRotation);
        private readonly Property _alphaTransitionMapRotationCoordProp = new(PropertyNames.AlphaTransitionMapRotationCoord);
        private readonly Property _alphaTransitionMapRotationOffsetsProp = new(PropertyNames.AlphaTransitionMapRotationOffsets);

        #endregion

        private readonly Property _rimTransparencyEnabledProp = new("_RimTransparencyEnabled");
        private readonly Property _rimTransparencyProgressProp = new("_RimTransparencyProgress");
        private readonly Property _rimTransparencyProgressCoordProp = new("_RimTransparencyProgressCoord");
        private readonly Property _rimTransparencySharpnessProp = new("_RimTransparencySharpness");
        private readonly Property _rimTransparencySharpnessCoordProp = new("_RimTransparencySharpnessCoord");
        private readonly Property _inverseRimTransparencyProp = new("_InverseRimTransparency");
        private readonly Property _luminanceTransparencyEnabledProp = new("_LuminanceTransparencyEnabled");
        private readonly Property _luminanceTransparencyProgressProp = new("_LuminanceTransparencyProgress");
        private readonly Property _luminanceTransparencyProgressCoordProp = new("_LuminanceTransparencyProgressCoord");
        private readonly Property _luminanceTransparencySharpnessProp = new("_LuminanceTransparencySharpness");
        private readonly Property _luminanceTransparencySharpnessCoordProp = new("_LuminanceTransparencySharpnessCoord");
        private readonly Property _inverseLuminanceTransparencyProp = new("_InverseLuminanceTransparency");

        #region Transparency Material Properties

        private readonly Property _softParticlesEnabledProp = new(PropertyNames.SoftParticlesEnabled);
        private readonly Property _softParticlesIntensityProp = new(PropertyNames.SoftParticlesIntensity);
        private readonly Property _depthFadeEnabledProp = new(PropertyNames.DepthFadeEnabled);
        private readonly Property _depthFadeNearProp = new(PropertyNames.DepthFadeNear);
        private readonly Property _depthFadeFarProp = new(PropertyNames.DepthFadeFar);
        private readonly Property _depthFadeWidthProp = new(PropertyNames.DepthFadeWidth);

        #endregion
    }
    internal static class DistortionVer2EditorUtility
    {
        private const int ProceduralTextureSize = 16;

        private static readonly Color HeaderBackgroundColorDark = new(40.0f / 256.0f, 40.0f / 256.0f, 40.0f / 256.0f);

        private static readonly Color ContentsBackgroundColorDark = new(48.0f / 256.0f, 48.0f / 256.0f, 48.0f / 256.0f);

        private static readonly Color BorderColorDark = new(16.0f / 256.0f, 16.0f / 256.0f, 16.0f / 256.0f);

        private static readonly Color HeaderBackgroundColorLight =
            new(170.0f / 256.0f, 170.0f / 256.0f, 170.0f / 256.0f);

        private static readonly Color ContentsBackgroundColorLight =
            new(193.0f / 256.0f, 193.0f / 256.0f, 193.0f / 256.0f);

        private static readonly Color BorderColorLight = new(101.0f / 256.0f, 101.0f / 256.0f, 101.0f / 256.0f);

        private static Texture2D _headerBackgroundTexture;
        private static Texture2D _contentsBackgroundTexture;
        private static GUIStyle _headerStyle;
        private static GUIStyle _contentsStyle;
        private static Texture _foldoutTrueIcon;
        private static Texture _foldoutFalseIcon;

        private static Color TextColor => EditorStyles.label.normal.textColor;

        private static Color HeaderBackgroundColor =>
            EditorGUIUtility.isProSkin ? HeaderBackgroundColorDark : HeaderBackgroundColorLight;

        private static Color ContentsBackgroundColor =>
            EditorGUIUtility.isProSkin ? ContentsBackgroundColorDark : ContentsBackgroundColorLight;

        private static Color BorderColor => EditorGUIUtility.isProSkin ? BorderColorDark : BorderColorLight;

        private static Texture2D HeaderBackgroundTexture
        {
            get
            {
                if (_headerBackgroundTexture == null)
                {
                    _headerBackgroundTexture = new Texture2D(ProceduralTextureSize, ProceduralTextureSize);
                    for (var i = 0; i < ProceduralTextureSize; i++)
                    for (var j = 0; j < ProceduralTextureSize; j++)
                    {
                        var isEdge = i == 0 || i == ProceduralTextureSize - 1 || j == 0 ||
                                     j == ProceduralTextureSize - 1;
                        var color = isEdge ? BorderColor : HeaderBackgroundColor;
                        _headerBackgroundTexture.SetPixel(i, j, color);
                    }

                    _headerBackgroundTexture.wrapMode = TextureWrapMode.Clamp;
                    _headerBackgroundTexture.filterMode = FilterMode.Point;
                    _headerBackgroundTexture.Apply();
                }

                return _headerBackgroundTexture;
            }
        }

        private static Texture2D ContentsBackgroundTexture
        {
            get
            {
                if (_contentsBackgroundTexture == null)
                {
                    _contentsBackgroundTexture = new Texture2D(ProceduralTextureSize, ProceduralTextureSize);
                    for (var i = 0; i < ProceduralTextureSize; i++)
                    for (var j = 0; j < ProceduralTextureSize; j++)
                    {
                        var isEdge = i == 0 || i == ProceduralTextureSize - 1 || j == 0;
                        var color = isEdge ? BorderColor : ContentsBackgroundColor;
                        _contentsBackgroundTexture.SetPixel(i, j, color);
                    }

                    _contentsBackgroundTexture.wrapMode = TextureWrapMode.Clamp;
                    _contentsBackgroundTexture.filterMode = FilterMode.Point;
                    _contentsBackgroundTexture.Apply();
                }

                return _contentsBackgroundTexture;
            }
        }

        private static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null || _headerBackgroundTexture == null)
                    _headerStyle = new GUIStyle
                    {
                        normal = new GUIStyleState
                        {
                            background = HeaderBackgroundTexture
                        },
                        border = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(8, 8, 0, 4)
                    };

                return _headerStyle;
            }
        }

        private static GUIStyle ContentsStyle
        {
            get
            {
                if (_contentsStyle == null || _contentsBackgroundTexture == null)
                    _contentsStyle = new GUIStyle
                    {
                        normal = new GUIStyleState
                        {
                            background = ContentsBackgroundTexture
                        },
                        border = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(8, 8, 0, 0),
                        margin = new RectOffset(0, 0, 0, 8)
                    };

                return _contentsStyle;
            }
        }



        public static void DrawTexture<TCustomCoord>(MaterialEditor editor, MaterialProperty textureProperty,
            bool drawTilingAndOffset) where TCustomCoord : Enum
        {
            DrawTexture<TCustomCoord>(editor, textureProperty, drawTilingAndOffset, null, null, null, null);
        }

        public static void DrawTexture<TCustomCoord>(MaterialEditor editor, MaterialProperty textureProperty,
            MaterialProperty offsetCoordXProperty, MaterialProperty offsetCoordYProperty,
            MaterialProperty channelsXProperty, MaterialProperty channelsYProperty) where TCustomCoord : Enum
        {
            DrawTexture<TCustomCoord>(editor, textureProperty, true,
                offsetCoordXProperty, offsetCoordYProperty, channelsXProperty, channelsYProperty);
        }

        private static void DrawTexture<TCustomCoord>(MaterialEditor editor, MaterialProperty textureProperty,
            bool drawTilingAndOffset,
            MaterialProperty offsetCoordXProperty, MaterialProperty offsetCoordYProperty,
            MaterialProperty channelsXProperty, MaterialProperty channelsYProperty) where TCustomCoord : Enum
        {
            var propertyCount = 0;
            if (drawTilingAndOffset) propertyCount += 2;

            var useOffsetCoord = offsetCoordXProperty != null && offsetCoordYProperty != null;
            if (useOffsetCoord) propertyCount += 1;

            var useChannelsX = channelsXProperty != null;
            var useChannelsY = channelsYProperty != null;
            var useChannels = useChannelsX || useChannelsY;
            if (useChannels) propertyCount += 1;
            var contentsHeight = propertyCount * EditorGUIUtility.singleLineHeight +
                                 (propertyCount - 2) * EditorGUIUtility.standardVerticalSpacing;
            var fullHeight = Mathf.Max(contentsHeight + 8, 64);
            fullHeight += EditorGUIUtility.standardVerticalSpacing;

            var fullRect = EditorGUILayout.GetControlRect(false, fullHeight);

            var innerRect = fullRect;
            innerRect = EditorGUI.IndentedRect(innerRect);
            innerRect.height -= 8;
            innerRect.y += 4;

            var textureRect = innerRect;
            textureRect.height = 56;
            textureRect.width = textureRect.height;
            textureRect.y += (innerRect.height - textureRect.height) / 2;

            var contentsRect = fullRect;
            contentsRect = EditorGUI.IndentedRect(contentsRect);
            contentsRect.height = contentsHeight;
            contentsRect.y += (fullHeight - contentsHeight) / 2;
            contentsRect.xMin += textureRect.width + 4;

            var labelRect = contentsRect;
            labelRect.height = EditorGUIUtility.singleLineHeight;
            labelRect.xMax -= fullRect.width - EditorGUIUtility.labelWidth;

            var propertyRect = contentsRect;
            propertyRect.height = EditorGUIUtility.singleLineHeight;
            propertyRect.xMin += labelRect.width;

            // Draw properties.
            using (new ResetIndentLevelScope())
            {
                // Texture
                Type textureType;
                switch (textureProperty.textureDimension)
                {
                    case TextureDimension.Unknown:
                    case TextureDimension.None:
                    case TextureDimension.Any:
                        textureType = typeof(Texture);
                        break;
                    case TextureDimension.Tex2D:
                    case TextureDimension.Cube:
                        textureType = typeof(Texture2D);
                        break;
                    case TextureDimension.Tex3D:
                        textureType = typeof(Texture3D);
                        break;
                    case TextureDimension.Tex2DArray:
                    case TextureDimension.CubeArray:
                        textureType = typeof(Texture2DArray);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.showMixedValue = textureProperty.hasMixedValue;
                    var texture = (Texture)EditorGUI.ObjectField(textureRect, textureProperty.textureValue, textureType,
                        false);
                    EditorGUI.showMixedValue = false;
                    if (changeCheckScope.changed)
                    {
                        editor.RegisterPropertyChangeUndo(textureProperty.name);
                        textureProperty.textureValue = texture;
                    }
                }

                // Labels
                if (drawTilingAndOffset)
                {
                    GUI.Label(labelRect, "Tiling");
                    labelRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    GUI.Label(labelRect, "Offset");
                }

                if (useOffsetCoord)
                {
                    labelRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    GUI.Label(labelRect, "Offset Coords");
                }

                if (useChannels)
                {
                    labelRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    GUI.Label(labelRect, "Channels");
                }

                // Tiling & Offsets
                if (drawTilingAndOffset)
                    using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                    {
                        var textureScaleAndOffset = textureProperty.textureScaleAndOffset;
                        var tiling = new Vector2(textureScaleAndOffset.x, textureScaleAndOffset.y);
                        var offset = new Vector2(textureScaleAndOffset.z, textureScaleAndOffset.w);
                        tiling = EditorGUI.Vector2Field(propertyRect, string.Empty, tiling);
                        propertyRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        offset = EditorGUI.Vector2Field(propertyRect, string.Empty, offset);
                        if (changeCheckScope.changed)
                        {
                            textureScaleAndOffset = new Vector4(tiling.x, tiling.y, offset.x, offset.y);
                            editor.RegisterPropertyChangeUndo(textureProperty.name);
                            textureProperty.textureScaleAndOffset = textureScaleAndOffset;
                        }
                    }

                // Offset Coords
                if (useOffsetCoord)
                {
                    propertyRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    var xRect = propertyRect;
                    xRect.width /= 2;
                    var xPropertyRect = xRect;
                    xPropertyRect.xMin += 12;
                    EditorGUI.LabelField(xRect, new GUIContent("X"));
                    DrawEnumContentsProperty<TCustomCoord>(editor, xPropertyRect, offsetCoordXProperty);

                    var yRect = xRect;
                    yRect.x += yRect.width + 2;
                    yRect.xMax -= 2;
                    var yPropertyRect = yRect;
                    yPropertyRect.xMin += 12;
                    EditorGUI.LabelField(yRect, new GUIContent("Y"));
                    DrawEnumContentsProperty<TCustomCoord>(editor, yPropertyRect, offsetCoordYProperty);
                }

                // Channels
                if (useChannels)
                {
                    propertyRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    var xRect = propertyRect;
                    if (useChannelsY) xRect.width /= 2;

                    var xPropertyRect = xRect;
                    xPropertyRect.xMin += 12;
                    EditorGUI.LabelField(xRect, new GUIContent("X"));
                    DrawEnumContentsProperty<ColorChannels>(editor, xPropertyRect, channelsXProperty);

                    if (useChannelsY)
                    {
                        var yRect = xRect;
                        yRect.x += yRect.width + 2;
                        yRect.xMax -= 2;
                        var yPropertyRect = yRect;
                        yPropertyRect.xMin += 12;
                        EditorGUI.LabelField(yRect, new GUIContent("Y"));
                        DrawEnumContentsProperty<ColorChannels>(editor, yPropertyRect, channelsYProperty);
                    }
                }
            }
        }



        public static void DrawPropertyAndCustomCoord<T>(MaterialEditor editor, string label, MaterialProperty property,
            MaterialProperty coordProperty) where T : Enum
        {
            var fullRect = EditorGUILayout.GetControlRect();
            var contentsRect = fullRect;
            contentsRect.xMin += EditorGUIUtility.labelWidth;
            var propertyRect = fullRect;
            propertyRect.xMax -= contentsRect.width / 4;
            var coordRect = contentsRect;
            coordRect.width -= contentsRect.width * 3 / 4 - 2;
            coordRect.x += contentsRect.width * 3 / 4 + 2;

            editor.ShaderProperty(propertyRect, property, label);

            using (new ResetIndentLevelScope())
            {
                var coord = (T)Enum.ToObject(typeof(T), Convert.ToInt32(coordProperty.floatValue));
                if (!Enum.IsDefined(typeof(T), coord))
                    EditorGUILayout.HelpBox(
                        "Invalid coord value\n", MessageType.Error, true);
                using (var ccs = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.showMixedValue = coordProperty.hasMixedValue;
                    coord = (T)EditorGUI.EnumPopup(coordRect, coord);
                    EditorGUI.showMixedValue = false;

                    if (ccs.changed)
                    {
                        editor.RegisterPropertyChangeUndo(coordProperty.name);
                        coordProperty.floatValue = Convert.ToInt32(coord);
                    }
                }
            }
        }





        public static void DrawEnumProperty<T>(MaterialEditor editor, string label, MaterialProperty property)
            where T : Enum
        {
            DrawProperty(label, rect => { DrawEnumContentsProperty<T>(editor, rect, property); });
        }

        public static void DrawEnumFlagsProperty<T>(MaterialEditor editor, string label, MaterialProperty property)
            where T : Enum
        {
            DrawProperty(label, rect => { DrawEnumFlagsContentsProperty<T>(editor, rect, property); });
        }

        public static void DrawToggleProperty(MaterialEditor editor, string label, MaterialProperty property)
        {
            DrawProperty(label, rect =>
            {
                var isOn = property.floatValue >= 0.5f;

                using (var ccs = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.showMixedValue = property.hasMixedValue;
                    isOn = EditorGUI.Toggle(rect, isOn);
                    EditorGUI.showMixedValue = false;
                    if (ccs.changed)
                    {
                        editor.RegisterPropertyChangeUndo(property.name);
                        property.floatValue = isOn ? 1.0f : 0.0f;
                    }
                }
            });
        }

        public static void DrawVector2Property(MaterialEditor editor, string label, MaterialProperty property)
        {
            DrawProperty(label, rect =>
            {
                var value = (Vector2)property.vectorValue;

                using (var ccs = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.showMixedValue = property.hasMixedValue;
                    value = EditorGUI.Vector2Field(rect, string.Empty, value);
                    EditorGUI.showMixedValue = false;

                    if (ccs.changed)
                    {
                        editor.RegisterPropertyChangeUndo(property.name);
                        property.vectorValue = value;
                    }
                }
            });
        }

        public static void DrawTwoFloatProperties(string title, MaterialProperty prop1, string prop1Label,
            MaterialProperty prop2, string prop2Label, MaterialEditor materialEditor, float propLabelWidth = 30f)
        {
            EditorGUI.showMixedValue = prop1.hasMixedValue || prop2.hasMixedValue;
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.PrefixLabel(rect, new GUIContent(title));
            var indent = EditorGUI.indentLevel;
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;

            using (var ccs = new EditorGUI.ChangeCheckScope())
            {
                EditorGUIUtility.labelWidth = propLabelWidth;
                var prop1Rect = new Rect(rect.x + labelWidth, rect.y, (rect.width - labelWidth) * 0.5f,
                    EditorGUIUtility.singleLineHeight);
                var prop1Value = EditorGUI.FloatField(prop1Rect, prop1Label, prop1.floatValue);

                var prop2Rect = new Rect(prop1Rect.x + prop1Rect.width, rect.y,
                    prop1Rect.width, EditorGUIUtility.singleLineHeight);
                prop2Rect.xMin += 4;
                var prop2Value = EditorGUI.FloatField(prop2Rect, prop2Label, prop2.floatValue);
                EditorGUIUtility.labelWidth = labelWidth;

                if (ccs.changed)
                {
                    materialEditor.RegisterPropertyChangeUndo(title);
                    prop1.floatValue = prop1Value;
                    prop2.floatValue = prop2Value;
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.showMixedValue = false;
        }

        public static void DrawEnumContentsProperty<T>(MaterialEditor editor, Rect rect, MaterialProperty property)
            where T : Enum
        {
            var value = (T)Enum.ToObject(typeof(T), (int)property.floatValue);
            using (var ccs = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.showMixedValue = property.hasMixedValue;
                if (!Enum.IsDefined(typeof(T), value))
                    EditorGUILayout.HelpBox(
                        "Invalid coord value\n", MessageType.Error, true);

                var intValue = Convert.ToInt32(EditorGUI.EnumPopup(rect, value));


                EditorGUI.showMixedValue = false;

                if (ccs.changed)
                {
                    editor.RegisterPropertyChangeUndo(property.name);
                    property.floatValue = intValue;
                }
            }
        }

        private static void DrawEnumFlagsContentsProperty<T>(MaterialEditor editor, Rect rect,
            MaterialProperty property)
            where T : Enum
        {
            var value = (T)Enum.ToObject(typeof(T), (int)property.floatValue);
            using (var ccs = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.showMixedValue = property.hasMixedValue;

                var intValue = Convert.ToInt32(EditorGUI.EnumFlagsField(rect, value));
                EditorGUI.showMixedValue = false;

                if (ccs.changed)
                {
                    editor.RegisterPropertyChangeUndo(property.name);
                    property.floatValue = intValue;
                }
            }
        }

        private static void DrawProperty(string label, Action<Rect> drawContents)
        {
            var fullRect = EditorGUILayout.GetControlRect();
            var contentsRect = EditorGUI.PrefixLabel(fullRect, new GUIContent(label));
            contentsRect.xMin -= 2;

            using (new ResetIndentLevelScope())
            {
                drawContents.Invoke(contentsRect);
            }
        }














        private static Texture GetFoldoutTrueIcon()
        {
            if (_foldoutTrueIcon == null) _foldoutTrueIcon = Resources.Load<Texture>("tex_editor_icon_foldout_true");

            return _foldoutTrueIcon;
        }

        private static Texture GetFoldoutFalseIcon()
        {
            if (_foldoutFalseIcon == null) _foldoutFalseIcon = Resources.Load<Texture>("tex_editor_icon_foldout_false");

            return _foldoutFalseIcon;
        }

        public class ResetIndentLevelScope : GUI.Scope
        {
            private readonly int _indentLevel;

            public ResetIndentLevelScope()
            {
                _indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
            }

            protected override void CloseScope()
            {
                EditorGUI.indentLevel = _indentLevel;
            }
        }

        public class FoldoutHeaderScope : GUI.Scope
        {
            private static GUIStyle _textStyle;

            public FoldoutHeaderScope(bool foldout, string text)
            {
                Foldout = foldout;
                EditorGUILayout.BeginVertical(HeaderStyle);
                var rect = EditorGUILayout.GetControlRect();
                var iconRect = rect;
                var height = iconRect.height;
                iconRect.width = 9;
                iconRect.height = 9;
                iconRect.y += (height - iconRect.height) / 2;

                var buttonRect = rect;
                buttonRect.y -= EditorGUIUtility.standardVerticalSpacing;
                buttonRect.height += EditorGUIUtility.standardVerticalSpacing * 2.0f;

                var textRect = rect;
                textRect.xMin += 14;

                var iconImage = foldout ? GetFoldoutTrueIcon() : GetFoldoutFalseIcon();
                GUI.DrawTexture(iconRect, iconImage, ScaleMode.ScaleToFit);
                if (_textStyle == null)
                    _textStyle = new GUIStyle
                    {
                        padding = new RectOffset(0, 0, 2, 0),
                        normal =
                        {
                            textColor = TextColor
                        },
                        fontStyle = FontStyle.Bold
                    };

                GUI.Label(textRect, text, _textStyle);
                if (GUI.Button(buttonRect, string.Empty, _textStyle)) Foldout = !Foldout;

                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(ContentsStyle);
                if (Foldout) GUILayout.Space(6);
            }

            public bool Foldout { get; }

            protected override void CloseScope()
            {
                // When not in foldout state, this space must be greater than or equal to 12 because the display will collapse.
                var space = Foldout ? 6 : 12;
                GUILayout.Space(space);
                EditorGUILayout.EndVertical();
            }
        }
    }

    internal sealed class DistortionVer2BoolPreference
    {
        private readonly string key;
        private bool value;
        public DistortionVer2BoolPreference(string key, bool defaultValue)
        {
            this.key = key;
            value = EditorPrefs.GetBool(key, defaultValue);
        }
        public bool Value
        {
            get => value;
            set
            {
                if (this.value == value) return;
                this.value = value;
                EditorPrefs.SetBool(key, value);
            }
        }
    }
}
