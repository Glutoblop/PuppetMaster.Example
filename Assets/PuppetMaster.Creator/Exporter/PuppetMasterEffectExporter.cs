using EditorAttributes;
using UnityEngine;
using System.IO;
using System.Reflection;
using System;
using System.IO.Compression;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class PuppetMasterEffectExporter : MonoBehaviour
{
    [Header("Input Data")]
    public RewardEffect RewardEffectPrefab;
    [EditorAttributes.FolderPath]
    public string EffectStreamingAssetsParentFolder;

    [Button("Export To Zip")]
    public void ExportEffect()
    {
#if UNITY_EDITOR
        // ############################
        // ########### VALIDATION START

        Debug.Log($"Validation Start");

        string effectPrefabPath = AssetDatabase.GetAssetPath(RewardEffectPrefab.GetInstanceID());
        AssetImporter effectAssetImporter = AssetImporter.GetAtPath(effectPrefabPath);
        effectAssetImporter.SetAssetBundleNameAndVariant(RewardEffectPrefab.name, "");

        string path = AssetDatabase.GetAssetPath(RewardEffectPrefab);
        Debug.Log("prefab path:" + path);

        string projectPath = Application.dataPath;
        projectPath = projectPath.Replace("/Assets", "");
        Debug.Log("project path:" + projectPath);

        string prefabFolder = new DirectoryInfo($"{projectPath}/{path}").Parent.FullName;
        Debug.Log($"absolute path: {prefabFolder}");

        var assemblyRefPath = $"{prefabFolder}/{RewardEffectPrefab.name}.asmdef";
        if (!File.Exists(assemblyRefPath))
        {
            Debug.LogError($"Missing AssemblyReference here: {assemblyRefPath}");
            return;
        }
        else
        {
            Debug.Log($"Effect AssemblyRef found: {assemblyRefPath}");
        }

        if (RewardEffectPrefab == null)
        {
            Debug.LogError($"{nameof(RewardEffectPrefab)} is null");
            return;
        }
        Debug.Log($"Success: {nameof(RewardEffectPrefab)} loaded");

        RewardEffect rewardEffectComp = RewardEffectPrefab.GetComponent<RewardEffect>();
        if (rewardEffectComp == null)
        {
            Debug.LogError($"The {nameof(RewardEffectPrefab)} provided does not have a {nameof(RewardEffect)} component.");
            return;
        }
        Debug.Log($"Success: {nameof(RewardEffectPrefab)} has {nameof(RewardEffect)} component.");

        var effectAttribute = rewardEffectComp.GetType().GetCustomAttribute<PuppetMasterEffectAttribute>();
        if (effectAttribute == null)
        {
            Debug.LogError($"The {nameof(RewardEffectPrefab)} provided has a {nameof(RewardEffect)} component, " +
                $"but this component does not contain a {nameof(PuppetMasterEffectAttribute)} on top of it.");
            return;
        }
        Debug.Log($"Success: {nameof(RewardEffect)} component has {nameof(PuppetMasterEffectAttribute)}");

        // ########### VALIDATION END
        // ############################


        // ############################
        // ########### EXPORT START

        Debug.Log($"Export Start");

        var outputPluginFolder = $"{Application.streamingAssetsPath}/Plugins";
        Directory.CreateDirectory(outputPluginFolder);

        var outputEffectFolder = $"{outputPluginFolder}/{RewardEffectPrefab.name}";
        Directory.CreateDirectory(outputEffectFolder);

        var outputBundleFolder = $"{outputEffectFolder}/AssetBundle";
        Directory.CreateDirectory(outputBundleFolder);
        BuildPipeline.BuildAssetBundles(outputBundleFolder, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

        // BUILD PROJECT - To get the dll
        BuildPlayerOptions buildOptions = new();
        buildOptions = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(buildOptions);
        BuildPipeline.BuildPlayer(buildOptions);

        //Check for Assembly dll created with name of the effect.
        var projectName = Application.productName;
        var buildFolder = Path.GetDirectoryName(buildOptions.locationPathName);
        var dllFolder = $"{buildFolder}/{projectName}_Data/Managed";
        Debug.Log($"Dll Folder: {dllFolder}");

        var dllFile = new FileInfo($"{dllFolder}/{RewardEffectPrefab.name}.dll");
        if (!dllFile.Exists)
        {
            Debug.Log($"{RewardEffectPrefab.name}.dll does not exist inside {dllFolder}");
            return;
        }

        Debug.Log($"Moving {RewardEffectPrefab.name}.dll to output folder\n" +
            $"{dllFile.FullName} -> {outputEffectFolder}/{RewardEffectPrefab.name}.dll");

        var outputDllLocation = $"{outputEffectFolder}/{RewardEffectPrefab.name}.dll";
        if (File.Exists(outputDllLocation)) File.Delete(outputDllLocation);
        File.Copy(dllFile.FullName, outputDllLocation);

        Copy(EffectStreamingAssetsParentFolder, $"{outputEffectFolder}/Media");

        effectAssetImporter.SetAssetBundleNameAndVariant("", "");

        var zipOutputFolder = new DirectoryInfo($"{Application.streamingAssetsPath}../../../Output").FullName;
        Directory.CreateDirectory(zipOutputFolder);

        File.Delete($"{zipOutputFolder}/{RewardEffectPrefab.name}.zip");
        Debug.Log($"Creating Plugin Zip file at {zipOutputFolder}/{RewardEffectPrefab.name}.zip");
        ZipFile.CreateFromDirectory($"{outputEffectFolder}", $"{zipOutputFolder}/{RewardEffectPrefab.name}.zip");

        
        System.Diagnostics.Process.Start("explorer.exe", zipOutputFolder);

#endif
    }


    private static void Copy(string sourceDirectory, string targetDirectory)
    {
        DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }
}
