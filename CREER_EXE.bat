@echo off
echo === Creation de FortniteStats.exe ===
echo.

where dotnet >nul 2>nul
if errorlevel 1 goto nosdk

cd /d "%~dp0"
dotnet publish "App\FortniteStats.csproj" -c Release -o "%~dp0publish"
if errorlevel 1 goto fail

if not exist "%~dp0publish\icones" mkdir "%~dp0publish\icones"
(
echo Icones personnalisees
echo ---------------------
echo Deposez ici des images PNG pour remplacer les icones integrees.
echo Le nom du fichier doit etre exactement celui de la cle, par exemple Chests.png
echo.
echo   TotalSearched.png    total fouille
echo   Chests.png           coffres
echo   AmmoBoxes.png        caisses de munitions
echo   OtherContainers.png  autres conteneurs
echo   Position.png         classement
echo   Eliminations.png     eliminations
echo   Duration.png         duree
echo   TeamChests.png       coffres de l'equipe
echo   Treasures.png        tresors
echo   VehicleMeters.png    vehicule
echo.
echo Taille conseillee : 64x64 pixels, fond transparent. Relancez FortniteStats.exe apres changement.
) > "%~dp0publish\icones\LISEZMOI.txt"

echo.
echo Termine ! Votre application se trouve ici :
echo   %~dp0publish\FortniteStats.exe
explorer "%~dp0publish"
pause
exit /b 0

:nosdk
echo Le SDK .NET est introuvable.
echo Installez le SDK .NET 10 depuis https://dotnet.microsoft.com/download/dotnet/10.0
echo puis fermez et rouvrez ce script.
pause
exit /b 1

:fail
echo.
echo La compilation a echoue. Copiez le message d'erreur ci-dessus et envoyez-le moi.
pause
exit /b 1
