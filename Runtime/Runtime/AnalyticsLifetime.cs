using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    public class AnalyticsLifetime : MonoBehaviour
    {
        float m_Time = 0.0F;
        
        void Awake()
        {
            hideFlags = HideFlags.NotEditable | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            
            #if !UNITY_ANALYTICS_DEVELOPMENT
            hideFlags = hideFlags | HideFlags.HideInInspector;
            #endif
            
            DontDestroyOnLoad(this.gameObject);
            
            // i hate this, but i don't know of a better way to know if we're running in a test..
            var mbs = FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in mbs)
            {
                if (mb.GetType().FullName == "UnityEngine.TestTools.TestRunner.PlaymodeTestsController")
                {
                    return;
                }
            }
            
            Events.Startup();
        }

        void Update()
        {
            // This is a very simple mechanism to flush the buffer, it might not be the most graceful, it might
            // not be the most graceful, but we can add the complexity later when its needed.
            // Once every 'n' flush the Events, then reset the timer.
            m_Time += Time.deltaTime;
            
            if (m_Time >= 60.0F)
            {
                Events.InternalTick();
                m_Time = 0.0F;
            }
            
        }

        void OnDestroy()
        {
            Events.Shutdown();
        }
    }

    public static class ContainerObject
    {
        static bool s_Created;
        static GameObject s_Container;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (!s_Created)
            {
                #if UNITY_ANALYTICS_DEVELOPMENT
                Debug.Log("Created Analytics Container");
                #endif
                
                s_Container = new GameObject("AnalyticsContainer");
                s_Container.AddComponent<AnalyticsLifetime>();

                s_Created = true;
            }
        }
    }
}
