/**
 * File: BalanceShellGameGimmick
 * 
 * 天秤ボスのカップマジックギミックSO
 * 
 */
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    [CreateAssetMenu(fileName = "BalanceShellGameGimmick", menuName = "Boss/Gimmick/Balance/ShellGame")]
    public class BalanceShellGameGimmick : BossGimmickSO
    {
        [System.Serializable]
        public class ContentEntry
        {
            [Tooltip("BalanceTrayContentBaseを継承したコンポーネントを持つPrefab")]
            public BalanceTrayContentBase prefab;
            [Min(0.0f)] public float weight = 1.0f;
        }
        [Header("===== 中身の候補 =====")]
        [SerializeField] private List<ContentEntry> _pool = new List<ContentEntry>();

        [Header("===== 進行 ======")]
        [Tooltip("開封後、中身が稼働する時間")]
        [SerializeField, Min(1.0f)] private float _resultDuration = 12.0f;
        [Tooltip("弱点が連続してこの回数出なかったら、次は必ず弱点オブジェクトを入れる(0で無効)")]
        [SerializeField, Min(0)] private int _weaknessPityRounds = 2;

        private BalanceShellGameDirector _director;
        private bool _isComplete;
        private int _roundWithoutWeakness;

        public override bool IsComplete => _isComplete;
        public override bool IsTick => true;

        public override void Initialize(BossContext context)
        {
            base.Initialize(context);

            _director = context.Transform.GetComponentInChildren<BalanceShellGameDirector>();
            if (_director != null) _director.Initialize(context);

           _roundWithoutWeakness = 0;
        }

        public override void Execute()
        {
            _isComplete = false;

            if(_director == null)
            {
                Debug.Log("[ShellGame] ボスの子にBalanceShellGameDirectorがありません", Context.Transform);
                _isComplete = true;
                return;
            }

            if(!TryPickPair(out var left,out var right))
            {
                Debug.LogWarning("[Shellgame] 中身の候補が２種類以上ありません",this);
                _isComplete = true;
                return;
            }

            WarnIfTimeoutTooShort();

            if(!_director.BeginRound(left,right,_resultDuration))
            {
                _isComplete = true;
            }
        }

        public override void Tick(float dt)
        {
            if (_isComplete) return;
            if (_director == null || _director.IsRoundFinished) _isComplete = true;
        }

        public override void Cancel()
        {
            if(_director != null) _director.Abort();
            _isComplete = true;
        }

        /// <summary>
        /// ボスの皿に入れるアイテムを抽選する
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private bool TryPickPair(out BalanceTrayContentBase left,out BalanceTrayContentBase right)
        {
            left = null;
            right = null;

            var valid = _pool.FindAll(e => e != null && e.prefab != null & e.weight > 0);
            if (valid.Count < 2) return false;

            // 弱点オブジェクトが入らない回数の天井チェック
            ContentEntry first = null;
            if(_weaknessPityRounds > 0 && _roundWithoutWeakness >= _weaknessPityRounds)
            {
                var weak = valid.FindAll(e => e.prefab.IsWeakness);
                if (weak.Count > 0) first = PickWeighted(valid);
            }
            if (first == null) first = PickWeighted(valid);

            // 同じ中身を入れないようにする
            var rest = valid.FindAll(e => e != first
                && e.prefab != first.prefab
                && !(first.prefab.IsWeakness && e.prefab.IsWeakness));
            if(rest.Count == 0) return false;

            ContentEntry second = PickWeighted(rest);

            bool hadWeakness = first.prefab.IsWeakness || second.prefab.IsWeakness;
            _roundWithoutWeakness = hadWeakness ? 0 : _roundWithoutWeakness + 1;

            if(UnityEngine.Random.value < 0.5f)
            {
                left = first.prefab;
                right = second.prefab;
            }
            else
            {
                left = second.prefab;
                right = first.prefab;
            }

            return true;
        }

        private static ContentEntry PickWeighted(List<ContentEntry> entries)
        {
            float total = 0.0f;
            foreach (var e in entries) total += e.weight;

            float r = UnityEngine.Random.value * total;
            foreach(var e in entries)
            {
                r -= e.weight;
                if (r <= 0.0f) return e;
            }
            return entries[entries.Count - 1];
        }

        private void WarnIfTimeoutTooShort()
        {
            var slots = Context.Controller != null ? Context.Controller.GimmickSlots : null;
            if (slots == null) return;

            float need = _director.EstimateMaxDuration(_resultDuration);
            foreach (var slot in slots)
            {
                if (slot == null || slot.runtimeGimmick != this || slot.data == null) continue;

                float timeout = slot.data.timeoutDuration;
                if(slot.data.waitForCompletion && timeout > 0.0f && timeout < need)
                {
                    Debug.Log($"[ShellGame] BossGimmickData 「{slot.data.name}」のtimeoutDuration({timeout}s)が" +
                        $"演出込みの最大所要時間(約{need:F0}s)より短いです。0(無制限)にするか延長してください", this);
                }
            }
        }

    }

}
