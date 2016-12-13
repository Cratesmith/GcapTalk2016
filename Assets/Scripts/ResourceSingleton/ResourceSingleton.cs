using UnityEngine;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

public abstract class ResourceSingleton<T> : ScriptableObject where T:ScriptableObject
{
    static T s_instance;
    protected static T instance
    {
        get 
        { 
            LoadAsset();

            if(s_instance==null)
            {
                throw new System.ArgumentNullException("Couldn't load asset for ResourceSingleton "+typeof(T).Name);
            }

            return s_instance; 
        } 
    }

    static void LoadAsset()
    {
        if(Application.isPlaying)
        {
            if(!s_instance)
            {
                s_instance = Resources.Load(typeof(T).Name) as T;
            }
        }

        #if UNITY_EDITOR
        if(!s_instance) 
        {
            ResourceSingletonBuilder.BuildResourceSingletonsIfDirty(); // ensure that singletons were built

            var temp = ScriptableObject.CreateInstance<T>();
            var monoscript  = MonoScript.FromScriptableObject(temp);
            ScriptableObject.DestroyImmediate(temp);
            var scriptPath  = AssetDatabase.GetAssetPath(monoscript);
            var assetDir    = Path.GetDirectoryName(scriptPath)+"/Resources/";
            var assetPath   = assetDir+Path.GetFileNameWithoutExtension(scriptPath)+".asset";
            s_instance = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            
        }
        #endif
    }
}

#region internal

#if UNITY_EDITOR
public class ResourceSingletonBuilder// : UnityEditor.AssetPostprocessor
{
    static bool s_hasRun = false;
    /*
    static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
    {
        if(importedAssets.Concat(movedAssets).Any(x=> x.EndsWith(".cs") || x.EndsWith(".js")))
        {
            BuildResourceSingletonsIfDirty();
        }
    }
    */
    [UnityEditor.Callbacks.DidReloadScripts]
    public static void BuildResourceSingletonsIfDirty()
    {
        if(s_hasRun)
        {
            return; 
        }

        BuildResourceSingletons();
    } 
        
    public static void BuildResourceSingletons()
    {
        var result = System.Reflection.Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => !t.IsAbstract && GetBaseType(t, typeof(ResourceSingleton<>)));

        var method = typeof(ResourceSingletonBuilder).GetMethod("BuildOrMoveAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if(method == null)
        {
            EditorApplication.delayCall += BuildResourceSingletons;
            return;
        }

        foreach(var i in result)
        {  
            var generic = method.MakeGenericMethod(new System.Type[] { i });
            generic.Invoke(null, new object[0]);
        }

        s_hasRun = true;
    }

    static bool GetBaseType(System.Type type, System.Type baseType)
    {
        if (type == null || baseType == null || type == baseType)
            return false;

        if (baseType.IsGenericType == false)
        {
            if (type.IsGenericType == false)
                return type.IsSubclassOf(baseType);
        }
        else
        {
            baseType = baseType.GetGenericTypeDefinition();
        }

        type = type.BaseType;
        System.Type objectType = typeof(object);
        while (type != objectType && type != null)
        {
            System.Type curentType = type.IsGenericType ?
                type.GetGenericTypeDefinition() : type;
            if (curentType == baseType)
                return true;

            type = type.BaseType;
        }

        return false;
    }

    static void BuildOrMoveAsset<T>() where T:ScriptableObject
    {
        var instance = Resources.Load(typeof(T).Name) as T;

        var temp = ScriptableObject.CreateInstance<T>();
        var monoscript = MonoScript.FromScriptableObject(temp);
        ScriptableObject.DestroyImmediate(temp);
        if(monoscript==null)
        {
            Debug.LogError("Couldn't find script named "+typeof(T).Name+".cs");
            return;
        }

        var scriptPath = AssetDatabase.GetAssetPath(monoscript);

        var assetDir = Path.GetDirectoryName(scriptPath)+"/Resources/";
        var assetPath  = assetDir+Path.GetFileNameWithoutExtension(scriptPath)+".asset";
        Directory.CreateDirectory(assetDir);

        if(instance && AssetDatabase.GetAssetPath(instance)!=assetPath)
        {
            Debug.Log("ResourceSingleton: Moving asset: "+ typeof(T).Name+ " from "+AssetDatabase.GetAssetPath(instance)+" to "+assetPath);
            FileUtil.MoveFileOrDirectory(AssetDatabase.GetAssetPath(instance), assetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        if(!instance && !File.Exists(assetPath))
        {
            Debug.Log("ResourceSingleton: Creating asset: " + typeof(T).Name + " at " + assetPath);
            instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }        
    }
}
#endif

#endregion