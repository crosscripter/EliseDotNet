#!/usr/bin/shell

rm *.dll
rm *.exe

gmcs -t:libary -out:elise.dll Elise.*.cs -optimize -res:Sources/KJV.src,Elise.Sources.KJV -res:Sources/WLC.src,Elise.Sources.WLC, -res:Sources/STR.src,Elise.Sources.STR -res:Sources/BYZ.src,Elise.Sources.BYZ

gmcs -t:exe -out:elise_cli.exe Elise_CLI.cs -r:elise.dll -optimize
cp elise_cli.exe elise.exe

gmcs -t:winexe -out:elise_gui.exe Elise_GUI.cs -r:elise.dll -optimize
cp elise_gui.exe elisew.exe

mono elisew.exe