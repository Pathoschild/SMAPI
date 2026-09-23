adb logcat -c

cls

adb logcat | findstr /c:DOTNET /c:MonoAndroid /c:AndroidRuntime /c:SMAPI-Tag