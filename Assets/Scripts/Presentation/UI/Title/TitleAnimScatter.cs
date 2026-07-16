/**
 * 小物を一定期間おきにばらまく
 * 寺
 */

using UnityEngine;
using System.Collections.Generic;

namespace Game.Presentation.UI.Title
{

    public class TitleAnimScatter : MonoBehaviour
    {
        [Header("======== 生成パラメータ =======")]
        [SerializeField]
        private List<Sprite> _scatterSprites;

        [SerializeField]
        private float _scatterInterval;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }


}
