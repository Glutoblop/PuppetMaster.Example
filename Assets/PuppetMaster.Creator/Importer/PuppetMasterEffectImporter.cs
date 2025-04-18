using EditorAttributes;
using PuppetMaster.Redemptions.Core.Attributes;
using PuppetMaster.Redemptions.Core.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class PuppetMasterEffectImporter : MonoBehaviour
{
    [Header("Input Data")]
    [Tooltip("After running the Exporter it will give you a zip file, this is the path to that zip file.")]
    [FilePath()][SerializeField] private string _ZipPath;
    [Button("Import From Zip")] private void LoadConfigData() => StartCoroutine(LoadTypeDataStart());
    [ReadOnly][SerializeField] private AssetBundle _LoadedBundle;
    [ReadOnly][SerializeField] private string _MediaPath;

    [Header("Loaded Data")]
    [ReadOnly][SerializeField] private GameObject EffectPrefab;
    [ReadOnly][SerializeField] private RewardEffect EffectInstance;
    [SerializeField] private EffectConfig EffectConfig;

    private IEnumerator Start()
    {
        if (EffectInstance == null)
        {
            Debug.LogError($"Make sure to run the 'Import From Zip' before starting.");
            yield break;
        }
    }

    /// <summary>Will load and create and instance of the effect asset bundle and store it in EffectInstance</summary>
    /// <returns></returns>
    private IEnumerator LoadAssetBundleAsEffectInstance()
    {
        AssetBundle.UnloadAllAssetBundles(true);

        if (string.IsNullOrEmpty(_ZipPath))
        {
            Debug.LogError($"Please provide a path to the zip file created by the Exporter.");
            yield break;
        }

        FileInfo zipFile = new FileInfo(_ZipPath);
        if (!zipFile.Exists)
        {
            Debug.LogError($"{_ZipPath} file does not exist.");
            yield break;
        }

        string pluginName = Path.GetFileName(_ZipPath);
        pluginName = pluginName.Replace(".zip", "");
        string pluginFolder = $"{Application.streamingAssetsPath}/Plugins/{pluginName}";

        if (Directory.Exists(pluginFolder))
        {
            try
            {
                RecursiveDelete(new DirectoryInfo(pluginFolder));
            }
            catch (Exception e)
            {

            }
        }
        ZipFile.ExtractToDirectory(_ZipPath, pluginFolder);

        _MediaPath = $"{pluginFolder}/Media";
        var bundlePath = $"{pluginFolder}/AssetBundle/{pluginName}";

        if (!File.Exists(bundlePath))
        {
            Debug.LogError($"Asset bundle does not exist in folder {bundlePath}");
            yield break;
        }

        string pluginFile = $"{pluginFolder}/{pluginName}.dll";
        if (!File.Exists(pluginFile))
        {
            Debug.LogError($"Plugin dll not present {pluginFile}");
            yield break;
        }

        Assembly pluginAssembly = Assembly.LoadFrom(pluginFile);
        if (pluginAssembly == null)
        {
            Debug.LogError($"Failed to load plugin dll.");
            yield break;
        }

        TypeInfo pluginEffectType = pluginAssembly.DefinedTypes.FirstOrDefault(s => s.GetCustomAttribute<PuppetMasterEffectAttribute>() != null);
        if (pluginEffectType == null)
        {
            Debug.LogError($"Failed to load plugin type from dll assembly.");
            yield break;
        }

        AssetBundleCreateRequest myLoadedAssetBundle = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return myLoadedAssetBundle;

        _LoadedBundle = myLoadedAssetBundle?.assetBundle;
        if (_LoadedBundle != null)
        {
            EffectPrefab = _LoadedBundle.LoadAsset<GameObject>($"{pluginName}");
            var obj = Instantiate(EffectPrefab);
            EffectInstance = obj.GetComponent<RewardEffect>();
            Debug.Log($"Plugin [{pluginName}] loaded.");
        }
        else
        {
            Debug.LogError($"Asset Bundle was null when loaded.");
        }
    }

    private IEnumerator LoadTypeDataStart()
    {
        yield return LoadAssetBundleAsEffectInstance();

        Component effectComp = EffectPrefab?.GetComponent(typeof(RewardEffect));
        if (effectComp != null)
        {
            var effectType = effectComp.GetType();
            PuppetMasterEffectAttribute effectAttribute = effectType.GetCustomAttribute<PuppetMasterEffectAttribute>();

            List<FieldInfo> fields = effectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(s => s.GetCustomAttribute<PuppetMasterFieldAttribute>() != null)
                .ToList();

            fields = fields
                .OrderByDescending(s =>
                    s.GetCustomAttribute<CommandActiveAttribute>() != null ||
                    s.GetCustomAttribute<CommandCooldownAttribute>() != null)
                .ToList();

            EffectConfig = new EffectConfig()
            {
                Type = effectAttribute.Name,
                Fields = new List<EffectFieldConfig>()
            };

            if (effectComp != null)
            {
                foreach (FieldInfo field in fields)
                {
                    PuppetMasterFieldAttribute attribute = field.GetCustomAttribute<PuppetMasterFieldAttribute>();
                    var fieldConfig = attribute?.GenerateConfig(field, effectComp);
                    if (fieldConfig != null)
                    {
                        EffectConfig.Fields.Add(fieldConfig);
                    }
                }
            }
        }

    }

    public static void RecursiveDelete(DirectoryInfo baseDir)
    {
        if (!baseDir.Exists)
            return;

        foreach (var dir in baseDir.EnumerateDirectories())
        {
            RecursiveDelete(dir);
        }
        baseDir.Delete(true);
    }
}
