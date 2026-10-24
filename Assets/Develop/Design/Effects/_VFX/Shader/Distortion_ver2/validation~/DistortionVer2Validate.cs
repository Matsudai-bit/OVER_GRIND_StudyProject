using System;
using System.IO;
using System.Linq;
using System.Text;
using ProjectEffect.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class DistortionVer2Validate
{
    const string Root = "Assets/Shader/Distortion_ver2";
    const int Size = 256;
    static readonly StringBuilder Report = new StringBuilder();
    static string Evidence => Path.GetFullPath(Root + "/validation~");
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); Report.AppendLine("PASS " + message); }

    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Evidence);
            Require(Application.dataPath.Replace('\\','/').EndsWith("/ShaderValidation/Assets"), "Isolated ShaderValidation project (Unity 6000.3.17f1, installed NOVA 3.6.0)");
            AssetDatabase.ImportAsset(Root + "/DistortionVer2.shader", ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/DistortionVer2.shader");
            Shader original = Shader.Find("Nova/Particles/Distortion");
            Require(shader && shader.name == DistortionVer2GUI.ShaderName, "Requested Distortion ver2 shader exists");
            Require(original && original != shader, "Original and ver2 are distinct shaders");
            Require(shader.isSupported, "Shader supported on " + SystemInfo.graphicsDeviceType);
            var oldMat = new Material(original);
            var mat = new Material(shader);
            var sphere = Sphere();
            var constant = Texture(new Color(.8f, .7f, .6f, 1));
            var fade = Texture(new Color(.4f, .4f, .4f, 1));
            try
            {
                var gui = new DistortionVer2GUI();
                gui.ValidateMaterial(mat);
                Require(!mat.IsKeywordEnabled("_TRANSPARENCY_BY_RIM") && !mat.IsKeywordEnabled("_TRANSPARENCY_BY_LUMINANCE"), "Both new masks default OFF");
                oldMat.SetTexture("_BaseMap", constant); mat.SetTexture("_BaseMap", constant);
                oldMat.SetFloat("_ZTest", 8); mat.SetFloat("_ZTest", 8);
                Nova.Editor.Core.Scripts.ParticlesDistortionMaterialPostProcessor.SetupMaterialKeywords(oldMat);
                DistortionVer2GUI.Setup(mat);
                var baseline = Render(oldMat, sphere, new Vector3(0,0,-4));
                var disabled = Render(mat, sphere, new Vector3(0,0,-4));
                Require(Energy(baseline) > 1, "Original distortion actually rendered");
                Require(MaxDelta(baseline, disabled) < .00001f, "Masks OFF matches original GPU output");

                mat.SetFloat("_RimTransparencySharpness", 0);
                mat.SetFloat("_LuminanceTransparencySharpness", 0);
                mat.SetFloat("_RimTransparencyEnabled", 1);
                DistortionVer2GUI.Setup(mat);
                var rim = Render(mat, sphere, new Vector3(0,0,-4));
                Require(Energy(rim) < Energy(baseline) * .92f && Energy(rim) > Energy(baseline) * .15f, "Rim attenuates sphere without hiding it");
                Require(rim[Size/2*Size+Size/2].r > disabled[Size/2*Size+Size/2].r * .95f, "Rim preserves front-facing center");
                int edge = Enumerable.Range(0, Size/2).First(x => baseline[Size/2*Size+x].r > .0001f) + 2;
                Require(rim[Size/2*Size+edge].r < baseline[Size/2*Size+edge].r * .5f, "Rim softens visible silhouette");
                foreach (Vector3 camera in new[] { new Vector3(4,0,0), new Vector3(2,2,-3) })
                {
                    var b = Render(oldMat, sphere, camera);
                    var r = Render(mat, sphere, camera);
                    Require(Energy(r) < Energy(b)*.92f && Energy(r) > Energy(b)*.15f, "Rim follows camera " + camera);
                }
                var bo = Render(oldMat, sphere, new Vector3(0,0,-4), true);
                var ro = Render(mat, sphere, new Vector3(0,0,-4), true);
                Require(Energy(ro) < Energy(bo)*.92f && Energy(ro) > Energy(bo)*.15f, "Orthographic camera rim works");
                var scaled = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(15,25,0), new Vector3(1.3f,.7f,1));
                Require(Energy(Render(mat,sphere,new Vector3(0,0,-4),false,scaled)) > 1, "Nonuniformly scaled sphere renders finite distortion");
                mat.SetFloat("_InverseRimTransparency", 1); DistortionVer2GUI.Setup(mat);
                var inverse = Render(mat, sphere, new Vector3(0,0,-4));
                Require(inverse[Size/2*Size+Size/2].r < .0001f, "Inverse Rim removes center");
                Require(inverse[Size/2*Size+edge].r > rim[Size/2*Size+edge].r, "Inverse Rim retains edge");
                mat.SetFloat("_InverseRimTransparency", 0);

                foreach(string mask in new[] {"Rim", "Luminance"})
                {
                    mat.SetFloat("_RimTransparencyEnabled", mask == "Rim" ? 1 : 0);
                    mat.SetFloat("_LuminanceTransparencyEnabled", mask == "Luminance" ? 1 : 0);
                    mat.SetFloat("_"+mask+"TransparencyProgress", 0); DistortionVer2GUI.Setup(mat);
                    Require(MaxDelta(disabled,Render(mat,sphere,new Vector3(0,0,-4))) < .00001f, mask + " Progress 0 preserves distortion");
                    mat.SetFloat("_"+mask+"TransparencyProgress", 1);
                    Require(Energy(Render(mat,sphere,new Vector3(0,0,-4))) < .00001f, mask + " Progress 1 removes distortion");
                    mat.SetFloat("_"+mask+"TransparencyProgress", .5f);
                    mat.SetFloat("_"+mask+"TransparencySharpness", 1);
                    Require(Render(mat,sphere,new Vector3(0,0,-4)).All(c => !float.IsNaN(c.r) && !float.IsInfinity(c.r)), mask + " Sharpness 1 has no NaN/Infinity");
                    mat.SetFloat("_"+mask+"TransparencySharpness", 0);
                }
                DistortionVer2GUI.Setup(mat);
                var lum = Render(mat,sphere,new Vector3(0,0,-4));
                float luminance = .8f*.298912f + .7f*.586611f + .6f*.114478f;
                float expected = Mathf.SmoothStep(0,1,luminance);
                Require(Mathf.Abs(Energy(lum)/Energy(disabled)-expected)<.002f,"Luminance GPU mask matches sampled RGB luminance");
                mat.SetFloat("_InverseLuminanceTransparency",1);
                var lumInverse = Render(mat,sphere,new Vector3(0,0,-4));
                Require(Mathf.Abs((Energy(lum)+Energy(lumInverse))/Energy(disabled)-1)<.002f,"Luminance Inverse complements mask");
                mat.SetFloat("_InverseLuminanceTransparency",0);
                mat.SetFloat("_RimTransparencyEnabled",1); DistortionVer2GUI.Setup(mat);
                var combined = Render(mat,sphere,new Vector3(0,0,-4));
                Require(Mathf.Abs(Energy(combined)/Energy(rim)-expected)<.002f,"Rim and Luminance multiply independently");

                mat.SetTexture("_AlphaTransitionMap",fade); mat.SetFloat("_AlphaTransitionMode",1); mat.SetFloat("_AlphaTransitionProgress",.5f);
                DistortionVer2GUI.Setup(mat);
                var composed = Render(mat,sphere,new Vector3(0,0,-4));
                Require(Mathf.Abs(Energy(composed)/Energy(combined)-.4f)<.002f,"Existing Alpha Transition Fade composes with both masks");
                mat.SetFloat("_AlphaTransitionMode",2); DistortionVer2GUI.Setup(mat);
                Require(Render(mat,sphere,new Vector3(0,0,-4)).All(c => !float.IsNaN(c.r)),"Dissolve + Rim + Luminance compiles and renders");
                mat.SetFloat("_AlphaTransitionMode",0); mat.SetFloat("_LuminanceTransparencyEnabled",0);
                mat.SetFloat("_RimTransparencyProgressCoord",1); DistortionVer2GUI.Setup(mat);
                var coords=Enumerable.Repeat(new Vector4(.5f,0,0,0),sphere.vertexCount).ToList(); sphere.SetUVs(1,coords);
                Require(Energy(Render(mat,sphere,new Vector3(0,0,-4))) < .00001f,"Rim Progress Custom Coord drives fade");
                mat.SetFloat("_RimTransparencyProgressCoord",0); DistortionVer2GUI.Setup(mat);
                mat.SetFloat("_SoftParticlesEnabled",1); mat.SetFloat("_DepthFadeEnabled",1); DistortionVer2GUI.Setup(mat);
                for(int pass=0;pass<mat.passCount;pass++) Require(mat.SetPass(pass),"Pass "+pass+" with Rim/Soft/Depth compiles");
                mat.SetFloat("_SoftParticlesEnabled",0); mat.SetFloat("_DepthFadeEnabled",0); mat.SetFloat("_LuminanceTransparencyEnabled",1);
                mat.SetTexture("_FlowMap",constant); mat.SetFloat("_FlowMapTarget",3); mat.SetFloat("_AlphaTransitionMode",1); DistortionVer2GUI.Setup(mat);
                for(int pass=0;pass<mat.passCount;pass++) Require(mat.SetPass(pass),"Pass "+pass+" with Rim/Luminance/Flow/Fade compiles");
                mat.SetTexture("_FlowMap",null);mat.SetFloat("_AlphaTransitionMode",0);mat.enableInstancing=true;DistortionVer2GUI.Setup(mat);
                var instanced = Render(mat,sphere,new Vector3(0,0,-4),false,null,true);
                oldMat.enableInstancing=true;
                var oldInstanced=Render(oldMat,sphere,new Vector3(0,0,-4),false,null,true);
                Report.AppendLine("GPU instance diagnostics: original="+Energy(baseline)+", originalInstanced="+Energy(oldInstanced)+", combined="+Energy(combined)+", newInstanced="+Energy(instanced)+", maxDelta="+MaxDelta(instanced,combined));
                Report.AppendLine("Keywords: "+string.Join(",",mat.shaderKeywords));
                foreach(var message in ShaderUtil.GetShaderMessages(shader))Report.AppendLine(message.severity+" "+message.message);
                SavePreview(baseline,rim,inverse,combined);
                Require(Energy(instanced)>1 && MaxDelta(instanced,combined)<.00005f,"NOVA procedural GPU instancing matches non-instanced Rim/Luminance output");
                Require(!ShaderUtil.ShaderHasError(shader), "No shader errors after GPU rendering and procedural instancing");
                var materialEditor = (MaterialEditor)UnityEditor.Editor.CreateEditor(mat);
                Require(materialEditor.customShaderGUI is DistortionVer2GUI,"Unity resolves new custom inspector");
                Object.DestroyImmediate(materialEditor);
                ValidateUnlitParity(shader, sphere, constant);
                ValidateCustomCoords(shader, sphere, constant);
                SavePreview(baseline,rim,inverse,combined);
                Require(!Directory.GetFiles(Root,"*.mat",SearchOption.AllDirectories).Any(), "No material assets created");
            }
            finally { Object.DestroyImmediate(oldMat); Object.DestroyImmediate(mat); Object.DestroyImmediate(sphere); Object.DestroyImmediate(constant); Object.DestroyImmediate(fade); }
            Report.AppendLine("DISTORTION_VER2_VALIDATION_OK");
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence+"/validation.txt",Report.ToString());
            Debug.Log(Report.ToString());
            EditorApplication.Exit(0);
        }
        catch(Exception e)
        {
            Report.AppendLine(e.ToString());
            Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence+"/validation.txt",Report.ToString());
            Debug.LogError(Report.ToString()); EditorApplication.Exit(1);
        }
    }

    static void ValidateUnlitParity(Shader shader, Mesh sphere, Texture2D texture)
    {
        var unlit = new Material(Shader.Find("Nova/Particles/UberUnlit"));
        var distortion = new Material(shader);
        try
        {
            sphere.colors = Enumerable.Repeat(Color.white, sphere.vertexCount).ToArray();
            sphere.SetUVs(1, Enumerable.Repeat(Vector4.zero, sphere.vertexCount).ToList());
            unlit.SetTexture("_BaseMap", texture); distortion.SetTexture("_BaseMap", texture);
            unlit.SetFloat("_ZTest", 8); distortion.SetFloat("_ZTest", 8);
            unlit.SetFloat("_BlendSrc", 5); unlit.SetFloat("_BlendDst", 0);
            // No tint/emission/vertex-alpha modulation; compare the transparency mask itself.
            unlit.shaderKeywords = new[] { "_BASE_MAP_MODE_2D" };
            DistortionVer2GUI.Setup(distortion);
            var baseline = Render(distortion, sphere, new Vector3(0,0,-4));
            foreach (string mask in new[] { "Rim", "Luminance" })
            foreach (float inverse in new[] { 0f, 1f })
            foreach (float sharpness in new[] { 0f, .5f, .9f })
            foreach (float progress in new[] { .2f, .5f, .8f })
            {
                foreach (string m in new[] { "Rim", "Luminance" })
                {
                    string keyword = "_TRANSPARENCY_BY_" + m.ToUpperInvariant();
                    if (m == mask) unlit.EnableKeyword(keyword); else unlit.DisableKeyword(keyword);
                    distortion.SetFloat("_"+m+"TransparencyEnabled", m == mask ? 1 : 0);
                }
                foreach (Material mat in new[] { unlit, distortion })
                {
                    mat.SetFloat("_"+mask+"TransparencyProgress", progress);
                    mat.SetFloat("_"+mask+"TransparencySharpness", sharpness);
                    mat.SetFloat("_Inverse"+mask+"Transparency", inverse);
                }
                DistortionVer2GUI.Setup(distortion);
                var u = Render(unlit, sphere, new Vector3(0,0,-4));
                var d = Render(distortion, sphere, new Vector3(0,0,-4));
                float delta = 0;
                for (int i=0; i<d.Length; i++)
                    if (baseline[i].r > .0001f) delta = Mathf.Max(delta, Mathf.Abs(d[i].r / baseline[i].r - u[i].r / .8f));
                Require(delta < .004f, mask+" matches actual NOVA Unlit GPU alpha: inverse="+inverse+", sharpness="+sharpness+", progress="+progress+", maxDelta="+delta);
            }
            Require(!ShaderUtil.ShaderHasError(unlit.shader), "Original Unlit reference rendered without shader errors");
        }
        finally { Object.DestroyImmediate(unlit); Object.DestroyImmediate(distortion); }
    }

    static void ValidateCustomCoords(Shader shader, Mesh sphere, Texture2D texture)
    {
        var mat = new Material(shader);
        try
        {
            mat.SetTexture("_BaseMap", texture); mat.SetFloat("_ZTest", 8);
            foreach (string mask in new[] { "Rim", "Luminance" })
            foreach (string parameter in new[] { "Progress", "Sharpness" })
            foreach (int coord in new[] { 1,11,21,31,2,12,22,32 })
            {
                mat.SetFloat("_RimTransparencyEnabled", mask == "Rim" ? 1 : 0);
                mat.SetFloat("_LuminanceTransparencyEnabled", mask == "Luminance" ? 1 : 0);
                mat.SetFloat("_"+mask+"TransparencyProgress", .3f);
                mat.SetFloat("_"+mask+"TransparencySharpness", .2f);
                string prop = "_"+mask+"Transparency"+parameter;
                float baseValue = mat.GetFloat(prop);
                mat.SetFloat(prop, baseValue+.25f); mat.SetFloat(prop+"Coord",0);
                DistortionVer2GUI.Setup(mat);
                var expected = Render(mat,sphere,new Vector3(0,0,-4));
                mat.SetFloat(prop,baseValue); mat.SetFloat(prop+"Coord",coord);
                var value = Vector4.zero; value[coord/10] = .25f;
                sphere.SetUVs(1,Enumerable.Repeat(coord%10 == 1 ? value : Vector4.zero,sphere.vertexCount).ToList());
                sphere.SetUVs(2,Enumerable.Repeat(coord%10 == 2 ? value : Vector4.zero,sphere.vertexCount).ToList());
                var actual = Render(mat,sphere,new Vector3(0,0,-4));
                Require(MaxDelta(expected,actual) < .00001f, mask+" "+parameter+" Custom Coord "+coord+" matches material value");
                mat.SetFloat(prop+"Coord",0);
            }
        }
        finally { Object.DestroyImmediate(mat); }
    }

    static Texture2D Texture(Color color)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBAFloat,false,true); t.SetPixels(new[]{color,color,color,color});t.Apply();return t;
    }
    static Mesh Sphere()
    {
        const int lat=64, lon=128;
        var vertices=new Vector3[(lat+1)*(lon+1)]; var uv=new Vector2[vertices.Length]; var triangles=new int[lat*lon*6];
        for(int y=0;y<=lat;y++) for(int x=0;x<=lon;x++)
        {
            float a=Mathf.PI*y/lat,b=2*Mathf.PI*x/lon; int i=y*(lon+1)+x;
            vertices[i]=new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b)); uv[i]=new Vector2((float)x/lon,(float)y/lat);
        }
        int k=0;for(int y=0;y<lat;y++)for(int x=0;x<lon;x++)
        {int a=y*(lon+1)+x,b=a+lon+1;triangles[k++]=a;triangles[k++]=a+1;triangles[k++]=b;triangles[k++]=a+1;triangles[k++]=b+1;triangles[k++]=b;}
        var mesh=new Mesh {vertices=vertices,normals=vertices,uv=uv,triangles=triangles}; mesh.RecalculateBounds();return mesh;
    }
    // HLSL float3x4 in a StructuredBuffer uses four packed float3 columns.
    struct InstanceData { public Vector3 column0,column1,column2,column3; public uint color; public Vector4 custom1,custom2; }
    static Color[] Render(Material mat,Mesh mesh,Vector3 position,bool ortho=false,Matrix4x4? model=null,bool procedural=false)
    {
        var rt=new RenderTexture(Size,Size,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);rt.Create();
        var read=new Texture2D(Size,Size,TextureFormat.RGBAFloat,false,true);
        var previous=RenderTexture.active;
        var cmd=new CommandBuffer {name="Distortion ver2 isolated GPU validation"};
        ComputeBuffer instanceBuffer=null;
        try
        {
            var view=Matrix4x4.Scale(new Vector3(1,1,-1))*Matrix4x4.TRS(position,Quaternion.LookRotation(-position.normalized),Vector3.one).inverse;
            var projection=ortho?Matrix4x4.Ortho(-1.5f,1.5f,-1.5f,1.5f,.1f,100):Matrix4x4.Perspective(45,1,.1f,100);
            cmd.SetRenderTarget(rt);cmd.ClearRenderTarget(true,true,Color.clear);
            cmd.SetViewProjectionMatrices(view,GL.GetGPUProjectionMatrix(projection,true));
            cmd.SetGlobalVector("_WorldSpaceCameraPos",position);cmd.SetGlobalVector("unity_OrthoParams",new Vector4(3,3,0,ortho?1:0));
            if(procedural)
            {
                instanceBuffer=new ComputeBuffer(1,84);
                instanceBuffer.SetData(new[]{new InstanceData {column0=Vector3.right,column1=Vector3.up,column2=Vector3.forward,column3=Vector3.zero,color=0xffffffff,custom1=Vector4.zero,custom2=Vector4.zero}});
                var properties=new MaterialPropertyBlock();properties.SetBuffer("unity_ParticleInstanceData",instanceBuffer);
                cmd.DrawMeshInstancedProcedural(mesh,0,mat,0,1,properties);
            }
            else cmd.DrawMesh(mesh,model??Matrix4x4.identity,mat,0,0);
            Graphics.ExecuteCommandBuffer(cmd);
            RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,Size,Size),0,0);read.Apply();return read.GetPixels();
        }
        finally{ RenderTexture.active=previous;cmd.Release();if(instanceBuffer!=null)instanceBuffer.Release();rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(read); }
    }
    static float Energy(Color[] p)=>p.Sum(c=>Mathf.Abs(c.r)+Mathf.Abs(c.g));
    static float MaxDelta(Color[] a,Color[] b)=>a.Zip(b,(x,y)=>Mathf.Max(Mathf.Abs(x.r-y.r),Mathf.Abs(x.g-y.g))).Max();
    static void SavePreview(params Color[][] images)
    {
        var t=new Texture2D(Size*images.Length,Size,TextureFormat.RGBA32,false);
        for(int i=0;i<images.Length;i++) for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
        {float v=Mathf.Clamp01(images[i][y*Size+x].r/.006f);t.SetPixel(i*Size+x,y,new Color(v,v,v,1));}
        t.Apply();File.WriteAllBytes(Evidence+"/mask_comparison.png",t.EncodeToPNG());Object.DestroyImmediate(t);
    }
}
