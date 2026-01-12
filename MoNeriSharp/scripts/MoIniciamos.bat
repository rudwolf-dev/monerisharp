@echo off
setlocal

REM Ruta del proyecto MoNeriSharp
set "PROJECT=R:\source\ia\MoNeriSharp\MoNeriSharp"

REM Ruta donde se guardará app.md
set "APP_MD=%PROJECT%\docs\obsidian\2-Modulos\app.md"

REM Crear carpeta si no existe
if not exist "%PROJECT%\docs\obsidian\2-Modulos" (
    mkdir "%PROJECT%\docs\obsidian\2-Modulos"
)

REM Crear contenido de app.md
(
    echo # App Console - moNeriSharp
    echo.
    echo ## 🎯 Objetivo
    echo Proyecto de consola en C# que sirve como punto de entrada para el pipeline modular de moNeriSharp.
    echo.
    echo ## 📂 Ubicacion
    echo - Carpeta: src/
    echo - Proyecto: MoNeriSharp.App.csproj
    echo.
    echo ## 🏗️ Funcion
    echo - Inicializa el chatbot modular.
    echo - Recibe input del usuario por consola.
    echo - Llama al ^PipelineRouter^ para procesar el texto.
    echo - Devuelve respuesta con metadatos (idioma, emocion, sarcasmo, machismo, rol).
    echo.
    echo ## 🔄 Relacion con Modulos
    echo - LanguageClassifier: deteccion de idioma.
    echo - EmotionClassifier: deteccion de emociones.
    echo - SarcasmDetector: deteccion de sarcasmo.
    echo - MachismoDetector: reformulacion inclusiva.
    echo - RoleSelector: asignacion de rol dinamico.
    echo - SQLAssistant, CulinaryAssistant, EducationalAssistant: tareas especializadas.
    echo.
    echo ## 📌 Notas
    echo Este archivo documenta el proyecto de consola y su conexion con los modulos TorchSharp.
    echo Se actualizara conforme se integren nuevos componentes.
) > "%APP_MD%"

echo Archivo creado: %APP_MD%
endlocal