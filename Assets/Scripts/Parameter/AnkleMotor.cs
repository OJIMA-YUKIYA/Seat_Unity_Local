/*Summary
 *  下肢駆動装置の足首のモーター用に目的角度をパルスに変換するメソッドを含む
 *
 *Log
 *  2018.11.10 金子 unity2018.2.13f1 実装
 */
using UnityEngine;

public class AnkleMotor : MonoBehaviour
{
    [SerializeField]
    private const float maxAngle = 25f; //[degrees]
    [SerializeField]
    private const float minAngle = -55f; //[degrees]
    private const float resolutionAng = 0.0144f; //[degrees]

    public int CalculatePulse(float objAng)
    {
        if (objAng > maxAngle) objAng = maxAngle;
        else if (objAng < minAngle) objAng = minAngle;        
        return (int)(objAng / resolutionAng);
    }
}
