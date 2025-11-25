#if DOTWEEN_TMP
using UnityEngine;
using TMPro;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening
{
    public static class DOTweenModuleTMP
    {
        public static Tweener DOText(this TextMeshProUGUI target, string endValue, float duration, bool richTextEnabled = true, ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)
        {
            if (target == null) return null;
            TweenerCore<string, string, StringOptions> t = DOTween.To(() => target.text, x => target.text = x, endValue, duration);
            t.SetOptions(richTextEnabled, scrambleMode, scrambleChars).SetTarget(target);
            return t;
        }
    }
}
#endif
