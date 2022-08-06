using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SystemInfoCheck
{
    [MenuItem("Assets/SystemCheck/CheckGraphicsDevice")]
    private static void CheckGraphicsDevice()
    {
        Debug.LogWarning("<color=#8CFF53>Graphics Device Info\n" +
                    $"graphicsDeviceID\t: { SystemInfo.graphicsDeviceID}\n" +
                    $"graphicsDeviceName\t: { SystemInfo.graphicsDeviceName}\n" +
                    $"graphicsDeviceType\t: { SystemInfo.graphicsDeviceType}\n" +
                    $"graphicsDeviceVendor\t: { SystemInfo.graphicsDeviceVendor}\n" +
                    $"graphicsDeviceVendorID\t: { SystemInfo.graphicsDeviceVendorID}\n" +
                    $"graphicsDeviceVersion\t: { SystemInfo.graphicsDeviceVersion}\n" +
                    $"graphicsMultiThreaded\t: { SystemInfo.graphicsMultiThreaded}\n" +
                    $"graphicsMemorySize\t: { SystemInfo.graphicsMemorySize}\n" +
                    $"graphicsShaderLevel\t: { SystemInfo.graphicsShaderLevel}\n" +
                    $"graphicsUVStartsAtTop\t: { SystemInfo.graphicsUVStartsAtTop}\n" +
                    $"supportsInstancing\t: {SystemInfo.supportsInstancing}</color>");
    }
}
