from contextlib import asynccontextmanager
from typing import Any, Dict

from fastapi import FastAPI, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware

from app.predictor import ModelPredictor
from app.schemas import (
    BatchPredictionResult,
    BatchTelemetryInput,
    HealthResponse,
    PredictionResult,
    RiskLevel,
    TelemetryInput,
)

predictor: ModelPredictor = ModelPredictor()


@asynccontextmanager
async def lifespan(app: FastAPI):
    global predictor
    if predictor is None or not predictor.is_loaded:
        predictor = ModelPredictor()
    yield


app = FastAPI(
    title="SmartFactoryDSS AI Servisi",
    description="Endüstriyel telemetri verilerine dayalı 24 saatlik makine arıza riski tahmin ve karar destek mikroservisi.",
    version="1.0.0",
    lifespan=lifespan,
)

# Enable CORS for frontend / web integrations
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health", response_model=HealthResponse, tags=["Sistem"])
def health_check():
    """AI servisinin ve eğitilmiş modelin durumunu kontrol eder."""
    model_type = None
    trained_at = None
    if predictor and predictor.metadata:
        model_type = predictor.metadata.get("model_type")
        trained_at = predictor.metadata.get("trained_at")

    return HealthResponse(
        status="healthy" if (predictor and predictor.is_loaded) else "degraded",
        model_loaded=bool(predictor and predictor.is_loaded),
        model_type=model_type,
        trained_at=trained_at,
    )


@app.post("/predict", response_model=PredictionResult, tags=["Tahmin"])
def predict_failure_risk(telemetry: TelemetryInput):
    """Tek bir makinenin anlık telemetri verilerini alarak arıza olasılığını ve risk seviyesini hesaplar."""
    if not predictor or not predictor.is_loaded:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Model henüz yüklenmedi veya hazır değil.",
        )

    try:
        return predictor.predict(telemetry)
    except Exception as ex:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Tahmin hesaplanırken hata oluştu: {str(ex)}",
        )


@app.post("/predict/batch", response_model=BatchPredictionResult, tags=["Tahmin"])
def predict_batch_failure_risk(batch_input: BatchTelemetryInput):
    """Birden fazla makine telemetri verisi için toplu risk tahmini gerçekleştirir."""
    if not predictor or not predictor.is_loaded:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Model henüz yüklenmedi veya hazır değil.",
        )

    try:
        predictions = predictor.predict_batch(batch_input.items)
        high_risk_count = sum(
            1 for p in predictions if p.risk_level in (RiskLevel.HIGH, RiskLevel.CRITICAL)
        )

        return BatchPredictionResult(
            total_processed=len(predictions),
            high_risk_count=high_risk_count,
            predictions=predictions,
        )
    except Exception as ex:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Toplu tahmin hesaplanırken hata oluştu: {str(ex)}",
        )


@app.get("/model/metadata", tags=["Model"])
def get_model_metadata() -> Dict[str, Any]:
    """Modelin eğitim metrikleri, öznitelikleri ve eşik değerlerini döndürür."""
    if not predictor or not predictor.metadata:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Model metaverisi bulunamadı.",
        )
    return predictor.metadata


if __name__ == "__main__":
    import uvicorn
    uvicorn.run("app.main:app", host="0.0.0.0", port=8000, reload=True)
