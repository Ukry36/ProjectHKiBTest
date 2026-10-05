using UnityEngine;
namespace StateMachine
{
    // 대상의 직계 자식을 각각 켜짐↔꺼짐으로 뒤집는다 — 회전 벽 미로에서 "가로벽 끄고 세로벽 켜기"를
    // 상태 기억 없이 토글로 하려고 만들었다. TargetEntityManipulateAction 안에 넣어 대상을 지목한다.
    //
    // SetEntityActiveAction을 쓰지 않는 이유: 그건 켤지 끌지가 고정이라 토글하려면 현재 상태를 어딘가
    // 기억해야 하고, 꺼진 오브젝트는 MapLocalManager.AutoFindEventTargets(FindObjectsOfType)가
    // 대상으로 찾지 못한다. 항상 켜져 있는 부모를 대상으로 두고 자식만 뒤집으면 둘 다 필요 없다.
    [System.Serializable]
    public class ToggleChildrenActiveAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            Transform parent = stateController.transform;
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(!child.activeSelf);
            }
        }
    }
}
