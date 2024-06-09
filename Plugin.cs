using BepInEx;
using UnityEngine.SceneManagement;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using DG.Tweening;

namespace AircraftRouteDisplayMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            SceneManager.sceneLoaded += OnSceneLoaded;

            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if ((scene.name == "MapPlayer" || scene.name == "London") && AircraftManager.Instance != null)
            {
                AircraftManager.Instance.AircraftCreateEvent.AddListener(HookAircraft);
            }
        }

        private void HookAircraft(Vector2 pos, Aircraft aircraft)
        {
            aircraft.gameObject.AddComponent<AircraftRouteBoundary>().aircraft = aircraft;
        }

        public class AircraftRouteBoundary : MonoBehaviour
        {
            public Aircraft aircraft;

            public LineRenderer boundaryL;
            public LineRenderer boundaryR;

            private void Start()
            {
                boundaryL = Initialize();
                boundaryR = Initialize();
            }

            public LineRenderer Initialize()
            {
                // LineRenderer r = gameObject.AddComponent<LineRenderer>();
                // r.startWidth = r.endWidth = 5;

                LineRenderer r = Instantiate(aircraft.LandingGuideLine);

                r.startColor = r.endColor = new Color(0.4f, 0.4f, 0.4f, 0f);
                return r;
            }

            public void Cancel(LineRenderer renderer)
            {
                renderer.material.DOKill();
                ShortcutExtensions.DOFade(renderer.material, 0f, 0f).SetUpdate(isIndependentUpdate: false);
            }

            public void Apply(LineRenderer renderer, Vector3[] path)
            {
                Debug.Log($"{renderer == null} {path == null}");

                renderer.positionCount = path.Length;
                renderer.SetPositions(path);
                renderer.material.DOKill();

                ShortcutExtensions.DOFade(renderer.material, 1f, 0.5f).SetUpdate(isIndependentUpdate: true).OnComplete(delegate
                {
                    ShortcutExtensions.DOFade(renderer.material, 0f, 1.5f).SetUpdate(isIndependentUpdate: false);
                });
            }
        }

        [HarmonyPatch(typeof(Aircraft), "ShowPath", new Type[] { typeof(List<Vector3>), typeof(bool) })]
        class AircraftShowPathPatcher
        {
            static void Postfix(List<Vector3> path, bool success, ref Aircraft __instance)
            {
                AircraftRouteBoundary boundaryObject = __instance.gameObject.GetComponent<AircraftRouteBoundary>();
                if (boundaryObject == null)
                {
                    return; // For compatibility.
                }

                if (!success)
                {
                    boundaryObject.Cancel(boundaryObject.boundaryL);
                    boundaryObject.Cancel(boundaryObject.boundaryR);
                    return;
                }

                float h = __instance.LandingGuideLine.startWidth * 1.3f;
                int l = path.Count;
                Vector3[] path2L = new Vector3[l - 1];
                Vector3[] path2R = new Vector3[l - 1];
                for (int i = 0; i < l - 1; i++)
                {
                    Vector3 v1 = path[i], v2 = path[i + 1];

                    float x1 = v1.x, y1 = v1.y;
                    float x2 = v2.x, y2 = v2.y;

                    float wx = (x1 + x2) / 2, wy = (y1 + y2) / 2;
                    float dx = x2 - x1, dy = y2 - y1, dd = (float)(h / Math.Sqrt(dx * dx + dy * dy));

                    float lx = dy * dd, ly = -dx * dd;

                    path2L[i] = new Vector3(wx + lx, wy + ly, 0);
                    path2R[i] = new Vector3(wx - lx, wy - ly, 0);
                }

                boundaryObject.Apply(boundaryObject.boundaryL, path2L);
                boundaryObject.Apply(boundaryObject.boundaryR, path2R);
            }
        }
    }
}
