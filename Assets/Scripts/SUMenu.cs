
namespace Microsoft.MixedReality.SceneUnderstanding.Samples.Unity
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class SUMenu : MonoBehaviour
    {

        string OnDeviceText = @"
        Welcome to the Scene Understanding App!

        This app displays scene objects from the scene understanding runtime, e.g. walls, floors, ceilings, etc.
            
            Speech Commands:
                    
            'update' or tap - display the latest data
            'auto refresh on'/'auto refresh off' - enable/disable auto refresh

            'scene objects on'/'scene objects off' - enable/disable scene objects
            'scene objects quad' - enable quad mode
            'scene objects mesh' - enable default (mesh) mode
            'scene objects wireframe' - enable wireframe mode

            'platform on'/'platform off' - enable/disable large horizontal surfaces (aka platform)
            'background on'/'background off' - enable/disable background objects
            'unknown on'/'unknown off' - enable/disable unknown objects
            'inference on'/'inference off' - enable/disable completely inferred objects (requires refresh)
                    
            'world mesh on'/'world mesh off' - enable/disable world mesh
            'mesh coarse', 'mesh medium' or 'mesh fine' - change world mesh level of detail
                    
            'minimap on'/'minimap off' - enable/disable minimap mode (do try this out :))
        
            'increase radius'/'decrease radius' - increase/decrease radius of the sphere around the camera, which is used when querying the environment

            'save data' - save current scene to disk

            'help on'/'help off' - enable/disable this help menu";

        string onPcText = @"
        Welcome to the Scene Understanding App!

        This app displays scene objects from the scene understanding runtime, e.g. walls, floors, ceilings, etc.
            
        Input Controls:
                    
            'W', 'A', 'S', 'D', 'Q', 'E' - change camera position (hold Shift to speed up)
            Mouse primary button + move mouse - change camera orientation
            'F' - focus on a scene object
            'R' - move camera back to origin
                        
            '1' - enable/disable scene objects
            '2' - enable quad mode
            '3' - enable default (mesh) mode
            '4' - enable wireframe mode
                        
            '5' - enable/disable large horizontal surfaces (aka platform)
            '6' - enable/disable background objects
            '7' - enable/disable unknown objects
            '8' - enable/disable completely inferred objects
                    
            '9' - enable/disable world mesh

            'H' - enable/disable this help menu
                
    ";

        void Start()
        {
            SceneUnderstandingManager su = this.gameObject.GetComponent<SUInputManager>().SuManager;
            string displayText = su.RunOnDevice ? OnDeviceText : onPcText;

            TextMesh menutext = this.gameObject.GetComponent<TextMesh>();
            menutext.text = displayText;

            Show();
        }

        public void Show()
        {
            this.gameObject.transform.position = Camera.main.transform.position + (Camera.main.transform.forward * 1.5f);
            this.GetComponent<MeshRenderer>().enabled = true;
            foreach(Transform child in this.transform)
            {
                child.GetComponent<MeshRenderer>().enabled = true;
            }
        }

        public void Hide()
        {
            this.GetComponent<MeshRenderer>().enabled = false;
            foreach(Transform child in this.transform)
            {
                child.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

}