#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BestHTTP.JSON;
using UnityEngine;
using VRC.Core;

namespace dVRC.Editor
{
    public class VRCAsset
    {
        public string Name { get; }
        public string Id { get; }
        public Texture2D Texture { get; }
        public int Version { get; }
        public string FileLocation { get; private set; }
        public byte[] FileBytes { get; private set; }
        public BuildPlatforms[] SupportedPlatforms { get; private set; } = Array.Empty<BuildPlatforms>();

        private VRCAssetType _vrcAssetType;
        private ApiWorld _world;
        private ApiAvatar _avatar;

        private string _fileEnding
        {
            get
            {
                switch (_vrcAssetType)
                {
                    case VRCAssetType.World:
                        return "w";
                    case VRCAssetType.Avatar:
                        return "a";
                }
                return String.Empty;
            }
        }

        public VRCAsset(ApiWorld world)
        {
            _vrcAssetType = VRCAssetType.World;
            _world = world;
            Name = world.name;
            Id = world.id;
            Texture = ReflectingTools.GetApiModelTextureFromCache(world.id);
            Version = world.version;
            List<BuildPlatforms> supportedPlatforms = new List<BuildPlatforms>();
            _world.Fetch(
                new[]
                {
                    BuildPlatforms.StandaloneWindows,
                    BuildPlatforms.Android,
                    BuildPlatforms.iOS
                }.GetPlatformString(), container =>
            {
                Json.JObject j = (Json.JObject) container.Data;
                Json.JArray up = j["unityPackages"].Array;
                foreach (Json.Token token in up)
                {
                    Json.JObject l = token.Object;
                    switch (l["platform"].StringInstance.ToLower())
                    {
                        case "standalonewindows":
                            supportedPlatforms.Add(BuildPlatforms.StandaloneWindows);
                            break;
                        case "android":
                            supportedPlatforms.Add(BuildPlatforms.Android);
                            break;
                        case "ios":
                            supportedPlatforms.Add(BuildPlatforms.iOS);
                            break;
                        case "web":
                            supportedPlatforms.Add(BuildPlatforms.Web);
                            break;
                    }
                }
                SupportedPlatforms = supportedPlatforms.ToArray();
            }, container =>
            {
                Debug.LogError("uh oh check");
            });
        }
        
        public VRCAsset(ApiAvatar avatar)
        {
            _vrcAssetType = VRCAssetType.Avatar;
            _avatar = avatar;
            Name = avatar.name;
            Id = avatar.id;
            Texture = ReflectingTools.GetApiModelTextureFromCache(avatar.id);
            List<BuildPlatforms> supportedPlatforms = new List<BuildPlatforms>();
            foreach (ApiAvatar.UnityPackage avatarUnityPackage in avatar.unityPackages)
            {
                if(avatarUnityPackage == null || avatarUnityPackage.platform == null) continue;
                switch (avatarUnityPackage.platform.ToLower())
                {
                    case "standalonewindows":
                        supportedPlatforms.Add(BuildPlatforms.StandaloneWindows);
                        break;
                    case "android":
                        supportedPlatforms.Add(BuildPlatforms.Android);
                        break;
                    case "ios":
                        supportedPlatforms.Add(BuildPlatforms.iOS);
                        break;
                    case "web":
                        supportedPlatforms.Add(BuildPlatforms.Web);
                        break;
                }
            }
            SupportedPlatforms = supportedPlatforms.ToArray();
            Version = avatar.version;
        }

        public void DownloadAsset(string path, BuildPlatforms platform, Action<float> percentage = null, Action onDone = null)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string outputFile = Path.Combine(path, Id + ".vrc" + _fileEnding);
            if(_world != null)
            {
                string AssetURL = String.Empty;
                _world.Fetch(platform.GetPlatformString(), container =>
                {
                    Json.JObject j = (Json.JObject) container.Data;
                    Json.JArray up = j["unityPackages"].Array;
                    bool didOne = false;
                    foreach (Json.Token token in up)
                    {
                        Json.JObject l = token.Object;
                        if (l["platform"].StringInstance == platform.GetPlatformString())
                        {
                            AssetURL = l["assetUrl"].StringInstance;
                            didOne = true;
                        }
                    }

                    if (!didOne && up.Count > 0)
                    {
                        Json.JObject l = up[0].Object;
                        AssetURL = l["assetUrl"].StringInstance;
                    }

                    if (!string.IsNullOrEmpty(AssetURL))
                        download2(AssetURL, outputFile, percentage, onDone);
                    else
                        Debug.LogError(
                            $"Could not find World build for platform {platform.GetPlatformString()}! Does it exist?");
                }, container => { Debug.LogError("uh oh"); });
            }
            else if(_avatar != null)
            {
                string AssetURL = _avatar.unityPackages.First(x => x.platform == platform.GetPlatformString()).assetUrl;
                download2(AssetURL, outputFile, percentage, onDone);
            }
        }

        private void download2(string AssetURL, string outputFile, Action<float> percentage = null, Action onDone = null)
        {
            ApiFile.DownloadFile(AssetURL, bytes =>
            {
                using (FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                }
                FileLocation = outputFile;
                FileBytes = bytes;
                if(onDone != null)
                    onDone.Invoke();
            }, e =>
            {
                Debug.LogError(e);
                if(onDone != null)
                    onDone.Invoke();
            }, (l, l1) =>
            {
                float percent =  l / l1 * 100f;
                if(percentage != null)
                    percentage.Invoke(percent);
            });
        }

        public override string ToString()
        {
            string g = Name + "\n" +
                       Id + "\n";
            return g;
        }
    }
}
#endif