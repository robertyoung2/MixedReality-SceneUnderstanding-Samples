namespace Microsoft.MixedReality.SceneUnderstanding.Samples.Unity
{
    using System;
    using System.Collections;
    using UnityEngine.XR.WSA.Input;
    using UnityEngine.Windows.Speech;
    using System.Collections.Generic;
    using UnityEngine;

    public class SUInputManager : MonoBehaviour
    {
        public SceneUnderstandingManager SuManager;
        public SUMenu suMenu;
        public delegate void InvokeCommand();
        private GameObject SuMinimap = null;
        private GestureRecognizer gestureRecognizer;
        private KeywordRecognizer keywordRecognizer;

        private Dictionary<string,InvokeCommand> speechCommands = new Dictionary<string, InvokeCommand>();

        void Start()
        {
            speechCommands.Add("update", new InvokeCommand( () => 
            {
                SuManager.StartDisplay();
            }));

            speechCommands.Add("auto refresh off", new InvokeCommand( () => 
            {             
                SuManager.AutoRefresh = false;
                SuManager.timeElapsedSinceLastAutoRefresh = SuManager.AutoRefreshIntervalInSeconds;
            }));

            speechCommands.Add("auto refresh on", new InvokeCommand( () => 
            {
                SuManager.AutoRefresh = true;
            }));

            speechCommands.Add("increase radius", new InvokeCommand( () => 
            {
                float fTempFloat = SuManager.BoundingSphereRadiusInMeters + 5.0f;
                fTempFloat = Mathf.Clamp(fTempFloat, 5.0f, 100.0f);
                SuManager.BoundingSphereRadiusInMeters = fTempFloat;
            }));

            speechCommands.Add("decrease radius", new InvokeCommand( () => 
            {
                float fTempFloat = SuManager.BoundingSphereRadiusInMeters - 5.0f;
                fTempFloat = Mathf.Clamp(fTempFloat, 5.0f, 100.0f);
                SuManager.BoundingSphereRadiusInMeters = fTempFloat;
            }));

            speechCommands.Add("scene objects off", new InvokeCommand( () => 
            {
                SuManager.RenderSceneObjects = false;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("scene objects on", new InvokeCommand( () => 
            {
                SuManager.RenderSceneObjects = true;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("scene objects quad", new InvokeCommand( () => 
            {
                SuManager.SceneObjectRenderMode = RenderMode.Quad;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("scene objects mask", new InvokeCommand( () => 
            {
                SuManager.SceneObjectRenderMode = RenderMode.QuadWithMask;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("scene objects mesh", new InvokeCommand( () => 
            {
                SuManager.SceneObjectRenderMode = RenderMode.Mesh;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("scene objects wireframe", new InvokeCommand( () => 
            {
                SuManager.SceneObjectRenderMode = RenderMode.Wireframe;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("inference off", new InvokeCommand( () => 
            {
                SuManager.RequestInferredRegions = false;
            }));

            speechCommands.Add("inference on", new InvokeCommand( () => 
            {
                SuManager.RequestInferredRegions = true;
            }));

            speechCommands.Add("world mesh off", new InvokeCommand( () => 
            {
                SuManager.RenderWorldMesh = false;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("world mesh on", new InvokeCommand( () => 
            {
                SuManager.RenderWorldMesh = true;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("mesh coarse", new InvokeCommand( () => 
            {
                SuManager.RenderQuality = SceneUnderstanding.SceneMeshLevelOfDetail.Coarse;
            }));

            speechCommands.Add("mesh medium", new InvokeCommand( () => 
            {
                SuManager.RenderQuality = SceneUnderstanding.SceneMeshLevelOfDetail.Medium;
            }));

            speechCommands.Add("mesh fine", new InvokeCommand( () => 
            {
                SuManager.RenderQuality = SceneUnderstanding.SceneMeshLevelOfDetail.Fine;
            }));

            speechCommands.Add("platform off", new InvokeCommand( () => 
            {
                SuManager.RenderPlatformSceneObjects = false;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("platform on", new InvokeCommand( () => 
            {
                SuManager.RenderPlatformSceneObjects = true;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("background off", new InvokeCommand( () => 
            {
                SuManager.RenderBackgroundSceneObjects = false;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("background on", new InvokeCommand( () => 
            {
                SuManager.RenderBackgroundSceneObjects = true;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("unknown off", new InvokeCommand( () => 
            {
                SuManager.RenderUnknownSceneObjects = false;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("unknown on", new InvokeCommand( () => 
            {
                SuManager.RenderUnknownSceneObjects = true;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("inferred off", new InvokeCommand( () => 
            {
                SuManager.RenderCompletelyInferredSceneObjects = false;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("inferred on", new InvokeCommand( () => 
            {
                SuManager.RenderCompletelyInferredSceneObjects = true;
                SuManager.StartDisplay();
            }));

            speechCommands.Add("minimap off", new InvokeCommand( () => 
            {
                if(SuMinimap != null)
                {
                    DestroyImmediate(SuMinimap);
                    SuMinimap = null;
                }
                SuManager.SceneRoot.SetActive(false);
            }));

            speechCommands.Add("minimap on", new InvokeCommand( () => 
            {
                if(SuMinimap == null)
                {
                    SuMinimap = Instantiate(SuManager.SceneRoot);
                    SuMinimap.name = "Minimap";
                    SuMinimap.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
                    SuMinimap.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                    SuManager.SceneRoot.SetActive(false);
                }
            }));

            speechCommands.Add("save data", new InvokeCommand( () => 
            {
                SuManager.StartDisplay();
            }));

            speechCommands.Add("help off", new InvokeCommand( () => 
            {
                if(suMenu != null)
                {
                    suMenu.Hide();
                }
                
            }));

            speechCommands.Add("help on", new InvokeCommand( () => 
            {
                if(suMenu != null)
                {
                    suMenu.Show();
                }
            }));

            List<string> keywordsList = new List<string>();
            
            foreach(KeyValuePair<string,InvokeCommand> command in speechCommands)
            {
               keywordsList.Add(command.Key);
            }

            keywordRecognizer = new KeywordRecognizer(keywordsList.ToArray());
            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
            keywordRecognizer.Start();

            gestureRecognizer = new GestureRecognizer();
            gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap);
            gestureRecognizer.Tapped += TapCallBack;
            gestureRecognizer.StartCapturingGestures();
        }

        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            string arg = args.text;
            Debug.Log("SUInputManager.OnPhraseRecognized: Phrase '" + arg + "'recognized");

            InvokeCommand cmd = speechCommands[arg];

            if(cmd != null)
            {
                cmd();
            }
        }

        private void TapCallBack(TappedEventArgs args)
        {
            Debug.Log("SUInputManager.TapCallBack: Tap recognized.");
            SuManager.StartDisplay();
        }

        private void Update()
        {
            if(SuManager.RunOnDevice)
            {
                return;
            }   

            if(Input.GetKeyDown(KeyCode.Alpha1))
            {
                InvokeCommand cmdOn = speechCommands["scene objects on"];
                InvokeCommand cmdOff = speechCommands["scene objects off"];

                if(SuManager.RenderSceneObjects)
                {
                    if(cmdOff != null)
                    {
                        cmdOff();
                    }
                }
                else
                {
                    if(cmdOn != null)
                    {
                        cmdOn();
                    }
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha2))
            {
                InvokeCommand cmd = speechCommands["scene objects quad"];

                if(cmd != null)
                {
                    cmd();
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha3))
            {
                InvokeCommand cmd = speechCommands["scene objects mesh"];

                if(cmd != null)
                {
                    cmd();
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha4))
            {
                InvokeCommand cmd = speechCommands["scene objects wireframe"];

                if(cmd != null)
                {
                    cmd();
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha5))
            {
                InvokeCommand cmdOn = speechCommands["platform on"];
                InvokeCommand cmdOff = speechCommands["platform off"];

                if(SuManager.RenderPlatformSceneObjects)
                {
                    if(cmdOff != null)
                    {
                        cmdOff();
                    }
                }
                else
                {
                    if(cmdOn != null)
                    {
                        cmdOn();
                    }
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha6))
            {
                InvokeCommand cmdOn = speechCommands["background on"];
                InvokeCommand cmdOff = speechCommands["background off"];

                if(SuManager.RenderBackgroundSceneObjects)
                {
                    if(cmdOff != null)
                    {
                        cmdOff();
                    }
                }
                else
                {
                    if(cmdOn != null)
                    {
                        cmdOn();
                    }
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha7))
            {
                InvokeCommand cmdOn = speechCommands["unknown on"];
                InvokeCommand cmdOff = speechCommands["unknown off"];

                if(SuManager.RenderUnknownSceneObjects)
                {
                    if(cmdOff != null)
                    {
                        cmdOff();
                    }
                }
                else
                {
                    if(cmdOn != null)
                    {
                        cmdOn();
                    }
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha8))
            {
                InvokeCommand cmdOn = speechCommands["inferred on"];
                InvokeCommand cmdOff = speechCommands["inferred off"];

                if(SuManager.RenderCompletelyInferredSceneObjects)
                {
                    if(cmdOff != null)
                    {
                        cmdOff();
                    }
                }
                else
                {
                    if(cmdOn != null)
                    {
                        cmdOn();
                    }
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha9))
            {
                InvokeCommand cmdOn = speechCommands["world mesh on"];
                InvokeCommand cmdOff = speechCommands["world mesh off"];

                if(SuManager.RenderWorldMesh)
                {
                    if(cmdOff != null)
                    {
                        cmdOff();
                    }
                }
                else
                {
                    if(cmdOn != null)
                    {
                        cmdOn();
                    }
                }
            }
            else if(Input.GetKeyDown(KeyCode.H))
            {
                InvokeCommand cmdOn = speechCommands["help on"];
                InvokeCommand cmdOff = speechCommands["help off"];
                
                if(suMenu.GetComponent<MeshRenderer>().enabled)
                {
                    if(cmdOff != null)
                    {
                        cmdOff();
                    }
                }
                else
                {
                    if(cmdOn != null)
                    {
                        cmdOn();
                    }
                }
            }

        }

    }
}