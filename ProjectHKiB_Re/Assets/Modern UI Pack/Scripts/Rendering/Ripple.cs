using UnityEngine;
using UnityEngine.UI;

namespace Michsky.MUIP
{
    public class Ripple : MonoBehaviour
    {
        public bool staticImageMode = false;
        public bool fade = true;
        public bool unscaledTime = false;
        public float speed;
        public float maxSize;
        public Color startColor;
        public Color transitionColor;
        Image colorImg;

        private float progress;

        void Start()
        {
            if (staticImageMode == true)
            {
                RectTransform rectTransform = GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.localScale = Vector3.one;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
            }

            else
                transform.localScale = new Vector3(0f, 0f, 0f);
            colorImg = GetComponent<Image>();
            colorImg.raycastTarget = false;
            colorImg.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a);
            progress = 0f;
            if (fade == false) speed *= 10;
        }

        void Update()
        {
            if (unscaledTime == false)
            {
                progress = Mathf.Lerp(progress, 1, Time.deltaTime * speed);
                if (fade == true)
                    colorImg.color = Color.Lerp(colorImg.color, new Color(transitionColor.r, transitionColor.g, transitionColor.b, transitionColor.a), Time.deltaTime * speed);
                if (staticImageMode == false)
                    transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(maxSize, maxSize, maxSize), Time.deltaTime * speed);
                if (progress >= 0.99)
                {
                    if (transform.parent.childCount == 1) { transform.parent.gameObject.SetActive(false); }
                    Destroy(gameObject);
                }

            }
            else
            {
                progress = Mathf.Lerp(progress, 1, Time.unscaledDeltaTime * speed);
                if (fade == true)
                    colorImg.color = Color.Lerp(colorImg.color, new Color(transitionColor.r, transitionColor.g, transitionColor.b, transitionColor.a), Time.unscaledDeltaTime * speed);
                if (staticImageMode == false)
                    transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(maxSize, maxSize, maxSize), Time.unscaledDeltaTime * speed);
                if (progress >= 0.99)
                {
                    if (transform.parent.childCount == 1) { transform.parent.gameObject.SetActive(false); }
                    Destroy(gameObject);
                }

            }
        }
    }
}