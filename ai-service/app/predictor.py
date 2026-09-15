import json
from pathlib import Path
from typing import List, Optional

import joblib
import pandas as pd

from app.schemas import (
    PredictionResult,
    RiskLevel,
    TelemetryInput,
)

BASE_DIR = Path(__file__).resolve().parent.parent
MODEL_PATH = BASE_DIR / "models" / "predictive_maintenance_model.joblib"
METADATA_PATH = BASE_DIR / "models" / "model_metadata.json"


class ModelPredictor:
    """Manages model loading and inference for predictive maintenance."""

    def __init__(self, model_path: Path = MODEL_PATH, metadata_path: Path = METADATA_PATH):
        self.model_path = model_path
        self.metadata_path = metadata_path
        self.model = None
        self.metadata = None
        self.load()

    def load(self):
        if self.model_path.exists():
            self.model = joblib.load(self.model_path)
        else:
            self.model = None

        if self.metadata_path.exists():
            with open(self.metadata_path, "r", encoding="utf-8") as f:
                self.metadata = json.load(f)
        else:
            self.metadata = None

    @property
    def is_loaded(self) -> bool:
        return self.model is not None

    def determine_risk(self, probability: float) -> tuple[RiskLevel, str]:
        """Calculates risk level and actionable recommendation based on failure probability."""
        if probability < 0.30:
            return (
                RiskLevel.LOW,
                "Normal operasyon devam edebilir. Rutin periyodik bakım takip edilmelidir.",
            )
        elif probability < 0.60:
            return (
                RiskLevel.MEDIUM,
                "Telemetri değerlerinde hafif anormallikler gözlendi. Bir sonraki vardiyada fiziksel kontrol önerilir.",
            )
        elif probability < 0.80:
            return (
                RiskLevel.HIGH,
                "24 saat içinde arıza riski yüksek! Bakım ekibi bilgilendirilmeli ve önleyici bakım başlatılmalıdır.",
            )
        else:
            return (
                RiskLevel.CRITICAL,
                "Acil kritik arıza riski! Ekipman durdurulmalı ve parça/yağlama kontrolleri derhal yapılmalıdır.",
            )

    def predict(self, telemetry: TelemetryInput) -> PredictionResult:
        if not self.is_loaded:
            raise RuntimeError("Makine öğrenmesi modeli henüz yüklenmedi veya eğitilmedi.")

        df = pd.DataFrame([
            {
                "MachineCategory": telemetry.machine_category,
                "AirTemperature_C": telemetry.air_temperature_c,
                "ProcessTemperature_C": telemetry.process_temperature_c,
                "RotationalSpeed_RPM": telemetry.rotational_speed_rpm,
                "Torque_Nm": telemetry.torque_nm,
                "ToolWear_Min": telemetry.tool_wear_min,
            }
        ])

        proba = float(self.model.predict_proba(df)[0, 1])
        prediction = bool(self.model.predict(df)[0] == 1)

        risk_level, recommendation = self.determine_risk(proba)

        return PredictionResult(
            machine_id=telemetry.machine_id,
            failure_probability=round(proba, 4),
            failure_predicted=prediction,
            risk_level=risk_level,
            recommendation=recommendation,
        )

    def predict_batch(self, items: List[TelemetryInput]) -> List[PredictionResult]:
        if not self.is_loaded:
            raise RuntimeError("Makine öğrenmesi modeli henüz yüklenmedi veya eğitilmedi.")

        return [self.predict(item) for item in items]
