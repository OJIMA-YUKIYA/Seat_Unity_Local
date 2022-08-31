/*Summary
 *  下肢駆動装置のスライダーのモーター用に目的位置をパルスに変換するメソッドを含む
 *
 *Log
 *  2018.11.10 金子 unity2018.2.13f1 実装
 */
using UnityEngine;

public class SliderMotor : MonoBehaviour {
    [SerializeField]
    public const float maxPosition = 90f;  //[mm]
    [SerializeField]
    private const float minPosition = -90f;  //[mm]
    private const float resolutionPos = 0.012f; //[mm]

    public int CalculatePulse(float objPos)
    {
        if (objPos > maxPosition) objPos = maxPosition;
        else if (objPos < minPosition) objPos = minPosition;
        return (int)(objPos / resolutionPos);
    }
}
