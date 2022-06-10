Dim FSO
Set FSO = CreateObject("Scripting.FileSystemObject")
Dim objShell
Set objShell = CreateObject("Wscript.Shell")
objShell.CurrentDirectory = FSO.getParentFolderName(WScript.ScriptFullName)
objShell.run "MicMuteControl.exe /tray", 0, false
