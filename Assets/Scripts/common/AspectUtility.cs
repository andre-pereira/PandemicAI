using UnityEngine;
using System.Collections;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Utility for maintaining a fixed aspect ratio on the main camera by applying letterboxing or pillarboxing.
    /// Also provides various properties to calculate adjusted screen dimensions and mouse positions.
    /// </summary>
    public class AspectUtility : MonoBehaviour
    {
        #region Fields and Properties

        /// <summary>
        /// The desired aspect ratio (width/height) to maintain. Default is 16:9.
        /// </summary>
        public float _wantedAspectRatio = 1.777778f;

        /// <summary>
        /// The background color used for letterboxing or pillarboxing areas.
        /// </summary>
        public Color _backgroundColor = Color.black;

        static float wantedAspectRatio;
        static Color backgroundColor;
        static Camera cam;
        static Camera backgroundCam;

        static int myWidth;
        static int myHeight;

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// Initializes the camera settings and starts monitoring for screen resolution changes.
        /// </summary>
        void Awake()
        {
            cam = GetComponent<Camera>();
            if (!cam)
            {
                cam = Camera.main;
            }
            if (!cam)
            {
                Debug.LogError("No camera available");
                return;
            }
            wantedAspectRatio = _wantedAspectRatio;
            backgroundColor = _backgroundColor;

            myWidth = myHeight = 0;
            StartCoroutine(CheckCamera());
        }

        /// <summary>
        /// Stops any running coroutines when the object is destroyed.
        /// </summary>
        void OnDestroy()
        {
            StopAllCoroutines();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Continuously checks for changes in screen resolution and updates the camera settings if necessary.
        /// </summary>
        /// <returns>An IEnumerator for coroutine processing.</returns>
        IEnumerator CheckCamera()
        {
            while (true)
            {
                if (myWidth != Screen.width || myHeight != Screen.height)
                {
                    SetCamera();
                }
                yield return new WaitForSeconds(0.33f);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the camera's viewport to maintain the desired aspect ratio by applying letterboxing or pillarboxing.
        /// Also creates a background camera if needed.
        /// </summary>
        public static void SetCamera()
        {
            myWidth = Screen.width;
            myHeight = Screen.height;

            float currentAspectRatio = (float)Screen.width / Screen.height;
            // Compare aspect ratios rounded to two decimals
            if ((int)(currentAspectRatio * 100) / 100.0f == (int)(wantedAspectRatio * 100) / 100.0f)
            {
                cam.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
                if (backgroundCam)
                {
                    Destroy(backgroundCam.gameObject);
                }
                return;
            }

            // Pillarbox: current aspect ratio is wider than desired.
            if (currentAspectRatio > wantedAspectRatio)
            {
                float inset = 1.0f - wantedAspectRatio / currentAspectRatio;
                cam.rect = new Rect(inset / 2, 0.0f, 1.0f - inset, 1.0f);
            }
            // Letterbox: current aspect ratio is taller than desired.
            else
            {
                float inset = 1.0f - currentAspectRatio / wantedAspectRatio;
                cam.rect = new Rect(0.0f, inset / 2, 1.0f, 1.0f - inset);
            }

            if (!backgroundCam)
            {
                // Create a new background camera to render the clear color behind the main camera.
                backgroundCam = new GameObject("BackgroundCam", typeof(Camera)).GetComponent<Camera>();
                backgroundCam.depth = int.MinValue;
                backgroundCam.clearFlags = CameraClearFlags.SolidColor;
                backgroundCam.backgroundColor = backgroundColor;
                backgroundCam.cullingMask = 0;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the adjusted screen height based on the camera's viewport.
        /// </summary>
        public static int screenHeight
        {
            get { return (int)(Screen.height * cam.rect.height); }
        }

        /// <summary>
        /// Gets the adjusted screen width based on the camera's viewport.
        /// </summary>
        public static int screenWidth
        {
            get { return (int)(Screen.width * cam.rect.width); }
        }

        /// <summary>
        /// Gets the horizontal offset (in pixels) of the camera's viewport.
        /// </summary>
        public static int xOffset
        {
            get { return (int)(Screen.width * cam.rect.x); }
        }

        /// <summary>
        /// Gets the vertical offset (in pixels) of the camera's viewport.
        /// </summary>
        public static int yOffset
        {
            get { return (int)(Screen.height * cam.rect.y); }
        }

        /// <summary>
        /// Gets the screen rectangle of the camera's viewport in pixel coordinates.
        /// </summary>
        public static Rect screenRect
        {
            get
            {
                return new Rect(
                    cam.rect.x * Screen.width,
                    cam.rect.y * Screen.height,
                    cam.rect.width * Screen.width,
                    cam.rect.height * Screen.height);
            }
        }

        /// <summary>
        /// Gets the adjusted mouse position relative to the camera's viewport.
        /// </summary>
        public static Vector3 mousePosition
        {
            get
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y -= (int)(cam.rect.y * Screen.height);
                mousePos.x -= (int)(cam.rect.x * Screen.width);
                return mousePos;
            }
        }

        /// <summary>
        /// Gets the adjusted GUI mouse position clamped within the camera's viewport.
        /// </summary>
        public static Vector2 guiMousePosition
        {
            get
            {
                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = Mathf.Clamp(mousePos.y, cam.rect.y * Screen.height, cam.rect.y * Screen.height + cam.rect.height * Screen.height);
                mousePos.x = Mathf.Clamp(mousePos.x, cam.rect.x * Screen.width, cam.rect.x * Screen.width + cam.rect.width * Screen.width);
                return mousePos;
            }
        }

        #endregion
    }
}
