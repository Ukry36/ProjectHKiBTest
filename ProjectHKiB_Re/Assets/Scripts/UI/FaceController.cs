using System;
using System.Collections;
using System.Text;
using DG.Tweening;
using UnityEngine;

public class FaceController : MonoBehaviour
{
    // 여기선 눈 깜박이는 거, 입 열고 닫는 것만 조종하고
    // 표정변경은 animator로 
    // 단점은 스프라이트 자체를 바꿔야 해서 돌려쓸 수 없음
    // 뭐 라이브러리나 이거나 그게 그거라 괜찮을 듯
    // 

    public Animator animator;
    private Sequence mouthTween;
    private Sequence sayingTween;
    private Sequence eyeTween;
    public Transform[] mouth_Normal;
    public Transform[] mouth_M;
    public Transform[] mouth_A;
    public Transform[] mouth_O;
    public Transform[] eye_Open;
    public Transform[] eye_Close;

    public bool blinkLoop = true;
    private bool blinkInProgress;

    [NaughtyAttributes.ResizableTextArea]
    public string testPhraseCode;
    [NaughtyAttributes.Button()]
    public void SayPhrase() => SayPhrase(testPhraseCode);

    private void SetActives(Transform[] transforms, bool set)
    {
        for (int i = 0; i < transforms.Length; i++) transforms[i].gameObject.SetActive(set);
    }

    public void PlayAnimation(string name)
    {
        if (animator) animator.Play(name);
    }

    public void SayPhrase(string code)
    {
        StopSaying();
        sayingTween = DOTween.Sequence();
        char prevCode = 'S';
        for (int i = 0; i < code.Length; i++)
        {
            if (code[i] == 'S') { prevCode = 'S'; continue; } // Skip
            sayingTween.AppendInterval(0.12f);
            if (code[i] == '_') { prevCode = 'S'; continue; } // Space
            if (code[i] == 'M') { sayingTween.AppendCallback(SayM); prevCode = 'M'; continue; } // M sound
            if (code[i] == 'O') { sayingTween.AppendCallback(SayO); prevCode = 'O'; continue; } // O sound
            if (code[i] == 'A') { sayingTween.AppendCallback(SayA); prevCode = 'A'; continue; } // A sound
            if (code[i] == 'L') // Link to previous sound
            {
                if (prevCode == 'M') { sayingTween.AppendCallback(SayM); prevCode = 'M'; continue; }
                if (prevCode == 'O') { sayingTween.AppendCallback(SayO); prevCode = 'O'; continue; }
                if (prevCode == 'A') { sayingTween.AppendCallback(SayA); prevCode = 'A'; continue; }
            }
        }
        sayingTween.Play();
    }

    [NaughtyAttributes.Button()]
    public void StopSaying() => sayingTween?.Complete();

    [NaughtyAttributes.Button()]
    public void SayM()
    {
        mouthTween?.Complete();
        SetActives(mouth_Normal, false);
        SetActives(mouth_M, true);
        mouthTween = DOTween.Sequence();
        mouthTween.AppendInterval(0.36f);
        mouthTween.OnComplete(SayEnd);
        mouthTween.Play();
    }
    [NaughtyAttributes.Button()]
    public void SayA()
    {
        mouthTween?.Complete();
        SetActives(mouth_Normal, false);
        SetActives(mouth_A, true);
        mouthTween = DOTween.Sequence();
        mouthTween.AppendInterval(0.36f);
        mouthTween.OnComplete(SayEnd);
        mouthTween.Play();
    }
    [NaughtyAttributes.Button()]
    public void SayO()
    {
        mouthTween?.Complete();
        SetActives(mouth_Normal, false);
        SetActives(mouth_O, true);
        mouthTween = DOTween.Sequence();
        mouthTween.AppendInterval(0.36f);
        mouthTween.OnComplete(SayEnd);
        mouthTween.Play();
    }

    public void SayEnd()
    {
        SetActives(mouth_O, false);
        SetActives(mouth_A, false);
        SetActives(mouth_M, false);
        SetActives(mouth_Normal, true);
    }

    public void Update()
    {
        if (blinkLoop && !blinkInProgress)
            StartCoroutine(BlinkLoop());
    }

    public IEnumerator BlinkLoop()
    {
        blinkInProgress = true;
        float interval = UnityEngine.Random.value > 0.05f ? UnityEngine.Random.Range(4f, 10f) : 0.2f;
        yield return new WaitForSeconds(interval);
        Blink();
        blinkInProgress = false;
    }

    [NaughtyAttributes.Button()]
    public void Blink()
    {
        eyeTween?.Complete();
        SetActives(eye_Close, true);
        SetActives(eye_Open, false);
        eyeTween = DOTween.Sequence();
        eyeTween.AppendInterval(0.12f);
        eyeTween.OnComplete(BlinkEnd);
        eyeTween.Play();
    }

    public void BlinkEnd()
    {
        SetActives(eye_Close, false);
        SetActives(eye_Open, true);
    }

    public void ChangeExpression(string animationName)
    {
        animator.Play(animationName);
    }
}
