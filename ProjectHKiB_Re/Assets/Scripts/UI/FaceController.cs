using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceController : MonoBehaviour
{
    // 여기선 눈 깜박이는 거, 입 열고 닫는 것만 조종하고
    // 표정변경은 animator로 
    // 단점은 스프라이트 자체를 바꿔야 해서 돌려쓸 수 없음
    // 뭐 라이브러리나 이거나 그게 그거라 괜찮을 듯
    // 

    public Animator animator;
    public void SayM()
    {

    }
    public void SayA()
    {

    }
    public void SayO()
    {

    }

    public IEnumerator BlinkLoop()
    {
        yield return new WaitForSeconds(0);
    }
    public void Blink()
    {

    }
    public void ChangeExpression(string animationName)
    {
        animator.Play(animationName);
    }
}
