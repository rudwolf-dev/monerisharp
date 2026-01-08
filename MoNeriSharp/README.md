# MoNeriSharp

MoNeriSharp es un proyecto en **C#/.NET** que implementa un modelo de lenguaje entrenado con **TorchSharp** y soporta múltiples formatos de dataset (`.parquet`, `.pt`, `.csv`, `.tsv`).  
El objetivo es construir, entrenar y evaluar un modelo de lenguaje ligero con vocabulario configurable y filtrado de tokens inválidos.

---

## ✨ Características principales

- **Soporte de datasets**:
  - `.parquet` (lectura con ParquetSharp)
  - `.pt` (tensores TorchSharp)
  - `.csv` / `.tsv` (texto tabular)

- **Gestión de vocabulario**:
  - Tokens especiales: `<PAD>`, `<UNK>`, `<BOS>`, `<EOS>`
  - Construcción inicial desde corpus
  - Expansión incremental con nuevos datasets
  - Filtros de palabras inválidas (errores, ruido, groserías, otros idiomas)

- **Entrenamiento**:
  - Modelo de lenguaje recurrente (`LanguageModel`)
  - Entrenamiento con `LanguageTrainer`
  - Guardado y carga de checkpoints (`moNeriLM.pt`)

- **Generación de texto**:
  - Prompt inicial configurable
  - Control de longitud máxima y temperatura

---

## 📦 Estructura del proyecto

```
MoNeriSharp/
├── Program.cs              # Punto de entrada principal
├── modules/                # Definición de modelos
├── Training/               # Lógica de entrenamiento
├── Data/                   # Manejo de datasets
├── Utils/
│   ├── Tokenizer.cs        # Tokenizador con vocabulario expansivo
│   ├── BadWordsFilter.cs   # Filtro de palabras inválidas
│   └── ...
└── models/                 # Carpeta donde se guardan vocabulario y checkpoints
```

---

## 🚀 Uso

### 1. Preparar datasets
Coloca tus archivos en `data/raw/` con alguno de los formatos soportados:
- `*.parquet`
- `*.pt`
- `*.csv` / `*.tsv`

### 2. Ejecutar el proyecto
```bash
dotnet run
```

### 3. Flujo de ejecución
1. Se detecta el dataset disponible (prioridad: `.parquet` > `.pt` > `.csv/.tsv`).
2. Se construye/expande el vocabulario (`vocab.json`).
3. Se entrena el modelo (`moNeriLM.pt`).
4. Se guarda el checkpoint y se genera texto de ejemplo.

---

## ⚙️ Configuración

- **Vocabulario máximo**: `50000` tokens (ajustable en `Program.cs`).
- **Entrenamiento**:
  - Epochs: `2`
  - Batch size: `32`
  - Longitud máxima: `20`
  - Learning rate: `3e-4`
  - Validation split: `0.1`
  - Patience: `3`

---

## 🧹 Filtrado de tokens

El archivo `BadWordsFilter.cs` permite limpiar el corpus de:
- Palabras erróneas (`asdfgh`, `qwerty`, `undefined`, etc.)
- Tokens en otros idiomas (`bonjour`, `ciao`, `こんにちは`, etc.)
- Groserías en español e inglés (`puta`, `mierda`, `fuck`, `shit`, etc.)

Ejemplo de uso en `Program.cs`:

```csharp
corpus = BadWordsFilter.CleanCorpus(corpus);
tokenizer.ExpandVocabulary(corpus, vocabSize: 50000);
```

---

## 📌 Próximos pasos

- Añadir soporte para **listas negras configurables** desde archivo externo (`JSON/TXT`).
- Implementar métricas de evaluación (perplejidad, accuracy).
- Extender el modelo con capas adicionales o embeddings preentrenados.
