using UnityEngine;
using UnityEngine.UI;

public class GraffitiPatternView : MonoBehaviour 
{
    public PlayerSkillDataSO playerSkillDataSO;
    public Color transparent = new(0,0,0,0);

    [NaughtyAttributes.Button]
    public void Inititialize()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    [NaughtyAttributes.Button]
    public void SetPattern() => SetPattern(playerSkillDataSO.graffitiCodes[0]);
    public void SetPattern(GraffitiCode graffitiCode)
    {
        UpdateGridSize(graffitiCode.Height, graffitiCode.Width);
        for (int i = 0; i < graffitiCode.Height * graffitiCode.Width; i++)
        {
            if (i >= transform.childCount) break;
            transform.GetChild(i).GetComponent<Image>().color = transparent;
        }

        for (int i = 0; i < graffitiCode.code.Count; i++)
        {
            int index = (int)graffitiCode.code[i].x + (int)graffitiCode.code[i].y * graffitiCode.Width;
            if (index >= transform.childCount) break;
            transform.GetChild(index).GetComponent<Image>().color = graffitiCode.color;
        }
    }

    public GridLayoutGroup grid;
    public void UpdateGridSize(int rows, int cols)
    {
        float width = GetComponent<RectTransform>().rect.width;
        float height = GetComponent<RectTransform>().rect.height;
        
        float availableWidth = width - grid.padding.horizontal;
        float availableHeight = height - grid.padding.vertical;

        int maxDim = Mathf.Max(rows, cols);
        
        float minSide = Mathf.Min(availableWidth, availableHeight);
        
        float combinedSpacing = grid.spacing.x * (maxDim - 1);
        float cellSize = (minSide - combinedSpacing) / maxDim;

        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i < rows * cols);
        }
    }
}