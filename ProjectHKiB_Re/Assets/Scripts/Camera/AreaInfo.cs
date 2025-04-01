using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class AreaInfo
{
    public PolygonCollider2D cameraBound;
    public float cameraResolusion;
    public float changeTime;
    public CinemachineBlendDefinition.Style changeStyle;
    public Sprite backGround;
    public List<string> areaBGMs;
    public float fadeTime;
    //public List<Weather.WeatherType> areaWhetherTypes;
}
