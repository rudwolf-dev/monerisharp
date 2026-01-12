# 📑 Catálogo de Versiones — moNeriSharp

## 🔖 v1.7

- **Objetivo principal:** fortalecer el `TransformerTrainer` para acercarlo a un modelo GPT‑mini/GPT‑like.
- **Cambios:**
    - Optimización del bucle de entrenamiento y gestión de batches.
    - Inclusión de métricas adicionales (fluidez, razonamiento lógico).
    - Uso de corpus más amplio y variado.
    - Primeros pasos hacia entrenamiento autoregresivo.
- **Impacto:** transición estratégica, preparando la arquitectura para soportar mejoras profundas en el Transformer.

---

## 🔖 v1.8

- **Objetivo principal:** refactorización modular y estandarización de la interfaz.
- **Cambios:**
    - **Tokenizador dividido en módulos:**
        - `WordCleaner.cs` → limpieza de puntuación, espacios y saltos de línea.
        - `VocabularyBuilder.cs` → construcción de vocabulario en tres modos distintos.
    - **Renombrado de modelos y trainers:**
        - `LanguageModel` → `LSTMModel`.
        - `TransformerLanguageModel` → `TransformerModel`.
        - `LanguageTrainer` → `LSTMTrainer`.
        - `TransformerTrainer` → ajustado para trabajar con `TransformerModel`.
    - **Nueva interfaz común `ILanguageModel`:**
        - `Forward(tokens, mask = null)` → unifica LSTM y Transformer.
- **Impacto:** mayor claridad, modularidad y compatibilidad entre modelos.

---

## 🔖 v1.9

- **Objetivo principal:** acercar aún más el Transformer a GPT‑mini/GPT‑5.
- **Cambios previstos:**
    - Implementación de **máscara causal** en entrenamiento autoregresivo.
    - **Positional embeddings aprendibles** en lugar de sinusoidales.
    - **Pre‑LayerNorm + residual connections** en cada bloque.
    - **Feed‑forward con GELU**.
    - Optimización con **AdamW + weight decay** y scheduler de learning rate (warmup + cosine decay).
    - Evaluación con benchmarks más exigentes (razonamiento lógico, generación larga).
- **Impacto esperado:** arquitectura GPT‑like completa, lista para escalar en profundidad y tamaño.

---

## 🔖 Roadmap futuro

- **v2.0:** integración multimodal (texto + voz).
- **v2.1:** soporte para datasets especializados (psicología, historia, literatura).
- **v2.2:** mejoras en generación controlada (estilos, emociones, sarcasmo).