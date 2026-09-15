from datetime import datetime, timezone
from enum import Enum
from typing import List, Optional
from pydantic import BaseModel, Field


class RiskLevel(str, Enum):
    LOW = "Düşük"
    MEDIUM = "Orta"
    HIGH = "Yüksek"
    CRITICAL = "Kritik"


class TelemetryInput(BaseModel):
    machine_id: str = Field(..., description="Makine kodu veya kimliği", json_schema_extra={"example": "CNC-01"})
    machine_category: str = Field(..., description="Makine kategorisi (LightDuty, MediumDuty, HeavyDuty)", json_schema_extra={"example": "MediumDuty"})
    air_temperature_c: float = Field(..., description="Ortam hava sıcaklığı (°C)", ge=-50, le=100, json_schema_extra={"example": 22.5})
    process_temperature_c: float = Field(..., description="Proses çalışma sıcaklığı (°C)", ge=-50, le=200, json_schema_extra={"example": 38.2})
    rotational_speed_rpm: float = Field(..., description="Dönme hızı (RPM)", ge=0, le=15000, json_schema_extra={"example": 1500.0})
    torque_nm: float = Field(..., description="Tork (Nm)", ge=0, le=2000, json_schema_extra={"example": 42.0})
    tool_wear_min: float = Field(..., description="Takım aşınma süresi (dakika)", ge=0, le=10000, json_schema_extra={"example": 45.0})


class PredictionResult(BaseModel):
    machine_id: str
    failure_probability: float = Field(..., description="24 saat içinde arıza olasılığı (0.0 - 1.0)")
    failure_predicted: bool = Field(..., description="Modelin arıza tahmini (True/False)")
    risk_level: RiskLevel = Field(..., description="Risk seviyesi derecelendirmesi")
    recommendation: str = Field(..., description="Bakım personeli için karar destek önerisi")
    evaluated_at: str = Field(default_factory=lambda: datetime.now(timezone.utc).isoformat())


class BatchTelemetryInput(BaseModel):
    items: List[TelemetryInput] = Field(..., description="Toplu telemetri verileri listesi")


class BatchPredictionResult(BaseModel):
    total_processed: int
    high_risk_count: int
    predictions: List[PredictionResult]


class HealthResponse(BaseModel):
    status: str
    model_loaded: bool
    model_type: Optional[str] = None
    trained_at: Optional[str] = None
    service_timestamp: str = Field(default_factory=lambda: datetime.now(timezone.utc).isoformat())
