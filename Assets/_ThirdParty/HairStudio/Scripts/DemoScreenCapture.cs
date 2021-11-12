using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HairStudio {
    public class DemoScreenCapture : MonoBehaviour
    {
        public int sizeFactor = 1;
        public bool capture;

        void OnValidate() {
            if (capture) {
                capture = false;
                ScreenCapture.CaptureScreenshot("screenshot", sizeFactor);
            }
        }
    }
}