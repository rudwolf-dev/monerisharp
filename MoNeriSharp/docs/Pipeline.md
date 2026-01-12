
```mermaid
flowchart TD

    subgraph Entrada["📂 Entrada de Datos"]
        DL[DataLoader.cs\nCarga CSV/TSV → Corpus homogéneo]
    end

    subgraph Preprocesamiento["🧹 Preprocesamiento"]
        TF[TokenFilter.cs\nElimina ruido y tokens inválidos]
        BF[BadWordsFilter.cs\nFiltra groserías ofensivas]
        DF[DatasetFilter.cs\nCensura categorías temáticas]
    end

    subgraph Tokenización["🔑 Tokenización"]
        TK[Tokenizer.cs\nConstrucción/expansión de vocabulario\nEncode/Decode → IDs]
    end

    subgraph Modelos["🧠 Modelos"]
        LSTM[LanguageModel.cs\nModelo recurrente clásico]
        TRF[TransformerLanguageModel.cs\nModelo basado en atención]
    end

    subgraph Entrenamiento["⚙️ Entrenamiento"]
        LT[LanguageTrainer.cs\nEntrena LSTM con corpus]
        TT[TransformerTrainer.cs\nEntrena Transformer con corpus]
    end

    subgraph Aplicación["💻 Program.cs"]
        CFG[Configuración interactiva\nEpochs, batchSize, filtros, vocab]
        SEL[Selección de modelo\nTransformer vs LSTM]
        TRN[Entrenamiento con Trainer]
        INT[Interacción con el modelo\nConversación en consola]
    end

    DL --> TF --> BF --> DF --> TK
    TK --> LSTM
    TK --> TRF
    LSTM --> LT
    TRF --> TT
    LT --> TRN
    TT --> TRN
    TRN --> INT
```

---

## 📌 Explicación del flujo

1. **Entrada de datos:**
    
    - `DataLoader` detecta columnas relevantes en CSV/TSV y construye corpus homogéneo con roles (`[USER]`, `[ASSISTANT]`, `<QUESTION>`, `<ANSWER>`).
2. **Preprocesamiento:**
    
    - `TokenFilter` elimina ruido y tokens inválidos.
    - `BadWordsFilter` filtra groserías (opcional).
    - `DatasetFilter` censura categorías temáticas.
3. **Tokenización:**
    
    - `Tokenizer` construye o expande vocabulario.
    - Convierte texto → IDs (`Encode`) y IDs → texto (`Decode`).
4. **Modelos:**
    
    - `LanguageModel` (LSTM).
    - `TransformerLanguageModel` (más potente en secuencias largas).
5. **Entrenamiento:**
    
    - `LanguageTrainer` o `TransformerTrainer` entrenan el modelo elegido.
    - Incluyen validación, early stopping, checkpoints y métricas (`Loss`, `Perplexity`).
6. **Aplicación (`Program.cs`):**
    
    - Configuración interactiva de hiperparámetros.
    - Selección de modelo.
    - Entrenamiento.
    - Interacción en consola con el modelo entrenado.
    - Guardado final del checkpoint.