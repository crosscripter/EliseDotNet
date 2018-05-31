@ECHO OFF

del *.dll /Q
del *.exe /Q

csc -nologo -t:library -out:Elise.dll Elise.*.cs -o -res:Sources/KJV.src,Elise.Sources.KJV -res:Sources/WLC.src,Elise.Sources.WLC -res:Sources/STR.src,Elise.Sources.STR -res:Sources/BYZ.src,Elise.Sources.BYZ
csc -nologo -t:exe -out:elise_cli.exe Elise_CLI.cs -r:Elise.dll -o
csc -nologo -t:winexe -out:elise_gui.exe Elise_GUI.cs -r:Elise.dll -o
elise_gui