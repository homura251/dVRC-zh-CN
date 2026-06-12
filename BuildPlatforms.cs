namespace dVRC
{
    public enum BuildPlatforms
    {
        StandaloneWindows,
        Android,
        iOS,
        Web
    }

    public static class BuildPlatformsExtensions
    {
        public static string GetPlatformString(this BuildPlatforms platforms)
        {
            switch (platforms)
            {
                case BuildPlatforms.Android:
                    return "android";
                case BuildPlatforms.iOS:
                    return "ios";
                case BuildPlatforms.Web:
                    return "web";
            }
            return "standalonewindows";
        }
        
        public static string GetPlatformString(this BuildPlatforms[] platforms)
        {
            string s = "";
            for (int i = 0; i < platforms.Length; i++)
            {
                BuildPlatforms platform = platforms[i];
                s += platform.GetPlatformString();
                if(i >= platforms.Length - 1) continue;
                s += ',';
            }
            return s;
        }
    }
}