using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class TitleLogoAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform _rootTransform;
    [SerializeField] private List<Sprite> _fillCollectableImages;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < 10;++i)
        {
            int index = Random.Range(0, _fillCollectableImages.Count);
            GameObject obj = new GameObject();
            obj.AddComponent<CanvasRenderer>();
            obj.AddComponent<Image>().sprite = _fillCollectableImages[index];
            var image = obj.GetComponent<Image>();
            image.rectTransform.parent = _rootTransform;

            float rad = Mathf.Deg2Rad * (float)(i * 36);
            image.rectTransform.localPosition = new Vector3(
                      Mathf.Cos(rad) * image.rectTransform.sizeDelta.x,
                      Mathf.Sin(rad) * image.rectTransform.sizeDelta.y,
                      0.0f
                );
            obj.AddComponent<Rigidbody2D>();
            obj.AddComponent<CircleCollider2D>();
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Running()
    {
        
    }


    void Drop()
    {

    }

    void PumpkinPop()
    {
        
    }

    void LogoPop()
    {

    }

    void GhostAssign()
    {

    }


}
