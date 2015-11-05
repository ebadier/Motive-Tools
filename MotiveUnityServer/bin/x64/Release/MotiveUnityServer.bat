echo off
REM Usage : Set the third argument to the IP ADRESS that Unity3D is running.
REM 		If you want to use unicast server, set the last argument to "unicast"
REM Example : 	MotiveUnityServer.exe "" "" "192.168.x.x" "unicast"
REM 			MotiveUnityServer.exe is equivalent to MotiveUnityServer.exe "" "" "127.0.0.1"
echo on
REM UnityServer controls :
REM q : Exit server
REM r : Reset server
REM p : Print server description
REM f : Print most recent frame ID
REM m : Switch Connection Type to Multicast
REM u : Switch Connection Type to Unicast
pause
MotiveUnityServer.exe "" "" "134.21.208.89" "unicast"