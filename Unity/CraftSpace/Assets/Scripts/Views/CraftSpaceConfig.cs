using UnityEngine;

namespace CraftSpace.Config
{
    [CreateAssetMenu(fileName = "CraftSpaceConfig", menuName = "CraftSpace/Configuration")]
    public class CraftSpaceConfig : ScriptableObject
    {
        [Header("Visual Settings")]
        [Tooltip("Use unlit shaders for maximum color fidelity")]
        public bool useUnlitShaders = true;
        
        [Tooltip("Background color for the scene")]
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f);
        
        // Other configuration options...
        
        // Singleton access
        private static CraftSpaceConfig _instance;
        public static CraftSpaceConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<CraftSpaceConfig>("CraftSpaceConfig");
                    if (_instance == null)
                    {
                        Debug.LogWarning("No CraftSpaceConfig found in Resources. Using default settings.");
                        _instance = CreateInstance<CraftSpaceConfig>();
                    }
                }
                return _instance;
            }
        }
    }
} 