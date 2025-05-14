#NoEnv  ; Recommended for performance and compatibility with future AutoHotkey releases.
; #Warn  ; Enable warnings to assist with detecting common errors.
SendMode Input  ; Recommended for new scripts due to its superior speed and reliability.
SetWorkingDir %A_ScriptDir%  ; Ensures a consistent starting directory.

FileCopy, D:\C#\UnityPlugin_BepInEx_MissionImpossible\bin\release\MissionImpossible_BepInEx_DemulShooter_Plugin.dll, BepInEx\plugins\MissionImpossible_BepInEx_DemulShooter_Plugin.dll, 1
Run, MissionImpossible.exe