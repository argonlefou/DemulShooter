#NoEnv  ; Recommended for performance and compatibility with future AutoHotkey releases.
; #Warn  ; Enable warnings to assist with detecting common errors.
SendMode Input  ; Recommended for new scripts due to its superior speed and reliability.
SetWorkingDir %A_ScriptDir%  ; Ensures a consistent starting directory.

FileCopy, D:\[.Programmation.]\UnityPlugin_BepInEx_SkullOfShadow\bin\release\SkullOfShadow_BepInEx_DemulShooter_Plugin.dll, BepInEx\plugins\SkullOfShadow_BepInEx_DemulShooter_Plugin.dll, 1
Run, Nerf.exe