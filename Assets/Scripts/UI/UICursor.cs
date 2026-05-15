using UnityEngine;

/// <summary>
/// PlayerMovement locks the cursor during gameplay; menu scenes must unlock it for UI buttons.
/// </summary>
public static class UICursor
{
    public static void UnlockForMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public static void LockForGameplay()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    public static void ApplyForScene(string sceneName)
    {
        // MainGameL1 locks the cursor only while GameManager.GameStarted is true.
        UnlockForMenu();
    }
}
