using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if VRC_SDK_VRCSDK3
using System.Diagnostics;
using VRC.Core;
#endif

namespace dVRC.Editor
{
    public class MainEditorWindow : EditorWindow
    {
        private static MainEditorWindow Instance { get; set; }
            
        [MenuItem("dVRC/主窗口")]
        private static void ShowWindow()
        {
            Instance = GetWindow<MainEditorWindow>();
            Instance.titleContent = new GUIContent("dVRC");
        }

        private const string OutputAssetBundles = "Assets/dVRC/Output/Bundles";
        private readonly RipperHandler _ripperHandler = new RipperHandler();

        private string GetSelectedButtonText(string id, VRCAssetType assetType)
        {
            string text;
            switch (assetType)
            {
                case VRCAssetType.Avatar:
                    if (SelectedTools.SelectedAvatarId == id)
                        text = "已选择";
                    else
                        text = "选择此 Avatar";
                    break;
                case VRCAssetType.World:
                    if (SelectedTools.SelectedWorldId == id)
                        text = "已选择";
                    else
                        text = "选择此世界";
                    break;
                default:
                    text = "选择此资源";
                    break;
            }
            return text;
        }

#if VRC_SDK_VRCSDK3
        private static Vector2 ShowList_ScrollView;

        private void ShowList()
        {
            ShowList_ScrollView = EditorGUILayout.BeginScrollView(ShowList_ScrollView);
            foreach (KeyValuePair<string,Texture2D> asset in VRCSdkControlPanel.ImageCache)
            {
                if (SelectedTools.GetAssetTypeFromId(asset.Key) == SelectedTools.SelectedAssetType)
                {
                    EditorGUILayout.BeginHorizontal();
                    VRCAsset vrcAsset = ReflectingTools.GetDynamicAsset(asset.Key, SelectedTools.SelectedAssetType);
                    GUILayout.Box(vrcAsset.Texture, new GUIStyle
                    {
                        fixedHeight = 64,
                        fixedWidth = 64
                    });
                    GUILayout.Label(vrcAsset.Name);
                    if (GUILayout.Button(GetSelectedButtonText(vrcAsset.Id, SelectedTools.SelectedAssetType)))
                        SelectedTools.SelectedAvatarId = vrcAsset.Id;
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("返回", EditorStyles.miniButtonRight))
            {
                ShowList_ScrollView = Vector2.zero;
                SelectedTools.SelectedAssetType = VRCAssetType.Unknown;
            }
        }
        
        private void ShowAssetScreen(string id)
        {
            if (!VRCSdkControlPanel.ImageCache.ContainsKey(id))
            {
                SelectedTools.SelectedAvatarId = String.Empty;
                SelectedTools.SelectedWorldId = String.Empty;
                return;
            }
            VRCAsset vrcAsset = ReflectingTools.GetDynamicAsset(id, SelectedTools.SelectedAssetType);
            GUILayout.Label("已选资源", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Box(vrcAsset.Texture, EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label(vrcAsset.ToString(), EditorStyles.centeredGreyMiniLabel);
            foreach (BuildPlatforms selectedPlatform in vrcAsset.SupportedPlatforms)
            {
                if (GUILayout.Button($"下载 {selectedPlatform.ToString()} 资源"))
                {
                    vrcAsset.DownloadAsset(OutputAssetBundles, selectedPlatform, null, () =>
                    {
                        SelectedTools.SelectedAvatarId = String.Empty;
                        SelectedTools.SelectedWorldId = String.Empty;
                        SelectedTools.SelectedAssetType = VRCAssetType.Unknown;
                        EditorUtility.DisplayDialog("dVRC", "下载完成：" + vrcAsset.Name, "确定");
                    });
                }
            }
            if (GUILayout.Button("返回", EditorStyles.miniButtonRight))
            {
                SelectedTools.SelectedAvatarId = String.Empty;
                SelectedTools.SelectedWorldId = String.Empty;
            }
        }

        private string PrettyPrintFile(string file) => Path.GetFileName(file);

        private void DrawManageFileMenu()
        {
            GUILayout.Label(PrettyPrintFile(SelectedTools.SelectedFile), EditorStyles.centeredGreyMiniLabel);
            if(SelectedTools.GetAssetTypeFromFileType(SelectedTools.SelectedFile) == VRCAssetType.World && !EditorApplication.isPlaying)
                GUILayout.Label("请进入播放模式后加载世界");
            else if (SelectedTools.GetAssetTypeFromFileType(SelectedTools.SelectedFile) == VRCAssetType.Avatar ||
                     (SelectedTools.GetAssetTypeFromFileType(SelectedTools.SelectedFile) == VRCAssetType.World &&
                      EditorApplication.isPlaying))
            {
                if (GUILayout.Button("加载到场景"))
                    AssetLoader.LoadAssetBundle(SelectedTools.SelectedFile);
            }
            if (GUILayout.Button("从磁盘删除资源"))
            {
                AssetLoader.DeleteAsset(SelectedTools.SelectedFile);
                SelectedTools.SelectedFile = String.Empty;
            }
            if (_ripperHandler.isPresent && !_ripperHandler.IsWorking)
            {
                if (GUILayout.Button("提取资源到文件夹") && !_ripperHandler.IsWorking)
                {
                    string path = EditorUtility.OpenFolderPanel("选择文件夹", "Assets", "");
                    if(string.IsNullOrEmpty(path))
                        return;
                    int childLength = Directory.GetDirectories(path).Length + Directory.GetFiles(path).Length;
                    if(childLength > 0)
                        if(!EditorUtility.DisplayDialog("dVRC",
                               "你选择的目录不是空目录。此操作会删除该目录中的全部文件！是否继续？",
                               "是", "否"))
                            return;
                    if (IOTools.IsChildDirectory(Application.dataPath, path))
                        if(!EditorUtility.DisplayDialog("dVRC",
                               "你选择的目录位于当前项目中，导出的 ExportedAssets 会写入当前项目，可能引发问题。仍要继续吗？",
                               "是", "否"))
                            return;
                    _ripperHandler.Rip(Path.GetFullPath(SelectedTools.SelectedFile), path, () =>
                    {
                        if (Directory.Exists(path))
                        {
                            EditorUtility.DisplayDialog("dVRC", "操作完成！", "确定");
                        }
                        else
                        {
                            Debug.LogWarning("目录 " + path + " 在导出后不存在！");
                            EditorUtility.DisplayDialog("dVRC",
                                "操作失败！请查看 Console 获取详细信息。", "确定");
                        }
                    });
                }
            }
            else
                if(_ripperHandler.IsWorking)
                {
                    GUILayout.Label("正在提取...", EditorStyles.centeredGreyMiniLabel);
                    Rect r = EditorGUILayout.GetControlRect();
                    EditorGUI.ProgressBar(r, _ripperHandler.Progress,
                        Mathf.RoundToInt(_ripperHandler.Progress * 100f) + "%");
                }
            if(!_ripperHandler.IsWorking)
                if(GUILayout.Button("返回", EditorStyles.miniButtonRight))
                    SelectedTools.SelectedFile = String.Empty;
            GUILayout.Label("注意：受 Unity 限制，直接加载世界通常没有太大意义。", EditorStyles.miniLabel);
        }

        private static Vector2 ManageAssets_ScrollView;
        
        private void DrawManageAssetBundles()
        {
            GUILayout.Label("或", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("管理文件");
            ManageAssets_ScrollView = EditorGUILayout.BeginScrollView(ManageAssets_ScrollView);
            if (Directory.Exists(OutputAssetBundles))
            {
                foreach (string file in Directory.GetFiles(OutputAssetBundles))
                {
                    if (!file.Split('.').Last().Contains("meta"))
                    {
                        if (GUILayout.Button(PrettyPrintFile(file)))
                            SelectedTools.SelectedFile = file;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
#endif
        private bool isDownloading;
        
        private void DrawRequestDownloadAssetRipper()
        {
            GUILayout.Label("未安装 AssetRipper！");
            GUILayout.Label(
                "AssetRipper 用于从 AssetBundle 中提取资源，并从磁盘加载到 Unity，而不是直接从内存加载。",
                EditorStyles.miniLabel);
            if (!isDownloading)
            {
                if (GUILayout.Button("下载 AssetRipper"))
                {
                    if (!isDownloading)
                    {
                        isDownloading = true;
                        _ripperHandler.Download(() =>
                        {
                            isDownloading = false;
                            EditorUtility.DisplayDialog("dVRC", "AssetRipper 下载完成！", "确定");
                        });
                    }
                }
            }
            else
                GUILayout.Label("正在下载 AssetRipper...", EditorStyles.miniBoldLabel);
            GUILayout.Label(_ripperHandler.WorkingDirectory, EditorStyles.miniLabel);
        }
        
        private void DrawAssetRipperManagement()
        {
            GUILayout.Label("已安装 AssetRipper！");
            if (!isDownloading)
            {
                if (GUILayout.Button("重新安装 AssetRipper"))
                {
                    if (!isDownloading)
                    {
                        Directory.Delete(_ripperHandler.WorkingDirectory, true);
                        isDownloading = true;
                        _ripperHandler.Download(() =>
                        {
                            isDownloading = false;
                            EditorUtility.DisplayDialog("dVRC", "AssetRipper 下载完成！", "确定");
                        });
                    }
                }
            }
            else
                GUILayout.Label("正在下载 AssetRipper...", EditorStyles.miniBoldLabel);
            GUILayout.Label(_ripperHandler.WorkingDirectory, EditorStyles.miniLabel);
        }

        public void OnGUI()
        {
            _ripperHandler.SetWorkingDirectory();
#if VRC_SDK_VRCSDK3
            if (APIUser.IsLoggedIn)
            {
                if(!string.IsNullOrEmpty(SelectedTools.SelectedFile)){}
                else if (!string.IsNullOrEmpty(SelectedTools.SelectedAvatarId))
                    ShowAssetScreen(SelectedTools.SelectedAvatarId);
                else if (!string.IsNullOrEmpty(SelectedTools.SelectedWorldId))
                    ShowAssetScreen(SelectedTools.SelectedWorldId);
                else
                {
                    if (!ReflectingTools.DidFetchContent())
                        GUILayout.Label("尚未获取内容，或没有可下载的内容！");
                    else
                        switch (SelectedTools.SelectedAssetType)
                        {
                            case VRCAssetType.Unknown:
                                GUILayout.Label("请选择要下载的资源");
                                GUILayout.BeginHorizontal();
                                if (GUILayout.Button("Avatar"))
                                    SelectedTools.SelectedAssetType = VRCAssetType.Avatar;
                                if (GUILayout.Button("世界"))
                                    SelectedTools.SelectedAssetType = VRCAssetType.World;
                                GUILayout.EndHorizontal();
                                break;
                            default:
                                ShowList();
                                break;
                        }
                }
            }
            else
                GUILayout.Label("请先在 VRC SDK Control Panel 中登录！");

            if (string.IsNullOrEmpty(SelectedTools.SelectedAvatarId) &&
                string.IsNullOrEmpty(SelectedTools.SelectedWorldId) && 
                SelectedTools.SelectedAssetType == VRCAssetType.Unknown)
            {
                if(!string.IsNullOrEmpty(SelectedTools.SelectedFile))
                    DrawManageFileMenu();
                else
                {
                    DrawManageAssetBundles();
                    if(!_ripperHandler.isPresent)
                        DrawRequestDownloadAssetRipper();
                    else
                        DrawAssetRipperManagement();
                }
            }
#else
            GUILayout.Label("未找到 VRC_SDK_VRCSDK3 脚本定义！请确认已安装 VRCSDK。");
#endif
        }
    }
}