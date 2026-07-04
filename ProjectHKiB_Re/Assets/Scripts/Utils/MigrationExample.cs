
public class MigrationExample
{
    /*
    [NaughtyAttributes.Button]
    public static void Migrate()
    {
        // 1. 프로젝트 내의 모든 StateSO 에셋을 검색합니다.
        string[] guids = AssetDatabase.FindAssets("t:StateSO");
        int migratedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StateSO stateSO = AssetDatabase.LoadAssetAtPath<StateSO>(path);

            if (stateSO == null) continue;

            stateSO.EnterActions = new StateAction[stateSO.enterActions.Length];
            stateSO.UpdateActions = new StateAction[stateSO.updateActions.Length];
            stateSO.ExitActions = new StateAction[stateSO.exitActions.Length];

            MigrateActionArray(stateSO.enterActions, stateSO.EnterActions);
            MigrateActionArray(stateSO.updateActions, stateSO.UpdateActions);
            MigrateActionArray(stateSO.exitActions, stateSO.ExitActions);

            if (stateSO.actionSequence != null)
            {
                for (int i = 0; i < stateSO.actionSequence.Length; i++)
                {
                    if (stateSO.actionSequence[i].action != null) stateSO.actionSequence[i].Action = CreateAndCopyAction(stateSO.actionSequence[i].action);
                }
            }

            if (stateSO.transitions != null)
            {
                foreach (var transition in stateSO.transitions)
                {
                    if (transition.action != null) transition.Action = CreateAndCopyAction(transition.action);
                    if (transition.decisions != null)
                    {
                        for (int i = 0; i < transition.decisions.Length; i++)
                        {
                            if (transition.decisions[i].decision != null) transition.decisions[i].Decision = CreateAndCopyDecision(transition.decisions[i].decision);
                        }
                    }
                }
            }

            EditorUtility.SetDirty(stateSO);
            migratedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"마이그레이션 완료! 총 {migratedCount}개의 StateSO 변환됨.");
    }

    private static void MigrateActionArray(StateActionSO[] oldArray, StateAction[] newList)
    {
        if (oldArray == null) return;
        for (int i = 0; i < oldArray.Length; i++)
        {
            newList[i] = CreateAndCopyAction(oldArray[i]);
        }
    }

    private static string targetAssembly = "Assembly-CSharp";
    private static StateAction CreateAndCopyAction(StateActionSO oldAction)
    {
        if (!oldAction) return null;
        string className = oldAction.GetType().Name;

        // 2. 새로운 네임스페이스("StateMachine") 환경에서의 타입 정보를 찾음
        string fullTypeName = $"StateMachine.{className}, {targetAssembly}";
        Type newType = Type.GetType(fullTypeName);

        if (newType == null)
        {
            Debug.LogError($"[마이그레이션 실패] {fullTypeName} 타입을 찾을 수 없습니다. 클래스 명을 확인해주세요.");
            return null;
        }

        // 3. 새로운 일반 C# 클래스 인스턴스 동적 생성
        StateAction newInstance = (StateAction)Activator.CreateInstance(newType);

        // 4. 기존 SO가 가지고 있던 필드 데이터를 리플렉션으로 새 클래스에 복사
        System.Reflection.FieldInfo[] oldFields = oldAction.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (System.Reflection.FieldInfo oldField in oldFields)
        {
            // 유니티 내부 시스템 필드(m_CachedPtr 등)는 제외
            if (oldField.Name.StartsWith("m_")) continue;

            // 새 클래스에서 동일한 이름의 필드를 찾음
            System.Reflection.FieldInfo newField = newType.GetField(oldField.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (newField != null && newField.FieldType == oldField.FieldType)
            {
                // 데이터 값 그대로 복사 (대사 텍스트, 수치 등 값 타입/스트링 등 호환 가능)
                object value = oldField.GetValue(oldAction);
                newField.SetValue(newInstance, value);
            }
        }

        return newInstance;
    }

    private static StateDecision CreateAndCopyDecision(StateDecisionSO oldDecision)
    {
        if (!oldDecision) return null;
        string className = oldDecision.GetType().Name;
        string fullTypeName = $"StateMachine.{className}, {targetAssembly}";
        Type newType = Type.GetType(fullTypeName);

        if (newType == null)
        {
            Debug.LogError($"[마이그레이션 실패] {fullTypeName} 타입을 찾을 수 없습니다. 클래스 명을 확인해주세요.");
            return null;
        }

        StateDecision newInstance = (StateDecision)Activator.CreateInstance(newType);

        System.Reflection.FieldInfo[] oldFields = oldDecision.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (System.Reflection.FieldInfo oldField in oldFields)
        {
            if (oldField.Name.StartsWith("m_")) continue;

            System.Reflection.FieldInfo newField = newType.GetField(oldField.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (newField != null && newField.FieldType == oldField.FieldType)
            {
                object value = oldField.GetValue(oldDecision);
                newField.SetValue(newInstance, value);
            }
        }

        return newInstance;
    }
*/

}